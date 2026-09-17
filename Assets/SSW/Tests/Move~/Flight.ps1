param([string]$Run = 'Flight01', [string]$Project = (Get-Location).Path, [string]$Build = 'Builds/Local/Game.exe', [int]$Port = 7796, [string]$HostJob = 'Witch', [string]$ClientJob = 'Magician', [int]$HostFps = 60, [int]$ClientFps = 60, [int]$Delay = 120, [int]$Jitter = 35, [int]$Loss = 3)
$ErrorActionPreference = 'Stop'
$root = "$Project/Logs/Move/$Run"
if (Test-Path "$root/host.json") { throw 'Use a fresh Run name' }
New-Item -ItemType Directory -Path $root -Force | Out-Null
$seq = @{host=0;client=0}
$checks = [Collections.Generic.List[string]]::new()
function Read($peer) {
    for ($attempt=0; $attempt -lt 25; $attempt++) {
        try {
            $state=Get-Content "$root/$peer.json" -Raw | ConvertFrom-Json
            if ($state) { return $state }
        } catch { }
        Start-Sleep -Milliseconds 10
    }
    return $null
}
function Own($state) { @($state.players | Where-Object owner)[0] }
function Other($state) { @($state.players | Where-Object { -not $_.owner })[0] }
function Send($peer,$op,$value=0,$x=0,$y=0) {
    $seq[$peer]++
    $json=@{seq=$seq[$peer];op=$op;value=$value;x=$x;y=$y}|ConvertTo-Json -Compress
    $writeUntil=[DateTime]::UtcNow.AddSeconds(2)
    while ($true) {
        try { [IO.File]::WriteAllText("$root/$peer.cmd.json",$json); break }
        catch [IO.IOException] { if ([DateTime]::UtcNow -gt $writeUntil) { throw }; Start-Sleep -Milliseconds 15 }
    }
    if ($op -eq 'quit') { return }
    $until=[DateTime]::UtcNow.AddSeconds(8)
    while ((Read $peer).seq -lt $seq[$peer]) {
        if ([DateTime]::UtcNow -gt $until) { throw "Unprocessed command $peer $op" }
        Start-Sleep -Milliseconds 20
    }
}
function Await($label,[scriptblock]$condition,$seconds=20) {
    $until=[DateTime]::UtcNow.AddSeconds($seconds)
    do {
        $script:h=Read host; $script:c=Read client
        if ($h.error -or $c.error) { throw "Runtime: $($h.error) $($c.error)" }
        if ($h -and $c -and (& $condition)) { return }
        Start-Sleep -Milliseconds 30
    } while ([DateTime]::UtcNow -lt $until)
    throw "Timeout: $label"
}
function Check($condition,$label) {
    if (-not $condition) { throw $label }
    $checks.Add("PASS $label")
    [IO.File]::WriteAllLines("$root/checks.txt",$checks)
}
function ErrorAtRest($state) {
    $p=Own $state
    [Math]::Sqrt([Math]::Pow($p.position.x-$p.viewPosition.x,2)+[Math]::Pow($p.position.y-$p.viewPosition.y,2))
}
function ServerError {
    $predicted = Own $script:c
    $authority = @($script:h.players | Where-Object { $_.id -eq $predicted.id })[0]
    if (-not $authority -or -not $predicted) { return [double]::PositiveInfinity }
    [Math]::Sqrt([Math]::Pow($predicted.position.x-$authority.position.x,2)+[Math]::Pow($predicted.position.y-$authority.position.y,2))
}
foreach($peer in @('host','client')) {
    $job=if($peer -eq 'host'){$HostJob}else{$ClientJob}
    Start-Process -FilePath (Join-Path $Project $Build) -WorkingDirectory $Project -ArgumentList @('--net-mode',$peer,'--net-job',$job,'--net-port',$Port,'--net-name',$peer,'--net-probe',"$root/$peer",'-logFile',"$root/$peer.log",'-screen-fullscreen','0') -WindowStyle Hidden | Out-Null
}
try {
    Await 'draft' { $h.phase -eq 'Draft' -and $c.phase -eq 'Draft' } 60
    Check ($h.build -eq $c.build) 'Peers use the same build'
    Send host fps $HostFps; Send client fps $ClientFps
    Send host choose 0; Send client choose 0
    Await 'combat' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' }
    Send host lag $Delay $Jitter $Loss; Send client lag $Delay $Jitter $Loss
    Start-Sleep -Milliseconds 1800
    Send host warp 0 -13 0; Send host warp 1 -8 0
    Await 'settle' { (Own $c).grounded -and (Other $h).grounded -and (ServerError) -lt 0.15 }
    Send client metrics
    Send host trace 1 4; Send client trace 1 4
    Send client move 0 1 0
    Start-Sleep -Milliseconds 950
    Send client move 0 0 0
    Send host move 0 1 0
    Start-Sleep -Milliseconds 650
    Send host move 0 0 0
    Await 'motion traces' { (Test-Path "$root/client.trace.1.json") -and (Test-Path "$root/host.trace.1.json") } 10
    Await 'movement convergence' { (ServerError) -lt 0.15 }
    Check ($true) "Continuous movement converges under $Delay ms delay, $Jitter ms jitter and $Loss percent loss per peer"
    $shotX=if($ClientJob -eq 'Witch'){-18}else{-10}
    Send host warp 0 13 0; Send host warp 1 $shotX 0
    Await 'settle for shots' { (Own $c).grounded -and (Other $h).grounded -and (ServerError) -lt 0.15 }
    $matches=(Own $c).matches
    Send host trace 2 12; Send client trace 2 12
    for ($round=0;$round -lt 3;$round++) {
        foreach ($peer in @('host','client')) {
            $job=if($peer -eq 'host'){$HostJob}else{$ClientJob}
            if($job -eq 'Magician') {
                Await 'card ready' { (Own (Read $peer)).readyIn -le 0 }
                Send $peer press 0 1 0
                Start-Sleep -Milliseconds 180
                Send $peer cast 0 1 0
            } else {
                Await 'potion ready' { (Own (Read $peer)).potion -ge 0 }
                Send $peer cast 1 1 0
            }
        }
        Start-Sleep -Milliseconds 2500
    }
    Await 'flight traces' { (Test-Path "$root/client.trace.2.json") -and (Test-Path "$root/host.trace.2.json") } 15
    Check ((Own $c).matches -ge $matches+3) 'All three guest projectiles hand over to authoritative objects'
    Await 'no lingering previews' { (Own $c).previews -eq 0 }
    $h|ConvertTo-Json -Depth 12|Set-Content "$root/final-host.json"
    $c|ConvertTo-Json -Depth 12|Set-Content "$root/final-client.json"
    Write-Output "PASS trajectory capture: $root"
}
catch {
    $_|Out-String|Set-Content "$root/failure.txt"
    $h|ConvertTo-Json -Depth 12|Set-Content "$root/failure-host.json"
    $c|ConvertTo-Json -Depth 12|Set-Content "$root/failure-client.json"
    throw
}
finally { Send host quit; Send client quit }

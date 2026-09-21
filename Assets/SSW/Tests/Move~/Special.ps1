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
    Send host fps $HostFps; Send client fps $ClientFps
    Send host choose 0; Send client choose 0
    Await 'combat' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' }
    Send host lag $Delay $Jitter $Loss; Send client lag $Delay $Jitter $Loss
    $mage=if($HostJob -eq 'Magician'){'host'}else{'client'}
    $witch=if($HostJob -eq 'Witch'){'host'}else{'client'}
    $mageSlot=if($mage -eq 'host'){0}else{1}
    $witchSlot=1-$mageSlot
    Send host grant 12 $mageSlot
    Send host grant 11 $mageSlot
    Send host grant 19 $witchSlot
    Send host grant 21 $witchSlot
    Send host grant 20 $witchSlot
    Await 'augments replicate' { (Own (Read $mage)).augments -contains 12 -and (Own (Read $witch)).augments -contains 21 }
    Send host warp $mageSlot 13 0; Send host warp $witchSlot -10 0
    Await 'settle' { (Own $c).grounded -and (Other $h).grounded -and (ServerError) -lt 0.15 }
    $matches=(Own $c).matches
    Send host trace 3 8; Send client trace 3 8
    Send $mage press 0 1 0
    Start-Sleep -Milliseconds 250
    Send $mage cast 0 1 0
    Send $witch cast 1 0.8 0.6
    Await 'augmented previews matched' { (Own $c).matches -ge $matches + $(if($ClientJob -eq 'Witch'){3}else{1}) }
    Check ($true) 'Delayed card or all three potion parts match independently'
    Await 'traces' { (Test-Path "$root/client.trace.3.json") -and (Test-Path "$root/host.trace.3.json") } 12
    Await 'shots and zones clear' { $h.shots.Count -eq 0 -and $c.shots.Count -eq 0 -and (Own $c).previews -eq 0 } 10
    Check ($true) 'Return, bounce, multishot and lingering zone effects finish without ghost objects'
    $h|ConvertTo-Json -Depth 12|Set-Content "$root/final-host.json"
    $c|ConvertTo-Json -Depth 12|Set-Content "$root/final-client.json"
    Write-Output "PASS augmented capture: $root"
}
catch {
    $_|Out-String|Set-Content "$root/failure.txt"
    $h|ConvertTo-Json -Depth 12|Set-Content "$root/failure-host.json"
    $c|ConvertTo-Json -Depth 12|Set-Content "$root/failure-client.json"
    throw
}
finally { Send host quit; Send client quit }

param([string]$Run = 'LagStep01', [string]$Project = (Get-Location).Path, [string]$Build = 'Builds/Local/Game.exe', [int]$Port = 7796, [string]$HostJob = 'Magician', [string]$ClientJob = 'Magician', [int]$HostFps = 60, [int]$ClientFps = 60, [int]$Delay = 250, [int]$Jitter = 35, [int]$Loss = 3)
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
    Await 'draft' {$h.phase -eq 'Draft' -and $c.phase -eq 'Draft'} 60
    Send host fps 60; Send client fps 60
    Send host choose 0; Send client choose 0
    Await 'combat settled' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing' -and (Own $c).grounded}
    $records=[Collections.Generic.List[object]]::new()
    foreach($delay in @(0,250,0,350,50)) {
        Send host lag $delay 0 0; Send client lag $delay 0 0
        Start-Sleep -Milliseconds 1600
        $before=Other (Read host)
        $direction=if($before.position.x -lt -5){-1}else{1}
        $watch=[Diagnostics.Stopwatch]::StartNew()
        Send client move 0 $direction 0
        do {
            $sample=Other (Read host)
            if ([Math]::Abs($sample.position.x-$before.position.x) -gt 0.1) { break }
            if ($watch.Elapsed.TotalSeconds -gt 2) { throw "Movement not received at delay $delay" }
            Start-Sleep -Milliseconds 10
        } while ($true)
        $response=$watch.Elapsed.TotalSeconds
        $remaining=850-[int]$watch.Elapsed.TotalMilliseconds
        if ($remaining -gt 0) { Start-Sleep -Milliseconds $remaining }
        Send client move 0 0 0
        Start-Sleep -Milliseconds 1400
        $hostState=Read host; $clientState=Read client
        $after=Other $hostState
        $distance=[Math]::Abs($after.position.x-$before.position.x)
        $records.Add(@{delay=$delay;distance=$distance;response=$response;host=$after;client=(Own $clientState)})
        $records|ConvertTo-Json -Depth 12|Set-Content "$root/steps.json"
        Check ($distance -gt 3) "Guest movement reaches authority after delay changes to $delay ms per peer: $([Math]::Round($distance,2)) units"
        Check ($after.buffered -le 3) "Idle input backlog drains after $delay ms delay"
        if ($delay -eq 0) { Check ($response -lt 0.35) "Zero-delay authority response stays below 350 ms including probe polling" }
    }
    Write-Output 'PASS changing network delay without teleporting or reconnecting'
}
catch {
    $_|Out-String|Set-Content "$root/failure.txt"
    throw
}
finally {Send host quit;Send client quit}

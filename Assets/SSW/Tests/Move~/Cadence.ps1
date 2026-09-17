param([string]$Run = 'Cadence01', [string]$Project = (Get-Location).Path, [string]$Build = 'Builds/Local/Game.exe', [int]$Port = 7796, [string]$HostJob = 'Magician', [string]$ClientJob = 'Magician', [int]$HostFps = 60, [int]$ClientFps = 60, [int]$Delay = 250, [int]$Jitter = 35, [int]$Loss = 3)
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
    Send host fps $HostFps; Send client fps $ClientFps
    foreach ($peer in @('host','client')) {
        $offer=(Own (Read $peer)).offer
        $cards=@($offer.x,$offer.y,$offer.z)
        $slot=@(0..2 | Where-Object { $cards[$_] -ne 16 })[0]
        Send $peer choose $slot
    }
    Await 'combat' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing'}
    Send host lag $Delay $Jitter $Loss; Send client lag $Delay $Jitter $Loss
    Start-Sleep -Milliseconds 2400
    $records=[Collections.Generic.List[object]]::new()
    foreach($peer in @('host','client')) {
        Await 'ready' {(Own (Read $peer)).readyIn -le 0}
        Send $peer press 0 0 1
        Start-Sleep -Milliseconds 180
        Send $peer cast 0 0 1
        $sent=Read $peer
        $action=(Own $sent).action
        Await 'accepted' {(Own (Read $peer)).confirmed -eq $action}
        Await 'cooldown complete' {(Own (Read $peer)).readyIn -le 0}
        $done=Read $peer
        $records.Add(@{peer=$peer;elapsed=$done.time-$sent.time;rtt=$done.rtt;rejected=(Own $done).rejected})
        Check ((Own $done).rejected -eq 0) "$peer accepted the shot"
        Send $peer press 0 0 1
        Start-Sleep -Milliseconds 180
        Send $peer cast 0 0 1
        $action=(Own (Read $peer)).action
        Await 'next shot approved' {(Own (Read $peer)).confirmed -eq $action}
        Check ((Own (Read $peer)).rejected -eq 0) "$peer can fire again when the local cooldown ends"
    }
    $records|ConvertTo-Json|Set-Content "$root/cadence.json"
    $gap=[double]$records[1].elapsed-[double]$records[0].elapsed
    Check ($gap -lt 0.35) "Guest cooldown does not add a network round trip: gap $([Math]::Round($gap,3)) seconds"
    Await 'ready for long charge' {(Own (Read client)).readyIn -le 0}
    Send host lag 450 40 2; Send client lag 450 40 2
    Start-Sleep -Milliseconds 1800
    Send client press 0 0 1
    Start-Sleep -Milliseconds 1100
    Check ((Own (Read client)).charging) 'Charge survives delayed acknowledgement beyond 800 ms'
    Send client cancel
    Await 'delayed charge canceled' {$p=Own $c; -not $p.charging -and $p.confirmed -eq $p.action}
    Send host lag 100 0 100; Send client lag 100 0 100
    Send client press 0 1 0
    $pressed=Read client
    $pressed|ConvertTo-Json -Depth 12|Set-Content "$root/unacked-press.json"
    Check ((Own $pressed).charging) 'Unacknowledged charge starts immediately'
    Start-Sleep -Milliseconds 180
    Send client cast 0 1 0
    Check ((Own (Read client)).previews -gt 0) 'Unacknowledged attack starts local feedback'
    Start-Sleep -Milliseconds 2900
    $expired=Read client
    Check ((Own $expired).previews -eq 0 -and -not (Own $expired).charging) 'Lost acknowledgement cannot leave an endless preview or charge'
    $expired|ConvertTo-Json -Depth 12|Set-Content "$root/expired-client.json"
    Write-Output 'PASS repeated attack, delayed charge and missing acknowledgement'

}
catch {
    $_|Out-String|Set-Content "$root/failure.txt"
    (Read host)|ConvertTo-Json -Depth 12|Set-Content "$root/failure-host.json"
    (Read client)|ConvertTo-Json -Depth 12|Set-Content "$root/failure-client.json"
    throw
}
finally {Send host quit;Send client quit}

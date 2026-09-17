param([string]$Run = 'Run1', [string]$Project = (Get-Location).Path, [string]$Build = 'Builds/Local/Game.exe', [int]$Port = 7796, [string]$HostJob = 'Witch', [string]$ClientJob = 'Magician', [int]$HostFps = 60, [int]$ClientFps = 60)
$ErrorActionPreference = 'Stop'
$root = "$Project/Logs/Move/$Run"
if (Test-Path "$root/host.json") { throw 'Use a fresh Run name' }
New-Item -ItemType Directory -Path $root -Force | Out-Null
$seq = @{host=0;client=0}
$checks = [Collections.Generic.List[string]]::new()
function Read($peer) { try { Get-Content "$root/$peer.json" -Raw | ConvertFrom-Json } catch { return $null } }
function Own($state) { @($state.players | Where-Object owner)[0] }
function Other($state) { @($state.players | Where-Object { -not $_.owner })[0] }
function Send($peer,$op,$value=0,$x=0,$y=0) {
    $seq[$peer]++
    [IO.File]::WriteAllText("$root/$peer.cmd.json",(@{seq=$seq[$peer];op=$op;value=$value;x=$x;y=$y}|ConvertTo-Json -Compress))
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
    Check ($h.probeVersion -eq 2 -and $c.probeVersion -eq 2 -and $h.build -eq $c.build) 'Both peers use the fresh instrumented build'
    Send host fps $HostFps; Send client fps $ClientFps
    Send host choose 0; Send client choose 0
    Await 'combat grounded' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' -and [Math]::Abs((Other $h).velocity.y) -lt 0.1 }
    Check ((Own $c).viewLinked -and (Own $h).viewLinked) 'Actual dinosaur renderers are attached to presentation transforms'
    Send host lag 100 10 1; Send client lag 100 10 1
    Start-Sleep -Milliseconds 650
    Send client measure 0 1 0
    Await 'client movement measurement' { $c.viewDelay -ge 0 -and $c.bodyDelay -ge 0 }
    Check ($c.viewDelay -lt 0.1) "Client visible movement starts in $([Math]::Round($c.viewDelay*1000)) ms"
    Check ($c.bodyDelay -lt 0.1) "Client collision body responds in $([Math]::Round($c.bodyDelay*1000)) ms despite simulated latency"
    Start-Sleep -Milliseconds 450
    Send client move 0 0 0
    Await 'stopped convergence' { (ErrorAtRest $c) -lt 0.15 -and [Math]::Abs((Other $h).velocity.x) -lt 0.1 -and (ServerError) -lt 0.15 }
    Check ((ServerError) -lt 0.15) 'Client collision body converges to the actual host authority after stopping'
    Send host measure 0 1 0
    Await 'host immediate response' { $h.bodyDelay -ge 0 }
    Check ($h.bodyDelay -lt 0.1) "Host movement starts in $([Math]::Round($h.bodyDelay*1000)) ms"
    Send host move 0 0 0
    Await 'client grounded' { [Math]::Abs((Other $h).velocity.y) -lt 0.1 }
    $jumpStartY = (Own $c).position.y
    Send client measure 1
    Await 'jump response' { $c.viewDelay -ge 0 -and $c.bodyDelay -ge 0 }
    Check ($c.viewDelay -lt 0.1) "Client jump presentation starts in $([Math]::Round($c.viewDelay*1000)) ms"
    Check ($c.bodyDelay -lt 0.1) 'Client jump collision body responds without a server round trip'
    Await 'full jump trajectory' { (Own $c).position.y -gt $jumpStartY + 1 }
    Check ($true) 'Jump keeps its upward velocity through consecutive physics steps'
    Await 'jump lands' { [Math]::Abs((Other $h).velocity.y) -lt 0.1 -and (ErrorAtRest $c) -lt 0.2 }
    Check ((ErrorAtRest $c) -lt 0.2) 'Jump lands and reconciles without persistent offset'
    Send host push 0 10 6
    Await 'server knockback' { (Other $h).velocity.y -gt 0.5 -or [Math]::Abs((Other $h).velocity.x) -gt 0.5 }
    Await 'client receives knockback' { [Math]::Abs((Own $c).velocity.x) -gt 0.5 -or (Own $c).velocity.y -gt 0.5 }
    Check ($true) 'Authoritative knockback reaches the client motor'
    Start-Sleep -Milliseconds 1300
    Await 'knockback settles' { [Math]::Abs((Other $h).velocity.y) -lt 0.1 -and (ErrorAtRest $c) -lt 0.25 }
    Check ((ErrorAtRest $c) -lt 0.25) 'Prediction reconciles after server knockback'
    $baseSpeed = (Own $c).speed
    Send host slow 0 0.5 1.2
    Await 'replicated slow' { (Own $c).speed -lt $baseSpeed * 0.8 }
    Check ($true) 'Server slow reaches the predicted movement speed'
    Await 'slow expires' { [Math]::Abs((Own $c).speed - $baseSpeed) -lt 0.01 }
    Send host warp 1 5 0
    Await 'teleport epoch' { (Own $c).epoch -eq (Other $h).epoch -and (Own $c).epoch -gt 0 }
    Await 'teleport grounded and converged' { (Other $h).grounded -and (Own $c).grounded -and (ServerError) -lt 0.15 }
    Check ((ServerError) -lt 0.15) 'Teleport resets owner prediction to the authoritative position'
    $matches = (Own $c).matches
    if ($ClientJob -eq 'Magician') {
        Await 'cast cooldown' { (Own $c).readyIn -le 0 }
        Send client cast 1 0 1
        Await 'immediate charge' { (Own $c).charging -and $c.castDelay -ge 0 }
        Check ($c.castDelay -lt 0.1) "Client charge feedback starts in $([Math]::Round($c.castDelay*1000)) ms"
        Send client cast 0 0 1
    } else {
        Await 'potion ready' { (Own $c).potion -ge 0 }
        Send client cast 1 0 1
    }
    Await 'visible local shot' { (Own $c).visiblePreviews -gt 0 -and $c.castDelay -ge 0 }
    Check ($c.castDelay -lt 0.1) "Client shot feedback starts in $([Math]::Round($c.castDelay*1000)) ms"
    Await 'authoritative shot matched' { (Own $c).matches -gt $matches -and (Own $c).confirmed -eq (Own $c).action }
    Check ($true) 'Anticipated shot matches the authoritative projectile'
    Await 'shot handoff complete' { (Own $c).previews -eq 0 }
    if ($ClientJob -eq 'Magician') {
        $rejected = (Own $c).rejected
        Send client press 0 0 1
        Send client release 0 0 1
        Await 'cooldown rejection' { (Own $c).rejected -gt $rejected -and (Own $c).confirmed -eq (Own $c).action }
        Check ((Own $c).previews -eq 0 -and -not (Own $c).charging) 'Rejected cooldown attack leaves no ghost shot or charge'
        Await 'cancel cooldown' { (Own $c).readyIn -le 0 }
        Send client press 0 0 1
        Send client cancel
        Await 'charge canceled' { -not (Own $c).charging -and (Own $c).confirmed -eq (Own $c).action }
        Check ((Own $c).previews -eq 0) 'Cancel clears local and server charge'
    }
    Send host warp 0 -8 0
    Send host warp 1 -6 0
    Await 'fighters settle for real hit' { (Own $h).grounded -and (Other $h).grounded -and (Own $c).grounded -and (ServerError) -lt 0.15 }
    $caster = if ($HostJob -eq 'Magician') { 'host' } else { 'client' }
    $targetId = if ($caster -eq 'host') { (Other $h).id } else { (Own $h).id }
    $victim = @($h.players | Where-Object { $_.id -eq $targetId })[0]
    $shooter = @($h.players | Where-Object { $_.id -ne $targetId })[0]
    $beforeHp = $victim.hp
    $dx = $victim.position.x - $shooter.position.x
    $dy = $victim.position.y - $shooter.position.y
    Await 'real hit cooldown' { (Own (Read $caster)).readyIn -le 0 }
    Send $caster press 0 $dx $dy
    Start-Sleep -Milliseconds 180
    Send $caster release 0 $dx $dy
    Await 'projectile actually damages target' { @($h.players | Where-Object { $_.id -eq $targetId })[0].hp -lt $beforeHp }
    Await 'hit health agrees on both peers' { [Math]::Abs(@($h.players | Where-Object { $_.id -eq $targetId })[0].hp - @($c.players | Where-Object { $_.id -eq $targetId })[0].hp) -lt 0.01 }
    Check ($true) 'Real card collision applies the same health change on host and client'
    Send client capture
    $h | ConvertTo-Json -Depth 12 | Set-Content "$root/combat-host.json"
    $c | ConvertTo-Json -Depth 12 | Set-Content "$root/combat-client.json"
    Start-Sleep -Milliseconds 1000
    Send host lag 100 0 100; Send client lag 100 0 100
    Send client move 0 1 0
    Start-Sleep -Milliseconds 3400
    $frozen = (Own (Read client)).position
    Start-Sleep -Milliseconds 250
    $afterFreeze = (Own (Read client)).position
    Check ([Math]::Abs($afterFreeze.x-$frozen.x) -lt 0.02 -and [Math]::Abs($afterFreeze.y-$frozen.y) -lt 0.02) 'Collision body stops advancing after snapshot timeout'
    Send client move 0 0 0
    Send host lag 100 10 1; Send client lag 100 10 1
    Await 'network recovery' { (ErrorAtRest $c) -lt 0.2 -and [Math]::Abs((Other $h).velocity.x) -lt 0.1 -and (ServerError) -lt 0.15 }
    Start-Sleep -Milliseconds 600
    Check ((ErrorAtRest (Read client)) -lt 0.2) 'Presentation recovers after packet loss ends'
    Check ((Other $h).buffered -le 8) 'Server input backlog recovers instead of retaining stale movement'
    Send client move 0 -1 0
    Start-Sleep -Milliseconds 2800
    Send client move 0 0 0
    Await 'wall convergence' { (ErrorAtRest $c) -lt 0.2 -and (ServerError) -lt 0.15 }
    Check ([Math]::Abs((Own $c).viewPosition.x) -lt 22) 'Predicted character stays within arena walls'
    Send host remote 100000
    Await 'respawn' { $h.round -eq 2 -and $h.phase -eq 'Countdown' -and $c.phase -eq 'Countdown' }
    Await 'respawn presentation' { $c.phase -eq 'Countdown' -and (ErrorAtRest $c) -lt 0.05 -and (ServerError) -lt 0.05 }
    Check ((ErrorAtRest $c) -lt 0.05) 'Prediction clears during respawn countdown'
    Await 'round two' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' }
    Send host remote 100000
    Await 'set draft' { $h.set -eq 2 -and $c.phase -eq 'Draft' }
    Await 'draft respawn synchronized' { $c.set -eq 2 -and (Own $c).objectId -eq (Other $h).objectId -and (ErrorAtRest $c) -lt 0.01 -and (ServerError) -lt 0.01 }
    $before=(Own $c).viewPosition.x
    Send client move 0 1 0; Send client jump
    Start-Sleep -Milliseconds 400
    Check ([Math]::Abs((Own (Read client)).viewPosition.x-$before) -lt 0.02) 'Movement prediction is disabled during card selection'
    Check ((Own $c).augments.Count -eq 1) 'Respawn preserves the selected augment'
    $h|ConvertTo-Json -Depth 12|Set-Content "$root/final-host.json"
    $c|ConvertTo-Json -Depth 12|Set-Content "$root/final-client.json"
    Write-Output "PASS $($checks.Count) movement checks"
}
catch {
    $_|Out-String|Set-Content "$root/failure.txt"
    $h | ConvertTo-Json -Depth 12 | Set-Content "$root/failure-host.json"
    $c | ConvertTo-Json -Depth 12 | Set-Content "$root/failure-client.json"
    throw
}
finally { Send host quit; Send client quit }

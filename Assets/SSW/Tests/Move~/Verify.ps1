param([string]$Run = 'Run1', [string]$Project = (Get-Location).Path, [string]$Build = 'Builds/Move/Game.exe')
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
foreach($peer in @('host','client')) {
    $job=if($peer -eq 'host'){'Witch'}else{'Magician'}
    Start-Process -FilePath (Join-Path $Project $Build) -WorkingDirectory $Project -ArgumentList @('--net-mode',$peer,'--net-job',$job,'--net-port','7796','--net-name',$peer,'--net-probe',"$root/$peer",'-logFile',"$root/$peer.log",'-screen-fullscreen','0') -WindowStyle Hidden | Out-Null
}
try {
    Await 'draft' { $h.phase -eq 'Draft' -and $c.phase -eq 'Draft' } 60
    Send host choose 0; Send client choose 0
    Await 'combat grounded' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' -and [Math]::Abs((Other $h).velocity.y) -lt 0.1 }
    Check ((Own $c).viewLinked -and (Own $h).viewLinked) 'Actual dinosaur renderers are attached to presentation transforms'
    Send host lag 100 10 1; Send client lag 100 10 1
    Start-Sleep -Milliseconds 650
    Send client measure 0 1 0
    Await 'client movement measurement' { $c.viewDelay -ge 0 -and $c.bodyDelay -ge 0 }
    Check ($c.viewDelay -lt 0.1) "Client visible movement starts in $([Math]::Round($c.viewDelay*1000)) ms"
    Check ($c.bodyDelay -gt 0.14 -and $c.viewDelay -lt $c.bodyDelay) "Server position still arrives after $([Math]::Round($c.bodyDelay*1000)) ms at simulated latency"
    Start-Sleep -Milliseconds 450
    Send client move 0 0 0
    Await 'stopped convergence' { (ErrorAtRest $c) -lt 0.15 -and [Math]::Abs((Other $h).velocity.x) -lt 0.1 }
    Check ((ErrorAtRest $c) -lt 0.15) 'Client presentation converges to server position after stopping'
    Send host measure 0 1 0
    Await 'host immediate response' { $h.bodyDelay -ge 0 }
    Check ($h.bodyDelay -lt 0.1) "Host movement starts in $([Math]::Round($h.bodyDelay*1000)) ms"
    Send host move 0 0 0
    Await 'client grounded' { [Math]::Abs((Other $h).velocity.y) -lt 0.1 }
    Send client measure 1
    Await 'jump response' { $c.viewDelay -ge 0 -and $c.bodyDelay -ge 0 }
    Check ($c.viewDelay -lt 0.1) "Client jump presentation starts in $([Math]::Round($c.viewDelay*1000)) ms"
    Check ($c.bodyDelay -gt 0.14) 'Jump remains server authoritative under latency'
    Await 'jump lands' { [Math]::Abs((Other $h).velocity.y) -lt 0.1 -and (ErrorAtRest $c) -lt 0.2 }
    Check ((ErrorAtRest $c) -lt 0.2) 'Jump lands and reconciles without persistent offset'
    Send host push 0 4 3
    Await 'server knockback' { (Other $h).velocity.y -gt 0.5 -or [Math]::Abs((Other $h).velocity.x) -gt 0.5 }
    Check ($true) 'Server knockback is still received by client'
    Start-Sleep -Milliseconds 1300
    Await 'knockback settles' { [Math]::Abs((Other $h).velocity.y) -lt 0.1 -and (ErrorAtRest $c) -lt 0.25 }
    Check ((ErrorAtRest $c) -lt 0.25) 'Prediction reconciles after server knockback'
    Send host lag 100 0 100; Send client lag 100 0 100
    Send client move 0 1 0
    Start-Sleep -Milliseconds 1400
    Check ((ErrorAtRest (Read client)) -lt 0.05) 'Prediction stops advancing when server snapshots are lost'
    Send client move 0 0 0
    Send host lag 100 10 1; Send client lag 100 10 1
    Await 'network recovery' { (ErrorAtRest $c) -lt 0.2 -and [Math]::Abs((Other $h).velocity.x) -lt 0.1 }
    Start-Sleep -Milliseconds 600
    Check ((ErrorAtRest (Read client)) -lt 0.2) 'Presentation recovers after packet loss ends'
    Send client move 0 -1 0
    Start-Sleep -Milliseconds 2800
    Send client move 0 0 0
    Await 'wall convergence' { (ErrorAtRest $c) -lt 0.2 }
    Check ([Math]::Abs((Own $c).viewPosition.x) -lt 22) 'Predicted character stays within arena walls'
    Send host remote 100000
    Await 'respawn' { $h.round -eq 2 -and $h.phase -eq 'Countdown' -and $c.phase -eq 'Countdown' }
    Check ((ErrorAtRest $c) -lt 0.05) 'Prediction clears during respawn countdown'
    Await 'round two' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' }
    Send host remote 100000
    Await 'set draft' { $h.set -eq 2 -and $c.phase -eq 'Draft' }
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
    throw
}
finally { Send host quit; Send client quit }

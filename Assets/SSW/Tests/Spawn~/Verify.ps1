param([string]$Run = 'Run1', [string]$Project = (Get-Location).Path, [string]$Build = 'Builds/Spawn/Game.exe')
$ErrorActionPreference = 'Stop'
$root = "$project/Logs/Spawn/$Run"
if (Test-Path "$root/host.json") { throw 'Use a new Run name for each verification' }
New-Item -ItemType Directory -Path $root -Force | Out-Null
$seq = @{ host = 0; client = 0 }
$checks = [Collections.Generic.List[string]]::new()
function Eval([string]$code) {
    $r = unity command eval --code $code --project-path $project --format json | ConvertFrom-Json
    if (-not $r.success -or -not $r.data.result.success) { throw ($r | ConvertTo-Json -Depth 8) }
    return $r.data.result.result
}
function Read($peer) {
    try { Get-Content "$root/$peer.json" -Raw | ConvertFrom-Json } catch { return $null }
}
function Send($peer, $op, $value = 0, $x = 0, $y = 0) {
    $seq[$peer]++
    [IO.File]::WriteAllText("$root/$peer.cmd.json", (@{seq=$seq[$peer];op=$op;value=$value;x=$x;y=$y} | ConvertTo-Json -Compress))
    if ($op -eq 'quit') { return }
    $until = [DateTime]::UtcNow.AddSeconds(5)
    while ((Read $peer).seq -lt $seq[$peer]) {
        if ([DateTime]::UtcNow -gt $until) { throw "Command was not received: $peer $op" }
        Start-Sleep -Milliseconds 40
    }
}
function Await($label, [scriptblock]$condition) {
    $until = [DateTime]::UtcNow.AddSeconds(30)
    do {
        $script:h = Read host
        $script:c = Read client
        if ($h.error -or $c.error) { throw "Runtime error: $($h.error) $($c.error)" }
        if ($h -and $c -and (& $condition)) { return }
        Start-Sleep -Milliseconds 40
    } while ([DateTime]::UtcNow -lt $until)
    throw "Timeout: $label"
}
function Check($condition, $label) {
    if (-not $condition) { throw $label }
    $checks.Add("PASS $label")
    [IO.File]::WriteAllLines("$root/checks.txt", $checks)
}
function AtSpawns($state, $first, $second) {
    if ($state.players.Count -ne 2) { return $false }
    foreach ($p in $state.players) {
        $expected = if ($p.side -eq 1) { $first } else { $second }
        if ([Math]::Abs($p.position.x - $expected) -gt 0.05) { return $false }
    }
    return $true
}
function Restored($first, $second, $label) {
    Await $label { (AtSpawns $h $first $second) -and (AtSpawns $c $first $second) }
    Check ((AtSpawns $h $first $second) -and (AtSpawns $c $first $second)) "${label}: both clients use the assigned markers"
    foreach ($state in @($h,$c)) {
        foreach ($p in $state.players) {
            Check ([Math]::Abs($p.hp-$p.max) -lt 0.01) "$label player $($p.id) restores health"
            Check ($p.augments.Count -ge 1) "$label player $($p.id) keeps augments"
        }
    }
}
try {
    $probePath = ("$root/host".Replace('\','/') | ConvertTo-Json -Compress)
    Eval ('var game = SSW.NetGame.GetOrCreate(); game.SetProfile(SSW.Fighter.Create("Host")); game.gameObject.AddComponent<SSW.NetProbe>().Init(' + $probePath + '); game.StartLocal(true,"127.0.0.1",SSW.PlayerJob.Witch,7795); return "Host started";') | Write-Output
    Start-Process -FilePath (Join-Path $project $Build) -WorkingDirectory $project -ArgumentList @('--net-mode','client','--net-job','Magician','--net-port','7795','--net-name','Client','--net-probe',"$root/client",'-logFile',"$root/client.log",'-screen-fullscreen','0') -WindowStyle Hidden | Out-Null
    Await 'initial draft' { $h.phase -eq 'Draft' -and $c.phase -eq 'Draft' }
    Check ((AtSpawns $h -6 6) -and (AtSpawns $c -6 6)) 'Initial spawn uses scene markers on both peers'
    Eval 'var points=(SSW.SpawnPoints)new UnityEditor.SerializedObject(SSW.NetGame.Current.Arena).FindProperty("_spawnPoints").objectReferenceValue; var data=new UnityEditor.SerializedObject(points); ((UnityEngine.Transform)data.FindProperty("_first").objectReferenceValue).position=new UnityEngine.Vector3(-10,-2.8f); ((UnityEngine.Transform)data.FindProperty("_second").objectReferenceValue).position=new UnityEngine.Vector3(10,-2.8f); return "Markers moved without saving scene";' | Out-Null
    Send host choose 0
    Send client choose 0
    Await 'combat' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' }
    Send host move 0 1 0
    Await 'player moved away' { ($h.players | Where-Object owner).position.x -gt -4 }
    Send host move 0 0 0
    Send host remote 100000
    Await 'next round' { $h.phase -eq 'Countdown' -and $c.phase -eq 'Countdown' -and $h.round -eq 2 }
    Restored -10 10 'Round restart'
    Await 'round two playing' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' }
    Send host remote 100000
    Await 'next set draft' { $h.phase -eq 'Draft' -and $c.phase -eq 'Draft' -and $h.set -eq 2 }
    Restored -10 10 'Set restart'
    Check (($h.players | Where-Object owner).ready -and -not ($c.players | Where-Object owner).ready) 'Only set loser selects augments after respawn'
    Eval 'var points=(SSW.SpawnPoints)new UnityEditor.SerializedObject(SSW.NetGame.Current.Arena).FindProperty("_spawnPoints").objectReferenceValue; points.transform.position+=UnityEngine.Vector3.right; return "Map marker parent moved";' | Out-Null
    Send client choose 0
    Await 'job choice' { ($c.players | Where-Object owner).augments.Count -eq 2 }
    Send client choose 0
    Await 'second set playing' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' }
    Send host damage 100000
    Await 'host loses round' { $h.phase -eq 'Countdown' -and $c.phase -eq 'Countdown' -and $h.round -eq 2 }
    Restored -9 11 'Moved parent restart'
    $h | ConvertTo-Json -Depth 12 | Set-Content "$root/final-host.json"
    $c | ConvertTo-Json -Depth 12 | Set-Content "$root/final-client.json"
    Write-Output "PASS $($checks.Count) spawn checks"
}
finally {
    Send client quit
    unity command editor_stop --project-path $project --format json | Out-Null
}

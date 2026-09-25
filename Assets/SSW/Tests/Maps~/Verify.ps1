param(
    [ValidateSet('Catalog','Flow','Effects')] [string]$Mode = 'Flow',
    [string]$Run = ('Run' + (Get-Date -Format 'yyyyMMddHHmmss')),
    [string]$Project = (Get-Location).Path,
    [string]$Build = 'Builds/MapRotation/Game.exe'
)
$ErrorActionPreference = 'Stop'
$root = "$Project/Logs/MapRotation25/$Run"
if (Test-Path -LiteralPath "$root/host.json") { throw 'Use a new run name.' }
[IO.Directory]::CreateDirectory($root) | Out-Null
$seq = @{ host = 0; client = 0 }
$checks = [Collections.Generic.List[string]]::new()
$visited = [Collections.Generic.List[string]]::new()
$clientProcess = $null
function Eval([string]$code) {
    [IO.File]::WriteAllText("$root/Eval.cs", $code)
    $reply = unity command eval_file --file "$root/Eval.cs" --caller plugin --skill unity-cli --project-path $Project --format json | ConvertFrom-Json
    if (-not $reply.success -or -not $reply.data.result.success) { throw ($reply | ConvertTo-Json -Depth 7) }
    return $reply.data.result.result
}
function Read($peer) {
    try { return Get-Content -LiteralPath "$root/$peer.json" -Raw | ConvertFrom-Json } catch { return $null }
}
function Await($label, [scriptblock]$condition, $timeout = 35) {
    $until = [DateTime]::UtcNow.AddSeconds($timeout)
    do {
        $script:h = Read host
        $script:c = Read client
        if ($h.error -or $c.error) { throw "Runtime error: $($h.error) $($c.error)" }
        if ($h -and $c -and (& $condition)) { return }
        Start-Sleep -Milliseconds 50
    } while ([DateTime]::UtcNow -lt $until)
    throw "Timeout: $label (host $($h.phase) set $($h.set); client $($c.phase) set $($c.set))"
}
function Send($peer, $op, $value = 0, $x = 0, $y = 0) {
    $seq[$peer]++
    $json = @{ seq=$seq[$peer]; op=$op; value=$value; x=$x; y=$y } | ConvertTo-Json -Compress
    [IO.File]::WriteAllText("$root/$peer.cmd.json", $json)
    if ($op -eq 'quit') { return }
    $until = [DateTime]::UtcNow.AddSeconds(5)
    while ((Read $peer).seq -lt $seq[$peer]) {
        if ([DateTime]::UtcNow -gt $until) { throw "Command timeout: $peer $op" }
        Start-Sleep -Milliseconds 30
    }
}
function Check($condition, $label) {
    if (-not $condition) { throw $label }
    $checks.Add("PASS $label")
    [IO.File]::WriteAllLines("$root/Checks.txt", $checks)
}
function AtSpawns($state) {
    if ($state.mapSpawns.Count -ne 2 -or $state.players.Count -ne 2) { return $false }
    foreach ($player in $state.players) {
        $slot = if ($player.side -eq 1) { 0 } else { 1 }
        $spawn = $state.mapSpawns[$slot]
        if ([Math]::Abs($spawn.x-$player.position.x) -gt 0.15 -or [Math]::Abs($spawn.y-$player.position.y) -gt 0.15) { return $false }
    }
    return $true
}
function CheckMap($label) {
    Await "$label map agreement" { $h.map -and $h.map -eq $c.map -and $h.mapObject -eq $c.mapObject -and (AtSpawns $h) -and (AtSpawns $c) }
    Check ($h.map -eq $c.map -and $h.mapObject -eq $c.mapObject) "$label same network map"
    Check ((AtSpawns $h) -and (AtSpawns $c)) "$label both spawn markers"
    $state = Eval 'var game = SSW.NetGame.Current; int maps = 0; foreach (var obj in game.Manager.SpawnManager.SpawnedObjectsList) if (obj.TryGetComponent<SSW.BattleMap>(out _)) maps++; return new { maps, ready = game.MapReady };'
    Check ($state.maps -eq 1 -and $state.ready) "$label one map and both peers loaded"
    Check ($h.players.Count -eq 2 -and $c.players.Count -eq 2) "$label two players"
    Check ($h.shots.Count -eq 0 -and $c.shots.Count -eq 0) "$label old projectiles cleared"
}
function PickAll {
    $until = [DateTime]::UtcNow.AddSeconds(25)
    while ([DateTime]::UtcNow -lt $until) {
        $script:h = Read host
        $script:c = Read client
        if ($h.error -or $c.error) { throw "Draft runtime error: $($h.error) $($c.error)" }
        if ($h.phase -eq 'Playing' -and $c.phase -eq 'Playing') { return }
        foreach ($peer in @('host','client')) {
            $state = Read $peer
            $local = $state.players | Where-Object owner
            if ($state.phase -eq 'Draft' -and $local.draftUnlocked -and -not $local.ready) { Send $peer choose 0 }
        }
        Start-Sleep -Milliseconds 180
    }
    throw 'Draft did not finish.'
}
try {
    if (-not (Eval 'return UnityEditor.EditorApplication.isPlaying;')) {
        unity command editor_play --caller plugin --skill unity-cli --project-path $Project --format json | Out-Null
    }
    $until = [DateTime]::UtcNow.AddSeconds(20)
    while ($true) {
        try { if (Eval 'return UnityEditor.EditorApplication.isPlaying && !UnityEditor.EditorApplication.isCompiling;') { break } } catch { }
        if ([DateTime]::UtcNow -gt $until) { throw 'Play Mode timeout.' }
        Start-Sleep -Milliseconds 200
    }
    $probe = "$root/host".Replace('\','/')
    Eval ('var game = SSW.NetGame.GetOrCreate(); game.SetProfile(SSW.Fighter.Create("Host")); game.gameObject.AddComponent<SSW.NetProbe>().Init("' + $probe + '"); game.StartLocal(true,"127.0.0.1",SSW.PlayerJob.Witch,7797); return true;') | Out-Null
    $clientProcess = Start-Process -FilePath (Join-Path $Project $Build) -WorkingDirectory $Project -ArgumentList @('--net-mode','client','--net-job','Magician','--net-port','7797','--net-name','Client','--net-probe',"$root/client",'-logFile',"$root/client.log",'-screen-fullscreen','0','-screen-width','1280','-screen-height','720') -WindowStyle Hidden -PassThru
    Await 'initial draft' { $h.phase -eq 'Draft' -and $c.phase -eq 'Draft' }
    CheckMap 'initial'
    if ($Mode -eq 'Catalog') {
        for ($i=0; $i -lt 14; $i++) {
            $name = Eval ('var game = SSW.NetGame.Current; var maps = game.GetComponent<SSW.MapRotation>(); typeof(SSW.MapRotation).GetField("_index", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(maps, ' + $i + '); maps.Restart(false); foreach (var player in game.Players) player.Drive.Teleport(game.Arena.Spawn(player.Side == 1 ? 0 : 1)); return game.Arena.Map.Title;')
            Await 'catalog map applied' { $h.map -eq $name -and $c.map -eq $name }
            CheckMap $name
            $visited.Add($name)
            Start-Sleep -Milliseconds 700
            $h = Read host
            $c = Read client
            Check ($h.mapBodies.Count -eq $c.mapBodies.Count) "$name body counts match"
            if ($h.mapBodies.Count -gt 0) {
                $maxError = 0.0
                for ($body=0; $body -lt $h.mapBodies.Count; $body++) {
                    $dx = $h.mapBodies[$body].x - $c.mapBodies[$body].x
                    $dy = $h.mapBodies[$body].y - $c.mapBodies[$body].y
                    $maxError = [Math]::Max($maxError,[Math]::Sqrt($dx*$dx+$dy*$dy))
                }
                Check ($maxError -lt 0.5) "$name moving platforms replicated"
            }
            Check ($h.timeScale -eq 1 -and $c.timeScale -eq 1) "$name draft stays at normal time"
        }
        Check (($visited | Select-Object -Unique).Count -eq 14) 'all 14 maps instantiated on both peers'
    } elseif ($Mode -eq 'Effects') {
        $name = Eval 'var game = SSW.NetGame.Current; var maps = game.GetComponent<SSW.MapRotation>(); int index = 0; while (maps.Prefabs[index].Title != "KDH_Map 17") index++; typeof(SSW.MapRotation).GetField("_index", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(maps, index); maps.Restart(false); foreach (var player in game.Players) player.Drive.Teleport(game.Arena.Spawn(player.Side == 1 ? 0 : 1)); return game.Arena.Map.Title;'
        Await 'clock map' { $h.map -eq $name -and $c.map -eq $name }
        CheckMap 'clock map'
        PickAll
        Await 'shared slow time' { $h.timeScale -lt 0.7 -and $c.timeScale -lt 0.7 } 20
        Check ([Math]::Abs($h.timeScale-$c.timeScale) -lt 0.12) 'clock time matches on both peers'
        Send host remote 100000
        Await 'time restored after knockout' { $h.timeScale -eq 1 -and $c.timeScale -eq 1 -and $h.phase -ne 'Playing' }
        Check ($h.timeScale -eq 1 -and $c.timeScale -eq 1) 'round end restores time'
        Await 'next round combat' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' -and $h.round -eq 2 }
        Eval 'var game = SSW.NetGame.Current; var maps = game.GetComponent<SSW.MapRotation>(); int index = 0; while (maps.Prefabs[index].Title != "RYU_MAP 1") index++; typeof(SSW.MapRotation).GetField("_index", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(maps, index); maps.Restart(false); foreach (var player in game.Players) player.Drive.Teleport(game.Arena.Spawn(player.Side == 1 ? 0 : 1)); return true;' | Out-Null
        Await 'swing map loaded' { $h.map -eq 'RYU_MAP 1' -and $c.map -eq 'RYU_MAP 1' -and $h.mapObject -eq $c.mapObject }
        Eval 'var game = SSW.NetGame.Current; var body = (UnityEngine.Rigidbody2D)new UnityEditor.SerializedObject(game.Arena.Map.GetComponent<SSW.MapMotion>()).FindProperty("_bodies").GetArrayElementAtIndex(0).objectReferenceValue; foreach (var player in game.Players) if (!player.IsOwner) { float feet = player.transform.position.y-player.Collider.bounds.min.y; player.Drive.Teleport(new UnityEngine.Vector2(body.position.x, body.GetComponent<UnityEngine.Collider2D>().bounds.max.y + feet + 0.02f)); } return true;' | Out-Null
        $targetX = Eval 'var body = (UnityEngine.Rigidbody2D)new UnityEditor.SerializedObject(SSW.NetGame.Current.Arena.Map.GetComponent<SSW.MapMotion>()).FindProperty("_bodies").GetArrayElementAtIndex(0).objectReferenceValue; return body.position.x;'
        Await 'client standing on swing' { ($c.players | Where-Object owner).grounded -and ($h.players | Where-Object { -not $_.owner }).grounded -and [Math]::Abs(($c.players | Where-Object owner).position.x - $targetX) -lt 0.2 -and [Math]::Abs(($h.players | Where-Object { -not $_.owner }).position.x - $targetX) -lt 0.2 }
        $startX = ($c.players | Where-Object owner).position.x
        Send client metrics
        Eval 'var map = SSW.NetGame.Current.Arena.Map; var body = (UnityEngine.Rigidbody2D)new UnityEditor.SerializedObject(map.GetComponent<SSW.MapMotion>()).FindProperty("_bodies").GetArrayElementAtIndex(0).objectReferenceValue; body.AddForce(UnityEngine.Vector2.right * (3f * body.mass), UnityEngine.ForceMode2D.Impulse); return true;' | Out-Null
        Start-Sleep -Milliseconds 1800
        $h = Read host
        $c = Read client
        $local = $c.players | Where-Object owner
        $remote = $h.players | Where-Object { -not $_.owner }
        @{startX=$startX; host=$h; client=$c} | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath "$root/Carry.json"
        Check ($local.position.x -gt $startX + 0.15) 'client rides platform without movement input'
        Check ([Math]::Abs($local.position.x - $remote.position.x) -lt 0.5) 'riding position matches server'
        Check ($local.maxCorrection -lt 0.6) 'riding prediction stays stable'
        Check ($h.timeScale -eq 1 -and $c.timeScale -eq 1) 'leaving clock map restores time'
        $beforeY = $c.mapBodies[10].y
        Eval 'foreach (var pin in SSW.NetGame.Current.Arena.Map.GetComponentsInChildren<SSW.Pin>()) pin.TakeDamage(float.MaxValue); return true;' | Out-Null
        Await 'cut platform falling' { $h.mapBodies[10].y -lt $beforeY - 0.4 -and $c.mapBodies[10].y -lt $beforeY - 0.4 } 5
        $h = Read host
        $c = Read client
        @{ beforeY=$beforeY; host=$h; client=$c } | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath "$root/Cut.json"
        Check ($c.mapBodies[10].y -lt $beforeY - 0.4) 'cut platform falls on client'
        Check ([Math]::Abs($h.mapBodies[10].y-$c.mapBodies[10].y) -lt 0.8) 'cut platform follows server'
        $previousObject = $h.mapObject
        $previousMap = $h.map
        Send host damage 100000
        Await 'third round countdown' { $h.phase -eq 'Countdown' -and $c.phase -eq 'Countdown' -and $h.round -eq 3 -and $c.round -eq 3 }
        CheckMap 'round restoration'
        Check ($h.map -eq $previousMap -and $c.map -eq $previousMap) 'small round keeps the same map'
        Check ($h.mapObject -ne $previousObject -and $h.mapObject -eq $c.mapObject) 'small round replaces damaged map on both peers'
        $restored = Eval 'var map = SSW.NetGame.Current.Arena.Map; var pins = map.GetComponentsInChildren<SSW.Pin>(); int healthy = 0; foreach (var pin in pins) if (pin.Current == pin.Max) healthy++; var joints = map.GetComponentsInChildren<UnityEngine.Joint2D>(); int attached = 0; foreach (var joint in joints) if (joint.enabled) attached++; return new { pins = pins.Length, healthy, joints = joints.Length, attached };'
        Check ($restored.pins -gt 0 -and $restored.pins -eq $restored.healthy -and $restored.joints -eq $restored.attached) 'all pins and ropes are restored'
        Await 'restored platform settles' { [Math]::Abs($h.mapBodies[10].y-$beforeY) -lt 0.2 -and [Math]::Abs($c.mapBodies[10].y-$beforeY) -lt 0.2 } 3
        @{ restored=$restored; beforeY=$beforeY; host=$h; client=$c } | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath "$root/Restore.json"
        Check ([Math]::Abs($h.mapBodies[10].y-$beforeY) -lt 0.2 -and [Math]::Abs($c.mapBodies[10].y-$beforeY) -lt 0.2) 'fallen platform returns to its starting position on both peers'

        if ($h.error -or $c.error) { throw "Effects error: $($h.error) $($c.error)" }
    } else {
        for ($set=1; $set -le 7; $set++) {
            Await "set $set draft" { $h.set -eq $set -and $c.set -eq $set -and $h.phase -eq 'Draft' -and $c.phase -eq 'Draft' }
            CheckMap "set $set"
            Check (-not $visited.Contains($h.map)) "set $set map not repeated"
            $visited.Add($h.map)
            $map = $h.map
            $owned = @{}
            foreach ($p in $h.players) { $owned["$($p.id)"] = @($p.augments) }
            PickAll
            $hostWins = $set % 2 -eq 1
            $damageOp = if ($hostWins) { 'remote' } else { 'damage' }
            Send host $damageOp 100000
            Await 'same-set countdown' { $h.phase -eq 'Countdown' -and $c.phase -eq 'Countdown' -and $h.set -eq $set -and $h.round -eq 2 }
            CheckMap "set $set round 2"
            Check ($h.map -eq $map -and $c.map -eq $map) "set $set keeps map between rounds"
            Check (-not $h.draftStatus -and -not $c.draftStatus) "set $set has no round reward"
            foreach ($p in $h.players) {
                foreach ($id in $owned["$($p.id)"]) { Check ($p.augments -contains $id) "set $set player $($p.id) keeps augment $id" }
                Check ([Math]::Abs($p.hp-$p.max) -lt 0.01) "set $set player $($p.id) health restored"
            }
            Await 'round 2 combat' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' }
            Send host $damageOp 100000
            if ($set -lt 7) {
                $next = $set + 1
                Await "set $next draft" { $h.set -eq $next -and $c.set -eq $next -and $h.phase -eq 'Draft' -and $c.phase -eq 'Draft' }
                Check ($h.map -ne $map -and $c.map -ne $map) "set $set changes map during reward"
                $loser = if ($hostWins) { $c.players | Where-Object owner } else { $h.players | Where-Object owner }
                $winner = if ($hostWins) { $h.players | Where-Object owner } else { $c.players | Where-Object owner }
                Check (-not $loser.ready -and $winner.ready) "set $set only loser drafts"
            } else {
                Await 'match finished' { $h.phase -eq 'Finished' -and $c.phase -eq 'Finished' }
                Check ($h.firstSets -eq 4 -and $h.secondSets -eq 3) 'match ends at 4:3'
                Check ($h.map -eq $map -and $c.map -eq $map) 'no map rotation after match'
            }
        }
        Check (($visited | Select-Object -Unique).Count -eq 7) 'seven sets use seven distinct maps'
    }
    $h | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath "$root/FinalHost.json"
    $c | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath "$root/FinalClient.json"
    $visited | ConvertTo-Json | Set-Content -LiteralPath "$root/Maps.json"
    Write-Output "PASS $Mode $($checks.Count) checks"
}
finally {
    try { Send client quit } catch { Write-Warning $_ }
    unity command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json | Out-Null
    if ($clientProcess) {
        if (-not $clientProcess.WaitForExit(5000)) { Stop-Process -Id $clientProcess.Id }
    }
}

param([string]$Run=('Run'+(Get-Date -Format 'yyyyMMddHHmmss')),[string]$Project=(Get-Location).Path,[string]$Build='Builds/MapFeel/Game.exe',[switch]$SkipTerrain,[switch]$TerrainOnly)
$ErrorActionPreference='Stop'
$Project=[IO.Path]::GetFullPath($Project).Replace('\','/')
$root="$Project/Logs/MapFeel25/$Run"
if(Test-Path $root){throw 'Use a new run name.'}
[IO.Directory]::CreateDirectory($root)|Out-Null
$seq=@{host=0;client=0}
$menuSeq=@{host=0;client=0}
$checks=[Collections.Generic.List[string]]::new()
$visited=[Collections.Generic.List[string]]::new()
$clientProcess=$null
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
    @{label=$label;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Failure.json"
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
    if (-not $condition) { @{label=$label;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Failure.json"; throw $label }
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
function Menu($peer) { try { Get-Content "$root/$peer.menu.json" -Raw|ConvertFrom-Json }catch{$null} }
function Click($peer,$target){
    $until=[DateTime]::UtcNow.AddSeconds(8)
    while(@((Menu $peer).buttons|Where-Object {$_.name -eq $target -and $_.enabled}).Count -eq 0){if([DateTime]::UtcNow -gt $until){throw "Button unavailable $target"};Start-Sleep -Milliseconds 80}
    $menuSeq[$peer]++
    [IO.File]::WriteAllText("$root/$peer.menu.cmd.json",(@{seq=$menuSeq[$peer];op='click';target=$target}|ConvertTo-Json -Compress))
    $until=[DateTime]::UtcNow.AddSeconds(6)
    while((Menu $peer).seq -lt $menuSeq[$peer]){if([DateTime]::UtcNow -gt $until){throw "Click timeout $target"};Start-Sleep -Milliseconds 70}
}
function Map($title){
    Eval ('var game=SSW.NetGame.Current;var rotation=game.GetComponent<SSW.MapRotation>();int index=0;while(rotation.Prefabs[index].Title!="'+$title+'")index++;typeof(SSW.MapRotation).GetField("_index",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(rotation,index);rotation.Restart(false);foreach(var p in game.Players)p.Drive.Teleport(game.Arena.Spawn(p.Side==1?0:1));return true;')|Out-Null
    Await 'test map loaded' {$h.map -eq $title -and $c.map -eq $title -and $h.mapObject -eq $c.mapObject}
}
function Visit($label){
    CheckMap $label
    if($visited.Count -gt 0){Check ($h.map -ne $visited[$visited.Count-1]) "$label changes map"}
    $cycle=[Math]::Floor($visited.Count/14)*14
    for($i=$cycle;$i -lt $visited.Count;$i++){Check ($visited[$i] -ne $h.map) "$label no duplicate in bag"}
    $visited.Add($h.map)
    [IO.File]::WriteAllText("$root/Maps.json",($visited|ConvertTo-Json))
}
try {
    Eval 'if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path=="Assets/RYU/00.Scene/StartMenu.unity")return true;for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new System.InvalidOperationException("Save scene changes before testing.");UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/RYU/00.Scene/StartMenu.unity");return true;'|Out-Null
    unity command editor_play --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    $until=[DateTime]::UtcNow.AddSeconds(35)
    do {
        try{$scene=Eval 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;'}catch{$scene=''}
        if($scene -eq 'MainMenu'){break}
        if([DateTime]::UtcNow -gt $until){throw 'StartMenu login did not reach MainMenu.'}
        Start-Sleep -Milliseconds 350
    }while($true)
    Eval ('var game=SSW.NetGame.GetOrCreate();game.SetLocalJob(SSW.PlayerJob.Witch);game.SetProfile(SSW.Fighter.Create("Host#88320"));game.gameObject.AddComponent<SSW.NetProbe>().Init("'+$root+'/host");game.gameObject.AddComponent<SSW.MenuProbe>().Init("'+$root+'/host");return true;')|Out-Null
    $clientProcess=Start-Process -FilePath (Join-Path $Project $Build) -WorkingDirectory $Project -ArgumentList @('--net-profile','audit-startmenu25','--net-name','Client#49873','--net-probe',"$root/client",'-logFile',"$root/client.log",'-screen-fullscreen','0','-screen-width','1280','-screen-height','720') -WindowStyle Hidden -PassThru
    Await 'both MainMenu' {(Menu 'host').scene -eq 'MainMenu' -and (Menu 'client').scene -eq 'MainMenu'}
    foreach($peer in @('host','client')){
        $menuSeq[$peer]++
        [IO.File]::WriteAllText("$root/$peer.menu.cmd.json",(@{seq=$menuSeq[$peer];op='job';value=2}|ConvertTo-Json -Compress))
        $until=[DateTime]::UtcNow.AddSeconds(5)
        while((Menu $peer).seq -lt $menuSeq[$peer]){if([DateTime]::UtcNow -gt $until){throw 'Job command timeout'};Start-Sleep -Milliseconds 80}
    }
    Click host 'Button_Play'
    Click client 'Button_Play'
    Click host 'Button_매치메이킹'
    Await 'first quick room' {(Menu 'host').joined} 90
    Click client 'Button_매치메이킹'
    Await 'Relay QuickPlay and scene load' {$h.phase -eq 'Draft' -and $c.phase -eq 'Draft' -and $h.map -eq $c.map} 90
    Check ((Menu 'host').count -eq 2 -and (Menu 'client').count -eq 2) 'StartMenu quick match connected through Relay'
    Check ($h.leftName -eq 'Host' -and $h.rightName -eq 'Client' -and $c.leftName -eq 'Client' -and $c.rightName -eq 'Host') 'VS labels remove account tags'
    Send host fps 60
    Send client fps 60
    Map 'KDH_Map 4'
    Send host trace 1 3
    Send client trace 1 3
    Await 'draft traces' {(Test-Path "$root/host.trace.1.json") -and (Test-Path "$root/client.trace.1.json")} 6
    foreach($peer in @('host','client')){
        $trace=Get-Content "$root/$peer.trace.1.json" -Raw|ConvertFrom-Json
        Check (@($trace.frames|Where-Object {$_.gustEffect -or $_.gust -gt 0}).Count -eq 0) "$peer no premature gust during draft"
    }
    PickAll
    Eval 'foreach(var p in SSW.NetGame.Current.Players){typeof(SSW.Health).GetMethod("SetMax",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.Health,new object[]{10000f});p.Health.Heal(10000);p.Drive.Teleport(new UnityEngine.Vector2(SSW.NetGame.Current.Arena.Map.Bounds.center.x+p.Side*6,-6.8f));}return true;'|Out-Null
    Send host trace 2 13
    Send client trace 2 13
    Await 'gust traces' {(Test-Path "$root/host.trace.2.json") -and (Test-Path "$root/client.trace.2.json")} 17
    $gusts=[Collections.Generic.List[object]]::new()
    foreach($peer in @('host','client')){
        $trace=Get-Content "$root/$peer.trace.2.json" -Raw|ConvertFrom-Json
        foreach($id in @(1,2,3)){
            $fx=$trace.frames|Where-Object gust -eq $id|Select-Object -First 1
            $force=$trace.frames|Where-Object pulse -eq $id|Select-Object -First 1
            Check ($fx -and $force -and $fx.gustEffect) "$peer gust $id force and effect present"
            $gap=[Math]::Abs($fx.time-$force.time)
            $gusts.Add(@{peer=$peer;pulse=$id;gap=$gap;velocity=$force.velocity.y})
            Check ($gap -lt 0.15) "$peer gust $id effect aligned with movement"
            Check ($force.velocity.y -gt 10) "$peer gust $id moves upward"
        }
    }
    foreach($peer in @("host","client")){ $trace=Get-Content "$root/$peer.trace.2.json" -Raw|ConvertFrom-Json; Check (($trace.frames.correction|Measure-Object -Maximum).Maximum -lt 0.5) "$peer gust has no large rollback" }
    $gusts|ConvertTo-Json|Set-Content "$root/Gust.json"
    Send client lag 80 20 2
    Send host trace 7 5
    Send client trace 7 5
    Await 'gust impaired network traces' {(Test-Path "$root/host.trace.7.json") -and (Test-Path "$root/client.trace.7.json")} 8
    Send client lag 0 0 0
    $trace=Get-Content "$root/client.trace.7.json" -Raw|ConvertFrom-Json
    Check (($trace.frames.correction|Measure-Object -Maximum).Maximum -lt 0.5) 'gust prediction remains stable with added delay jitter and packet loss'
    Check (($trace.frames.pulse|Measure-Object -Maximum).Maximum -gt ($trace.frames.pulse|Measure-Object -Minimum).Minimum) 'gust continues during impaired network'
    $teleport=Eval 'var values=new System.Collections.Generic.List<object>();foreach(var p in SSW.NetGame.Current.Players){uint before=p.Drive.PulseSequence;p.Drive.Teleport(new UnityEngine.Vector2(SSW.NetGame.Current.Arena.Map.Bounds.center.x+p.Side*6,-6.8f));values.Add(new{before,after=p.Drive.PulseSequence});}return values;'
    foreach($item in $teleport){Check ($item.before -gt 0 -and $item.before -eq $item.after) 'teleport retains applied gust number'}
    Map 'KDH_Map 2'
    Send host move 0 0
    Send client move 0 0
    Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Drive.Teleport(new UnityEngine.Vector2(0,-8));return true;'|Out-Null
    Send host trace 3 4
    Send client trace 3 4
    Send host move 0 1
    Send client move 0 1
    Await 'water two traces' {(Test-Path "$root/host.trace.3.json") -and (Test-Path "$root/client.trace.3.json")} 7
    Send host move 0 0
    Send client move 0 0
    foreach($peer in @('host','client')){
        $trace=Get-Content "$root/$peer.trace.3.json" -Raw|ConvertFrom-Json
        $height=($trace.frames.body.y|Measure-Object -Maximum).Maximum
        Check ($height -gt 4) "$peer waterfall two reaches upper level"
    }
    Map 'KDH_Map 15'
    Send host trace 4 4
    Send client trace 4 4
    Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Drive.Teleport(new UnityEngine.Vector2(p.Side*7,-8));return true;'|Out-Null
    Await 'water fifteen traces' {(Test-Path "$root/host.trace.4.json") -and (Test-Path "$root/client.trace.4.json")} 7
    foreach($peer in @('host','client')){
        $trace=Get-Content "$root/$peer.trace.4.json" -Raw|ConvertFrom-Json
        $height=($trace.frames.body.y|Measure-Object -Maximum).Maximum
        Check ($height -gt 4) "$peer waterfall fifteen reaches upper level"
    }
    Map 'KDH_Map 17'
    Send host trace 5 18
    Send client trace 5 18
    Await 'tempo traces' {(Test-Path "$root/host.trace.5.json") -and (Test-Path "$root/client.trace.5.json")} 23
    foreach($peer in @('host','client')){
        $trace=Get-Content "$root/$peer.trace.5.json" -Raw|ConvertFrom-Json
        Check (@($trace.frames|Where-Object timeScale -lt 0.3).Count -gt 0) "$peer slow phase runs"
        Check ($trace.frames[-1].timeScale -eq 1) "$peer slow phase restores normal speed"
    }
    Map 'KDH_Map 13'
    Check ($h.cameraSize -eq 9 -and $c.cameraSize -eq 9) 'fixed camera size nine on both peers'
    Send host trace 6 5
    Send client trace 6 5
    Eval 'var g=SSW.NetGame.Current;var f=g.Arena.Map.GetComponentInChildren<SSW.SakuraField>();var ps=f.GetComponent<UnityEngine.ParticleSystem>();ps.Stop(true,UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.simulationSpace=UnityEngine.ParticleSystemSimulationSpace.World;main.startDelay=0;var em=ps.emission;em.enabled=false;ps.Play();foreach(var p in g.Players){p.Drive.Teleport(new UnityEngine.Vector2(p.Side*6,0));var emit=new UnityEngine.ParticleSystem.EmitParams{position=new UnityEngine.Vector3(p.Side*6,0.8f,0),velocity=UnityEngine.Vector3.down*20,startLifetime=2,startSize=0.4f};ps.Emit(emit,8);}return true;'|Out-Null
    Await 'sakura traces' {(Test-Path "$root/host.trace.6.json") -and (Test-Path "$root/client.trace.6.json")} 8
    foreach($peer in @('host','client')){
        $trace=Get-Content "$root/$peer.trace.6.json" -Raw|ConvertFrom-Json
        Check (($trace.frames.sakuraEffects|Measure-Object -Maximum).Maximum -gt 0) "$peer Sakura collision effect visible"
        $speeds=$trace.frames.speed|Measure-Object -Minimum -Maximum
        Check ($speeds.Maximum -gt $speeds.Minimum*1.1) "$peer Sakura collision haste applies and expires"
    }
    Map 'KDH_Map 2'
    $a=Get-Content "$root/host.trace.6.json" -Raw|ConvertFrom-Json
    $b=Get-Content "$root/client.trace.6.json" -Raw|ConvertFrom-Json
    Check ($a.frames[0].petalSeed -gt 0 -and $a.frames[0].petalSeed -eq $b.frames[0].petalSeed) 'Sakura shared random seed'
    Await 'tempo restored after map change' {$h.timeScale -eq 1 -and $c.timeScale -eq 1}
    Check ($true) 'map transition restores time scale'
    Map 'KDH_Map 13'
    Check (-not $h.error -and -not $c.error) 'Sakura revisit has no pool lifecycle error'
    Send host remote 100000
    Await 'fresh round countdown' {$h.round -eq 2 -and $c.round -eq 2 -and $h.phase -eq 'Countdown' -and $c.phase -eq 'Countdown'}
    CheckMap 'round reset'
    Check ($h.cameraSize -eq 9 -and $c.cameraSize -eq 9) 'round change retains fixed camera size'
    @{checks=$checks.Count;host=$h;client=$c;build=$Build}|ConvertTo-Json -Depth 14|Set-Content "$root/Result.json"
    Write-Output "PASS $($checks.Count) map feel checks"
}finally{
    foreach($peer in @('client','host')){try{if((Read $peer).listening){Send $peer exit}}catch{Write-Warning $_.Exception.Message}}
    $until=[DateTime]::UtcNow.AddSeconds(15)
    do{if(-not(Menu host).joined -and -not(Menu client).joined){break};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow -lt $until)
    try{Send client quit}catch{}
    if($clientProcess -and -not $clientProcess.WaitForExit(5000)){Stop-Process -Id $clientProcess.Id}
    unity command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
}
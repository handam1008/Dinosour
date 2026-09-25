param([string]$Run=('Run'+(Get-Date -Format 'yyyyMMddHHmmss')),[string]$Project=(Get-Location).Path,[string]$Build='Builds/TerrainFix/Game.exe',[switch]$SkipTerrain,[switch]$TerrainOnly)
$ErrorActionPreference='Stop'
$Project=[IO.Path]::GetFullPath($Project).Replace('\','/')
$root="$Project/Logs/StartMenu25/$Run"
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
    PickAll
    Eval 'foreach(var p in SSW.NetGame.Current.Players){typeof(SSW.Health).GetMethod("SetMax",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.Health,new object[]{10000f});p.Health.Heal(10000);}return true;'|Out-Null
    if(-not $SkipTerrain){
    Map 'KDH_Map 9'
    Eval 'var g=SSW.NetGame.Current;var floor=g.Arena.Map.transform.Find("KDH_Map 9/Part of/Part (6)").GetComponent<UnityEngine.Collider2D>();foreach(var p in g.Players){float feet=p.transform.position.y-p.Collider.bounds.min.y;p.Drive.Teleport(new UnityEngine.Vector2(p.Side*5,floor.bounds.max.y+feet+0.1f));}return true;'|Out-Null
    Await 'both standing on volcano' {
        $a=$h.players|Where-Object owner;$b=$c.players|Where-Object owner
        $a.grounded -and $b.grounded -and [Math]::Abs($a.position.x-5) -lt 0.5 -and [Math]::Abs($b.position.x+5) -lt 0.5 -and $a.position.y -gt -6.8 -and $a.position.y -lt -6.2 -and $b.position.y -gt -6.8 -and $b.position.y -lt -6.2
    } 8
    Send client metrics
    $until=[DateTime]::UtcNow.AddSeconds(8)
    $minY=100.0
    while([DateTime]::UtcNow -lt $until){
        $h=Read host;$c=Read client
        foreach($p in @($h.players)+@($c.players)){$minY=[Math]::Min($minY,$p.position.y)}
        if($h.error -or $c.error){throw "Terrain error $($h.error) $($c.error)"}
        Start-Sleep -Milliseconds 60
    }
    Check ($minY -gt -7.5) 'neither peer falls through shaking floor for eight seconds'
    Check (($c.players|Where-Object owner).maxCorrection -lt 0.9) 'shaking floor client prediction stays bounded'
    @{minimumY=$minY;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Floor.json"
    $before=($c.players|Where-Object owner).position.y
    Send client jump
    Await 'jump from shaking floor' {($c.players|Where-Object owner).position.y -gt $before+0.5} 5
    Check ($true) 'client can jump from shaking floor'
    foreach($title in @('KDH_Map 2','KDH_Map 15')){
        Map $title
        $zone=Eval 'var g=SSW.NetGame.Current;var zone=g.Arena.Map.GetComponentInChildren<SSW.LiftZone>().GetComponent<UnityEngine.Collider2D>().bounds;float y=zone.min.y+1;foreach(var p in g.Players)p.Drive.Teleport(new UnityEngine.Vector2(zone.center.x+p.Side*zone.size.x*0.225f,y));return new{y,x=zone.center.x,exitX=zone.center.x+zone.size.x*0.225f,top=zone.max.y};'
        Send client metrics
        Await 'waterfall lifts both peers' {($h.players|Where-Object owner).position.y -gt $zone.y+0.5 -and ($c.players|Where-Object owner).position.y -gt $zone.y+0.5 -and ($c.players|Where-Object owner).velocity.y -gt 0} 5
        Check ($true) "$title waterfall lifts server and client"
        @{zone=$zone;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Water$($title.Split(' ')[-1]).json"
        Eval ('foreach(var p in SSW.NetGame.Current.Players)p.Drive.Teleport(new UnityEngine.Vector2('+($zone.exitX).ToString([Globalization.CultureInfo]::InvariantCulture)+'f,2));return true;')|Out-Null
        Await 'leaving water restores gravity' {($h.players|Where-Object owner).velocity.y -lt -1 -and ($c.players|Where-Object owner).velocity.y -lt -1} 4
        Check ($true) "$title gravity resumes outside waterfall"
    }
    }
    if($TerrainOnly){@{checks=$checks.Count;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Result.json";Write-Output "PASS terrain $($checks.Count) checks";return}
    $initial=Eval 'var g=SSW.NetGame.Current;var r=g.GetComponent<SSW.MapRotation>();r.Begin();foreach(var p in g.Players)p.Drive.Teleport(g.Arena.Spawn(p.Side==1?0:1));return new{id=g.Arena.Map.NetworkObjectId,title=g.Arena.Map.Title};'
    Await 'new map snapshot received' {$h.mapObject -eq $initial.id -and $c.mapObject -eq $initial.id}
    $visited.Clear()
    for($set=1;$set -le 7;$set++){
        if($set -gt 1){Await 'set draft' {$h.set -eq $set -and $c.set -eq $set -and $h.phase -eq 'Draft' -and $c.phase -eq 'Draft'}}
        if($set -eq 1){Await 'fresh first map' {$h.mapObject -eq $c.mapObject};$visited.Add($h.map)}else{Visit "set $set round 1"}
        PickAll
        $winner=if($set%2 -eq 1){'remote'}else{'damage'}
        $loser=if($winner -eq 'remote'){'damage'}else{'remote'}
        for($round=1;$round -le 3;$round++){
            $previous=$h.map
            $owned=@{};foreach($p in $h.players){$owned["$($p.id)"]=@($p.augments)}
            $op=if($round -eq 2){$loser}else{$winner}
            Send host $op 100000
            if($set -eq 1 -and $round -eq 1){
                Await 'victory banner' {$h.wipeTitle -eq 'Host 승리!' -and $c.wipeTitle -eq 'Host 승리!'} 4
                Check ($true) 'victory banners remove account tags'
            }
            if($round -lt 3){
                $next=$round+1
                Await 'next round countdown' {$h.set -eq $set -and $c.set -eq $set -and $h.round -eq $next -and $c.round -eq $next -and $h.phase -eq 'Countdown' -and $c.phase -eq 'Countdown'}
                Visit "set $set round $next"
                Check (-not $h.draftStatus -and -not $c.draftStatus) "set $set round $next skips draft"
                foreach($p in $h.players){Check ([Math]::Abs($p.hp-$p.max) -lt 0.01) 'round health restored';foreach($id in $owned["$($p.id)"]){Check ($p.augments -contains $id) 'round retains augments'}}
                Await 'round playing' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing'}
            }elseif($set -lt 7){
                $nextSet=$set+1
                Await 'next set draft' {$h.set -eq $nextSet -and $c.set -eq $nextSet -and $h.phase -eq 'Draft' -and $c.phase -eq 'Draft'}
                Check ($h.map -ne $previous) 'set end also changes map'
                $localWinner=if($winner -eq 'remote'){$h.players|Where-Object owner}else{$c.players|Where-Object owner}
                $localLoser=if($winner -eq 'remote'){$c.players|Where-Object owner}else{$h.players|Where-Object owner}
                Check ($localWinner.ready -and -not $localLoser.ready) 'only set loser selects cards'
            }else{
                Await 'match complete' {$h.phase -eq 'Finished' -and $c.phase -eq 'Finished'}
                Check ($h.firstSets -eq 4 -and $h.secondSets -eq 3) 'complete 21-round match at four to three sets'
                Check ($h.map -eq $previous -and $c.map -eq $previous) 'final match result keeps last map'
            }
        }
        Write-Output "PASS set $set"
    }
    Check ($visited.Count -eq 21) 'all twenty-one rounds have a map'
    Check (@($visited|Select-Object -First 14|Sort-Object -Unique).Count -eq 14) 'first fourteen rounds use every map exactly once'
    @{checks=$checks.Count;maps=$visited;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Result.json"
    Write-Output "PASS $($checks.Count) checks"
}finally{
    foreach($peer in @('client','host')){try{if((Read $peer).listening){Send $peer exit}}catch{Write-Warning $_.Exception.Message}}
    $until=[DateTime]::UtcNow.AddSeconds(15)
    do{if(-not(Menu host).joined -and -not(Menu client).joined){break};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow -lt $until)
    try{Send client quit}catch{}
    if($clientProcess -and -not $clientProcess.WaitForExit(5000)){Stop-Process -Id $clientProcess.Id}
    unity command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
}
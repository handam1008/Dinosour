param([string]$Run=('Run'+(Get-Date -Format 'yyyyMMddHHmmss')),[string]$Project=(Get-Location).Path,[string]$Build='Builds/SceneMatch/Game.exe')
$ErrorActionPreference='Stop'
$Project=[IO.Path]::GetFullPath($Project).Replace('\','/')
$root="$Project/Logs/SceneMatch25/$Run"
if(Test-Path $root){throw 'Use a new run name.'}
[IO.Directory]::CreateDirectory($root)|Out-Null
$seq=@{host=0;client=0}
$menuSeq=@{host=0;client=0}
$checks=[Collections.Generic.List[string]]::new()
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
function ReadyMenus {
    Await 'both MainMenu ready' {(Menu host).scene -eq 'MainMenu' -and (Menu client).scene -eq 'MainMenu' -and @((Menu host).buttons|Where-Object {$_.name -eq 'Button_Play' -and $_.enabled}).Count -eq 1 -and @((Menu client).buttons|Where-Object {$_.name -eq 'Button_Play' -and $_.enabled}).Count -eq 1}
}
function BeginMatch([switch]$Reverse,[switch]$Restart) {
    ReadyMenus
    Click host 'Button_Play'
    Click client 'Button_Play'
    $first=if($Reverse){'client'}else{'host'}
    $second=if($Reverse){'host'}else{'client'}
    Click $first 'Button_매치메이킹'
    Await 'first quick session' {(Menu $first).joined} 90
    if($Restart){
        $code=(Menu host).code
        Eval 'SSW.NetGame.Current.Manager.Shutdown();return true;'|Out-Null
        Await 'quick matching restarts after network stops' {(Menu host).joined -and (Menu host).code -ne $code -and $h.listening} 90
        Check ($true) 'waiting matchmaking recovers after local network stops'
    }
    Click $second 'Button_매치메이킹'
    Await 'Relay scene loaded' {$h.phase -eq 'Draft' -and $c.phase -eq 'Draft' -and $h.map -eq $c.map} 90
    PickAll
}
function CheckFrame($label) {
    $bounds=Eval 'var b=SSW.NetGame.Current.Arena.Map.Bounds;return new{x=b.center.x,y=b.center.y,halfX=b.extents.x,halfY=b.extents.y};'
    foreach($s in @($h,$c)){
        Check ([Math]::Abs($s.cameraPosition.x-$bounds.x) -lt 0.002 -and [Math]::Abs($s.cameraPosition.y-$bounds.y) -lt 0.002) "$label camera centered"
        Check ($s.cameraSize+0.002 -ge $bounds.halfY -and $s.cameraSize*$s.cameraAspect+0.002 -ge $bounds.halfX) "$label whole map visible"
    }
}
try {
    Eval 'if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path=="Assets/RYU/00.Scene/StartMenu.unity")return true;for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new System.InvalidOperationException("Save scene changes before testing.");UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/RYU/00.Scene/StartMenu.unity");return true;'|Out-Null
    unity command editor_play --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    Start-Sleep -Seconds 4
    $until=[DateTime]::UtcNow.AddSeconds(40)
    do{
        try{$scene=Eval 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;'}catch{$scene=''}
        if($scene -eq 'MainMenu'){break}
        if([DateTime]::UtcNow -gt $until){throw 'StartMenu did not reach MainMenu'}
        Start-Sleep -Milliseconds 300
    }while($true)
    Eval ('SSW.PlayerJobStorage.Save(SSW.PlayerJob.Witch);var g=SSW.NetGame.GetOrCreate();g.SetLocalJob(SSW.PlayerJob.Witch);g.SetProfile(SSW.Fighter.Create("Host"));g.gameObject.AddComponent<SSW.NetProbe>().Init("'+$root+'/host");g.gameObject.AddComponent<SSW.MenuProbe>().Init("'+$root+'/host");return true;')|Out-Null
    $clientProcess=Start-Process -FilePath (Join-Path $Project $Build) -WorkingDirectory $Project -ArgumentList @('--net-profile','audit-scene25','--net-name','Client','--net-probe',"$root/client",'-logFile',"$root/client.log",'-screen-fullscreen','0','-screen-width','1280','-screen-height','720') -WindowStyle Hidden -PassThru
    ReadyMenus
    $menuSeq.client++
    [IO.File]::WriteAllText("$root/client.menu.cmd.json",(@{seq=$menuSeq.client;op='job';value=2}|ConvertTo-Json -Compress))
    Await 'client job selected' {(Menu client).seq -ge $menuSeq.client}
    Eval 'SSW.MapTravel.LoadScene("SuperUltraLegendScene");return true;'|Out-Null
    Await 'sandbox spawned' {$h.scene -eq 'SuperUltraLegendScene' -and $h.players.Count -eq 2 -and $h.cameraSize -gt 0}
    $fx=Eval 'var p=SSW.NetGame.Current.Practice;int a=p.Player.GetComponentsInChildren<UnityEngine.ParticleSystem>(true).Length,b=p.Target.GetComponentsInChildren<UnityEngine.ParticleSystem>(true).Length;p.Target.Health.TakeDamage(1);return new{local=p.Player.GetComponentsInChildren<UnityEngine.ParticleSystem>(true).Length-a,target=p.Target.GetComponentsInChildren<UnityEngine.ParticleSystem>(true).Length-b};'
    Check ($fx.local -eq 0 -and $fx.target -eq 1) 'hit particles only appear on damaged player'
    for($i=0;$i -lt 2;$i++){
        $id=($h.players|Where-Object {-not $_.owner}).objectId
        Send host remote 100000
        Await 'sandbox target respawns' {($h.players|Where-Object {-not $_.owner}).objectId -ne $id}
        Send host remote 1
    }
    Check ($true) 'damage still works after repeated respawns'
    Eval 'UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");return true;'|Out-Null
    ReadyMenus
    Await 'unexpected scene exit stops old network' {-not $h.listening}
    Check ($true) 'direct scene load shuts down sandbox network'
    BeginMatch -Restart
    Check ($true) 'StartMenu and sandbox return can match through Relay'
    Eval 'foreach(var p in SSW.NetGame.Current.Players){typeof(SSW.Health).GetMethod("SetMax",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.Health,new object[]{10000f});p.Health.Heal(10000);}return true;'|Out-Null
    foreach($title in @('KDH_Map 2','KDH_Map 15','KDH_Map 9')){
        Map $title
        Await 'new camera frame' {[Math]::Abs($h.cameraSize-$c.cameraSize) -lt 50 -and $h.map -eq $title -and $c.map -eq $title}
        CheckFrame $title
        $beforeH=$h;$beforeC=$c
        Send host move 0 1
        Send client move 0 -1
        Send host jump
        Send client jump
        Send host remote 1
        Send host damage 1
        Eval 'SSW.CameraShakeFeedback.Play(5f,2f);return true;'|Out-Null
        Start-Sleep -Milliseconds 400
        Send host move
        Send client move
        $h=Read host;$c=Read client
        foreach($pair in @(@($beforeH,$h),@($beforeC,$c))){
            $a=$pair[0];$b=$pair[1]
            Check ([Math]::Abs($a.cameraPosition.x-$b.cameraPosition.x) -lt 0.0001 -and [Math]::Abs($a.cameraPosition.y-$b.cameraPosition.y) -lt 0.0001 -and [Math]::Abs($a.cameraSize-$b.cameraSize) -lt 0.0001) "$title camera stays still during movement, jumping and damage"
        }
    }
    for($i=0;$i -lt 2;$i++){
        $before=$h.mapObject
        Send host remote 100000
        Await 'next round map and players' {$h.mapObject -ne $before -and $h.mapObject -eq $c.mapObject -and $h.players.Count -eq 2 -and $c.players.Count -eq 2}
        PickAll
        Send host remote 1
        Send host damage 1
        CheckFrame "after respawn $i"
    }
    Check ($true) 'round changes preserve working damage feedback'
    Send client exit
    Send host exit
    ReadyMenus
    Await 'old sessions left' {-not (Menu host).joined -and -not (Menu client).joined -and -not $h.listening -and -not $c.listening}
    BeginMatch
    Check ($true) 'second Relay match succeeds after both return to menu'
    Send host remote 1
    CheckFrame 'rematch'
    foreach($after in @($false,$true)){
        Send client exit
        Send host exit
        ReadyMenus
        BeginMatch -Reverse
        Check ($c.server -and -not $h.server) 'Editor joins as remote client for failure test'
        $beforeFault=Eval ('var s=SSW.MultiplayerSessionManager.Current;var field=typeof(SSW.MultiplayerSessionManager).GetField("_session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var session=(Unity.Services.Multiplayer.ISession)field.GetValue(s);SSW.SessionFault.Arm(session,'+$after.ToString().ToLowerInvariant()+');return new{type=session.Type,count=SSW.SessionFault.Count};')
        Send host exit
        Await 'failed leave still returns to menu' {$h.scene -eq 'MainMenu' -and -not $h.listening}
        Send client exit
        ReadyMenus
        BeginMatch
        $recovered=Eval 'var s=SSW.MultiplayerSessionManager.Current;var field=typeof(SSW.MultiplayerSessionManager).GetField("_session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var session=(Unity.Services.Multiplayer.ISession)field.GetValue(s);return new{type=session.Type,count=SSW.SessionFault.Count};'
        Check ($recovered.count -eq $beforeFault.count+1) "leave fault injected once after response=$after"
        Check ($h.connected -and $c.connected) "matching recovers from leave failure after response=$after"
        if($after){Check ($recovered.type -ne $beforeFault.type) 'confirmed departed client bypasses stale SDK registration'}
    }
    Check (-not $h.error -and -not $c.error) 'no runtime exceptions on either peer'
    @{checks=$checks.Count;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Result.json"
    Write-Output "PASS $($checks.Count) checks"
}finally{
    foreach($peer in @('client','host')){try{if((Read $peer).listening){Send $peer exit}}catch{Write-Warning $_.Exception.Message}}
    $until=[DateTime]::UtcNow.AddSeconds(15)
    do{if(-not(Menu host).joined -and -not(Menu client).joined){break};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow -lt $until)
    try{if($clientProcess){Send client quit}}catch{}
    if($clientProcess -and -not $clientProcess.WaitForExit(5000)){Stop-Process -Id $clientProcess.Id}
    unity command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
}
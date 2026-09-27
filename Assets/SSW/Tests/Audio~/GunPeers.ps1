param([string]$Run=('Run'+(Get-Date -Format 'yyyyMMddHHmmss')),[string]$Project=(Get-Location).Path,[string]$Build='Builds/GunSound27/Game.exe',[switch]$Quick)
$ErrorActionPreference='Stop'
$Project=[IO.Path]::GetFullPath($Project).Replace('\','/')
$root="$Project/Logs/GunSound27/$Run"
if(Test-Path -LiteralPath $root){throw 'Use a new run name'}
[IO.Directory]::CreateDirectory($root)|Out-Null
$cli=Join-Path $env:LOCALAPPDATA 'Unity/bin/unity.exe'
$checks=[Collections.Generic.List[string]]::new()
$seq=@{host=0;client=0}
$clientProcess=$null
$previous=$null
$evalIndex=0
$names=@('KDH_CompleteQuestRevolutionSound','KDH_LightningHitSound','KDH_ReloadSound','KDH_ShringkingSound','KDH_LightningStackSound','KDH_DotDamageSound','KDH_appliedAirBulletSound','KDH_GravityBulletSound','KDH_IceBulletSound','KDH_PoisonBulletSound','KDH_ShurikenBulletSound','GunShot')
function Eval([string]$code){
    $script:evalIndex++
    $path="$root/Eval$evalIndex.cs"
    [IO.File]::WriteAllText($path,$code)
    $reply=& $cli command eval_file --file $path --caller plugin --skill unity-cli --project-path $Project --format json|ConvertFrom-Json
    $reply|ConvertTo-Json -Depth 14|Set-Content -LiteralPath "$root/Eval$evalIndex.json"
    if(-not $reply.success -or -not $reply.data.result.success){throw ($reply|ConvertTo-Json -Depth 9)}
    $value=$reply.data.result.result
    if($value -is [string] -and ($value.StartsWith('{') -or $value.StartsWith('['))){return $value|ConvertFrom-Json}
    return $value
}
function Read($peer){
    for($n=0;$n -lt 5;$n++){try{return Get-Content -LiteralPath "$root/$peer.json" -Raw|ConvertFrom-Json}catch{Start-Sleep -Milliseconds 15}}
    return $null
}
function Await([string]$label,[scriptblock]$condition,[int]$timeout=10){
    $until=[DateTime]::UtcNow.AddSeconds($timeout)
    do{
        $script:h=Read host;$script:c=Read client
        if($h.error -or $c.error){throw "Probe error: $($h.error) $($c.error)"}
        if($h -and $c -and (& $condition)){return}
        Start-Sleep -Milliseconds 30
    }while([DateTime]::UtcNow -lt $until)
    @{label=$label;host=$h;client=$c}|ConvertTo-Json -Depth 15|Set-Content -LiteralPath "$root/Timeout.json"
    throw "Timeout: $label"
}
function Send($peer,$op,$value=0,$x=0,$y=0){
    $seq[$peer]++
    [IO.File]::WriteAllText("$root/$peer.cmd.json",(@{seq=$seq[$peer];op=$op;value=$value;x=$x;y=$y}|ConvertTo-Json -Compress))
    if($op -eq 'quit'){return}
    $until=[DateTime]::UtcNow.AddSeconds(5)
    while((Read $peer).seq -lt $seq[$peer]){if([DateTime]::UtcNow -gt $until){throw "Command timeout: $peer $op"};Start-Sleep -Milliseconds 20}
}
function Check($condition,[string]$label){
    if(-not $condition){throw $label}
    $checks.Add($label)
    [IO.File]::WriteAllLines("$root/Checks.txt",$checks)
    Write-Output "PASS $label"
}
function Count($snapshot,[string]$name){return [int]($snapshot.audio.cues|Where-Object name -eq $name|Select-Object -ExpandProperty count)}
function ResetAudio{
    Start-Sleep -Milliseconds 300
    foreach($peer in @('host','client')){Send $peer sound 5;Send $peer sound 0}
}
function Audio([hashtable]$expected,[string]$label,[int]$timeout=7){
    if($expected.Count -gt 0){
        Await $label {foreach($name in $expected.Keys){if((Count $h $name) -lt $expected[$name] -or (Count $c $name) -lt $expected[$name]){return $false}};return $true} $timeout
    }
    Start-Sleep -Milliseconds 500
    $left=Read host;$right=Read client
    foreach($name in $names){
        $want=if($expected.ContainsKey($name)){$expected[$name]}else{0}
        if((Count $left $name) -ne $want -or (Count $right $name) -ne $want){throw "$label $name expected $want host=$(Count $left $name) client=$(Count $right $name)"}
    }
    Check $true "$label exact counts on both peers"
    if($expected.Count -gt 0){Check ($left.audio.effectsPeak -gt 0.001 -and $right.audio.effectsPeak -gt 0.001) "$label has audio output on both peers"}
    @{expected=$expected;host=$left.audio;client=$right.audio}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath "$root/$label.json"
}
$hit=@'
var game=SSW.NetGame.Current;
var player=System.Linq.Enumerable.First(game.Players,p=>p.OwnerClientId==@ID@UL);
var target=System.Linq.Enumerable.First(game.Players,p=>p!=player);
var gun=player.GetComponent<SSW.GunCast>();
const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var augments=(System.Collections.Generic.HashSet<KDH.Scripts.Arguments.GunnerAugmentType>)typeof(SSW.GunCast).GetField("_augments",flags).GetValue(gun);
augments.Clear();
if("@TYPE@"!="None")augments.Add(System.Enum.Parse<KDH.Scripts.Arguments.GunnerAugmentType>("@TYPE@"));
typeof(SSW.GunCast).GetField("_shrinkAt",flags).SetValue(gun,0f);
typeof(SSW.GunCast).GetField("_lightningAt",flags).SetValue(gun,0f);
typeof(SSW.GunCast).GetField("_marks",flags).SetValue(gun,0);
gun.Progress=0;
foreach(var p in game.Players){
    p.Health.Heal(p.Health.Max);p.Drive.Teleport(new UnityEngine.Vector2(p.Side*-3f,3f));
    var cast=p.GetComponent<SSW.GunCast>();var state=new SSW.WeaponState{Ammo=cast.Capacity,Reload=p.InputSequence+100000,Filled=p.InputSequence};
    for(int i=0;i<cast.Capacity;i++)state.Loaded.Add(p.InputSequence);
    typeof(SSW.JobCast).GetProperty("State",flags).SetValue(cast,state);
}
var motion=(SSW.MotionState)typeof(SSW.MotionView).GetField("_state",flags).GetValue(target.Drive);
motion.Grounded=@GROUNDED@;
typeof(SSW.MotionView).GetField("_state",flags).SetValue(target.Drive,motion);
var obj=new UnityEngine.GameObject("Sound hit specification");
obj.AddComponent<Unity.Netcode.NetworkObject>();
var bolt=obj.AddComponent<SSW.NetBolt>();
try{
    var spec=typeof(SSW.NetBolt).GetField("_spec",flags).GetValue(bolt);
    for(var type=spec.GetType();type!=null;type=type.BaseType){var field=type.GetField("m_InternalValue",flags);if(field!=null){field.SetValue(spec,new SSW.BoltSpec{Damage=@DAMAGE@f,Charged=@CHARGED@,Scale=1f,Aspect=1f});break;}}
    for(int i=0;i<@HITS@;i++)gun.Hit(target,bolt);
}finally{UnityEngine.Object.Destroy(obj);}
return new{progress=gun.Progress,health=target.Health.Current};
'@
try{
    $previous=Eval 'var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(s.isDirty||UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Saved stopped scene required");var r=new{scene=s.path,job=SSW.PlayerJobStorage.Load().ToString()};UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/SSW/MainMenu.unity");return Newtonsoft.Json.JsonConvert.SerializeObject(r);'
    $previous|ConvertTo-Json|Set-Content -LiteralPath "$root/Previous.json"
    & $cli command editor_play --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    $until=[DateTime]::UtcNow.AddSeconds(45)
    do{& $cli command editor_status --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null;try{$scene=Eval 'return UnityEditor.EditorApplication.isPlaying&&!UnityEditor.EditorApplication.isCompiling?UnityEngine.SceneManagement.SceneManager.GetActiveScene().name:"";'}catch{$scene=''};if($scene -eq 'MainMenu'){break};Start-Sleep -Milliseconds 300}while([DateTime]::UtcNow -lt $until)
    if($scene -ne 'MainMenu'){throw 'Menu did not become ready'}
    Eval ('var g=SSW.NetGame.GetOrCreate();g.gameObject.AddComponent<SSW.NetProbe>().Init("'+$root+'/host");g.StartLocal(true,"127.0.0.1",SSW.PlayerJob.Gunner,30718);return true;')|Out-Null
    $clientProcess=Start-Process -FilePath (Join-Path $Project $Build) -WorkingDirectory $Project -ArgumentList @('--net-mode','client','--net-address','127.0.0.1','--net-port','30718','--net-job','Gunner','--net-probe',"$root/client",'-logFile',"$root/client.log",'-screen-fullscreen','0','-screen-width','800','-screen-height','450') -WindowStyle Hidden -PassThru
    Await 'both drafts' {$h.phase -eq 'Draft' -and $c.phase -eq 'Draft'} 60
    foreach($peer in @('host','client')){Send $peer lag 60 10 0;Send $peer isolate;Send $peer wideground}
    Eval 'var g=SSW.NetGame.Current;typeof(SSW.BattleMap).GetField("_fallY",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(g.Arena.Map,-1000f);foreach(var p in g.Players){p.Drive.Teleport(new UnityEngine.Vector2(p.Side*-3f,3f));p.Draft.Restore(System.Array.Empty<int>());}g.Match.Picked();return true;'|Out-Null
    Await 'both playing' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing'} 30
    Eval 'SSW.NetGame.Current.Local.Block(true);return !SSW.NetGame.Current.Local.Input.inputIsActive;'|Out-Null
    Send client escape
    Await 'client physical gameplay input blocked' {$c.menuOpen} 5
    foreach($peer in @('host','client')){Send $peer sound 1 1 0;Send $peer sound 2 1;Send $peer sound 4}
    Eval @'
var game=SSW.NetGame.Current;
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
foreach(var player in game.Players){
    player.GetComponent<SSW.BuffHealth>().SetBase(10000);player.Health.Heal(10000);
    player.Drive.Teleport(new UnityEngine.Vector2(player.Side*-3f,3f));
    var gun=player.GetComponent<SSW.GunCast>();
    var state=new SSW.WeaponState{Ammo=gun.Capacity,Reload=player.InputSequence+100000,Filled=player.InputSequence};for(int i=0;i<gun.Capacity;i++)state.Loaded.Add(player.InputSequence);
    typeof(SSW.JobCast).GetProperty("State",flags).SetValue(gun,state);
}
UnityEngine.Physics2D.IgnoreCollision(game.Players[0].Collider,game.Players[1].Collider);
return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(game.Players,p=>new{p.OwnerClientId,p.InputSequence,health=p.Health.Current,state=p.GetComponent<SSW.GunCast>().Status}));
'@|Out-Null
    Await 'test magazines replicated' {$h.players[0].ammo -eq 9 -and $h.players[1].ammo -eq 9 -and $c.players[0].ammo -eq 9 -and $c.players[1].ammo -eq 9} 5
    foreach($peer in @('host','client')){
        Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Drive.Teleport(new UnityEngine.Vector2(p.Side*-3f,3f));return true;'|Out-Null
        Await 'grounded shot positions' {$h.players[0].grounded -and $h.players[1].grounded -and $c.players[0].grounded -and $c.players[1].grounded} 6
        $actor=(Read $peer).players|Where-Object owner
        $prepare=@'
var game=SSW.NetGame.Current;var p=System.Linq.Enumerable.First(game.Players,p=>p.OwnerClientId==@ID@UL);var t=System.Linq.Enumerable.First(game.Players,candidate=>candidate!=p);var gun=p.GetComponent<SSW.GunCast>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var augments=(System.Collections.Generic.HashSet<KDH.Scripts.Arguments.GunnerAugmentType>)typeof(SSW.GunCast).GetField("_augments",flags).GetValue(gun);augments.Clear();augments.Add(KDH.Scripts.Arguments.GunnerAugmentType.IceBullet);
var state=new SSW.WeaponState{Ammo=1,Reload=p.InputSequence+100000,Filled=p.InputSequence};state.Loaded.Add(p.InputSequence);
typeof(SSW.JobCast).GetProperty("State",flags).SetValue(gun,state);
var direction=(t.Body.position-p.Body.position).normalized;var aim=((UnityEngine.Vector2)t.Collider.bounds.center-gun.View.Muzzle(p.Body.position,direction,p.Drive.Scale)).normalized;
return new{actor=p.OwnerClientId,target=t.OwnerClientId,hp=t.Health.Current,fired=p.Cast.Shots,aim=new{x=aim.x,y=aim.y}};
'@
        $prepared=Eval $prepare.Replace('@ID@',[string]$actor.id)
        $prepared|ConvertTo-Json -Depth 6|Set-Content -LiteralPath "$root/$peer-ShotSetup.json"
        ResetAudio
        Send $peer press 0 $prepared.aim.x $prepared.aim.y
        Send $peer release 0 $prepared.aim.x $prepared.aim.y
        Await "$peer real projectile hit" {($h.players|Where-Object id -eq $prepared.actor).fired -eq $prepared.fired+1 -and ($h.players|Where-Object id -eq $prepared.target).hp -lt $prepared.hp} 5
        Audio @{'GunShot'=1;'KDH_IceBulletSound'=1} "$peer-projectile"
        $cases=@(
            @{name='no-augment';type='None';sounds=@{}},
            @{name='rejected-hit';type='IceBullet';damage=0;sounds=@{}},
            @{name='air-grounded';type='AirBullet';grounded=$true;sounds=@{}},
            @{name='air';type='AirBullet';sounds=@{'KDH_appliedAirBulletSound'=1}},
            @{name='gravity';type='GravityBullet';sounds=@{'KDH_GravityBulletSound'=1}},
            @{name='ice';type='IceBullet';sounds=@{'KDH_IceBulletSound'=1}},
            @{name='fire';type='FireBullet';sounds=@{'KDH_DotDamageSound'=6}},
            @{name='poison';type='PoisonBullet';sounds=@{'KDH_PoisonBulletSound'=1;'KDH_DotDamageSound'=8}},
            @{name='poison-refresh';type='PoisonBullet';hits=2;sounds=@{'KDH_PoisonBulletSound'=2;'KDH_DotDamageSound'=8}},
            @{name='shuriken';type='ShurikenBullet';sounds=@{'KDH_ShurikenBulletSound'=1}},
            @{name='shrink-cooldown';type='ShrinkingDeviceAbility';hits=2;sounds=@{'KDH_ShringkingSound'=1}},
            @{name='evolution-uncharged';type='Quest_EvolutionAbility';hits=3;sounds=@{}},
            @{name='evolution-complete';type='Quest_EvolutionAbility';hits=3;charged=$true;sounds=@{'KDH_CompleteQuestRevolutionSound'=1}},
            @{name='lightning-cooldown';type='LightningBullet';hits=4;sounds=@{'KDH_LightningStackSound'=3;'KDH_LightningHitSound'=1}}
        )
        if($Quick){$cases=@($cases|Where-Object name -in @('poison','lightning-cooldown'))}
        foreach($case in $cases){
            ResetAudio
            $hits=if($case.ContainsKey('hits')){$case.hits}else{1}
            $damage=if($case.ContainsKey('damage')){$case.damage}else{1}
            $code=$hit.Replace('@ID@',[string]$actor.id).Replace('@TYPE@',$case.type).Replace('@HITS@',[string]$hits).Replace('@DAMAGE@',[string]$damage).Replace('@GROUNDED@',([bool]$case.grounded).ToString().ToLowerInvariant()).Replace('@CHARGED@',([bool]$case.charged).ToString().ToLowerInvariant())
            $result=Eval $code
            Audio $case.sounds "$peer-$($case.name)"
            if($case.name -eq 'evolution-complete'){Check ($result.progress -eq 2) "$peer evolution only completes at second charged hit"}
        }
    }
    Eval 'var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;foreach(var p in SSW.NetGame.Current.Players)((System.Collections.Generic.HashSet<KDH.Scripts.Arguments.GunnerAugmentType>)typeof(SSW.GunCast).GetField("_augments",f).GetValue(p.GetComponent<SSW.GunCast>())).Clear();return true;'|Out-Null
    ResetAudio
    $reload=Get-Content -LiteralPath "$Project/Assets/SSW/Tests/Audio~/GunReload.cs" -Raw
    Eval $reload.Replace('@REPORT@',"$root/ReloadState.json")|Out-Null
    Await 'reload checks complete' {Test-Path -LiteralPath "$root/ReloadState.json"} 15
    $reloadResult=Get-Content -LiteralPath "$root/ReloadState.json" -Raw|ConvertFrom-Json
    Check $reloadResult.success "reload state checks: $($reloadResult.error)"
    Audio @{'KDH_ReloadSound'=2;'GunShot'=1} 'reload'
    Check ((Read host).audio.listeners -eq 1 -and (Read client).audio.listeners -eq 1) 'one active listener on each peer'
    @{success=$true;count=$checks.Count;checks=$checks;reload=$reloadResult;host=(Read host).audio;client=(Read client).audio;scope='same PC Editor host and Windows development client; 60ms delay plus 10ms jitter each direction; real ice projectile from each owner; other authoritative GunCast.Hit effects use injected specifications'}|ConvertTo-Json -Depth 15|Set-Content -LiteralPath "$root/Result.json"
    Write-Output "PASS $($checks.Count) peer checks: $root"
}catch{
    @{success=$false;error=$_.ToString();count=$checks.Count;host=(Read host);client=(Read client)}|ConvertTo-Json -Depth 15|Set-Content -LiteralPath "$root/Failure.json"
    throw
}finally{
    if($clientProcess -and -not $clientProcess.HasExited){try{Send client sound 7;Send client quit}catch{};if(-not $clientProcess.WaitForExit(3000)){Stop-Process -Id $clientProcess.Id}}
    & $cli command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    if($previous){
        Start-Sleep -Milliseconds 800
        & $cli command editor_status --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
        Eval ('if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Still playing");SSW.PlayerJobStorage.Save(SSW.PlayerJob.'+$previous.job+');var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(s.isDirty)throw new System.InvalidOperationException("Unsaved scene");UnityEditor.SceneManagement.EditorSceneManager.OpenScene("'+$previous.scene+'");return true;')|Out-Null
    }
}

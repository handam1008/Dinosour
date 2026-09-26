param([string]$Run=('Run'+(Get-Date -Format 'yyyyMMddHHmmss')),[string]$Project=(Get-Location).Path,[string]$Build='Builds/Boundary/Game.exe',[switch]$SkipEdges,[switch]$LavaOnly,[switch]$HealOnly,[switch]$EdgeMotion,[switch]$WitchOnly,[switch]$StatsOnly,[switch]$StatsProfiles,[switch]$MaterialsOnly,[switch]$MapRefreshOnly,[switch]$MapResume,[switch]$MapCycle,[switch]$MapAreas,[ValidateRange(0,5)][int]$MapFrom=0,[switch]$EffectsOnly,[string[]]$EdgeModes=@('move','dash'),[int[]]$EdgeMaps=@(),[switch]$SwingOnly,[switch]$SwingCutOnly,[int[]]$SwingMaps=@(11,12,14,15,16),[string]$SwingJob='Gunner',[switch]$CombatOnly,[string[]]$CombatJobs=@('Gunner','Gambler','Magician','Witch','Swordsman','Assassin','Knife'))
$ErrorActionPreference='Stop'
$Project=[IO.Path]::GetFullPath($Project).Replace('\','/')
$root="$Project/Logs/Boundary/$Run"
if(Test-Path $root){throw 'Use a new run name.'}
[IO.Directory]::CreateDirectory($root)|Out-Null
$seq=@{host=0;client=0}
$checks=[Collections.Generic.List[string]]::new()
$clientProcess=$null
function Eval([string]$code){
    [IO.File]::WriteAllText("$root/Eval.cs",$code)
    $reply=unity command eval_file --file "$root/Eval.cs" --caller plugin --skill unity-cli --project-path $Project --format json|ConvertFrom-Json
    if(-not $reply.success -or -not $reply.data.result.success){throw ($reply|ConvertTo-Json -Depth 8)}
    return $reply.data.result.result
}
function Read($peer){for($attempt=0;$attempt -lt 5;$attempt++){try{return Get-Content -LiteralPath "$root/$peer.json" -Raw|ConvertFrom-Json}catch{Start-Sleep -Milliseconds 10}};return $null}
function Await($label,[scriptblock]$condition,$timeout=35){
    $until=[DateTime]::UtcNow.AddSeconds($timeout)
    do{
        $script:h=Read host;$script:c=Read client
        if($h.error -or $c.error){@{label=$label;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/RuntimeFailure.json";throw "Runtime error: $($h.error) $($c.error)"}
        if($h -and $c -and (& $condition)){return}
        Start-Sleep -Milliseconds 50
    }while([DateTime]::UtcNow -lt $until)
    @{label=$label;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Failure.json"
    throw "Timeout: $label"
}
function Send($peer,$op,$value=0,$x=0,$y=0){
    $seq[$peer]++
    [IO.File]::WriteAllText("$root/$peer.cmd.json",(@{seq=$seq[$peer];op=$op;value=$value;x=$x;y=$y}|ConvertTo-Json -Compress))
    if($op -eq 'quit'){return}
    $until=[DateTime]::UtcNow.AddSeconds(5)
    while((Read $peer).seq -lt $seq[$peer]){if([DateTime]::UtcNow -gt $until){throw "Command timeout: $peer $op"};Start-Sleep -Milliseconds 30}
}
function Check($condition,$label){
    if(-not $condition){@{label=$label;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Failure.json";throw $label}
    $checks.Add("PASS $label")
    [IO.File]::WriteAllLines("$root/Checks.txt",$checks)
}
function Map($index){
    $data=Eval ('var g=SSW.NetGame.Current;var r=g.GetComponent<SSW.MapRotation>();typeof(SSW.MapRotation).GetField("_index",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(r,'+$index+');r.Restart(false);foreach(var p in g.Players)p.Drive.Teleport(g.Arena.Spawn(p.Side==1?0:1));return new{id=g.Arena.Map.NetworkObjectId,title=g.Arena.Map.Title};')
    Await 'map loaded by both peers' {$h.mapObject -eq $data.id -and $c.mapObject -eq $data.id} 10
    return $data
}
try{
    $previousScene=Eval 'if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("An existing play session is active");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new System.InvalidOperationException("Unsaved scene changes");string path=scene.path;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/RYU/00.Scene/StartMenu.unity");return path;'
    unity command editor_play --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    $until=[DateTime]::UtcNow.AddSeconds(50)
    do{try{$scene=Eval 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;'}catch{$scene=''};if($scene -eq 'MainMenu'){break};Start-Sleep -Milliseconds 500}while([DateTime]::UtcNow -lt $until)
    $job=if($WitchOnly){'Witch'}elseif($SwingOnly){$SwingJob}else{'Swordsman'}
    Eval ('var g=SSW.NetGame.GetOrCreate();g.gameObject.AddComponent<SSW.NetProbe>().Init("'+$root+'/host");g.StartLocal(true,"127.0.0.1",SSW.PlayerJob.'+$job+',30716);return true;')|Out-Null
    $clientProcess=Start-Process -FilePath (Join-Path $Project $Build) -WorkingDirectory $Project -ArgumentList @('--net-mode','client','--net-address','127.0.0.1','--net-port','30716','--net-job',$job,'--net-probe',"$root/client",'-logFile',"$root/client.log",'-screen-fullscreen','0','-screen-width','800','-screen-height','450') -WindowStyle Hidden -PassThru
    Await 'both drafts' {$h.phase -eq 'Draft' -and $c.phase -eq 'Draft'} 90
    Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Draft.Restore(System.Array.Empty<int>());SSW.NetGame.Current.Match.Picked();return true;'|Out-Null
    Await 'playing' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing'}
    $step=Eval 'return UnityEngine.Time.fixedDeltaTime;'
    if($StatsOnly -or $StatsProfiles -or $MaterialsOnly){. "$PSScriptRoot/../Stats~/Runtime.ps1";return}
    Eval 'foreach(var p in SSW.NetGame.Current.Players){typeof(SSW.Health).GetMethod("SetMax",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.Health,new object[]{10000f});p.Health.Heal(10000f);}return true;'|Out-Null
    if($HealOnly){. "$PSScriptRoot/../Common~/Healing.ps1";return}
    if($WitchOnly){. "$PSScriptRoot/../Augments~/Witch.ps1";return}
    if($MapRefreshOnly){. "$PSScriptRoot/RefreshRun.ps1" -SkipLoads:$MapResume -OnlyCycle:$MapCycle -AreasOnly:$MapAreas -From $MapFrom;return}
    if($EffectsOnly){. "$PSScriptRoot/../Augments~/EffectsRun.ps1";return}
    if($EdgeMotion){. "$PSScriptRoot/EdgesRun.ps1";return}
    if($SwingOnly){. "$PSScriptRoot/SwingRun.ps1";return}
    if($CombatOnly){. "$PSScriptRoot/CombatRun.ps1";return}
    if(-not $SkipEdges -and -not $LavaOnly){
        for($index=0;$index -lt 14;$index++){
            $map=Map $index
            $result=Eval @'
var g=SSW.NetGame.Current;
var map=g.Arena.Map;
foreach(var effect in map.GetComponentsInChildren<UnityEngine.ParticleSystem>())effect.Stop(true,UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);
var edges=new UnityEditor.SerializedObject(map.GetComponent<SSW.MapEdges>()).FindProperty("_edges");
var checks=new System.Collections.Generic.List<object>();
for(int i=0;i<edges.arraySize;i++)
{
    var entry=edges.GetArrayElementAtIndex(i);
    var border=(UnityEngine.Collider2D)entry.FindPropertyRelative("Shape").objectReferenceValue;
    float damage=entry.FindPropertyRelative("Damage").floatValue;
    UnityEngine.Vector2 normal=entry.FindPropertyRelative("Force").vector2Value.normalized;
    foreach(var p in g.Players)
    {
        var bounds=border.bounds;
        var half=UnityEngine.Vector2.Scale(((UnityEngine.CapsuleCollider2D)p.Collider).size,p.Collider.transform.lossyScale)*0.5f;
        float extent=UnityEngine.Mathf.Abs(normal.x)*(bounds.extents.x+half.x)+UnityEngine.Mathf.Abs(normal.y)*(bounds.extents.y+half.y);
        var point=(UnityEngine.Vector2)bounds.center+normal*(extent+0.016f)-(UnityEngine.Vector2)p.Collider.transform.TransformVector(p.Collider.offset);
        p.Drive.Teleport(point+normal*0.5f);
        float before=p.Health.Current;
        p.Drive.Teleport(point);
        float hit=before-p.Health.Current;
        if(UnityEngine.Mathf.Abs(hit-damage)>0.001f)throw new System.InvalidOperationException(map.Title+" "+border.name+" client="+p.OwnerClientId+" damage="+hit+" expected="+damage+" gap="+p.Collider.Distance(border).distance+" canAct="+p.CanAct+" point="+point+" center="+p.Collider.bounds.center);
        map.Touch(p);
        if(before-p.Health.Current!=hit)throw new System.InvalidOperationException("Duplicate boundary hit");
        checks.Add(new{id=p.OwnerClientId,edge=border.name,damage=hit});
        p.Drive.Teleport(g.Arena.Spawn(p.Side==1?0:1));
    }
}
return new{map=map.Title,checks=checks.ToArray(),hp=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,hp=p.Health.Current}))};
'@
            $result|ConvertTo-Json -Depth 8|Set-Content "$root/Edges$index.json"
            Await 'boundary health replicated' {foreach($target in $result.hp){$p=$c.players|Where-Object id -eq $target.id;if([Math]::Abs($p.hp-$target.hp) -gt 0.01){return $false}};return $true} 5
            Check ($result.checks.Count -eq 8) "$($map.title) four edges hurt both peers exactly once"
        }
    }
    Map 0|Out-Null
    $walk=Eval 'var g=SSW.NetGame.Current;var entry=new UnityEditor.SerializedObject(g.Arena.Map.GetComponent<SSW.MapEdges>()).FindProperty("_edges");UnityEngine.Collider2D wall=null;for(int i=0;i<entry.arraySize;i++){var e=entry.GetArrayElementAtIndex(i);if(e.FindPropertyRelative("Force").vector2Value.x>0)wall=(UnityEngine.Collider2D)e.FindPropertyRelative("Shape").objectReferenceValue;}foreach(var p in g.Players)p.Drive.Teleport(new UnityEngine.Vector2(wall.bounds.max.x+p.Collider.bounds.extents.x+0.5f,p.IsOwner?2f:0f));return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,hp=p.Health.Current}));'
    Send host move 0 -1 0
    Send client move 0 1 0
    Await 'both peers walk into damaging wall' {foreach($before in $walk){$p=$h.players|Where-Object id -eq $before.id;if($p.hp -gt $before.hp-9.9){return $false}};return $true} 5
    Send host move
    Send client move
    Check ($true) 'real movement contacts wall and damages host and client'
    Map 6|Out-Null
    Await 'server fireballs visible on client' {$h.embers.Count -gt 0 -and $c.embers.Count -gt 0 -and @($c.embers|Where-Object {$_.id -in $h.embers.id}).Count -gt 0} 8
    Check ($true) 'client displays fireballs with server particle ids'
    @{host=$h.embers;client=$c.embers}|ConvertTo-Json -Depth 8|Set-Content "$root/LavaSync.json"
    $launch=@'
var g=SSW.NetGame.Current;
var effect=g.Arena.Map.GetComponentInChildren<SSW.Lava>().GetComponent<UnityEngine.ParticleSystem>();
effect.Stop(true,UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);
var emission=effect.emission;emission.enabled=false;
var drift=effect.velocityOverLifetime;drift.enabled=false;
var main=effect.main;main.gravityModifier=0f;
foreach(var p in g.Players){p.Drive.Teleport(new UnityEngine.Vector2(p.Side*7f,6f));p.Drive.Freeze(2f);}
UnityEngine.Physics2D.SyncTransforms();
var scale=main.scalingMode==UnityEngine.ParticleSystemScalingMode.Hierarchy?effect.transform.lossyScale:main.scalingMode==UnityEngine.ParticleSystemScalingMode.Local?effect.transform.localScale:UnityEngine.Vector3.one;
var space=UnityEngine.Matrix4x4.TRS(effect.transform.position,effect.transform.rotation,scale).inverse;
var result=new{mode=main.simulationSpace.ToString(),scaling=main.scalingMode.ToString(),mask=effect.collision.collidesWith.value,hp=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,hp=p.Health.Current,x=p.Body.position.x,y=p.Body.position.y}))};
effect.Play();
foreach(var p in g.Players)
{
    var point=(UnityEngine.Vector3)p.Body.position+p.Collider.transform.TransformVector(p.Collider.offset)+UnityEngine.Vector3.left*2f;
    var velocity=UnityEngine.Vector3.right*12f;
    if(effect.main.simulationSpace==UnityEngine.ParticleSystemSimulationSpace.Local){point=space.MultiplyPoint3x4(point);velocity=space.MultiplyVector(velocity);}
    effect.Emit(new UnityEngine.ParticleSystem.EmitParams{position=point,velocity=velocity,startLifetime=5f,startSize=0.1f,applyShapeToPosition=false},1);
}
return result;
'@
    $lava=Eval $launch
    $lava|ConvertTo-Json -Depth 6|Set-Content "$root/LavaBefore.json"
    Await 'native fireball collisions damage both peers' {foreach($before in $lava.hp){$p=$c.players|Where-Object id -eq $before.id;if([Math]::Abs($p.hp-($before.hp-20)) -gt 0.01){return $false}};return $true} 4
    Check ($true) 'native fireball collision applies 20 once to host and client'
    Await 'collided fireballs removed on both peers' {$h.embers.Count -eq 0 -and $c.embers.Count -eq 0} 3
    Check ($true) 'server collision removes client fireballs'
    @{host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/LavaAfter.json"
    if($LavaOnly){Write-Output "PASS lava runtime checks: $root";return}
    foreach($rider in @('host','client')){
    Map 13|Out-Null
    $code=@'
var g=SSW.NetGame.Current;
var blocks=g.Arena.Map.GetComponentsInChildren<SSW.Swing>();
var selected=System.Linq.Enumerable.OrderBy(blocks,x=>UnityEngine.Mathf.Abs(x.transform.position.x)).First();
var body=selected.GetComponent<UnityEngine.Rigidbody2D>();
var bounds=selected.GetComponent<UnityEngine.Collider2D>().bounds;
var player=System.Linq.Enumerable.First(g.Players,p=>p.IsOwner==@OWNER@);
float foot=player.Body.position.y-player.Collider.bounds.min.y;
player.Drive.Teleport(new UnityEngine.Vector2(bounds.center.x+0.3f,bounds.max.y+foot+1f));
System.Linq.Enumerable.First(g.Players,p=>p!=player).Drive.Teleport(new UnityEngine.Vector2(-12f,0f));
return new{name=selected.name,index=System.Array.IndexOf(blocks,selected),x=body.position.x,y=body.position.y,angle=body.rotation,player=player.OwnerClientId};
'@
    $swing=Eval $code.Replace('@OWNER@',$(if($rider -eq 'host'){'true'}else{'false'}))
    $swing|ConvertTo-Json|Set-Content "$root/Swing-$rider-Before.json"
    Await 'rider lands on suspended block' {($h.players|Where-Object id -eq $swing.player).grounded} 4
    Send $rider move 0 1 0
    Start-Sleep -Milliseconds 250
    Send $rider move
    Start-Sleep -Milliseconds 400
    $after=Eval ('var b=SSW.NetGame.Current.Arena.Map.GetComponentsInChildren<SSW.Swing>()['+$swing.index+'].GetComponent<UnityEngine.Rigidbody2D>();return new{x=b.position.x,y=b.position.y,angle=b.rotation,speed=b.linearVelocity.magnitude};')
    $after|ConvertTo-Json|Set-Content "$root/Swing-$rider-After.json"
    Check ([Math]::Abs($after.x-$swing.x) -gt 0.005 -or [Math]::Abs($after.angle-$swing.angle) -gt 0.25) "$rider standing and walking moves suspended block"
    Await 'moving blocks synchronized' {if($h.mapBodies.Count -ne $c.mapBodies.Count){return $false};for($i=0;$i -lt $h.mapBodies.Count;$i++){if([Math]::Abs($h.mapBodies[$i].x-$c.mapBodies[$i].x) -gt 0.35 -or [Math]::Abs($h.mapBodies[$i].y-$c.mapBodies[$i].y) -gt 0.35){return $false}};return $true} 5
    Check ($true) "$rider suspended block positions agree across peers"
    }
    $cut=Eval 'var g=SSW.NetGame.Current;var pin=g.Arena.Map.GetComponentInChildren<SSW.Pin>();var settings=new UnityEditor.SerializedObject(pin);var block=(SSW.Swing)settings.FindProperty("_target").objectReferenceValue;var before=block.transform.position;pin.TakeDamage(1f);return new{name=block.name,index=System.Array.IndexOf(g.Arena.Map.GetComponentsInChildren<SSW.Swing>(),block),y=before.y};'
    Start-Sleep -Milliseconds 600
    $fell=Eval ('var b=SSW.NetGame.Current.Arena.Map.GetComponentsInChildren<SSW.Swing>()['+$cut.index+'];return new{cut=b.IsCut,y=b.transform.position.y};')
    Check ($fell.cut -and $fell.y -lt $cut.y-0.2) 'cut rope releases suspended block'
    Map 13|Out-Null
    $reset=Eval 'return System.Linq.Enumerable.All(SSW.NetGame.Current.Arena.Map.GetComponentsInChildren<SSW.Swing>(),b=>!b.IsCut);'
    Check $reset 'new round restores every rope and block'
    foreach($peer in @('host','client')){Send $peer wideground 0 0 2}
    Eval 'var g=SSW.NetGame.Current;foreach(var p in g.Players){float foot=p.Body.position.y-p.Collider.bounds.min.y;p.Drive.Teleport(new UnityEngine.Vector2(p.IsOwner?-2f:-0.8f,2.3f+foot+0.03f));}return true;'|Out-Null
    Await 'sword test players on floor' {$h.players[0].grounded -and $h.players[1].grounded} 4
    foreach($peer in @('host','client')){
        $before=Read host
        $actor=(Read $peer).players|Where-Object owner
        $target=$before.players|Where-Object id -ne $actor.id
        $direction=if($peer -eq 'host'){1}else{-1}
        Send $peer press 0 $direction 0
        Send $peer release 0 $direction 0
        Await 'sword basic hit deals ten' {($h.players|Where-Object id -eq $target.id).hp -eq $target.hp-10 -and ($c.players|Where-Object id -eq $target.id).hp -eq $target.hp-10} 4
        Check ($true) "$peer basic sword hit deals ten on both peers"
    }
    $cooldowns=@{}
    foreach($peer in @('host','client')){
        Send $peer cycle 1 $(if($peer -eq 'host'){-1}else{1}) 0
        Send $peer parry
        Await 'sword cooldowns started' {$p=$h.players|Where-Object id -eq ((Read $peer).players|Where-Object owner).id;$p.skillReady -gt 0 -and $p.parryReady -gt 0} 3
        $actor=(Read $peer).players|Where-Object owner
        $server=$h.players|Where-Object id -eq $actor.id
        $cooldowns[$peer]=@{id=$actor.id;skill=$server.skillReady;parry=$server.parryReady}
        Check (($server.skillReady-$server.processed)*$step -gt 6.2 -and ($server.parryReady-$server.processed)*$step -gt 5.3) "$peer dash seven seconds and parry six seconds"
        Send $peer cycle 1 1 0
        Send $peer parry
        Start-Sleep -Milliseconds 200
        $server=(Read host).players|Where-Object id -eq $actor.id
        Check ($server.skillReady -eq $cooldowns[$peer].skill -and $server.parryReady -eq $cooldowns[$peer].parry) "$peer cannot repeat skills during cooldown"
    }
    Await 'both cooldowns expired' {foreach($entry in $cooldowns.Values){$p=$h.players|Where-Object id -eq $entry.id;if($p.processed -lt $entry.skill){return $false}};return $true} 10
    foreach($peer in @('host','client')){
        Send $peer cycle 1 $(if($peer -eq 'host'){1}else{-1}) 0
        Send $peer parry
        Await 'skills reusable after cooldown' {$p=$h.players|Where-Object id -eq $cooldowns[$peer].id;$p.skillReady -gt $cooldowns[$peer].skill -and $p.parryReady -gt $cooldowns[$peer].parry} 3
        Check ($true) "$peer can use skills after cooldown"
    }
    @{checks=$checks.Count;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Result.json"
    Write-Output "PASS $($checks.Count) runtime checks: $root"
}finally{
    if($clientProcess -and -not $clientProcess.HasExited){Send client quit;if(-not $clientProcess.WaitForExit(3000)){Stop-Process -Id $clientProcess.Id}}
    unity command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    if($previousScene){
        Start-Sleep -Milliseconds 1000
        Eval ('if(UnityEditor.EditorApplication.isPlaying)return false;var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!s.isDirty && s.path!="'+$previousScene+'")UnityEditor.SceneManagement.EditorSceneManager.OpenScene("'+$previousScene+'");return true;')|Out-Null
    }
}

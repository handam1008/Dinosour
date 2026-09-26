param([string]$Run=('Peers'+(Get-Date -Format 'yyyyMMddHHmmss')),[string]$Project=(Get-Location).Path,[string]$Build='Builds/Balance/Game.exe',[ValidateSet('FireBullet','GravityBullet','IceBullet','PoisonBullet','ShurikenBullet')][string[]]$Effects=@('FireBullet','GravityBullet','IceBullet','PoisonBullet','ShurikenBullet'))
$ErrorActionPreference='Stop'
$Project=[IO.Path]::GetFullPath($Project).Replace('\','/')
$root="$Project/Logs/Balance26/$Run"
if(Test-Path $root){throw 'Use a new run name'}
[IO.Directory]::CreateDirectory($root)|Out-Null
$cli=Join-Path $env:LOCALAPPDATA 'Unity/bin/unity.exe'
$checks=[Collections.Generic.List[string]]::new()
$seq=@{host=0;client=0}
$clientProcess=$null
$previous=$null
function Eval([string]$code){
    [IO.File]::WriteAllText("$root/Eval.cs",$code)
    $reply=& $cli command eval_file --file "$root/Eval.cs" --caller plugin --skill unity-cli --project-path $Project --format json|ConvertFrom-Json
    if(-not $reply.success -or -not $reply.data.result.success){throw ($reply|ConvertTo-Json -Depth 7)}
    $result=$reply.data.result.result
    if($result -is [string] -and ($result.StartsWith('{') -or $result.StartsWith('['))){return $result|ConvertFrom-Json}
    return $result
}
function Read($peer){
    for($n=0;$n -lt 5;$n++){try{return Get-Content "$root/$peer.json" -Raw|ConvertFrom-Json}catch{Start-Sleep -Milliseconds 15}}
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
    @{label=$label;host=$h;client=$c}|ConvertTo-Json -Depth 15|Set-Content "$root/Failure.json"
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
try{
    $previous=Eval 'var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(s.isDirty||UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Saved stopped scene required");var r=new{scene=s.path,job=SSW.PlayerJobStorage.Load().ToString()};UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/RYU/00.Scene/StartMenu.unity");return Newtonsoft.Json.JsonConvert.SerializeObject(r);'
    & $cli command editor_play --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    $until=[DateTime]::UtcNow.AddSeconds(45)
    do{try{$scene=Eval 'return UnityEditor.EditorApplication.isPlaying&&!UnityEditor.EditorApplication.isCompiling?UnityEngine.SceneManagement.SceneManager.GetActiveScene().name:"";'}catch{$scene=''};if($scene -in @('MainMenu','StartMenu')){break};Start-Sleep -Milliseconds 300}while([DateTime]::UtcNow -lt $until)
    if($scene -notin @('MainMenu','StartMenu')){throw 'Menu did not become ready'}
    Eval ('var g=SSW.NetGame.GetOrCreate();g.gameObject.AddComponent<SSW.NetProbe>().Init("'+$root+'/host");g.StartLocal(true,"127.0.0.1",SSW.PlayerJob.Gunner,30718);return true;')|Out-Null
    $clientProcess=Start-Process -FilePath (Join-Path $Project $Build) -WorkingDirectory $Project -ArgumentList @('--net-mode','client','--net-address','127.0.0.1','--net-port','30718','--net-job','Gunner','--net-probe',"$root/client",'-logFile',"$root/client.log",'-screen-fullscreen','0','-screen-width','800','-screen-height','450') -WindowStyle Hidden -PassThru
    Await 'both drafts' {$h.phase -eq 'Draft' -and $c.phase -eq 'Draft'} 60
    Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Draft.Restore(System.Array.Empty<int>());SSW.NetGame.Current.Match.Picked();return true;'|Out-Null
    Await 'both playing' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing'} 30
    foreach($peer in @('host','client')){Send $peer lag 60 10 0;Send $peer isolate;Send $peer wideground 0 0 0}
    $spawn=@'
var g=SSW.NetGame.Current;
var prefab=(SSW.NetPlayer)new UnityEditor.SerializedObject(g).FindProperty("_playerPrefab").objectReferenceValue;
var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var previous=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{p.OwnerClientId,p.Side,p.Info}));
typeof(SSW.NetGame).GetMethod("ClearShots",f).Invoke(g,null);
foreach(var p in System.Linq.Enumerable.ToArray(g.Players))p.NetworkObject.Despawn();
var deck=UnityEngine.Resources.Load<SSW.NetDeck>("Network/Deck");int id=-1;
for(int i=0;i<deck.Count;i++)if(deck.At(i) is SSW.GunnerArgument a&&a.type==KDH.Scripts.Arguments.GunnerAugmentType.@TYPE@)id=i;
if(id<0)throw new System.InvalidOperationException("Missing augment");
foreach(var old in previous){var p=UnityEngine.Object.Instantiate(prefab);p.Init(SSW.PlayerJob.Gunner,old.Side,old.Info);p.NetworkObject.SpawnAsPlayerObject(old.OwnerClientId,true);p.Draft.Restore(new[]{id});}
UnityEngine.Physics2D.IgnoreCollision(g.Players[0].Collider,g.Players[1].Collider);
foreach(var p in g.Players){var cast=p.GetComponent<SSW.GunCast>();if(new UnityEditor.SerializedObject(cast).FindProperty("_balance").objectReferenceValue==null)throw new System.InvalidOperationException("Missing saved balance reference");p.Drive.Teleport(new UnityEngine.Vector2(p.Side*-3f,3f));}
return Newtonsoft.Json.JsonConvert.SerializeObject(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,objectId=p.NetworkObjectId})));
'@
    foreach($type in $Effects){
        $created=Eval $spawn.Replace('@TYPE@',$type)
        if($created.Count -ne 2 -or $null -eq $created[0].objectId){throw 'Invalid actor payload'}
        Await "$type actors and augment replicated" {foreach($p in $created){$r=$c.players|Where-Object id -eq $p.id;if($r.objectId -ne $p.objectId -or $r.augments.Count -ne 1){return $false}};return $true} 12
        foreach($peer in @('host','client')){
            foreach($side in @('host','client')){Send $side move}
            Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Drive.Teleport(new UnityEngine.Vector2(p.Side*-3f,3f));return true;'|Out-Null
            Await 'both grounded' {$h.players[0].grounded -and $h.players[1].grounded -and $c.players[0].grounded -and $c.players[1].grounded} 6
            $actor=(Read $peer).players|Where-Object owner
            $other=if($peer -eq 'host'){'client'}else{'host'}
            $prepared=Eval ((@'
var g=SSW.NetGame.Current;var p=System.Linq.Enumerable.First(g.Players,p=>p.OwnerClientId==@ID@UL);var t=System.Linq.Enumerable.First(g.Players,t=>t!=p);var gun=p.GetComponent<SSW.GunCast>();
var state=new SSW.WeaponState{Ammo=1,Reload=p.InputSequence+10000,Filled=p.InputSequence};state.Loaded.Add(p.InputSequence);
typeof(SSW.JobCast).GetProperty("State",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(gun,state);
UnityEngine.Vector2 direction=(t.Body.position-p.Body.position).normalized;
UnityEngine.Vector2 aim=((UnityEngine.Vector2)t.Collider.bounds.center-gun.View.Muzzle(p.Body.position,direction,p.Drive.Scale)).normalized;
return Newtonsoft.Json.JsonConvert.SerializeObject(new{actor=p.OwnerClientId,target=t.OwnerClientId,hp=t.Health.Current,position=new{x=t.Body.position.x,y=t.Body.position.y},aim=new{x=aim.x,y=aim.y},distance=UnityEngine.Vector2.Distance(p.Body.position,t.Body.position),damage=p.Stats.Damage,speed=t.Stats.MoveSpeed,fired=p.Cast.Shots,canAttack=p.CanAttack});
'@).Replace('@ID@',[string]$actor.id))
            if($null -eq $prepared.aim.x -or $null -eq $prepared.hp -or -not $prepared.canAttack){throw 'Invalid shot preparation'}
            $prepared|ConvertTo-Json -Depth 5|Set-Content "$root/$type-$peer-before.json"
            Start-Sleep -Milliseconds 250
            Send $peer press 0 $prepared.aim.x $prepared.aim.y
            Send $peer release 0 $prepared.aim.x $prepared.aim.y
            Await "$peer $type shot accepted" {($h.players|Where-Object id -eq $prepared.actor).fired -eq $prepared.fired+1} 3
            $extra=switch($type){'FireBullet'{6};'PoisonBullet'{16};'ShurikenBullet'{$prepared.distance};default{0}}
            $expected=$prepared.hp-$prepared.damage-$extra
            if($type -eq 'GravityBullet'){
                Await "$peer actual gravity impact moves target" {$p=$h.players|Where-Object id -eq $prepared.target;[Math]::Abs($p.position.x-$prepared.position.x) -gt 0.1} 4
                Check $true "$peer gravity impulse moves authoritative target"
                Await "$peer gravity replication" {$a=$h.players|Where-Object id -eq $prepared.target;$b=$c.players|Where-Object id -eq $prepared.target;[Math]::Abs($a.position.x-$b.position.x) -lt 0.5 -and [Math]::Abs($a.velocity.x) -lt 0.3} 5
                Check $true "$peer impulse decays and both peers converge"
            }
            if($type -eq 'IceBullet'){
                $slow=$prepared.speed*0.7
                Await "$peer slow on both peers" {$a=$h.players|Where-Object id -eq $prepared.target;$b=$c.players|Where-Object id -eq $prepared.target;[Math]::Abs($a.speed-$slow) -lt 0.02 -and [Math]::Abs($b.speed-$slow) -lt 0.02} 2
                Send $other move 0 1 0
                Await "$peer target moves at slowed speed" {$a=$h.players|Where-Object id -eq $prepared.target;[Math]::Abs([Math]::Abs($a.velocity.x)-$slow) -lt 0.15} 2
                Check $true "$peer ice applies original 0.3 and target moves at 70 percent speed"
                Send $other move
                Await "$peer slow restored" {$a=$h.players|Where-Object id -eq $prepared.target;$b=$c.players|Where-Object id -eq $prepared.target;[Math]::Abs($a.speed-$prepared.speed) -lt 0.02 -and [Math]::Abs($b.speed-$prepared.speed) -lt 0.02} 4
                Check $true "$peer slow expiry restores both peers"
            }
            Await "$peer $type exact final health on both peers" {$a=$h.players|Where-Object id -eq $prepared.target;$b=$c.players|Where-Object id -eq $prepared.target;[Math]::Abs($a.hp-$expected) -lt 0.03 -and [Math]::Abs($b.hp-$expected) -lt 0.03} 7
            Check $true "$peer $type actual projectile damage $($prepared.damage+$extra) replicated"
            Start-Sleep -Milliseconds 700
            $final=(Read host).players|Where-Object id -eq $prepared.target
            Check ([Math]::Abs($final.hp-$expected) -lt 0.03) "$peer $type has no extra late damage"
            @{before=$prepared;host=(Read host);client=(Read client)}|ConvertTo-Json -Depth 15|Set-Content "$root/$type-$peer.json"
        }
    }
    @{success=$true;count=$checks.Count;checks=$checks;effects=$Effects;host=(Read host);client=(Read client);scope='same PC Editor host and development client; 60ms+10ms jitter each direction; ammo/augment setup injected; real Cast.Attack projectile collision'}|ConvertTo-Json -Depth 15|Set-Content "$root/Result.json"
    Write-Output "PASS $($checks.Count) peer checks: $root"
}catch{
    @{success=$false;error=$_.ToString();count=$checks.Count;before=$prepared;host=(Read host);client=(Read client)}|ConvertTo-Json -Depth 15|Set-Content "$root/Failure.json"
    throw
}finally{
    if($clientProcess -and -not $clientProcess.HasExited){try{Send client quit}catch{};if(-not $clientProcess.WaitForExit(3000)){Stop-Process -Id $clientProcess.Id}}
    & $cli command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    if($previous){
        Start-Sleep -Milliseconds 800
        Eval ('if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Still playing");SSW.PlayerJobStorage.Save(SSW.PlayerJob.'+$previous.job+');var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(s.isDirty)throw new System.InvalidOperationException("Unsaved scene");UnityEditor.SceneManagement.EditorSceneManager.OpenScene("'+$previous.scene+'");return true;')|Out-Null
    }
}

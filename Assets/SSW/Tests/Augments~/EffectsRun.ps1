function SpawnPair($hostJob,$clientJob,$hostCards='',$clientCards=''){
    $code=@'
var g=SSW.NetGame.Current;
var prefab=(SSW.NetPlayer)new UnityEditor.SerializedObject(g).FindProperty("_playerPrefab").objectReferenceValue;
var old=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{p.OwnerClientId,p.Side,p.Info}));
typeof(SSW.NetGame).GetMethod("ClearShots",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(g,null);
foreach(var p in System.Linq.Enumerable.ToArray(g.Players))p.NetworkObject.Despawn();
foreach(var saved in old)
{
    bool host=saved.OwnerClientId==0;
    var p=UnityEngine.Object.Instantiate(prefab);
    p.Init(host?SSW.PlayerJob.@HOSTJOB@:SSW.PlayerJob.@CLIENTJOB@,saved.Side,saved.Info);
    p.NetworkObject.SpawnAsPlayerObject(saved.OwnerClientId,true);
    p.Draft.Restore(host?new int[]{@HOSTCARDS@}:new int[]{@CLIENTCARDS@});
    p.Drive.Teleport(new UnityEngine.Vector2(p.Side* -0.55f,5f));
}
UnityEngine.Physics2D.IgnoreCollision(g.Players[0].Collider,g.Players[1].Collider);
return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,objectId=p.NetworkObjectId,job=p.Job.ToString()}));
'@
    $spawn=Eval $code.Replace('@HOSTJOB@',$hostJob).Replace('@CLIENTJOB@',$clientJob).Replace('@HOSTCARDS@',$hostCards).Replace('@CLIENTCARDS@',$clientCards)
    Await 'replacement players arrive' {foreach($p in $spawn){$remote=$c.players|Where-Object id -eq $p.id;if($null -eq $remote -or $remote.objectId -ne $p.objectId -or $remote.job -ne $p.job){return $false}};return $true} 10
    Await 'replacement players grounded' {$h.players.Count -eq 2 -and $h.players[0].grounded -and $h.players[1].grounded -and $c.players[0].grounded -and $c.players[1].grounded} 6
}
function Isolate{
    foreach($peer in @('host','client')){Send $peer isolate;Send $peer wideground 0 0 3}
}
function ResetPlaces{
    Eval 'foreach(var p in SSW.NetGame.Current.Players)p.Drive.Teleport(new UnityEngine.Vector2(p.Side* -0.55f,5f));return true;'|Out-Null
    Await 'players grounded again' {$h.players[0].grounded -and $h.players[1].grounded} 5
}
function Hit($peer){
    $actor=(Read $peer).players|Where-Object owner
    Send $peer press 0 $actor.side 0
    Send $peer release 0 $actor.side 0
}
function Aura($peer){
    $owner=if($peer -eq 'host'){'true'}else{'false'}
    return Eval ('var p=System.Linq.Enumerable.First(SSW.NetGame.Current.Players,p=>p.IsOwner=='+$owner+');var k=p.GetComponent<SSW.KnifeCast>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;return new{now=UnityEngine.Time.time,start=(float)typeof(SSW.KnifeCast).GetField("_auraStart",flags).GetValue(k),until=(float)typeof(SSW.KnifeCast).GetField("_auraUntil",flags).GetValue(k),ready=(float)typeof(SSW.KnifeCast).GetField("_auraReady",flags).GetValue(k)};')
}
Isolate
Send host lag 60 10 0
Send client lag 60 10 0
SpawnPair Swordsman Swordsman '59' '59'
Await 'three health on both peers' {foreach($p in @($h.players)+@($c.players)){if($p.max -ne 3 -or $p.hp -ne 3){return $false}};return $true} 5
Check $true 'E1 TenLives has max health three on both peers'
foreach($peer in @('host','client')){
    $target=(Read $peer).players|Where-Object {-not $_.owner}
    foreach($expected in @(2,1)){
        Hit $peer
        Await 'normal sword hit reduced to one' {($h.players|Where-Object id -eq $target.id).hp -eq $expected -and ($c.players|Where-Object id -eq $target.id).hp -eq $expected} 5
        Check $true "E1 $peer real sword attack leaves $expected health on both peers"
        Start-Sleep -Milliseconds 2100
    }
}
$before=(Read host).players|Where-Object owner
Hit client
Await 'lethal third hit respawns player with retained augment' {$p=$h.players|Where-Object id -eq $before.id;$q=$c.players|Where-Object id -eq $before.id;$p.objectId -ne $before.objectId -and $p.max -eq 3 -and $p.hp -eq 3 -and $q.max -eq 3 -and $q.hp -eq 3 -and $p.augments -contains 59 -and $h.phase -eq 'Playing'} 20
Check $true 'E1 normal lethal hit restores three health and retained card next round'
Isolate
foreach($victim in @('host','client')){
    $attacker=if($victim -eq 'host'){'client'}else{'host'}
    if($victim -eq 'host'){SpawnPair Assassin Swordsman '46' ''}else{SpawnPair Swordsman Assassin '' '46'}
    $target=(Read $victim).players|Where-Object owner
    Hit $attacker
    Await 'Escape haste starts after real hit' {($h.players|Where-Object id -eq $target.id).speed -gt 11.1 -and ($c.players|Where-Object id -eq $target.id).speed -gt 11.1} 3
    Check ([Math]::Abs(($h.players|Where-Object id -eq $target.id).speed-11.2) -lt 0.01) "E2 $victim Escape uses 1.6 multiplier"
    Send $victim move 0 1 0
    Await 'Escape actual movement' {[Math]::Abs([Math]::Abs(($h.players|Where-Object id -eq $target.id).velocity.x)-11.2) -lt 0.2} 2
    Send $victim move
    Check $true "E2 $victim actual movement reaches 11.2"
    Await 'Escape expires naturally' {[Math]::Abs(($h.players|Where-Object id -eq $target.id).speed-7) -lt 0.01 -and [Math]::Abs(($c.players|Where-Object id -eq $target.id).speed-7) -lt 0.01} 4
    ResetPlaces
    $hp=(Read host).players|Where-Object id -eq $target.id
    Hit $attacker
    Await 'second hit lands during Escape cooldown' {($h.players|Where-Object id -eq $target.id).hp -lt $hp.hp} 3
    Check ([Math]::Abs(($h.players|Where-Object id -eq $target.id).speed-7) -lt 0.01) "E2 $victim cannot refresh haste during cooldown"
    Start-Sleep -Milliseconds 3200
    Hit $attacker
    Await 'Escape can trigger after duration plus cooldown' {($h.players|Where-Object id -eq $target.id).speed -gt 11.1 -and ($c.players|Where-Object id -eq $target.id).speed -gt 11.1} 3
    Check $true "E2 $victim haste reusable after five seconds"
}
foreach($caster in @('host','client')){
    $other=if($caster -eq 'host'){'client'}else{'host'}
    if($caster -eq 'host'){SpawnPair Assassin Swordsman '49' ''}else{SpawnPair Swordsman Assassin '' '49'}
    $actor=(Read $caster).players|Where-Object owner
    $target=(Read $other).players|Where-Object owner
    Send $caster cycle 1 0 1
    Send $caster cycle 0 0 1
    $first=Aura $caster
    Check ([Math]::Abs(($first.until-$first.start)-6) -lt 0.01 -and [Math]::Abs(($first.ready-$first.until)-7) -lt 0.01) "E4 $caster aura duration six and cooldown seven"
    Await 'aura reaches original maximum reduction' {$a=($h.players|Where-Object id -eq $target.id).speed;$b=($c.players|Where-Object id -eq $target.id).speed;[Math]::Abs($a-4.9) -lt 0.02 -and [Math]::Abs($b-4.9) -lt 0.02} 5
    $before=(Read host).players|Where-Object id -eq $actor.id
    Hit $other
    Await 'aura weakens real sword attack' {($h.players|Where-Object id -eq $actor.id).hp -lt $before.hp} 2
    $after=(Read host).players|Where-Object id -eq $actor.id
    Check ([Math]::Abs(($before.hp-$after.hp)-$target.stats.Damage*0.7) -lt 0.02) "E4 $caster aura reduces real enemy damage by thirty percent"
    Await 'aura expires on both peers' {[Math]::Abs(($h.players|Where-Object id -eq $target.id).speed-7) -lt 0.01 -and [Math]::Abs(($c.players|Where-Object id -eq $target.id).speed-7) -lt 0.01} 5
    Start-Sleep -Milliseconds 800
    Send $caster cycle 1 0 1
    Send $caster cycle 0 0 1
    $blocked=Aura $caster
    Check ($blocked.start -eq $first.start) "E4 $caster skill does not restart aura during cooldown"
    $wait=[Math]::Max(0,($first.ready-$blocked.now+0.4)*1000)
    Start-Sleep -Milliseconds ([int]$wait)
    Send $caster cycle 1 0 1
    Send $caster cycle 0 0 1
    $again=Aura $caster
    Check ($again.start -gt $first.start -and $again.start -ge $first.ready) "E4 $caster aura reusable after thirteen seconds"
    @{first=$first;blocked=$blocked;again=$again}|ConvertTo-Json|Set-Content "$root/Aura-$caster.json"
}
SpawnPair Gambler Gambler '24,27,28,30' '24,27,28,30'
Eval 'foreach(var p in SSW.NetGame.Current.Players){typeof(SSW.Health).GetMethod("SetMax",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.Health,new object[]{1000000f});p.Health.Heal(1000000f);p.Cast.Weapon.Progress=20;}return true;'|Out-Null
foreach($peer in @('host','client')){
    Send $peer press 0 0 1
    Send $peer release 0 0 1
}
Await 'real roulette shots start guaranteed jackpot' {$h.players[0].progress -eq 0 -and $h.players[1].progress -eq 0 -and $h.players[0].max -eq 200 -and $h.players[1].max -eq 200 -and $c.players[0].max -eq 200 -and $c.players[1].max -eq 200} 5
Check $true 'E3 normal input starts guaranteed jackpot on both owners'
$stress=Eval @'
var g=SSW.NetGame.Current;
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var roll=typeof(SSW.CoinCast).GetMethod("Roll",flags);
var timer=typeof(SSW.CoinCast).GetField("_jackpotUntil",flags);
var probability=typeof(SSW.CoinCast).GetField("_probability",flags);
var saved=UnityEngine.Random.state;
var seeds=new System.Collections.Generic.List<int>();
for(int seed=0;seed<100000 && seeds.Count<100;seed++)
{
    UnityEngine.Random.InitState(seed);
    float main=UnityEngine.Random.Range(0f,100f);
    float old=UnityEngine.Random.Range(0f,100f);
    if(main>=21f && main<24f || main>50f && old>=4f && old<5f)seeds.Add(seed);
}
var result=new System.Collections.Generic.List<object>();
foreach(var p in g.Players)
{
    var cast=p.GetComponent<SSW.CoinCast>();
    float until=(float)timer.GetValue(cast);
    float chance=(float)probability.GetValue(cast);
    cast.Progress=20;
    foreach(int seed in seeds)
    {
        UnityEngine.Random.InitState(seed);
        roll.Invoke(cast,null);
        if(cast.Progress!=20 || (float)timer.GetValue(cast)!=until || (float)probability.GetValue(cast)!=chance)
            throw new System.InvalidOperationException("Active jackpot consumed progress or probability");
    }
    result.Add(new{id=p.OwnerClientId,until,chance,seeds=seeds.Count,progress=cast.Progress});
}
UnityEngine.Random.state=saved;
return result.ToArray();
'@
$stress|ConvertTo-Json -Depth 5|Set-Content "$root/JackpotStress.json"
Check ($stress.Count -eq 2 -and $stress[0].seeds -eq 100 -and $stress[1].seeds -eq 100) 'E3 seeded server Roll injection preserves progress and probability during active jackpot'
Await 'jackpot ends naturally on both peers' {$h.players[0].max -eq 100 -and $h.players[1].max -eq 100 -and $c.players[0].max -eq 100 -and $c.players[1].max -eq 100} 20
foreach($peer in @('host','client')){Send $peer press 0 0 1;Send $peer release 0 0 1}
Await 'preserved progress starts next jackpot' {$h.players[0].progress -eq 0 -and $h.players[1].progress -eq 0 -and $h.players[0].max -eq 200 -and $h.players[1].max -eq 200 -and $c.players[0].max -eq 200 -and $c.players[1].max -eq 200} 5
Check $true 'E3 normal input consumes saved twenty only after prior jackpot ends'
@{checks=$checks.Count;host=$h;client=$c}|ConvertTo-Json -Depth 14|Set-Content "$root/Result.json"
Write-Output "PASS $($checks.Count) augmentation checks: $root"

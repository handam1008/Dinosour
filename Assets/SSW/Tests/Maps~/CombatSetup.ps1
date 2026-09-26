function CombatSpawn($job,[bool]$zeroDamage=$false){
    $code=@'
var g=SSW.NetGame.Current;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var prefab=(SSW.NetPlayer)new UnityEditor.SerializedObject(g).FindProperty("_playerPrefab").objectReferenceValue;
var old=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{p.OwnerClientId,p.Side,p.Info}));
typeof(SSW.NetGame).GetMethod("ClearShots",flags).Invoke(g,null);
foreach(var p in System.Linq.Enumerable.ToArray(g.Players))p.NetworkObject.Despawn();
foreach(var previous in old)
{
    var p=UnityEngine.Object.Instantiate(prefab);
    var settings=new UnityEditor.SerializedObject(p);
    var book=UnityEngine.Object.Instantiate((SSW.StatsBook)settings.FindProperty("_statsBook").objectReferenceValue);
    var stats=book.At(SSW.PlayerJob.@JOB@);stats.Gravity=0f;if(@ZERO@)stats.SkillDamage=0f;book.Replace(new[]{stats});
    settings.FindProperty("_statsBook").objectReferenceValue=book;settings.ApplyModifiedPropertiesWithoutUndo();
    p.Init(SSW.PlayerJob.@JOB@,previous.Side,previous.Info);p.NetworkObject.SpawnAsPlayerObject(previous.OwnerClientId,true);p.Draft.Restore(System.Array.Empty<int>());
    p.Drive.Teleport(g.Arena.Spawn(p.Side==1?0:1));
}
UnityEngine.Physics2D.IgnoreCollision(g.Players[0].Collider,g.Players[1].Collider);
return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,objectId=p.NetworkObjectId}));
'@
    $spawn=Eval $code.Replace('@JOB@',$job).Replace('@ZERO@',$zeroDamage.ToString().ToLowerInvariant())
    Await "$job attack fixture spawns on both peers" {foreach($s in $spawn){$p=$c.players|Where-Object id -eq $s.id;if($p.objectId -ne $s.objectId -or $p.job -ne $job -or $p.stats.Gravity -ne 0){return $false}};return $true} 10
}
function CombatAim($peer,$mode='basic'){
    $code=@'
var g=SSW.NetGame.Current;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var p=System.Linq.Enumerable.First(g.Players,x=>x.IsOwner==@OWNER@);
var pins=g.Arena.Map.GetComponentsInChildren<SSW.Pin>(true);
SSW.Pin pin=null;UnityEngine.Vector2 point=default,direction=default,position=default;
var filter=new UnityEngine.ContactFilter2D{useTriggers=false};filter.SetLayerMask(p.GroundMask);
var overlaps=new UnityEngine.Collider2D[16];var capsule=(UnityEngine.CapsuleCollider2D)p.Collider;
bool melee=p.Job==SSW.PlayerJob.Swordsman||p.Job==SSW.PlayerJob.Assassin&&"@MODE@"=="basic";
foreach(var candidate in System.Linq.Enumerable.OrderBy(pins,x=>UnityEngine.Mathf.Abs(x.transform.position.x)))
{
    var shape=(UnityEngine.Collider2D)new UnityEditor.SerializedObject(candidate).FindProperty("_hit").objectReferenceValue;
    point=shape.bounds.center;
    foreach(var axis in new[]{UnityEngine.Vector2.right,UnityEngine.Vector2.left,UnityEngine.Vector2.up,UnityEngine.Vector2.down})
    {
        direction=axis;
        if(melee){var offset=UnityEngine.Vector2.Scale(p.Stats.HitOffset,p.transform.lossyScale);position=point-axis*offset.x-new UnityEngine.Vector2(-axis.y,axis.x)*offset.y;}
        else if(p.Job==SSW.PlayerJob.Witch)
        {
            if(axis!=UnityEngine.Vector2.up)continue;
            var hand=(UnityEngine.SpriteRenderer)typeof(SSW.NetCast).GetField("_hand",flags).GetValue(p.Cast);
            position=point-axis*2f-UnityEngine.Vector2.right*(hand.transform.position.x-p.Body.position.x);
        }
        else position=point-axis*(p.Job==SSW.PlayerJob.Gunner?2.5f:1.25f);
        if(SSW.ShotQuery.Ground(position,point,0.125f,p.GroundMask,out _))continue;
        if(UnityEngine.Physics2D.OverlapCapsule(position+(UnityEngine.Vector2)p.transform.TransformVector(capsule.offset),UnityEngine.Vector2.Scale(capsule.size,p.transform.lossyScale),capsule.direction,0f,filter,overlaps)>0)continue;
        pin=candidate;break;
    }
    if(pin!=null)break;
}
if(pin==null)throw new System.InvalidOperationException("No unobstructed pin attack fixture exists in this map");
var settings=new UnityEditor.SerializedObject(pin);
var swing=(SSW.Swing)settings.FindProperty("_target").objectReferenceValue;
var body=(UnityEngine.Rigidbody2D)new UnityEditor.SerializedObject(swing).FindProperty("_body").objectReferenceValue;
foreach(var actor in g.Players){actor.Drive.Teleport(g.Arena.Spawn(actor.Side==1?0:1));}
p.Drive.Teleport(position);
if(p.Job==SSW.PlayerJob.Witch)
{
    var stock=(SSW.NetStock)typeof(SSW.NetCast).GetField("_stock",flags).GetValue(p.Cast);
    int damage=-1;for(int i=0;i<stock.BaseCount;i++)if(stock.At(stock.BaseAt(i)) is DamagePotion)damage=stock.BaseAt(i);
    if(damage<0)throw new System.InvalidOperationException("Damage potion missing");
    var bag=(SSW.CastStock)typeof(SSW.NetCast).GetField("_inventory",flags).GetValue(p.Cast);
    typeof(SSW.NetCast).GetMethod("SyncEpoch",flags).Invoke(p.Cast,null);
    for(int i=0;i<2&&bag.Held.Id!=0;i++)
        if(!bag.Take(bag.Held.Id,p.Epoch,g.ServerTime,out _))throw new System.InvalidOperationException("Potion fixture epoch mismatch");
    bag.Add(damage,g.ServerTime,1.25);
    typeof(SSW.NetCast).GetField("_brewAt",flags).SetValue(p.Cast,g.ServerTime+1000d);
    typeof(SSW.NetCast).GetMethod("PublishStock",flags).Invoke(p.Cast,null);
}
if(p.Job==SSW.PlayerJob.Gunner){var gun=(SSW.GunCast)p.Cast.Weapon;for(int i=0;i<5;i++)direction=(point-gun.View.Muzzle(p.Body.position,direction,p.Drive.Scale)).normalized;}
UnityEngine.Physics2D.SyncTransforms();
if(SSW.ShotQuery.Ground(p.Body.position,point,0.1f,p.GroundMask,out var wall))throw new System.InvalidOperationException("Fixture attack is blocked by "+wall.collider.name);
return new{map=g.Arena.Map.Title,index=System.Array.IndexOf(pins,pin),count=pins.Length,id=p.OwnerClientId,objectId=p.NetworkObjectId,job=p.Job.ToString(),x=body.position.x,y=body.position.y,angle=body.rotation,ax=direction.x,ay=direction.y,px=point.x,py=point.y};
'@
    $setup=Eval $code.Replace('@OWNER@',$(if($peer -eq 'host'){'true'}else{'false'})).Replace('@MODE@',$mode)
    Await 'fixture positions and epochs settled' {$a=$h.players|Where-Object id -eq $setup.id;$b=$c.players|Where-Object id -eq $setup.id;$a.epoch -eq $b.epoch -and [Math]::Abs($a.position.x-$b.position.x) -lt 0.1 -and [Math]::Abs($a.position.y-$b.position.y) -lt 0.1} 5
    Start-Sleep -Milliseconds 500
    if($setup.job -eq 'Gunner'){Await 'gun has a naturally loaded round' {($h.players|Where-Object id -eq $setup.id).ammo -gt 0 -and ($c.players|Where-Object id -eq $setup.id).ammo -gt 0} 6}
    if($setup.job -eq 'Witch'){Await 'damage potion fixture reaches owner' {($c.players|Where-Object id -eq $setup.id).heldView -ge 0 -and ($h.players|Where-Object id -eq $setup.id).potion -eq ($c.players|Where-Object id -eq $setup.id).potion} 5}
    return $setup
}

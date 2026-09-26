$ErrorActionPreference='Stop'
MapLoad 1|Out-Null
$gearCode=@'
var g=SSW.NetGame.Current;var map=g.Arena.Map;var area=map.GetComponentInChildren<SSW.MapArea>();var settings=new UnityEditor.SerializedObject(area);var shape=(UnityEngine.Collider2D)settings.FindProperty("_shape").objectReferenceValue;
foreach(var script in map.GetComponentsInChildren<UnityEngine.MonoBehaviour>())if(script.GetType().Name=="KDH_Rotation")script.enabled=false;
if(shape.attachedRigidbody!=null)shape.attachedRigidbody.angularVelocity=0f;
var before=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,hp=p.Health.Current}));
foreach(var p in g.Players){float foot=p.Body.position.y-p.Collider.bounds.min.y;var edge=shape.ClosestPoint((UnityEngine.Vector2)shape.bounds.center+UnityEngine.Vector2.up*20f);p.Drive.Freeze(20f);p.Drive.Teleport(edge+UnityEngine.Vector2.up*(foot+@GAP@));}
return new{damage=settings.FindProperty("_damage").floatValue,force=settings.FindProperty("_force").floatValue,hp=before};
'@
$gear=Eval $gearCode.Replace('@GAP@','0.016f')
Await 'gear contact damages both actors once' {MapClientHealth $gear.hp $gear.damage} 5
Check ([Math]::Abs($gear.damage-10) -lt 0.001) 'gear uses source ten damage on both peers'
$hit=@{host=$h;client=$c}
$hold=@(MapSamples 0.8)
Check (MapClientHealth $gear.hp $gear.damage) 'persistent gear contact does not repeat damage'
Eval $gearCode.Replace('@GAP@','2f')|Out-Null
$away=@(MapSamples 0.3)
$again=Eval $gearCode.Replace('@GAP@','0.016f')
Await 'gear exit and reentry rearm contact damage' {MapClientHealth $again.hp $again.damage} 5
Check $true 'gear reentry damages both actors again'
MapSave 'MapGear' @{control='Gear rotation paused in memory to isolate sustained contact and reentry; original damage and collision queries retained';setup=$gear;hit=$hit;hold=$hold;away=$away;reentry=$again;host=$h;client=$c}
MapLoad 6|Out-Null
$ice=Eval @'
var g=SSW.NetGame.Current;var map=g.Arena.Map;SSW.MapArea ice=null;
foreach(var area in map.GetComponentsInChildren<SSW.MapArea>()){area.enabled=false;if(new UnityEditor.SerializedObject(area).FindProperty("_outside").boolValue)ice=area;}
if(ice==null)throw new System.InvalidOperationException("Ice area missing");
var edges=new UnityEditor.SerializedObject(map.GetComponent<SSW.MapEdges>()).FindProperty("_edges");for(int i=0;i<edges.arraySize;i++)((UnityEngine.Collider2D)edges.GetArrayElementAtIndex(i).FindPropertyRelative("Shape").objectReferenceValue).enabled=false;
var settings=new UnityEditor.SerializedObject(ice);var shape=(UnityEngine.Collider2D)settings.FindProperty("_shape").objectReferenceValue;
foreach(var p in g.Players){p.Drive.Teleport(new UnityEngine.Vector2(shape.bounds.center.x+p.Side,shape.bounds.max.y+2f));p.Drive.Freeze(30f);}ice.enabled=true;
return new{damage=settings.FindProperty("_damage").floatValue,slow=settings.FindProperty("_slow").floatValue,hp=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,hp=p.Health.Current,speed=p.Stats.MoveSpeed}))};
'@
$untouched=@(MapSamples 1.2)
Check (MapClientHealth $ice.hp 0) 'ice does not damage actors that never entered its area'
$icePlace=@'
var g=SSW.NetGame.Current;var area=System.Linq.Enumerable.First(g.Arena.Map.GetComponentsInChildren<SSW.MapArea>(),a=>new UnityEditor.SerializedObject(a).FindProperty("_outside").boolValue);var shape=(UnityEngine.Collider2D)new UnityEditor.SerializedObject(area).FindProperty("_shape").objectReferenceValue;
foreach(var p in g.Players){p.Drive.Teleport(new UnityEngine.Vector2(shape.bounds.center.x+p.Side,@HEIGHT@));p.Drive.Freeze(30f);}
UnityEngine.Physics2D.SyncTransforms();return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,hp=p.Health.Current,gap=p.Collider.Distance(shape).distance}));
'@
$inside=Eval $icePlace.Replace('@HEIGHT@','shape.bounds.center.y')
Check (@($inside|Where-Object gap -le 0).Count -eq 2) 'both actors overlap ice volume before exit'
$warm=@(MapSamples 1.2)
Check (MapClientHealth $inside 0) 'ice preserves HP while actors remain inside'
$outside=Eval $icePlace.Replace('@HEIGHT@','shape.bounds.max.y+2f')
Await 'ice exit first damage tick' {MapClientHealth $outside $ice.damage} 3
Check $true 'ice applies first delayed outside damage on both peers'
Await 'ice exit second damage tick' {MapClientHealth $outside ($ice.damage*2)} 3
Check $true 'ice repeats outside damage once per second'
foreach($before in $ice.hp){foreach($snapshot in @($h,$c)){$player=$snapshot.players|Where-Object id -eq $before.id;Check ([Math]::Abs($player.speed-$before.speed*(1-$ice.slow)) -lt 0.01) "ice source slow applies to actor $($before.id)"}}
$return=Eval $icePlace.Replace('@HEIGHT@','shape.bounds.center.y')
$reentered=@(MapSamples 1.3)
Check (MapClientHealth $return 0) 'ice reentry cancels further outside damage'
foreach($before in $ice.hp){foreach($snapshot in @($h,$c)){Check ([Math]::Abs(($snapshot.players|Where-Object id -eq $before.id).speed-$before.speed) -lt 0.01) "ice slow expires for actor $($before.id)"}}
MapSave 'MapIce' @{control='Other map damage components and border colliders disabled in memory to isolate ice entry and exit; source one-second damage and slow preserved';setup=$ice;untouched=$untouched;inside=$inside;warm=$warm;outside=$outside;returned=$return;reentered=$reentered;host=$h;client=$c}

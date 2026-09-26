param([switch]$SkipCycle,[switch]$OnlyCycle,[switch]$SkipLoads,[switch]$AreasOnly,[ValidateRange(0,5)][int]$From=0)
$ErrorActionPreference='Stop'
if($AreasOnly){$SkipCycle=$true}
foreach($command in @('Read','Await','Send','Check','Eval','Map')){
    if(-not(Get-Command $command -CommandType Function -ErrorAction SilentlyContinue)){throw 'Dot-source this script from an active Hazards.ps1 host/client session.'}
}
$mapCases=[Collections.Generic.List[object]]::new()
$mapTransitions=[Collections.Generic.List[object]]::new()
$mapLimits=[Collections.Generic.List[string]]::new()
$mapLimits.Add('Visual checks compare ten-hertz host/client diagnostic snapshots, not rendered pixels or zero network delay. Timing and position tolerances are recorded in check labels.')
$mapLimits.Add('Old-map cleanup checks every spawned BattleMap ID on both peers. Detached non-network objects and external effect pools are not enumerated by mapState.ids.')
$mapLimits.Add('Draft pause checks explicitly set MatchPhase.Draft, deal choices, then restore unchanged owned augments and call Picked. Flood damage advances only the shared clock; native particle impacts use delayed controlled emission. These controls are recorded separately from natural visual behavior.')
$mapLimits.Add('Maps 4,5,10,15,16 retain existing assets. Their external effect pools are outside this refresh and are not claimed repaired.')
function MapSave($name,$data){$data|ConvertTo-Json -Depth 22|Set-Content -LiteralPath "$root/$name.json"}
function MapReadPair{
    $script:h=Read host;$script:c=Read client
    if(-not $h -or -not $c){throw 'Missing probe snapshot'}
    if($h.error -or $c.error){throw "Runtime error: $($h.error) $($c.error)"}
}
function MapSamples([double]$seconds){
    $samples=[Collections.Generic.List[object]]::new()
    $until=[DateTime]::UtcNow.AddSeconds($seconds)
    do{MapReadPair;$samples.Add([pscustomobject]@{host=$h;client=$c});Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow -lt $until)
    return $samples.ToArray()
}
function MapColor($left,$right){
    if(-not $left -or -not $right){return [double]::PositiveInfinity}
    $difference=0.0
    foreach($channel in @('r','g','b','a')){$difference=[Math]::Max($difference,[Math]::Abs([double]$left.$channel-[double]$right.$channel))}
    return $difference
}
function MapRange($values){
    $range=$values|Measure-Object -Minimum -Maximum
    return [double]$range.Maximum-[double]$range.Minimum
}
function MapBody($sample,[string]$view,$ride,$history){
    $snapshot=$sample.$view
    $player=$snapshot.players|Where-Object id -eq $ride.player
    if($view -eq 'host' -or $player.owner){return $snapshot.mapBodies[$ride.index]}
    $time=$player.viewTime
    $left=$history[0].host
    $right=$history[-1].host
    foreach($item in $history){
        if($item.host.serverTime -le $time){$left=$item.host}
        if($item.host.serverTime -ge $time){$right=$item.host;break}
    }
    $blend=if($right.serverTime -gt $left.serverTime){[Math]::Max(0,[Math]::Min(1,($time-$left.serverTime)/($right.serverTime-$left.serverTime)))}else{0}
    $a=$left.mapBodies[$ride.index];$b=$right.mapBodies[$ride.index]
    return [pscustomobject]@{x=$a.x+($b.x-$a.x)*$blend;y=$a.y+($b.y-$a.y)*$blend}
}
function MapIds([uint64]$current,[uint64]$previous){
    foreach($snapshot in @($h,$c)){
        if(-not $snapshot.mapState -or $snapshot.mapObject -ne $current -or @($snapshot.mapState.ids).Count -ne 1 -or $snapshot.mapState.ids[0] -ne $current){return $false}
        if($previous -ne 0 -and $previous -in $snapshot.mapState.ids){return $false}
    }
    return $true
}
function MapClean([uint64]$current,[uint64]$previous,[string]$label){
    Await "$label removes previous map from both spawn registries" {MapIds $current $previous} 8
    Check $true "$label leaves only current BattleMap ID on both peers"
    $mapTransitions.Add(@{label=$label;previous=$previous;current=$current;host=@($h.mapState.ids);client=@($c.mapState.ids)})
    MapSave 'MapTransitions' $mapTransitions.ToArray()
}
function MapMovingNear([double]$tolerance){
    if(-not $h.mapState -or -not $c.mapState -or $h.mapState.moving.Count -ne $c.mapState.moving.Count){return $false}
    for($i=0;$i -lt $h.mapState.moving.Count;$i++){
        foreach($axis in @('x','y','z')){if([Math]::Abs($h.mapState.moving[$i].$axis-$c.mapState.moving[$i].$axis) -gt $tolerance){return $false}}
    }
    return $true
}
function MapPark{
    Send host move
    Send client move
    Eval 'var g=SSW.NetGame.Current;foreach(var p in g.Players){typeof(SSW.Health).GetMethod("SetMax",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p.Health,new object[]{10000f});p.Health.Heal(10000f);p.Drive.Teleport(g.Arena.Spawn(p.Side==1?0:1)+UnityEngine.Vector3.up*2f);p.Drive.Freeze(40f);}return true;'|Out-Null
}
function MapLoad([int]$number){
    $entry=@($mapCatalog|Where-Object number -eq $number)
    if($entry.Count -ne 1){throw "Map number $number is missing or duplicated"}
    MapReadPair
    $previous=$h.mapObject
    $loaded=Map $entry[0].index
    MapPark
    MapClean $loaded.id $previous "feature map $number"
    return $loaded
}
function MapClientHealth($values,[double]$loss){
    foreach($before in $values){
        $left=$h.players|Where-Object id -eq $before.id;$right=$c.players|Where-Object id -eq $before.id
        if(-not $left -or -not $right -or [Math]::Abs($left.hp-($before.hp-$loss)) -gt 0.01 -or [Math]::Abs($right.hp-($before.hp-$loss)) -gt 0.01){return $false}
    }
    return $true
}
$mapReadCode=@'
var g=SSW.NetGame.Current;var map=g.Arena.Map;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var bodies=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(map.GetComponentsInChildren<UnityEngine.Rigidbody2D>(true),b=>b.bodyType!=UnityEngine.RigidbodyType2D.Static));
UnityEngine.Object Ref(UnityEngine.Object o,string field)=>new UnityEditor.SerializedObject(o).FindProperty(field).objectReferenceValue;
float Float(UnityEngine.Object o,string field)=>new UnityEditor.SerializedObject(o).FindProperty(field).floatValue;
double Clock(object o,string field)=>((Unity.Netcode.NetworkVariable<double>)o.GetType().GetField(field,flags).GetValue(o)).Value;
object V(UnityEngine.Vector3 value)=>new{value.x,value.y,value.z};object C(UnityEngine.Color value)=>new{value.r,value.g,value.b,value.a};
var edges=new UnityEditor.SerializedObject(map.GetComponent<SSW.MapEdges>()).FindProperty("_edges");var edgeData=new System.Collections.Generic.List<object>();
for(int i=0;i<edges.arraySize;i++){var e=edges.GetArrayElementAtIndex(i);var shape=(UnityEngine.Collider2D)e.FindPropertyRelative("Shape").objectReferenceValue;edgeData.Add(new{name=shape.name,enabled=shape.enabled,damage=e.FindPropertyRelative("Damage").floatValue,force=V(e.FindPropertyRelative("Force").vector2Value)});}
var orbitData=new System.Collections.Generic.List<object>();
foreach(var orbit in map.GetComponentsInChildren<SSW.MapOrbit>(true)){var settings=new UnityEditor.SerializedObject(orbit);var parts=settings.FindProperty("_parts");var values=new System.Collections.Generic.List<object>();for(int i=0;i<parts.arraySize;i++){var body=(UnityEngine.Rigidbody2D)parts.GetArrayElementAtIndex(i).objectReferenceValue;values.Add(new{index=System.Array.IndexOf(bodies,body),position=V(body.position),velocity=V(body.linearVelocity),angle=body.rotation});}orbitData.Add(new{speed=Float(orbit,"_speed"),level=settings.FindProperty("_level").boolValue,parts=values});}
var floodData=new System.Collections.Generic.List<object>();
foreach(var flood in map.GetComponentsInChildren<SSW.MapFlood>(true)){var water=(UnityEngine.Transform)Ref(flood,"_water");var target=(UnityEngine.Transform)Ref(flood,"_target");floodData.Add(new{water=V(water.position),target=V(target.position),duration=Float(flood,"_duration"),startedAt=Clock(flood,"_startedAt"),animators=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(water.GetComponentsInChildren<UnityEngine.Animator>(true),a=>new{enabled=a.isActiveAndEnabled,controller=a.runtimeAnimatorController!=null,time=a.GetCurrentAnimatorStateInfo(0).normalizedTime}))});}
var breathData=new System.Collections.Generic.List<object>();
foreach(var breath in map.GetComponentsInChildren<SSW.MapBreath>(true)){var effect=(UnityEngine.ParticleSystem)Ref(breath,"_effect");breathData.Add(new{active=breath.Active,delay=Float(breath,"_delay"),duration=effect.main.duration,startedAt=Clock(breath,"_startedAt"),particles=effect.particleCount,playing=effect.isPlaying,first=C(((UnityEngine.SpriteRenderer)Ref(breath,"_first")).color),second=C(((UnityEngine.SpriteRenderer)Ref(breath,"_second")).color)});}
var shakeData=new System.Collections.Generic.List<object>();
foreach(var shake in map.GetComponentsInChildren<SSW.MapShake>(true)){var settings=new UnityEditor.SerializedObject(shake);var calls=settings.FindProperty("onShake.m_PersistentCalls.m_Calls");var bindings=new System.Collections.Generic.List<object>();for(int i=0;i<calls.arraySize;i++){var c=calls.GetArrayElementAtIndex(i);var target=c.FindPropertyRelative("m_Target").objectReferenceValue;bindings.Add(new{method=c.FindPropertyRelative("m_MethodName").stringValue,target=target==null?"":target.GetType().Name});}shakeData.Add(new{position=V(((UnityEngine.Transform)Ref(shake,"_target")).position),duration=V(settings.FindProperty("_duration").vector2Value),amount=V(settings.FindProperty("_amount").vector2Value),bindings});}
var flashData=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(map.GetComponentsInChildren<SSW.MapFlash>(true),flash=>new{at=Clock(flash,"_at"),color=C(((UnityEngine.SpriteRenderer)Ref((SSW.MapTint)Ref(flash,"_tint"),"_sprite")).color),end=C(new UnityEditor.SerializedObject(Ref(flash,"_tint")).FindProperty("_end").colorValue)}));
var sakuraData=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(map.GetComponentsInChildren<SSW.SakuraField>(true),field=>new{duration=Float(field,"_duration"),speed=Float(field,"_speed"),effect=Ref(field,"_effect")!=null,effectLife=System.Linq.Enumerable.Max(((UnityEngine.ParticleSystem)Ref(field,"_effect")).GetComponentsInChildren<UnityEngine.ParticleSystem>(true),effect=>effect.main.duration+effect.main.startDelay.constantMax+effect.main.startLifetime.constantMax),particles=((UnityEngine.ParticleSystem)Ref(field,"_petals")).particleCount,seed=((UnityEngine.ParticleSystem)Ref(field,"_petals")).randomSeed,children=field.transform.childCount}));
var areas=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(map.GetComponentsInChildren<SSW.MapArea>(true),area=>new{damage=Float(area,"_damage"),delay=Float(area,"_delay"),interval=Float(area,"_interval"),slow=Float(area,"_slow"),slowDuration=Float(area,"_slowDuration"),shape=Ref(area,"_shape")!=null,phase=Ref(area,"_phase")==null?"":Ref(area,"_phase").GetType().Name}));
return Newtonsoft.Json.JsonConvert.SerializeObject(new{id=map.NetworkObjectId,title=map.Title,now=g.Manager.ServerTime.Time,ready=map.Ready,fallY=map.FallY,spawns=new[]{V(map.Spawn(0)),V(map.Spawn(1))},mapCount=System.Linq.Enumerable.Count(g.Manager.SpawnManager.SpawnedObjectsList,o=>o.GetComponent<SSW.BattleMap>()!=null),edges=edgeData,orbits=orbitData,floods=floodData,breaths=breathData,shakes=shakeData,flashes=flashData,sakura=sakuraData,areas});
'@
function MapState{return (Eval $mapReadCode)|ConvertFrom-Json}
$mapCatalog=Eval 'var r=SSW.NetGame.Current.GetComponent<SSW.MapRotation>();return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(r.Prefabs,(map,index)=>{string path=UnityEditor.AssetDatabase.GetAssetPath(map);var match=System.Text.RegularExpressions.Regex.Match(path,@"/Map(\d+)\.prefab$");return new{index,title=map.Title,path,number=match.Success?int.Parse(match.Groups[1].Value):0};}));'
MapSave 'MapCatalog' $mapCatalog
MapSave 'MapLimits' $mapLimits.ToArray()
Check ($mapCatalog.Count -eq 17) 'seventeen registered map prefabs'
Await 'map suite starts during combat' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing' -and $h.players.Count -eq 2 -and $c.players.Count -eq 2} 15
Await 'both builds expose mapState diagnostics' {$h.mapState -and $c.mapState -and $null -ne $h.mapState.ids -and $null -ne $c.mapState.ids} 8
MapClean $h.mapObject 0 'initial map'
MapPark
if(-not $OnlyCycle -and -not $AreasOnly){
    foreach($entry in $(if($SkipLoads){@()}else{$mapCatalog})){
        MapReadPair
        $previousId=$h.mapObject
        $loaded=Map $entry.index
        MapPark
        MapClean $loaded.id $previousId "forced $($entry.title)"
        Await 'fixed orthographic camera size on both peers' {[Math]::Abs($h.cameraSize-9) -lt 0.001 -and [Math]::Abs($c.cameraSize-9) -lt 0.001} 5
        $before=MapState
        MapSave "MapLoad-$($entry.number)" $before
        Check ($before.ready -and $before.mapCount -eq 1 -and $before.edges.Count -eq 4) "$($entry.title) loaded with one network map and four edges"
        foreach($slot in 0,1){Check ([Math]::Abs($h.mapSpawns[$slot].x-$c.mapSpawns[$slot].x) -lt 0.001 -and [Math]::Abs($h.mapSpawns[$slot].y-$c.mapSpawns[$slot].y) -lt 0.001) "$($entry.title) spawn $slot identical"}
        $cameraHost=$h.cameraPosition;$cameraClient=$c.cameraPosition
        $edgeResult=Eval @'
var g=SSW.NetGame.Current;var map=g.Arena.Map;
foreach(var area in map.GetComponentsInChildren<SSW.MapArea>(true))area.enabled=false;
foreach(var effect in map.GetComponentsInChildren<UnityEngine.ParticleSystem>(true))effect.Stop(true,UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);
var edges=new UnityEditor.SerializedObject(map.GetComponent<SSW.MapEdges>()).FindProperty("_edges");var results=new System.Collections.Generic.List<object>();
for(int i=0;i<edges.arraySize;i++){
var e=edges.GetArrayElementAtIndex(i);var shape=(UnityEngine.Collider2D)e.FindPropertyRelative("Shape").objectReferenceValue;float damage=e.FindPropertyRelative("Damage").floatValue;var normal=e.FindPropertyRelative("Force").vector2Value.normalized;
foreach(var p in g.Players){var bounds=shape.bounds;var capsule=(UnityEngine.CapsuleCollider2D)p.Collider;var half=UnityEngine.Vector2.Scale(capsule.size,p.Collider.transform.lossyScale)*0.5f;float extent=UnityEngine.Mathf.Abs(normal.x)*(bounds.extents.x+half.x)+UnityEngine.Mathf.Abs(normal.y)*(bounds.extents.y+half.y);var point=(UnityEngine.Vector2)bounds.center+normal*(extent+0.016f)-(UnityEngine.Vector2)p.Collider.transform.TransformVector(p.Collider.offset);p.Drive.Teleport(point+normal*0.5f);float before=p.Health.Current;p.Drive.Teleport(point);float hit=before-p.Health.Current;if(UnityEngine.Mathf.Abs(hit-damage)>0.001f)throw new System.InvalidOperationException(map.Title+" "+shape.name+" damage="+hit+" expected="+damage);map.Touch(p);if(UnityEngine.Mathf.Abs(before-p.Health.Current-hit)>0.001f)throw new System.InvalidOperationException("Duplicate edge damage");results.Add(new{player=p.OwnerClientId,edge=shape.name,hit});p.Drive.Teleport(g.Arena.Spawn(p.Side==1?0:1)+UnityEngine.Vector3.up*2f);}}
return new{hits=results,hp=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,hp=p.Health.Current}))};
'@
        Await 'boundary HP replicated after isolated contact' {MapClientHealth $edgeResult.hp 0} 5
        Check ($edgeResult.hits.Count -eq 8) "$($entry.title) four map contacts damage both actors once"
        Check ([Math]::Abs($h.cameraPosition.x-$cameraHost.x) -lt 0.001 -and [Math]::Abs($h.cameraPosition.y-$cameraHost.y) -lt 0.001 -and [Math]::Abs($c.cameraPosition.x-$cameraClient.x) -lt 0.001 -and [Math]::Abs($c.cameraPosition.y-$cameraClient.y) -lt 0.001) "$($entry.title) camera remains fixed during contact teleports"
        $mapCases.Add([pscustomobject]@{kind='forced-load';index=$entry.index;number=$entry.number;state=$before;edges=$edgeResult;host=$h;client=$c})
        MapSave 'MapForcedLoads' $mapCases.ToArray()
    }
    if($From -le 0){foreach($peer in @('host','client')){
        MapLoad 1|Out-Null
        $orbit=MapState
        Check ($orbit.orbits.Count -eq 1 -and $orbit.orbits[0].parts.Count -eq 4 -and [Math]::Abs($orbit.orbits[0].speed-60) -lt 0.001) 'map 1 retains four sixty-degree orbit parts'
        $setup=@'
var g=SSW.NetGame.Current;var map=g.Arena.Map;var orbit=map.GetComponentInChildren<SSW.MapOrbit>();var list=new UnityEditor.SerializedObject(orbit).FindProperty("_parts");var parts=new System.Collections.Generic.List<UnityEngine.Rigidbody2D>();for(int i=0;i<list.arraySize;i++)parts.Add((UnityEngine.Rigidbody2D)list.GetArrayElementAtIndex(i).objectReferenceValue);var body=System.Linq.Enumerable.First(System.Linq.Enumerable.OrderByDescending(parts,b=>b.linearVelocity.y));var p=System.Linq.Enumerable.First(g.Players,p=>p.IsOwner==@OWNER@);var stateField=typeof(SSW.MotionView).GetField("_state",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var state=(SSW.MotionState)stateField.GetValue(p.Drive);state.FreezeTime=0f;stateField.SetValue(p.Drive,state);var shape=body.GetComponent<UnityEngine.Collider2D>();float foot=p.Body.position.y-p.Collider.bounds.min.y;p.Drive.Teleport(new UnityEngine.Vector2(shape.bounds.center.x,shape.bounds.max.y+foot+0.025f));var all=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(map.GetComponentsInChildren<UnityEngine.Rigidbody2D>(true),b=>b.bodyType!=UnityEngine.RigidbodyType2D.Static));return new{player=p.OwnerClientId,index=System.Array.IndexOf(all,body),half=shape.bounds.extents.x+p.Collider.bounds.extents.x,body=new{x=body.position.x,y=body.position.y},position=new{x=p.Body.position.x,y=p.Body.position.y}};
'@
        $ride=Eval $setup.Replace('@OWNER@',$(if($peer -eq 'host'){'true'}else{'false'}))
        MapSave "MapOrbit-$peer-Setup" $ride
        Await "$peer lands on orbit part" {
            foreach($snapshot in @($h,$c)){
                $rider=$snapshot.players|Where-Object id -eq $ride.player
                $part=$snapshot.mapBodies[$ride.index]
                if(-not $rider.grounded -or [Math]::Abs($rider.position.x-$part.x) -gt 0.8 -or [Math]::Abs(($rider.position.y-$part.y)-($ride.position.y-$ride.body.y)) -gt 0.45){return $false}
            }
            return $true
        } 4
        $settling=@(MapSamples 0.5)
        $samples=@(MapSamples 0.9)
        $history=@($settling)+@($samples)
        MapSave "MapOrbit-$peer" @{setup=$ride;settling=$settling;samples=$samples;comparison='Authoritative and local-owner bodies use the current surface. Remote observer positions use host surface history at their recorded interpolation viewTime.'}
        foreach($view in @('host','client')){
            $first=$samples[0].$view;$last=$samples[-1].$view
            $p0=$first.players|Where-Object id -eq $ride.player;$p1=$last.players|Where-Object id -eq $ride.player
            $b0=MapBody $samples[0] $view $ride $history;$b1=MapBody $samples[-1] $view $ride $history
            $bodyMoved=[Math]::Sqrt([Math]::Pow($b1.x-$b0.x,2)+[Math]::Pow($b1.y-$b0.y,2))
            $playerMoved=[Math]::Sqrt([Math]::Pow($p1.position.x-$p0.position.x,2)+[Math]::Pow($p1.position.y-$p0.position.y,2))
            $contact=@($samples|Where-Object {$s=$_.$view;$p=$s.players|Where-Object id -eq $ride.player;$b=MapBody $_ $view $ride $history;$p.grounded -and [Math]::Abs($p.position.x-$b.x) -le $ride.half -and [Math]::Abs(($p.position.y-$b.y)-($p0.position.y-$b0.y)) -lt 0.45}).Count
            Check ($bodyMoved -gt 0.15 -and $playerMoved -gt 0.1 -and $contact -ge $samples.Count*0.65) "$peer rider follows orbit without input on $view snapshots"
            $horizontal=[Math]::Abs($b1.x-$b0.x)
            $relative=[Math]::Abs(($p1.position.x-$b1.x)-($p0.position.x-$b0.x))
            Check ($horizontal -gt 0.2 -and $relative -lt 0.35) "$peer rider follows horizontal orbit within 0.35 units on $view snapshots"
        }
        Await 'orbit bodies converge across ten-hertz snapshots' {if($h.mapBodies.Count -ne $c.mapBodies.Count){return $false};for($i=0;$i -lt $h.mapBodies.Count;$i++){if([Math]::Abs($h.mapBodies[$i].x-$c.mapBodies[$i].x) -gt 0.65 -or [Math]::Abs($h.mapBodies[$i].y-$c.mapBodies[$i].y) -gt 0.65){return $false}};return $true} 4
        Check $true "$peer orbit bodies agree within 0.65 units at probe sample rate"
    }}
    if($From -le 1){foreach($number in @(8,12)){
        $draftControl=Eval 'var g=SSW.NetGame.Current;typeof(SSW.NetMatch).GetMethod("SetPhase",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(g.Match,new object[]{SSW.MatchPhase.Draft});foreach(var p in g.Players){p.Draft.Deal();if(p.Draft.Ready)throw new System.InvalidOperationException("No draft choices available for pause regression");}return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,owned=System.Linq.Enumerable.ToArray(p.Draft.Owned)}));'
        Await 'controlled draft is waiting on both peers' {$h.phase -eq 'Draft' -and $c.phase -eq 'Draft'} 5
        MapLoad $number|Out-Null
        $heldSamples=@(MapSamples 1.1)
        MapSave "MapFlood-$number-Draft" @{control='SetPhase Draft and Deal hold both choices; Restore keeps the same owned augments before Picked';owned=$draftControl;samples=$heldSamples}
        foreach($view in @('host','client')){
            $paused=@($heldSamples|Where-Object {$_.$view.phase -eq 'Draft' -and $_.$view.mapState.floods.Count -eq 1 -and $_.$view.mapState.floods[0].startedAt -lt 0}).Count
            $heights=@($heldSamples|ForEach-Object {$_.$view.mapState.floods[0].height})
            Check ($paused -eq $heldSamples.Count -and (MapRange $heights) -lt 0.001) "map $number flood remains still with no start clock throughout draft on $view"
        }
        $draftRestore=Eval 'var g=SSW.NetGame.Current;foreach(var p in g.Players)p.Draft.Restore(System.Linq.Enumerable.ToArray(p.Draft.Owned));g.Match.Picked();return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,owned=System.Linq.Enumerable.ToArray(p.Draft.Owned)}));'
        foreach($before in $draftControl){
            $after=$draftRestore|Where-Object id -eq $before.id
            Check (($before.owned -join ',') -eq ($after.owned -join ',')) "map $number draft regression preserves actor $($before.id) owned augments"
        }
        Await 'flood draft resumes into combat' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing'} 8
        MapPark
        $f0=MapState
        Check ($f0.floods.Count -eq 1 -and $f0.areas.Count -eq 1 -and $f0.areas[0].shape -and [Math]::Abs($f0.floods[0].duration-50) -lt 0.001) "map $number flood bindings and source duration"
        Await 'both flood clocks initialized' {$h.mapState.floods.Count -eq 1 -and $c.mapState.floods.Count -eq 1 -and $h.mapState.floods[0].startedAt -ge 0 -and [Math]::Abs($h.mapState.floods[0].startedAt-$c.mapState.floods[0].startedAt) -lt 0.001} 5
        $riseSamples=@(MapSamples 1.1);$f1=MapState
        MapSave "MapFlood-$number-Rise" @{before=$f0;after=$f1;samples=$riseSamples}
        foreach($view in @('host','client')){
            $water0=$riseSamples[0].$view.mapState.floods[0];$water1=$riseSamples[-1].$view.mapState.floods[0]
            Check ($water1.height -gt $water0.height+0.01 -and $water1.height -le $water1.target+0.01) "map $number water rises naturally on $view"
            $animated=$false
            for($i=0;$i -lt $water1.animators.Count;$i++){
                $times=@($riseSamples|ForEach-Object {$_.$view.mapState.floods[0].animators[$i].time})
                if($water1.animators[$i].enabled -and (MapRange $times) -gt 0.01){$animated=$true}
            }
            Check $animated "map $number water Animator advances on $view"
        }
        $floodAligned=$true
        foreach($sample in $riseSamples){
            $left=$sample.host.mapState.floods[0];$right=$sample.client.mapState.floods[0]
            if([Math]::Abs($left.startedAt-$right.startedAt) -ge 0.001 -or [Math]::Abs($left.target-$right.target) -ge 0.001 -or [Math]::Abs($left.duration-$right.duration) -ge 0.001 -or [Math]::Abs($left.height-$right.height) -ge 0.35){$floodAligned=$false}
        }
        Check $floodAligned "map $number flood clocks and targets match; sampled heights within 0.35 units"
        Await 'server clock permits a nonnegative controlled flood start' {$h.serverTime -gt $h.mapState.floods[0].duration+1} 55
        Eval 'var g=SSW.NetGame.Current;var flood=g.Arena.Map.GetComponentInChildren<SSW.MapFlood>();var clock=(Unity.Netcode.NetworkVariable<double>)typeof(SSW.MapFlood).GetField("_startedAt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(flood);clock.Value=g.Manager.ServerTime.Time-new UnityEditor.SerializedObject(flood).FindProperty("_duration").floatValue;return true;'|Out-Null
        Await 'advanced flood height and clock reach both peers' {$left=$h.mapState.floods[0];$right=$c.mapState.floods[0];[Math]::Abs($left.startedAt-$right.startedAt) -lt 0.001 -and [Math]::Abs($left.height-$left.target) -lt 0.01 -and [Math]::Abs($right.height-$right.target) -lt 0.01} 5
        Check $true "map $number controlled full-height flood replicated"
        $fullFlood=@{host=$h;client=$c}
        $wet=Eval @'
var g=SSW.NetGame.Current;var map=g.Arena.Map;var area=map.GetComponentInChildren<SSW.MapArea>();var settings=new UnityEditor.SerializedObject(area);var shape=(UnityEngine.Collider2D)settings.FindProperty("_shape").objectReferenceValue;area.enabled=false;foreach(var p in g.Players){float low=UnityEngine.Mathf.Max(map.FallY+2f,shape.bounds.min.y+p.Collider.bounds.extents.y+0.2f);float high=UnityEngine.Mathf.Min(map.Bounds.max.y-2f,shape.bounds.max.y-p.Collider.bounds.extents.y-0.2f);if(low>high)throw new System.InvalidOperationException("No safe flood contact height");p.Drive.Teleport(new UnityEngine.Vector2(map.Bounds.center.x+p.Side*2f,(low+high)*0.5f));p.Drive.Freeze(20f);}UnityEngine.Physics2D.SyncTransforms();foreach(var p in g.Players)if(p.Collider.Distance(shape).distance>0f)throw new System.InvalidOperationException("Test player is not inside flood volume");area.enabled=true;return new{now=g.Manager.ServerTime.Time,damage=settings.FindProperty("_damage").floatValue,delay=settings.FindProperty("_delay").floatValue,interval=settings.FindProperty("_interval").floatValue,hp=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,hp=p.Health.Current}))};
'@
        $damage=if($number -eq 8){10}else{15}
        Check ([Math]::Abs($wet.delay-3) -lt 0.001 -and [Math]::Abs($wet.interval-1) -lt 0.001 -and [Math]::Abs($wet.damage-$damage) -lt 0.001) "map $number immersion uses three-second grace and $damage damage per second"
        $grace=@(MapSamples 1.4);$safe=$true
        foreach($sample in $grace){foreach($view in @('host','client')){foreach($before in $wet.hp){if([Math]::Abs(($sample.$view.players|Where-Object id -eq $before.id).hp-$before.hp) -gt 0.01){$safe=$false}}}}
        Check $safe "map $number immersion grace preserves both actors HP"
        Await 'first immersion damage replicated' {MapClientHealth $wet.hp $wet.damage} 5
        $firstTick=@{host=$h;client=$c}
        Await 'second immersion damage replicated' {MapClientHealth $wet.hp ($wet.damage*2)} 3
        $secondTick=@{host=$h;client=$c}
        $dry=Eval 'var g=SSW.NetGame.Current;var area=g.Arena.Map.GetComponentInChildren<SSW.MapArea>();var shape=(UnityEngine.Collider2D)new UnityEditor.SerializedObject(area).FindProperty("_shape").objectReferenceValue;foreach(var p in g.Players){p.Drive.Teleport(new UnityEngine.Vector2(g.Arena.Map.Bounds.center.x+p.Side*2f,shape.bounds.max.y+3f));p.Drive.Freeze(10f);}return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,hp=p.Health.Current}));'
        $exitSamples=@(MapSamples 1.3);MapReadPair
        Check (MapClientHealth $dry 0) "map $number leaving water stops subsequent damage on both peers"
        MapSave "MapFlood-$number-Damage" @{control='Shared flood clock advanced to source duration before immersion; damage timers unchanged';fullHeight=$fullFlood;setup=$wet;grace=$grace;first=$firstTick;second=$secondTick;exit=$exitSamples}
    }}
    if($From -le 2){
    MapLoad 3|Out-Null
    $breathStart=MapState
    Check ($breathStart.breaths.Count -eq 1 -and $breathStart.areas[0].phase -eq 'MapBreath') 'map 3 breath controls its damage phase'
    Await 'breath clocks and timings reach both peers' {$left=$h.mapState.breaths[0];$right=$c.mapState.breaths[0];$h.mapState.breaths.Count -eq 1 -and $c.mapState.breaths.Count -eq 1 -and $left.startedAt -ge 0 -and [Math]::Abs($left.startedAt-$right.startedAt) -lt 0.001 -and [Math]::Abs($left.delay-$right.delay) -lt 0.001 -and [Math]::Abs($left.duration-$right.duration) -lt 0.001} 5
    $breathPeriod=$breathStart.breaths[0].delay+$breathStart.breaths[0].duration
    Check ($breathPeriod -gt 0 -and $breathPeriod -lt 30) 'map 3 breath has a bounded native period'
    $breathSamples=@(MapSamples ($breathPeriod+1.2))
    MapSave 'MapBreathCycle' @{settings=$breathStart;samples=$breathSamples}
    foreach($view in @('host','client')){
        $emitted=$false;$ended=$false;$changed=$false
        $initial=$breathSamples[0].$view.mapState.breaths[0]
        foreach($sample in $breathSamples){
            $value=$sample.$view.mapState.breaths[0]
            if($value.active -and $value.playing -and $value.particles -gt 0){$emitted=$true}
            if($emitted -and -not $value.active -and -not $value.playing){$ended=$true}
            if((MapColor $initial.first $value.first) -gt 0.03 -or (MapColor $initial.second $value.second) -gt 0.03){$changed=$true}
        }
        Check ($emitted -and $ended -and $changed) "map 3 native breath warning, emission and stop observed on $view"
    }
    $breathAligned=0
    foreach($sample in $breathSamples){
        $left=$sample.host.mapState.breaths[0];$right=$sample.client.mapState.breaths[0]
        if([Math]::Abs($left.startedAt-$right.startedAt) -lt 0.001 -and $left.active -eq $right.active -and (MapColor $left.first $right.first) -le 0.25 -and (MapColor $left.second $right.second) -le 0.25){$breathAligned++}
    }
    Check ($breathAligned -ge $breathSamples.Count*0.7) 'map 3 breath phases and colors agree within 0.25 on at least seventy percent of ten-hertz pairs'
    Await 'both peers enter an emitting breath phase' {$h.mapState.breaths[0].active -and $c.mapState.breaths[0].active -and $h.mapState.breaths[0].particles -gt 0 -and $c.mapState.breaths[0].particles -gt 0} ($breathPeriod+2)
    $breath=@{host=$h;client=$c}
    $burn=Eval 'var g=SSW.NetGame.Current;var area=g.Arena.Map.GetComponentInChildren<SSW.MapArea>();var shape=(UnityEngine.Collider2D)new UnityEditor.SerializedObject(area).FindProperty("_shape").objectReferenceValue;foreach(var p in g.Players){var point=(UnityEngine.Vector2)shape.bounds.center;point.x+=p.Side*0.15f;point.y=UnityEngine.Mathf.Max(point.y,g.Arena.FallY+1.5f);p.Drive.Teleport(point);p.Drive.Freeze(15f);}UnityEngine.Physics2D.SyncTransforms();foreach(var p in g.Players)if(p.Collider.Distance(shape).distance>0f)throw new System.InvalidOperationException("Breath target misses damage volume");return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,hp=p.Health.Current}));'
    Await 'active breath damages both actors and replicates HP' {foreach($p in $burn){if(($h.players|Where-Object id -eq $p.id).hp -ge $p.hp -or ($c.players|Where-Object id -eq $p.id).hp -ge $p.hp){return $false}};return $true} 12
    Check $true 'map 3 native breath area damages both actors'
    MapSave 'MapBreath' @{before=$breathStart;active=$breath;cycle=$breathSamples;damage=$burn;host=$h;client=$c}
    }
    if($From -le 3){
    MapLoad 14|Out-Null
    $shake0=MapState
    Check ($shake0.shakes.Count -eq 4 -and $shake0.areas.Count -eq 4 -and $shake0.breaths.Count -eq 0) 'map 14 has four shake and slow areas'
    $shakeSamples=@(MapSamples 0.9);$shake1=MapState;$moved=$false
    MapSave 'MapShakeMovement' @{before=$shake0;after=$shake1;samples=$shakeSamples}
    for($i=0;$i -lt 4;$i++){if([Math]::Abs($shake1.shakes[$i].position.x-$shake0.shakes[$i].position.x) -gt 0.005){$moved=$true}}
    Check $moved 'map 14 server platforms shake using source settings'
    foreach($view in @('host','client')){
        Check ($shakeSamples[0].$view.mapState.moving.Count -eq 4) "map 14 exposes all four moving targets on $view"
        for($i=0;$i -lt 4;$i++){
            $positions=@($shakeSamples|ForEach-Object {$_.$view.mapState.moving[$i].x})
            Check ((MapRange $positions) -gt 0.005) "map 14 target $i moves on $view"
        }
    }
    Await 'map 14 moving targets agree across ten-hertz snapshots' {MapMovingNear 0.65} 4
    Check $true 'map 14 host/client moving targets agree within 0.65 units'
    $slow=Eval 'var g=SSW.NetGame.Current;var area=g.Arena.Map.GetComponentInChildren<SSW.MapArea>();var settings=new UnityEditor.SerializedObject(area);var shape=(UnityEngine.Collider2D)settings.FindProperty("_shape").objectReferenceValue;foreach(var p in g.Players){float foot=p.Body.position.y-p.Collider.bounds.min.y;p.Drive.Teleport(new UnityEngine.Vector2(shape.bounds.center.x+p.Side*0.15f,shape.bounds.max.y+foot+0.016f));p.Drive.Freeze(5f);}return new{slow=settings.FindProperty("_slow").floatValue,duration=settings.FindProperty("_slowDuration").floatValue,players=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,speed=p.Stats.MoveSpeed}))};'
    Await 'shake contact slows both actors on both peers' {foreach($p in $slow.players){if(($h.players|Where-Object id -eq $p.id).speed -ge $p.speed*0.9 -or ($c.players|Where-Object id -eq $p.id).speed -ge $p.speed*0.9){return $false}};return $true} 3
    Check $true 'map 14 contact slowdown reaches both peers'
    MapSave 'MapShake' @{before=$shake0;after=$shake1;contact=$slow;samples=$shakeSamples;host=$h;client=$c}
    }
    if($From -le 4){
    MapLoad 9|Out-Null
    $flash0=MapState
    Check ($flash0.flashes.Count -eq 1 -and @($flash0.shakes[0].bindings|Where-Object {$_.method -eq 'Play' -and $_.target -eq 'MapFlash'}).Count -eq 1) 'map 9 shake event targets MapFlash.Play'
    Await 'fireball particle identities shared' {$h.embers.Count -gt 0 -and $c.embers.Count -gt 0 -and @($c.embers|Where-Object {$_.id -in $h.embers.id}).Count -gt 0} 8
    Check $true 'map 9 native fireballs visible with shared IDs on both peers'
    Await 'both peers receive initial flash timestamp' {$h.mapState.flashes.Count -eq 1 -and $c.mapState.flashes.Count -eq 1 -and $h.mapState.flashes[0].at -ge 0 -and [Math]::Abs($h.mapState.flashes[0].at-$c.mapState.flashes[0].at) -lt 0.001} 5
    $flashSamples=@(MapSamples ($flash0.shakes[0].duration.y+1.2))
    MapSave 'MapFlashSamples' @{settings=$flash0;samples=$flashSamples}
    foreach($view in @('host','client')){
        $times=@($flashSamples|ForEach-Object {$_.$view.mapState.flashes[0].at})
        $initial=$flashSamples[0].$view.mapState.flashes[0].color
        $changed=@($flashSamples|Where-Object {(MapColor $initial $_.$view.mapState.flashes[0].color) -gt 0.03}).Count
        Check ((MapRange $times) -gt 0.1 -and $changed -gt 0) "map 9 native shake event retriggers a visible flash on $view"
    }
    $flashAligned=0
    foreach($sample in $flashSamples){
        $left=$sample.host.mapState.flashes[0];$right=$sample.client.mapState.flashes[0]
        if([Math]::Abs($left.at-$right.at) -lt 0.001 -and (MapColor $left.color $right.color) -le 0.2){$flashAligned++}
    }
    Check ($flashAligned -ge $flashSamples.Count*0.7) 'map 9 shared flash timestamps and colors agree within 0.2 on at least seventy percent of ten-hertz pairs'
    Await 'native flash returns toward its source end color on both peers' {(MapColor $h.mapState.flashes[0].color $flash0.flashes[0].end) -lt 0.02 -and (MapColor $c.mapState.flashes[0].color $flash0.flashes[0].end) -lt 0.02} 5
    Check $true 'map 9 flash fades to source end color on both peers'
    MapSave 'MapFlash' @{before=$flash0;samples=$flashSamples;host=$h;client=$c}
    }
    $particleCode=@'
var g=SSW.NetGame.Current;var map=g.Arena.Map;var source=map.GetComponentInChildren<@TYPE@>();var settings=new UnityEditor.SerializedObject(source);var effect=(UnityEngine.ParticleSystem)settings.FindProperty("@FIELD@").objectReferenceValue;effect.Stop(true,UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);var emission=effect.emission;emission.enabled=false;var drift=effect.velocityOverLifetime;drift.enabled=false;var main=effect.main;main.gravityModifier=0f;foreach(var p in g.Players){p.Drive.Teleport(new UnityEngine.Vector2(map.Bounds.center.x+p.Side*6f,map.Bounds.center.y+4f));p.Drive.Freeze(5f);}UnityEngine.Physics2D.SyncTransforms();var scale=main.scalingMode==UnityEngine.ParticleSystemScalingMode.Hierarchy?effect.transform.lossyScale:main.scalingMode==UnityEngine.ParticleSystemScalingMode.Local?effect.transform.localScale:UnityEngine.Vector3.one;var space=UnityEngine.Matrix4x4.TRS(effect.transform.position,effect.transform.rotation,scale).inverse;effect.Play();System.Collections.IEnumerator Emit(){yield return new UnityEngine.WaitForSeconds(1f);if(map==null||!map.IsSpawned)yield break;foreach(var p in g.Players){var point=(UnityEngine.Vector3)p.Body.position+p.Collider.transform.TransformVector(p.Collider.offset)+UnityEngine.Vector3.left*2f;var velocity=UnityEngine.Vector3.right*12f;if(effect.main.simulationSpace==UnityEngine.ParticleSystemSimulationSpace.Local){point=space.MultiplyPoint3x4(point);velocity=space.MultiplyVector(velocity);}effect.Emit(new UnityEngine.ParticleSystem.EmitParams{position=point,velocity=velocity,startLifetime=5f,startSize=0.1f,applyShapeToPosition=false},1);}}g.StartCoroutine(Emit());return new{now=g.Manager.ServerTime.Time,emitAfter=1f,players=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(g.Players,p=>new{id=p.OwnerClientId,hp=p.Health.Current,speed=p.Stats.MoveSpeed}))};
'@
    if($From -le 4){
    $lava=Eval $particleCode.Replace('@TYPE@','SSW.Lava').Replace('@FIELD@','_effect')
    Await 'controlled native fireballs damage both actors once' {MapClientHealth $lava.players 20} 4
    Check $true 'map 9 actual particle collision applies twenty damage to both actors'
    Await 'collided fireballs clear on both peers' {$h.embers.Count -eq 0 -and $c.embers.Count -eq 0} 4
    Check $true 'map 9 collided particle removal replicated'
    MapSave 'MapLavaDamage' @{control='Native particles emitted toward frozen actors; collision handler unchanged';setup=$lava;host=$h;client=$c}
    }
    MapLoad 13|Out-Null
    $sakura=MapState
    Check ($sakura.sakura.Count -eq 1 -and $sakura.sakura[0].effect -and [Math]::Abs($sakura.sakura[0].speed-1.2) -lt 0.001 -and [Math]::Abs($sakura.sakura[0].duration-0.5) -lt 0.001) 'map 13 current Sakura speed and duration bound'
    Await 'native petals visible with common seed on both peers' {$h.mapState.sakura.Count -eq 1 -and $c.mapState.sakura.Count -eq 1 -and $h.mapState.sakura[0].particles -gt 0 -and $c.mapState.sakura[0].particles -gt 0 -and $h.mapState.sakura[0].seed -eq $c.mapState.sakura[0].seed} 8
    Check $true 'map 13 natural petals use the same seed and are visible on both peers'
    $petalsVisible=@{host=$h;client=$c}
    Eval 'var field=SSW.NetGame.Current.Arena.Map.GetComponentInChildren<SSW.SakuraField>();var petals=(UnityEngine.ParticleSystem)new UnityEditor.SerializedObject(field).FindProperty("_petals").objectReferenceValue;var emission=petals.emission;emission.enabled=false;petals.Stop(true,UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);return true;'|Out-Null
    $effectWait=[Math]::Max(1.0,[double]$sakura.sakura[0].effectLife+0.5)
    Check ($effectWait -lt 30) 'map 13 impact effect has a bounded source lifetime'
    $quiet=@(MapSamples $effectWait)
    $petalBase=@{host=$h.mapState.sakura[0].children;client=$c.mapState.sakura[0].children}
    Check ($petalBase.host -eq $petalBase.client) 'map 13 impact effect children settle equally before controlled contact'
    $petal=Eval $particleCode.Replace('@TYPE@','SSW.SakuraField').Replace('@FIELD@','_petals')
    $petalSamples=@(MapSamples 3)
    MapSave 'MapSakuraContact' @{baseline=$petalBase;setup=$petal;samples=$petalSamples}
    foreach($view in @('host','client')){
        foreach($player in $petal.players){
            $peak=($petalSamples|ForEach-Object {($_.$view.players|Where-Object id -eq $player.id).speed}|Measure-Object -Maximum).Maximum
            Check ($peak -ge $player.speed*2.15) "map 13 native petal collision boosts actor $($player.id) on $view"
        }
        $peakChildren=($petalSamples|ForEach-Object {$_.$view.mapState.sakura[0].children}|Measure-Object -Maximum).Maximum
        Check ($peakChildren -gt $petalBase.$view) "map 13 native petal collision creates map-owned impact effects on $view"
    }
    Await 'petal buffs expire and impact effects are destroyed on both peers' {
        if($h.mapState.sakura[0].children -ne $petalBase.host -or $c.mapState.sakura[0].children -ne $petalBase.client){return $false}
        foreach($player in $petal.players){foreach($snapshot in @($h,$c)){if([Math]::Abs(($snapshot.players|Where-Object id -eq $player.id).speed-$player.speed) -gt 0.01){return $false}}}
        return $true
    } ($effectWait+3)
    Check $true 'map 13 speed returns to base and impact children return to baseline on both peers'
    MapSave 'MapSakura' @{settings=$sakura;native=$petalsVisible;quiet=$quiet;baseline=$petalBase;control='Native petals are first observed, then stopped. Two delayed native particles test collision, buff and impact cleanup without direct buff calls.';setup=$petal;samples=$petalSamples;ended=@{host=$h;client=$c}}
}
if($AreasOnly -or -not $OnlyCycle){. "$PSScriptRoot/AreasRun.ps1"}
if(-not $SkipCycle){
    MapReadPair
    Check ($h.firstSets -eq 0 -and $h.secondSets -eq 0 -and $h.firstWins -eq 0 -and $h.secondWins -eq 0) 'death-cycle test starts from untouched score'
    $cycle=[Collections.Generic.List[object]]::new();$seen=[Collections.Generic.HashSet[string]]::new()
    $beforeBegin=$h.mapObject
    $firstMap=Eval 'var g=SSW.NetGame.Current;g.GetComponent<SSW.MapRotation>().Begin();foreach(var p in g.Players){p.Drive.Teleport(g.Arena.Spawn(p.Side==1?0:1)+UnityEngine.Vector3.up*2f);p.Drive.Freeze(40f);}return new{id=g.Arena.Map.NetworkObjectId,title=g.Arena.Map.Title};'
    Await 'fresh random bag first map on both peers' {$h.mapObject -eq $firstMap.id -and $c.mapObject -eq $firstMap.id} 10
    MapPark
    MapClean $firstMap.id $beforeBegin 'random bag Begin'
    $seen.Add($firstMap.title)|Out-Null
    $cycle.Add(@{step=0;map=$firstMap;kind='MapRotation.Begin';host=$h;client=$c})
    for($round=1;$round -le 16;$round++){
        MapReadPair
        $winner=if($round%2 -eq 1){$h.first}else{$h.second}
        $oldId=$h.mapObject;$oldPlayers=@($h.players.objectId)
        $kill=Eval ('var g=SSW.NetGame.Current;var winner='+[string]$winner+'ul;var loser=System.Linq.Enumerable.First(g.Players,p=>p.OwnerClientId!=winner);var result=new{loser=loser.OwnerClientId,objectId=loser.NetworkObjectId,hp=loser.Health.Current};loser.Health.ReceiveDamage(new SSW.DamageRequest(null,loser.Health.Max*10f,SSW.DamageTag.Environment|SSW.DamageTag.IgnoreDefense));if(loser.Health.Current>0f)throw new System.InvalidOperationException("Lethal test damage did not kill the actor");return result;')
        Await 'real death advances map through ResetRound' {$h.mapObject -ne $oldId -and $h.mapObject -eq $c.mapObject -and $h.players.Count -eq 2 -and $c.players.Count -eq 2 -and $h.phase -in @('Draft','Countdown','Playing')} 18
        $cleanup=Eval ('var g=SSW.NetGame.Current;if(g.Manager.SpawnManager.SpawnedObjects.ContainsKey('+[string]$oldId+'ul))throw new System.InvalidOperationException("Previous map remains spawned");foreach(ulong id in new ulong[]{'+(($oldPlayers|ForEach-Object {[string]$_+'ul'}) -join ',')+'})if(g.Manager.SpawnManager.SpawnedObjects.ContainsKey(id))throw new System.InvalidOperationException("Previous round player remains spawned");if(g.State.Phase==SSW.MatchPhase.Draft){foreach(var p in g.Players)p.Draft.Restore(System.Linq.Enumerable.ToArray(p.Draft.Owned));g.Match.Picked();}return new{map=g.Arena.Map.Title,id=g.Arena.Map.NetworkObjectId,winner=g.State.Winner,remaining=((SSW.MapBag)typeof(SSW.MapRotation).GetField("_bag",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(g.GetComponent<SSW.MapRotation>())).Remaining,mapCount=System.Linq.Enumerable.Count(g.Manager.SpawnManager.SpawnedObjectsList,o=>o.GetComponent<SSW.BattleMap>()!=null)};')
        Await 'next round resumes without changing owned augments' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing'} 18
        MapPark
        MapReadPair
        MapClean $cleanup.id $oldId "death $round"
        Check ($cleanup.winner -eq $winner -and $cleanup.mapCount -eq 1) "death $round keeps alternating winner and one host map"
        Check ($seen.Add([string]$cleanup.map)) "death $round selects a previously unused map from random bag"
        Check ($h.firstSets -lt 4 -and $h.secondSets -lt 4) "death $round avoids completing the match"
        foreach($old in $oldPlayers){Check (-not($old -in $c.players.objectId)) "death $round replaces client player object $old"}
        Check ([Math]::Abs($h.cameraSize-9) -lt 0.001 -and [Math]::Abs($c.cameraSize-9) -lt 0.001) "death $round keeps camera size nine"
        $cycle.Add(@{step=$round;kind='actual-lethal-damage';victim=$kill;next=$cleanup;host=$h;client=$c})
        MapSave 'MapDeathCycle' $cycle.ToArray()
    }
    Check ($seen.Count -eq 17 -and $cleanup.remaining -eq 0) 'seventeen unique maps consumed by sixteen actual deaths after Begin'
}else{$mapLimits.Add('Actual-death random-bag cycle explicitly skipped; forced loads do not validate natural rotation.')}
MapSave 'MapLimits' $mapLimits.ToArray()
MapSave 'MapRefreshResult' @{checks=$checks.Count;forcedLoads=$mapCases.Count;mapTransitions=$mapTransitions.Count;deathCycle=if($SkipCycle){'skipped'}else{'Begin plus sixteen real deaths'};limitations=$mapLimits.ToArray();host=$h;client=$c}
Write-Output "PASS map runtime checks with explicit observation limits: $root"

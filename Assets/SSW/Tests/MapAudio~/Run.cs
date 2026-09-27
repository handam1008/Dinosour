var game=SSW.NetGame.Current;
if(!UnityEditor.EditorApplication.isPlaying||game==null||!game.Manager.IsServer||game.Players.Count!=2||!game.CanFight)
    throw new System.InvalidOperationException("An editor host and Windows client must be playing");
string root="@ROOT@";
const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var checks=new System.Collections.Generic.List<string>();
var evidence=new System.Collections.Generic.List<object>();
string stage="setup",error=null;
string[] names={"Map_Breath","Map_VolcanoErupt","Map_SakuraPickup","Map_TimeStop","Map_BoundaryHit"};
var began=UnityEngine.Time.realtimeSinceStartupAsDouble;
T Field<T>(object obj,string field)=>(T)obj.GetType().GetField(field,flags).GetValue(obj);
void Save(string name,bool success)=>System.IO.File.WriteAllText(root+"/"+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
    success,stage,error,count=checks.Count,checks,evidence,seconds=UnityEngine.Time.realtimeSinceStartupAsDouble-began,
    scope="Same PC Editor host and Windows client; 60ms delay and 10ms jitter each direction; actual particle collisions, emitted particles and boundary contacts; accelerated replicated clocks"
},Newtonsoft.Json.Formatting.Indented));
void Check(bool value,string label){if(!value)throw new System.InvalidOperationException(stage+": "+label);checks.Add(stage+": "+label);}
Newtonsoft.Json.Linq.JObject Snapshot(string peer){try{return Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(root+"/"+peer+".json"));}catch(System.IO.IOException){return null;}catch(Newtonsoft.Json.JsonReaderException){return null;}}
int Count(string peer,string name){var items=Snapshot(peer)?["audio"]?["cues"];if(items!=null)foreach(var item in items)if((string)item["name"]==name)return (int)item["count"];return 0;}
async System.Threading.Tasks.Task Wait(float seconds){double until=UnityEngine.Time.realtimeSinceStartupAsDouble+seconds;while(UnityEngine.Time.realtimeSinceStartupAsDouble<until){if(!UnityEditor.EditorApplication.isPlaying||game==null)throw new System.InvalidOperationException("Play stopped");await System.Threading.Tasks.Task.Delay(16);}}
async System.Threading.Tasks.Task Until(System.Func<bool> condition,float timeout,string label){double until=UnityEngine.Time.realtimeSinceStartupAsDouble+timeout;while(!condition()){if(UnityEngine.Time.realtimeSinceStartupAsDouble>=until)throw new System.TimeoutException(stage+": "+label);await Wait(.02f);}}
async System.Threading.Tasks.Task Send(string peer,string op,int value=0,float x=0,float y=0){
    int seq=(int)Snapshot(peer)["seq"]+1;
    System.IO.File.WriteAllText(root+"/"+peer+".cmd.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{seq,op,value,x,y}));
    await Until(()=>(int?)Snapshot(peer)?["seq"]>=seq,5f,"command "+peer+" "+op);
}
async System.Threading.Tasks.Task Reset(){await Wait(.25f);foreach(string peer in new[]{"host","client"}){await Send(peer,"sound",5);await Send(peer,"sound",0);}}
async System.Threading.Tasks.Task Audio(string cue,int count,bool output=true){
    if(count>0)await Until(()=>Count("host",cue)>=count&&Count("client",cue)>=count,5f,"sound delivery "+cue);
    await Wait(.35f);
    foreach(string name in names){int expected=name==cue?count:0;Check(Count("host",name)==expected&&Count("client",name)==expected,name+" exact count "+expected+" host="+Count("host",name)+" client="+Count("client",name));}
    if(count>0&&output){await Until(()=>(float?)Snapshot("host")?["audio"]?["effectsPeak"]>.001f&&(float?)Snapshot("client")?["audio"]?["effectsPeak"]>.001f,3f,"audible samples");Check(true,"both peers have nonzero audio output");}
    evidence.Add(new{stage,cue,count,host=Snapshot("host"),client=Snapshot("client")});Save("Progress",false);
}
void Phase(SSW.MatchPhase phase)=>typeof(SSW.NetMatch).GetMethod("SetPhase",flags).Invoke(game.Match,new object[]{phase});
void Clock(object item,double age)=>Field<Unity.Netcode.NetworkVariable<double>>(item,"_startedAt").Value=game.Manager.ServerTime.Time-age;
UnityEngine.Vector2 Impulse(SSW.NetPlayer player){var motion=Field<SSW.MotionState>(player.Drive,"_state");return new UnityEngine.Vector2(motion.External,motion.Velocity.y);}
async System.Threading.Tasks.Task Map(string name){
    stage=name;Save("Progress",false);
    var rotation=game.GetComponent<SSW.MapRotation>();int index=-1;for(int i=0;i<rotation.Prefabs.Count;i++)if(rotation.Prefabs[i].name==name)index=i;
    Check(index>=0,"map registered");typeof(SSW.MapRotation).GetField("_index",flags).SetValue(rotation,index);rotation.Restart(false);
    ulong id=game.Arena.Map.NetworkObjectId;
    typeof(SSW.BattleMap).GetField("_fallY",flags).SetValue(game.Arena.Map,-1000f);
    await Until(()=>(ulong?)Snapshot("client")?["mapObject"]==id,8f,"map received");
    foreach(string peer in new[]{"host","client"})await Send(peer,"isolate");
    foreach(var p in game.Players){p.Buffs.SetBaseHealth(10000f);p.Health.Heal(10000f);p.Drive.Teleport(new UnityEngine.Vector2(p.Side*3f,3f));}
    await Wait(.6f);
    await Reset();
}
void Emit(UnityEngine.ParticleSystem ps,uint seed,UnityEngine.Vector3 position,UnityEngine.Vector3 velocity,float lifetime){
    var main=ps.main;
    if(main.simulationSpace!=UnityEngine.ParticleSystemSimulationSpace.World){position=ps.transform.InverseTransformPoint(position);velocity=ps.transform.InverseTransformVector(velocity);}
    ps.Emit(new UnityEngine.ParticleSystem.EmitParams{position=position,velocity=velocity,startLifetime=lifetime,startSize=.12f,randomSeed=seed},1);
}
async System.Threading.Tasks.Task Run(){
try{
    foreach(string peer in new[]{"host","client"}){await Send(peer,"sound",1,1f,0f);await Send(peer,"sound",2,1f);await Send(peer,"sound",4);}
    await Until(()=>game.Manager.ServerTime.Time>30d,32f,"clock fixture has a positive origin");
    await Map("Map03");
    var breath=game.Arena.Map.GetComponentInChildren<SSW.MapBreath>();
    float delay=Field<float>(breath,"_delay"),duration=Field<UnityEngine.ParticleSystem>(breath,"_effect").main.duration;
    Clock(breath,delay-.2f);
    stage="breath first cycle";await Audio("Map_Breath",1);
    Check(breath.Active&&(bool?)Snapshot("client")?["mapState"]?["breaths"]?[0]?["active"]==true,"breath visuals active on both peers");
    await Wait(.3f);await Audio("Map_Breath",1);
    stage="breath next cycle";Clock(breath,delay+duration+delay+.1f);await Audio("Map_Breath",2);
    stage="breath outside combat";Phase(SSW.MatchPhase.Waiting);await Reset();Clock(breath,2*(delay+duration)+delay+.1f);await Audio(null,0);Phase(SSW.MatchPhase.Playing);

    await Map("Map09");
    var lava=game.Arena.Map.GetComponentInChildren<SSW.Lava>();var fire=Field<UnityEngine.ParticleSystem>(lava,"_effect");
    var collision=fire.collision;collision.enabled=false;var emission=fire.emission;emission.enabled=false;fire.Play();
    stage="volcano one new fireball";Emit(fire,81001,fire.transform.position,UnityEngine.Vector3.zero,2f);await Audio("Map_VolcanoErupt",1);
    stage="volcano existing snapshots";await Audio("Map_VolcanoErupt",1);
    stage="volcano burst";Emit(fire,81002,fire.transform.position,UnityEngine.Vector3.zero,1f);Emit(fire,81003,fire.transform.position,UnityEngine.Vector3.zero,1f);await Audio("Map_VolcanoErupt",3);
    await Wait(2f);stage="volcano retired seed reused";Emit(fire,81001,fire.transform.position,UnityEngine.Vector3.zero,1f);await Audio("Map_VolcanoErupt",4);
    stage="volcano outside combat";Phase(SSW.MatchPhase.Waiting);await Reset();Emit(fire,81004,fire.transform.position,UnityEngine.Vector3.zero,3f);await Audio(null,0);
    stage="volcano no delayed replay";Phase(SSW.MatchPhase.Playing);await Audio(null,0);
    fire.Stop(true,UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);await Wait(.15f);
    stage="volcano natural emission";emission=fire.emission;emission.enabled=true;fire.Play();
    await Until(()=>Count("host","Map_VolcanoErupt")>0,12f,"natural fireball");emission=fire.emission;emission.enabled=false;
    int natural=Count("host","Map_VolcanoErupt");await Audio("Map_VolcanoErupt",natural);Check(fire.particleCount>0,"native emitter has live fireballs");

    foreach(string name in new[]{"Map13","Map16"}){
        await Map(name);
        var sakura=game.Arena.Map.GetComponentInChildren<SSW.SakuraField>();var petals=Field<UnityEngine.ParticleSystem>(sakura,"_petals");
        emission=petals.emission;emission.enabled=false;petals.Play();
        foreach(var p in game.Players){
            stage=name+" petals owner "+p.OwnerClientId;await Reset();
            var bounds=p.Collider.bounds;int before=sakura.transform.childCount;
            for(uint i=0;i<12;i++)Emit(petals,82000+i,bounds.center+UnityEngine.Vector3.up*(bounds.extents.y+.15f),UnityEngine.Vector3.down*12f,.15f);
            await Audio("Map_SakuraPickup",1);
            Check(sakura.transform.childCount>before,"collision also produced buff effect");
            stage=name+" repeat after cooldown owner "+p.OwnerClientId;bounds=p.Collider.bounds;
            Emit(petals,83000,bounds.center+UnityEngine.Vector3.up*(bounds.extents.y+.15f),UnityEngine.Vector3.down*12f,.15f);await Audio("Map_SakuraPickup",2);
        }
        stage=name+" outside combat";Phase(SSW.MatchPhase.Waiting);await Reset();
        foreach(var p in game.Players){var bounds=p.Collider.bounds;Emit(petals,84000+(uint)p.OwnerClientId,bounds.center+UnityEngine.Vector3.up*(bounds.extents.y+.15f),UnityEngine.Vector3.down*12f,.15f);}
        await Audio(null,0);Phase(SSW.MatchPhase.Playing);
    }

    await Map("Map17");var tempo=game.Arena.Map.GetComponent<SSW.MapTempo>();delay=Field<float>(tempo,"_delay");duration=Field<float>(tempo,"_duration");
    stage="clock starts once";Clock(tempo,delay-.2f);await Audio("Map_TimeStop",1);await Wait(.4f);await Audio("Map_TimeStop",1);
    stage="clock ends silently";Clock(tempo,delay+duration+.1f);await Audio("Map_TimeStop",1);Check(!Field<bool>(tempo,"_active")&&UnityEngine.Time.timeScale==1f,"clock restores time scale");
    stage="clock next activation";Clock(tempo,delay+duration+delay+.1f);await Audio("Map_TimeStop",2);
    stage="clock outside combat";Phase(SSW.MatchPhase.Waiting);await Reset();await Audio(null,0);Check(UnityEngine.Time.timeScale==1f,"noncombat restores time scale");Phase(SSW.MatchPhase.Playing);

    await Map("Map02");var map=game.Arena.Map;var edges=new UnityEditor.SerializedObject(map.GetComponent<SSW.MapEdges>()).FindProperty("_edges");
    stage="boundary all sides both owners";
    var left=UnityEngine.Vector2.zero;var bottom=UnityEngine.Vector2.zero;
    for(int i=0;i<edges.arraySize;i++)((UnityEngine.Collider2D)edges.GetArrayElementAtIndex(i).FindPropertyRelative("Shape").objectReferenceValue).enabled=true;
    UnityEngine.Physics2D.SyncTransforms();int expectedHits=0;
    foreach(var p in game.Players){
        for(int i=0;i<edges.arraySize;i++){
            var entry=edges.GetArrayElementAtIndex(i);var border=(UnityEngine.Collider2D)entry.FindPropertyRelative("Shape").objectReferenceValue;
            var normal=entry.FindPropertyRelative("Force").vector2Value.normalized;var bounds=border.bounds;
            var half=UnityEngine.Vector2.Scale(((UnityEngine.CapsuleCollider2D)p.Collider).size,p.Collider.transform.lossyScale)*.5f;
            float extent=UnityEngine.Mathf.Abs(normal.x)*(bounds.extents.x+half.x)+UnityEngine.Mathf.Abs(normal.y)*(bounds.extents.y+half.y);
            var point=(UnityEngine.Vector2)bounds.center+normal*(extent+.016f)-(UnityEngine.Vector2)p.Collider.transform.TransformVector(p.Collider.offset);
            if(normal.x>0)left=point;if(normal.y>0)bottom=point;
            for(int n=0;n<2;n++){
                p.Drive.Teleport(point+normal*.5f);float hp=p.Health.Current;p.Drive.Teleport(point);
                Check(UnityEngine.Vector2.Dot(Impulse(p),normal)>0,"boundary applies inward impulse side "+i+" owner "+p.OwnerClientId);
                float hit=p.Health.Current;map.Touch(p);map.Touch(p);Check(p.Health.Current==hit&&hit<hp,"continuous contact does not repeat damage");expectedHits++;
            }
            p.Drive.Teleport(new UnityEngine.Vector2(p.Side*3f,3f));
        }
    }
    await Audio("Map_BoundaryHit",expectedHits);
    stage="boundary corner single sound";await Reset();var actor=game.Players[0];actor.Drive.Teleport(new UnityEngine.Vector2(left.x,bottom.y));
    Check(Impulse(actor).x>0&&Impulse(actor).y>0,"corner applies combined inward impulse");map.Touch(actor);actor.Drive.Teleport(new UnityEngine.Vector2(3f,3f));await Audio("Map_BoundaryHit",1);
    stage="boundary outside combat";Phase(SSW.MatchPhase.Waiting);await Reset();actor.Drive.Teleport(left);map.Touch(actor);await Audio(null,0);actor.Drive.Teleport(new UnityEngine.Vector2(3f,3f));
    Phase(SSW.MatchPhase.Playing);Check(game.Players.Count==2&&game.Connected,"connection survived all maps");
    Save("Result",true);
}catch(System.Exception ex){error=ex.ToString();evidence.Add(new{stage,host=Snapshot("host"),client=Snapshot("client")});Save("Result",false);}
finally{foreach(string peer in new[]{"host","client"})try{await Send(peer,"sound",7);}catch{}UnityEngine.Time.timeScale=1f;}
}
_=Run();
return new{running=true,root};

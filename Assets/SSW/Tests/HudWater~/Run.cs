var game=SSW.NetGame.Current;
if(!UnityEditor.EditorApplication.isPlaying||game==null||!game.Manager.IsServer||game.Players.Count!=2||!game.CanFight)throw new System.InvalidOperationException("Two playing peers required");
string root="@ROOT@",stage="setup",error=null;
const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var checks=new System.Collections.Generic.List<string>();var evidence=new System.Collections.Generic.List<object>();
var began=UnityEngine.Time.realtimeSinceStartupAsDouble;
T Field<T>(object obj,string field)=>(T)obj.GetType().GetField(field,flags).GetValue(obj);
void Save(string name,bool success)=>System.IO.File.WriteAllText(root+"/"+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(new{success,stage,error,count=checks.Count,checks,evidence,seconds=UnityEngine.Time.realtimeSinceStartupAsDouble-began,scope="Same PC editor host and Windows client, 60ms delay plus 10ms jitter per direction, six jobs, native map colliders and damage"},Newtonsoft.Json.Formatting.Indented));
void Check(bool condition,string label){if(!condition)throw new System.InvalidOperationException(stage+": "+label);checks.Add(stage+": "+label);}
Newtonsoft.Json.Linq.JObject Snapshot(string peer){try{return Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(root+"/"+peer+".json"));}catch(System.IO.IOException){return null;}catch(Newtonsoft.Json.JsonReaderException){return null;}}
Newtonsoft.Json.Linq.JToken Player(string peer,ulong id)=>System.Linq.Enumerable.FirstOrDefault(Snapshot(peer)?["players"]??new Newtonsoft.Json.Linq.JArray(),p=>(ulong)p["id"]==id);
async System.Threading.Tasks.Task Wait(float seconds){double until=UnityEngine.Time.realtimeSinceStartupAsDouble+seconds;while(UnityEngine.Time.realtimeSinceStartupAsDouble<until){if(!UnityEditor.EditorApplication.isPlaying||game==null)throw new System.InvalidOperationException("Play stopped");await System.Threading.Tasks.Task.Delay(10);}}
async System.Threading.Tasks.Task Until(System.Func<bool> condition,float seconds,string label){double until=UnityEngine.Time.realtimeSinceStartupAsDouble+seconds;while(!condition()){if(UnityEngine.Time.realtimeSinceStartupAsDouble>=until)throw new System.TimeoutException(stage+": "+label);await Wait(.02f);}}
async System.Threading.Tasks.Task Send(string peer,string op,int value=0,float x=0,float y=0){int seq=(int)Snapshot(peer)["seq"]+1;System.IO.File.WriteAllText(root+"/"+peer+".cmd.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{seq,op,value,x,y}));await Until(()=>(int?)Snapshot(peer)?["seq"]>=seq,5f,"command "+peer+" "+op);}
void Phase(SSW.MatchPhase phase)=>typeof(SSW.NetMatch).GetMethod("SetPhase",flags).Invoke(game.Match,new object[]{phase});
void Record(){evidence.Add(new{stage,host=Snapshot("host"),client=Snapshot("client")});Save("Progress",false);}
async System.Threading.Tasks.Task Spawn(SSW.PlayerJob first,SSW.PlayerJob second){
    Phase(SSW.MatchPhase.Waiting);
    var saved=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(game.Players,p=>new{p.OwnerClientId,p.Side,p.Info}));
    foreach(var p in game.Players.ToArray())p.NetworkObject.Despawn();
    var prefab=Field<SSW.NetPlayer>(game,"_playerPrefab");
    foreach(var item in saved){var p=UnityEngine.Object.Instantiate(prefab,new UnityEngine.Vector3(item.Side*4f,3f),UnityEngine.Quaternion.identity);p.Init(item.OwnerClientId==0?first:second,item.Side,item.Info);p.NetworkObject.SpawnAsPlayerObject(item.OwnerClientId,true);p.Draft.Restore(System.Array.Empty<int>());}
    UnityEngine.Physics2D.IgnoreCollision(game.Players[0].Collider,game.Players[1].Collider);
    await Until(()=>System.Linq.Enumerable.All(game.Players,p=>(ulong?)Player("client",p.OwnerClientId)?["objectId"]==p.NetworkObjectId),6f,"replacement players");
    foreach(string peer in new[]{"host","client"})await Send(peer,"block",1);
    Phase(SSW.MatchPhase.Playing);await Wait(.5f);
}
async System.Threading.Tasks.Task Map(string name){
    stage=name;Save("Progress",false);foreach(string peer in new[]{"host","client"}){await Send(peer,"move");await Send(peer,"wideground",0,0,-500);}
    var rotation=game.GetComponent<SSW.MapRotation>();int index=-1;for(int i=0;i<rotation.Prefabs.Count;i++)if(rotation.Prefabs[i].name==name)index=i;
    Check(index>=0,"registered map");typeof(SSW.MapRotation).GetField("_index",flags).SetValue(rotation,index);rotation.Restart(false);
    ulong id=game.Arena.Map.NetworkObjectId;typeof(SSW.BattleMap).GetField("_fallY",flags).SetValue(game.Arena.Map,-1000f);
    await Until(()=>(ulong?)Snapshot("client")?["mapObject"]==id,6f,"map replicated");
    foreach(var p in game.Players){p.Buffs.SetBaseHealth(10000f);p.Health.Heal(10000f);var state=Field<SSW.MotionState>(p.Drive,"_state");state.FreezeTime=0f;typeof(SSW.MotionView).GetField("_state",flags).SetValue(p.Drive,state);p.Drive.Teleport(game.Arena.Spawn(p.Side==1?0:1));}
    await Wait(.3f);
}
void Hud(){
    foreach(string peer in new[]{"host","client"}){
        var all=Snapshot(peer)["players"];var own=System.Linq.Enumerable.First(all,p=>(bool)p["owner"]);var other=System.Linq.Enumerable.First(all,p=>!(bool)p["owner"]);
        foreach(var p in all){var h=p["hud"];Check((int)h["bars"]==1,"one bar "+peer+" "+p["id"]);Check((float)h["error"]<.005f,"bar follows body "+peer+" "+p["id"]);float gap=(float)h["bar"]["y"]-(float)p["viewPosition"]["y"];Check(gap>.1f&&gap<4f,"bar remains near head "+peer+" "+p["id"]);Check((int)h["icons"]==p["augments"].Count(),"replicated augment count "+peer+" "+p["id"]);}
        if((int)other["hud"]["icons"]>0){Check((bool)other["hud"]["visible"],"opponent HUD visible "+peer);Check((float)other["hud"]["top"]["y"]<=(float)own["hud"]["top"]["y"]-(float)own["hud"]["size"]["y"]-30f,"opponent below own "+peer);}
    }
}
async System.Threading.Tasks.Task Run(){try{
    var deck=Field<SSW.NetDeck>(game.Local.Draft,"_deck");
    var jobs=new[]{SSW.PlayerJob.Witch,SSW.PlayerJob.Magician,SSW.PlayerJob.Swordsman,SSW.PlayerJob.Assassin,SSW.PlayerJob.Gambler,SSW.PlayerJob.Gunner};
    if(@FULL@){for(int job=0;job<jobs.Length;job++){
        stage="HUD "+jobs[job];await Spawn(jobs[job],jobs[(job+1)%jobs.Length]);
        foreach(var p in game.Players){var ids=deck.Candidates(p.Job,System.Array.Empty<int>(),false);p.Draft.Restore(ids.GetRange(0,UnityEngine.Mathf.Min(ids.Count,p.IsOwner?2:3)));}
        await Wait(.6f);Hud();
        foreach(var p in game.Players){p.Drive.Teleport(new UnityEngine.Vector2(p.Side*7f,4f));p.Health.ReceiveDamage(new SSW.DamageRequest(null,1f,SSW.DamageTag.Environment));}
        await Wait(.8f);Hud();Record();
    }
    stage="HUD expanded";
    foreach(var p in game.Players){p.Draft.Restore(deck.Candidates(p.Job,p.Draft.Owned,false));p.Draft.Restore(deck.Candidates(p.Job,p.Draft.Owned,true));}
    await Wait(.7f);Hud();UnityEngine.ScreenCapture.CaptureScreenshot(root+"/Hud.png");await Wait(.2f);Record();
    }
    await Spawn(SSW.PlayerJob.Witch,SSW.PlayerJob.Gambler);
    if(@FULL@)foreach(string name in new[]{"Map08","Map12"}){
        await Map(name);var flood=game.Arena.Map.GetComponentInChildren<SSW.MapFlood>();var water=Field<UnityEngine.Transform>(flood,"_water");
        await Wait(UnityEngine.Mathf.Max(0f,Field<float>(flood,"_duration")+2f-(float)game.Manager.ServerTime.Time));
        Field<Unity.Netcode.NetworkVariable<double>>(flood,"_startedAt").Value=game.Manager.ServerTime.Time-Field<float>(flood,"_duration")-1f;
        await Wait(.3f);Check(water.GetComponent<UnityEngine.Collider2D>().isTrigger,"water surface is passable");
        var damage=water.GetComponentInChildren<SSW.MapArea>();Check(damage.enabled,"native water damage enabled");
        var initial=new System.Collections.Generic.Dictionary<ulong,float>();
        foreach(var p in game.Players){float foot=p.Body.position.y-p.Collider.bounds.min.y;p.Drive.Teleport(new UnityEngine.Vector2(14f,water.position.y+foot+1f));initial.Add(p.OwnerClientId,p.Health.Current);}
        await Until(()=>System.Linq.Enumerable.All(game.Players,p=>p.Body.position.y<water.position.y-.3f),5f,"both fall through surface");Check(true,"both owners fall through water");
        foreach(var p in game.Players)p.Drive.Freeze(Field<float>(damage,"_delay")+4f);
        await Until(()=>System.Linq.Enumerable.All(game.Players,p=>p.Health.Current<initial[p.OwnerClientId]),Field<float>(damage,"_delay")+6f,"native damage");
        await Wait(.6f);foreach(var p in game.Players)Check((float)Player("client",p.OwnerClientId)["hp"]<initial[p.OwnerClientId],"damage replicated "+p.OwnerClientId);
        Hud();Record();UnityEngine.ScreenCapture.CaptureScreenshot(root+"/"+name+".png");await Wait(.2f);
    }
    foreach(string name in new[]{"Map16","Map17"})foreach(string peer in new[]{"host","client"}){
        await Map(name);stage=name+" rope "+peer;
        var p=System.Linq.Enumerable.First(game.Players,x=>x.IsOwner==(peer=="host"));
        foreach(var other in game.Players)if(other!=p)other.Drive.Freeze(30f);
        await Send(peer,"block",0);
        var swing=System.Linq.Enumerable.OrderByDescending(game.Arena.Map.GetComponentsInChildren<SSW.Swing>(),s=>s.GetComponent<UnityEngine.Collider2D>().bounds.size.x).First();
        var body=swing.GetComponent<UnityEngine.Rigidbody2D>();var shape=swing.GetComponent<UnityEngine.BoxCollider2D>();
        float foot=p.Body.position.y-p.Collider.bounds.min.y;var top=shape.transform.TransformPoint(new UnityEngine.Vector3(-shape.size.x*.5f,shape.size.y*.5f));
        p.Drive.Teleport(new UnityEngine.Vector2(top.x-p.Collider.bounds.extents.x-.1f,top.y+foot-.3f));await Wait(.2f);float start=p.Body.position.y;
        await Send(peer,"move",0,p.Side,0);await Until(()=>p.Body.position.y<start-.5f||p.Body.position.x>top.x+1f,3f,"escapes edge by falling or moving onto top");await Send(peer,"move");Check(true,"edge escape succeeds");Record();
        swing.ResetMap();UnityEngine.Physics2D.SyncTransforms();
        p.Drive.Teleport(new UnityEngine.Vector2(body.position.x+.2f,shape.bounds.max.y+foot+.8f));
        await Until(()=>p.Drive.Grounded,4f,"lands on rope platform");Check(UnityEngine.Mathf.Abs(p.Collider.Distance(shape).distance)<.15f,"stands on selected rope platform");
        await Send(peer,"move",0,p.Side,0);await Wait(.25f);await Send(peer,"move");await Wait(.2f);
        float before=p.Body.position.y;await Send(peer,"jump");await Until(()=>p.Body.position.y>before+.4f,3f,"jump leaves platform");Check(true,"jump succeeds");await Wait(.6f);Hud();Record();UnityEngine.ScreenCapture.CaptureScreenshot(root+"/"+name+"-"+peer+".png");await Wait(.2f);await Send(peer,"block",1);
    }
    stage="round replacement";
    foreach(var p in game.Players){var ids=deck.Candidates(p.Job,p.Draft.Owned,false);p.Draft.Restore(ids.GetRange(0,UnityEngine.Mathf.Min(ids.Count,2)));}
    await Wait(.4f);ulong old=game.Local.NetworkObjectId;
    game.Local.Health.ReceiveDamage(new SSW.DamageRequest(null,100000f,SSW.DamageTag.Environment|SSW.DamageTag.IgnoreDefense));
    await Until(()=>game.Local!=null&&game.Local.NetworkObjectId!=old&&game.CanFight,15f,"actual death replaces players");
    await Wait(.8f);Hud();foreach(var p in game.Players)Check(p.Draft.OwnedCount==2,"augments restored after death "+p.OwnerClientId);Record();
    Save("Result",true);
}catch(System.Exception ex){error=ex.ToString();var contacts=new System.Collections.Generic.List<object>();foreach(var p in game.Players)foreach(var shape in UnityEngine.Object.FindObjectsByType<UnityEngine.Collider2D>()){if(shape==p.Collider||!shape.enabled||shape.isTrigger)continue;var d=p.Collider.Distance(shape);if(d.isValid&&d.distance<.2f)contacts.Add(new{p.OwnerClientId,name=shape.name,parent=shape.transform.parent!=null?shape.transform.parent.name:null,pos=shape.transform.position.ToString("F3"),d.distance,normal=d.normal.ToString("F3")});}evidence.Add(new{contacts});Record();Save("Result",false);}}
_=Run();return new{running=true,root};

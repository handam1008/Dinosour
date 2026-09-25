var checks=new System.Collections.Generic.List<string>();
void Check(bool value,string label){if(!value)throw new System.Exception(label);checks.Add(label);}
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
try{
UnityEngine.GameObject Object(string name,int layer){var go=new UnityEngine.GameObject(name);go.layer=layer;UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);return go;}
var floor=Object("Terrain",8);
var box=floor.AddComponent<UnityEngine.BoxCollider2D>();box.size=new UnityEngine.Vector2(40,2);floor.transform.position=new UnityEngine.Vector3(1000,-1);
var player=Object("Player",3);player.transform.position=new UnityEngine.Vector3(1000,1);
var body=player.AddComponent<UnityEngine.Rigidbody2D>();body.bodyType=UnityEngine.RigidbodyType2D.Kinematic;
var shape=player.AddComponent<UnityEngine.CapsuleCollider2D>();shape.size=new UnityEngine.Vector2(1,2);
var motion=player.AddComponent<SSW.PlayerController>();
var data=new UnityEditor.SerializedObject(motion);data.FindProperty("_whatIsGround").intValue=256;data.ApplyModifiedPropertiesWithoutUndo();
var motor=new SSW.MotionMotor(shape,motion,-30);
var state=new SSW.MotionState{Position=new UnityEngine.Vector2(1000,1),Scale=1};
void Step(SSW.MotionFrame input){UnityEngine.Physics2D.SyncTransforms();motor.Step(ref state,input,7,1f/60f,true);body.position=state.Position;}
for(int i=0;i<300;i++){
float top=UnityEngine.Mathf.Sin(i*0.43f)*0.18f;floor.transform.position=new UnityEngine.Vector3(1000,top-1);
Step(default);Check(state.Position.y-1-top > -0.02f,"moving floor tick "+i);
}
float y=state.Position.y;Step(new SSW.MotionFrame{Jump=1});Check(state.Velocity.y>0&&state.Position.y>y,"jump after moving floor recovery");
floor.transform.position=new UnityEngine.Vector3(1000,-1);
var platform=floor.AddComponent<UnityEngine.PlatformEffector2D>();platform.useOneWay=true;box.usedByEffector=true;
state=new SSW.MotionState{Position=new UnityEngine.Vector2(1000,-0.05f),Velocity=new UnityEngine.Vector2(0,13),Scale=1};body.position=state.Position;
for(int i=0;i<12;i++)Step(default);
Check(state.Position.y>1,"one-way platform allows rising from below");
state=new SSW.MotionState{Position=new UnityEngine.Vector2(1000,1.4f),Velocity=new UnityEngine.Vector2(0,-5),Scale=1};body.position=state.Position;
for(int i=0;i<20;i++)Step(default);
Check(state.Grounded&&state.Position.y>0.98f,"land on one-way platform");
Step(new SSW.MotionFrame{Jump=1,Move=UnityEngine.Vector2.down});
for(int i=0;i<15;i++)Step(new SSW.MotionFrame{Jump=1});
Check(state.Position.y<0.5f,"intentional platform drop stays open");
box.usedByEffector=false;UnityEngine.Object.DestroyImmediate(platform);
box.size=new UnityEngine.Vector2(5,1);floor.transform.position=new UnityEngine.Vector3(1000,0);floor.transform.rotation=UnityEngine.Quaternion.Euler(0,0,45);
state=new SSW.MotionState{Position=new UnityEngine.Vector2(1000,4),Scale=1};body.position=state.Position;
for(int i=0;i<90;i++)Step(default);
Check(state.Grounded,"land on 45 degree slope");
y=state.Position.y;Step(new SSW.MotionFrame{Jump=1});Check(state.Position.y>y&&state.Velocity.y>0,"jump from slope");
floor.transform.rotation=UnityEngine.Quaternion.identity;floor.transform.position=new UnityEngine.Vector3(1001.5f,2);box.size=new UnityEngine.Vector2(1,8);
body.position=new UnityEngine.Vector2(990,20);UnityEngine.Physics2D.SyncTransforms();
var cast=new SSW.MotionCast(shape,256);var position=new UnityEngine.Vector2(1000.8f,2);var velocity=UnityEngine.Vector2.right*4;
cast.Recover(ref position,ref velocity,false,0);
Check(position.x<=1000.501f&&velocity.x<0.01f,"virtual position recovers wall penetration");
Check(body.position==new UnityEngine.Vector2(990,20),"recovery restores actual rigidbody pose");
floor.SetActive(false);
var water=Object("Lift",0);water.transform.position=new UnityEngine.Vector3(1000,0);var trigger=water.AddComponent<UnityEngine.BoxCollider2D>();trigger.isTrigger=true;trigger.size=new UnityEngine.Vector2(4,10);water.AddComponent<SSW.LiftZone>();
state=new SSW.MotionState{Position=new UnityEngine.Vector2(1000,0),Scale=1};body.position=state.Position;
for(int i=0;i<30;i++)Step(default);
Check(state.Position.y>2&&state.Velocity.y>0,"lift zone applies to predicted motor");
state.Position.x=1006;body.position=state.Position;float before=state.Velocity.y;
for(int i=0;i<30;i++)Step(default);
Check(state.Velocity.y<0&&state.Velocity.y<before,"outside lift gravity resumes");
for(int seed=0;seed<20;seed++){
var bag=new SSW.MapBag(14,seed);int last=-1;
for(int cycle=0;cycle<3;cycle++){
var used=new System.Collections.Generic.HashSet<int>();
for(int i=0;i<14;i++){int next=bag.Next();Check(next!=last&&used.Add(next),"map cycle "+seed+":"+cycle+":"+i);last=next;}
}}
Check(SSW.Fighter.Create("공룡#88320").DisplayName(1)=="공룡","nickname tag hidden");
Check(SSW.Fighter.Create("").DisplayName(2)=="플레이어 2","empty nickname fallback");
System.IO.File.WriteAllText("Logs/StartMenu25/TerrainChecks.json",Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented));
return new{checks=checks.Count};
}finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}

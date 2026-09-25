var results=new System.Collections.Generic.List<object>();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
try{
foreach(string name in new[]{"Map02","Map15"}){
var map=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/SSW/Maps/Battle/"+name+".prefab"),scene);map.transform.position=new UnityEngine.Vector3(1000,0);
var player=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/SSW/Resources/Network/Player.prefab"),scene);
try{
var lift=map.GetComponentInChildren<SSW.LiftZone>();var config=new UnityEditor.SerializedObject(lift);float rise=lift.RiseSpeed,accel=lift.Acceleration;
var shape=player.GetComponent<UnityEngine.CapsuleCollider2D>();var body=player.GetComponent<UnityEngine.Rigidbody2D>();body.bodyType=UnityEngine.RigidbodyType2D.Kinematic;var motor=new SSW.MotionMotor(shape,player.GetComponent<SSW.PlayerController>(),UnityEngine.Physics2D.gravity.y*body.gravityScale);
foreach(bool before in new[]{true,false})foreach(float steerY in name=="Map02"?new[]{-8f,-6f,-4f,-2f,0f,100f}:new[]{100f}){
config.FindProperty("_riseSpeed").floatValue=before?(name=="Map02"?5.5f:15f):rise;config.FindProperty("_acceleration").floatValue=before?90f:accel;config.ApplyModifiedPropertiesWithoutUndo();
var state=new SSW.MotionState{Position=new UnityEngine.Vector2(name=="Map02"?1000:1007,-8),Scale=1};body.position=state.Position;player.transform.position=state.Position;float max=-100;bool upper=false;var samples=new System.Collections.Generic.List<object>();
for(int i=0;i<240;i++){
UnityEngine.Physics2D.SyncTransforms();var input=new SSW.MotionFrame{Tick=(uint)i+1,Move=state.Position.y>steerY?UnityEngine.Vector2.right:UnityEngine.Vector2.zero};motor.Step(ref state,input,7,1f/60f,true);body.position=state.Position;
max=UnityEngine.Mathf.Max(max,state.Position.y);if(state.Grounded&&state.Position.y>3)upper=true;
if(i%6==0)samples.Add(new{i,x=state.Position.x-1000,y=state.Position.y,vy=state.Velocity.y,grounded=state.Grounded});
}
results.Add(new{name,before,steerY,max,upper,samples});
}
}finally{UnityEngine.Object.DestroyImmediate(player);UnityEngine.Object.DestroyImmediate(map);}
}
}finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
var entries=Newtonsoft.Json.Linq.JArray.FromObject(results);bool landed=false,lifted=false;foreach(var item in entries){if((bool)item["before"])continue;if((string)item["name"]=="Map02"&&(float)item["steerY"]==-8f)landed=(bool)item["upper"]&&(float)item["max"]>6f;if((string)item["name"]=="Map15")lifted=(float)item["max"]>5f;}if(!landed||!lifted)throw new System.Exception("Waterfall upper reach failed");
System.IO.File.WriteAllText("Logs/MapFeel25/Water.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));return new{runs=results.Count};

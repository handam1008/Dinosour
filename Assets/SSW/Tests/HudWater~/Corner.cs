if(!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play required");
var source=UnityEngine.Resources.Load<UnityEngine.GameObject>("Network/Player");
var capsule=source.GetComponent<UnityEngine.CapsuleCollider2D>();
var motion=source.GetComponent<SSW.PlayerController>();
var actor=new UnityEngine.GameObject("CornerProbe");var block=new UnityEngine.GameObject("RopeEdgeProbe");
var rows=new System.Collections.Generic.List<object>();
try{
var body=actor.AddComponent<UnityEngine.Rigidbody2D>();body.bodyType=UnityEngine.RigidbodyType2D.Kinematic;
var shape=actor.AddComponent<UnityEngine.CapsuleCollider2D>();shape.size=capsule.size;shape.offset=capsule.offset;shape.direction=capsule.direction;
actor.transform.localScale=source.transform.localScale;
block.layer=8;var box=block.AddComponent<UnityEngine.BoxCollider2D>();box.size=new UnityEngine.Vector2(13f,1.3f);block.transform.position=new UnityEngine.Vector3(1000f,1000f,0f);
float foot=(capsule.offset.y-capsule.size.y*.5f)*source.transform.localScale.y;
float half=capsule.size.x*.5f*source.transform.localScale.x;
foreach(float angle in new[]{-30f,-10f,0f,10f,30f})foreach(float dx in new[]{-.3f,-.1f,.1f})foreach(float dy in new[]{-.3f,0f,.3f})foreach(float input in new[]{-1f,0f,1f}){
block.transform.rotation=UnityEngine.Quaternion.Euler(0f,0f,angle);
var edge=block.transform.TransformPoint(new UnityEngine.Vector3(-6.5f,.65f));
var start=new UnityEngine.Vector2(edge.x-half+dx,edge.y-foot+dy);body.position=start;
UnityEngine.Physics2D.SyncTransforms();var motor=new SSW.MotionMotor(shape,motion,-30f);
var state=new SSW.MotionState{Position=start,Scale=1f,BodyScale=1f,Aim=UnityEngine.Vector2.right};
for(int i=0;i<90;i++){motor.Step(ref state,new SSW.MotionFrame{Move=new UnityEngine.Vector2(input,0f)},4f,1f/60f,true);body.position=state.Position;}
if(!state.Grounded&&state.Velocity.sqrMagnitude<.0001f)throw new System.InvalidOperationException("Suspended at edge: "+angle+","+dx+","+dy+","+input);var contact=shape.Distance(box);rows.Add(new{angle,dx,dy,input,start=start.ToString("F4"),end=state.Position.ToString("F4"),delta=(state.Position-start).ToString("F4"),ground=state.Grounded,velocity=state.Velocity.ToString("F4"),normal=(-contact.normal).ToString("F4"),distance=contact.distance});
}
}finally{UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(block);}
System.IO.File.WriteAllText("@ROOT@/Corners.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows.Count;

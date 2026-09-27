var g=SSW.NetGame.Current;
var normal=new UnityEngine.Vector2(@X@,@Y@);
var entries=new UnityEditor.SerializedObject(g.Arena.Map.GetComponent<SSW.MapEdges>()).FindProperty("_edges");
UnityEngine.Collider2D wall=null;
float damage=0f;
float force=0f;
for(int i=0;i<entries.arraySize;i++)
{
    var e=entries.GetArrayElementAtIndex(i);
    var f=e.FindPropertyRelative("Force").vector2Value;
    if(UnityEngine.Vector2.Dot(f,normal)<=0f)continue;
    wall=(UnityEngine.Collider2D)e.FindPropertyRelative("Shape").objectReferenceValue;
    damage=e.FindPropertyRelative("Damage").floatValue;
    force=f.magnitude;
}
if(wall==null)throw new System.InvalidOperationException("Boundary direction missing");
var actors=new System.Collections.Generic.List<object>();
foreach(var p in g.Players)
{
    var capsule=(UnityEngine.CapsuleCollider2D)p.Collider;
    var size=UnityEngine.Vector2.Scale(capsule.size,p.transform.lossyScale);
    var bounds=wall.bounds;
    float extent=UnityEngine.Mathf.Abs(normal.x)*(bounds.extents.x+size.x*0.5f)+UnityEngine.Mathf.Abs(normal.y)*(bounds.extents.y+size.y*0.5f);
    var tangent=new UnityEngine.Vector2(-normal.y,normal.x);
    var center=(UnityEngine.Vector2)bounds.center+normal*(extent+0.8f);
    if(normal.x!=0f)center.y=bounds.max.y-(p.IsOwner?3f:5f);
    bool placed=false;
    for(int i=0;i<18;i++)
    {
        var point=center+tangent*(normal.x!=0f?i*0.2f:(p.IsOwner?1f:-1f)*(2f+i*0.8f));
        var hits=UnityEngine.Physics2D.CapsuleCastAll(point,size,capsule.direction,0f,-normal,0.9f,p.GroundMask);
        bool blocked=System.Linq.Enumerable.Any(hits,h=>h.collider!=wall && !h.collider.isTrigger);
        if(blocked)continue;
        p.Drive.Teleport(point-(UnityEngine.Vector2)p.Collider.transform.TransformVector(p.Collider.offset));
        if(normal.y!=0f)p.Drive.Launch(-normal.y*8f*p.Body.mass);
        placed=true;
        break;
    }
    if(!placed)throw new System.InvalidOperationException("No clear native boundary approach");
    actors.Add(new{id=p.OwnerClientId,owner=p.IsOwner,side=p.Side,hp=p.Health.Current,epoch=p.Epoch,x=p.Body.position.x,y=p.Body.position.y,grounded=p.Drive.Grounded,gap=p.Collider.Distance(wall).distance});
}
var samples=new System.Collections.Generic.List<object>();
double until=UnityEditor.EditorApplication.timeSinceStartup+3.5;
UnityEditor.EditorApplication.CallbackFunction sample=null;
sample=()=>
{
    if(UnityEditor.EditorApplication.isPlaying && g!=null)
        foreach(var p in g.Players)
            samples.Add(new{time=UnityEngine.Time.unscaledTimeAsDouble,id=p.OwnerClientId,hp=p.Health.Current,grounded=p.Drive.Grounded,gap=p.Collider.Distance(wall).distance,x=p.Body.position.x,y=p.Body.position.y,vx=p.Velocity.x,vy=p.Velocity.y});
    if(UnityEditor.EditorApplication.timeSinceStartup<until && UnityEditor.EditorApplication.isPlaying)return;
    UnityEditor.EditorApplication.update-=sample;
    System.IO.File.WriteAllText("@PATH@",Newtonsoft.Json.JsonConvert.SerializeObject(new{samples},Newtonsoft.Json.Formatting.Indented));
};
UnityEditor.EditorApplication.update+=sample;
return new{map=g.Arena.Map.Title,edge=wall.name,damage,force,actors};

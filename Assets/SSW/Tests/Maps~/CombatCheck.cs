var checks=new System.Collections.Generic.List<string>();
void Check(bool condition,string label){if(!condition)throw new System.InvalidOperationException(label);checks.Add(label);}
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
var game=UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.NetGame>("Assets/SSW/Resources/Network/NetGame.prefab");
var rotation=(SSW.MapRotation)new UnityEditor.SerializedObject(game).FindProperty("_maps").objectReferenceValue;
int layer=UnityEngine.LayerMask.NameToLayer("Ground");
Check(layer>=0,"ground layer exists");
int mask=1<<layer;
try
{
    foreach(var prefab in rotation.Prefabs)
    {
        if(prefab.GetComponentsInChildren<SSW.Pin>(true).Length==0)continue;
        var map=UnityEngine.Object.Instantiate(prefab);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(map.gameObject,scene);
        map.transform.position+=UnityEngine.Vector3.right*500f;
        foreach(var collider in map.GetComponentsInChildren<UnityEngine.Collider2D>(true))collider.enabled=false;
        var pins=map.GetComponentsInChildren<SSW.Pin>(true);
        var pin=pins[0];
        typeof(SSW.Pin).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(pin,null);
        var shape=(UnityEngine.Collider2D)new UnityEditor.SerializedObject(pin).FindProperty("_hit").objectReferenceValue;
        UnityEngine.Physics2D.SyncTransforms();
        UnityEngine.Vector2 center=shape.bounds.center;
        UnityEngine.Vector2 from=center+UnityEngine.Vector2.left*2f;
        UnityEngine.Vector2 to=center+UnityEngine.Vector2.right*2f;
        foreach(var size in new[]{UnityEngine.Vector2.zero,new UnityEngine.Vector2(0.25f,0.5f)})
        {
            Check(SSW.MapCombat.Sweep(from,to,0.125f,size,mask,out var hit)&&hit.collider==shape,prefab.Title+" circle/capsule finds trigger pin");
            Check(!SSW.ShotQuery.Ground(from,to,0.125f,size,mask,out _),prefab.Title+" pin does not block walking or ground sight ray");
            Check(SSW.ShotQuery.Touches(shape,hit.centroid,0.125f,size),prefab.Title+" prediction holds at live pin");
        }
        var obstacle=new UnityEngine.GameObject("Obstacle");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obstacle,scene);
        var solid=obstacle.AddComponent<UnityEngine.BoxCollider2D>();
        solid.size=new UnityEngine.Vector2(0.2f,2f);
        obstacle.layer=layer;
        obstacle.transform.position=center+UnityEngine.Vector2.left;
        UnityEngine.Physics2D.SyncTransforms();
        Check(SSW.MapCombat.Sweep(from,to,0.125f,mask,out var near)&&near.collider==solid,prefab.Title+" nearer wall wins over pin");
        Check(SSW.MapCombat.Sweep(from,to,0.125f,0,out var returning)&&returning.collider==shape,prefab.Title+" returning card ignores wall and still finds pin");
        var ignored=new System.Collections.Generic.HashSet<SSW.Pin>{pin};
        Check(!SSW.MapCombat.Sweep(from,to,0.125f,0,out _,ignored),prefab.Title+" zero-damage penetrating shot cannot hit same pin twice");
        SSW.MapCombat.Strike(null,from,center,UnityEngine.Vector2.right,UnityEngine.Vector2.one,mask,10f);
        Check(pin.Current==1f,prefab.Title+" melee cannot cut through wall");
        solid.isTrigger=true;
        UnityEngine.Physics2D.SyncTransforms();
        Check(SSW.MapCombat.Sweep(from,to,0.125f,mask,out var trigger)&&trigger.collider==shape,prefab.Title+" unrelated ground trigger ignored");
        solid.isTrigger=false;
        obstacle.layer=UnityEngine.LayerMask.NameToLayer("Player");
        UnityEngine.Physics2D.SyncTransforms();
        Check(SSW.MapCombat.Sweep(from,to,0.125f,mask,out var actor)&&actor.collider==shape,prefab.Title+" player layer remains in player sweep only");
        obstacle.layer=layer;
        obstacle.transform.position=center+UnityEngine.Vector2.right;
        UnityEngine.Physics2D.SyncTransforms();
        Check(SSW.MapCombat.Sweep(from,to,0.125f,mask,out var far)&&far.collider==shape,prefab.Title+" pin wins over farther wall");
        SSW.MapCombat.Strike(null,from,center,UnityEngine.Vector2.right,UnityEngine.Vector2.one,mask,10f);
        Check(pin.Current==0f&&!shape.enabled,prefab.Title+" unblocked melee cuts pin once");
        Check(!SSW.ShotQuery.Touches(shape,center,0.125f,default),prefab.Title+" cut pin releases predicted projectile");
        Check(SSW.MapCombat.Sweep(from,to,0.125f,mask,out var cut)&&cut.collider==solid,prefab.Title+" cut pin leaves only farther wall");
        pin.Heal(10f);
        Check(pin.Current==0f,prefab.Title+" healing cannot resurrect cut pin");
        UnityEngine.Object.DestroyImmediate(obstacle);
        UnityEngine.Object.DestroyImmediate(map.gameObject);
    }
    System.IO.Directory.CreateDirectory("Logs/Swing");
    System.IO.File.WriteAllText("Logs/Swing/CombatCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented));
    return new{passed=checks.Count};
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
}

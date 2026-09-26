int checks=0;
void Check(bool value,string label){if(!value)throw new System.InvalidOperationException(label);checks++;}
var root=new UnityEngine.GameObject("CameraCheck");
try{
var view=root.AddComponent<UnityEngine.Camera>();root.transform.position=new UnityEngine.Vector3(0,0,-10);
var framing=root.AddComponent<SSW.SandboxCameraFollow>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
typeof(SSW.SandboxCameraFollow).GetMethod("Awake",flags).Invoke(framing,null);
int maps=0;
foreach(string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/SSW/Maps/Battle"})){
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
var map=prefab.GetComponent<SSW.BattleMap>();if(map==null)continue;maps++;
foreach(float aspect in new[]{4f/3f,16f/9f,21f/9f,9f/16f})foreach(int side in new[]{1,-1}){
view.aspect=aspect;framing.SetBounds(map.Bounds);framing.SetSide(side);
Check(view.orthographic&&UnityEngine.Mathf.Approximately(view.orthographicSize,9f),map.Title+" size must be nine");
Check(UnityEngine.Vector2.Distance(root.transform.position,map.Bounds.center)<0.001f,map.Title+" center changed");
var position=root.transform.position;float size=view.orthographicSize;framing.Shake(10,2);
typeof(SSW.SandboxCameraFollow).GetMethod("LateUpdate",flags).Invoke(framing,null);
Check(root.transform.position==position&&view.orthographicSize==size,map.Title+" camera moved");
}
}
Check(maps==14,"Expected fourteen maps");
}finally{UnityEngine.Object.DestroyImmediate(root);}
foreach(var path in new[]{"Assets/SSW/SuperUltraLegendScene.unity","Assets/SSW/Scenes/Crate.unity","Assets/SSW/Scenes/Swing.unity","Assets/SSW/Scenes/Wind.unity"}){
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
try{
int cameras=0;foreach(var go in scene.GetRootGameObjects())foreach(var item in go.GetComponentsInChildren<UnityEngine.Transform>(true)){
Check(UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject)==0,path+" missing script");
if(item.TryGetComponent<SSW.SandboxCameraFollow>(out _))cameras++;
}
Check(cameras==1,path+" fixed camera missing");
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
}
var player=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/SSW/Resources/Network/Player.prefab");
Check(player.GetComponentsInChildren<NKY.Scripts.FeedBack.FeedBackModule>(true).Length==0,"Shared feedback remains");
Check(player.GetComponentsInChildren<Unity.Cinemachine.CinemachineImpulseSource>(true).Length==0,"Impulse remains");
var effects=player.GetComponentsInChildren<SSW.HitFeedback>(true);Check(effects.Length==1,"Local feedback missing");
var data=new UnityEditor.SerializedObject(effects[0]);Check(data.FindProperty("_health").objectReferenceValue==player.GetComponent<SSW.Health>(),"Health binding wrong");
Check(data.FindProperty("_effects").arraySize==1,"Hit particle missing");
System.IO.File.WriteAllText("Logs/MapFeel25/Camera.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{checks}));
return new{checks};
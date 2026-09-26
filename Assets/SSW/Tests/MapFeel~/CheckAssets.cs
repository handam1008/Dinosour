var checks=new System.Collections.Generic.List<string>();
void Check(bool value,string label){if(!value)throw new System.Exception(label);checks.Add(label);}
foreach(string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/SSW/Maps/Battle"})){
string path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
try{
foreach(var t in root.GetComponentsInChildren<UnityEngine.Transform>(true))Check(UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,path+" missing script "+t.name);
if(root.TryGetComponent<SSW.BattleMap>(out var map))Check(map.Spawn(0)!=map.Spawn(1),path+" spawn bindings");
if(root.GetComponentInChildren<SSW.MapGust>() is SSW.MapGust gust){var so=new UnityEditor.SerializedObject(gust);var effect=(UnityEngine.ParticleSystem)so.FindProperty("_effect").objectReferenceValue;Check(effect!=null&&so.FindProperty("_tint").objectReferenceValue!=null,"gust bindings");Check(!effect.main.loop&&!effect.main.playOnAwake&&effect.main.startDelay.constant==0,"gust waits for force");}
if(root.TryGetComponent<SSW.MapTempo>(out var tempo)){var so=new UnityEditor.SerializedObject(tempo);Check(so.FindProperty("_startTint").objectReferenceValue!=null&&so.FindProperty("_endTint").objectReferenceValue!=null,"tempo bindings");}
if(path.EndsWith("Map13.prefab")){Check(UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(root.transform.GetChild(0).gameObject)==UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/KDH/GameModules/Maps/KDH_Map 13.prefab"),"Sakura uses latest authoring prefab");Check(root.GetComponentInChildren<SSW.SakuraField>()!=null,"network Sakura field");var field=new UnityEditor.SerializedObject(root.GetComponentInChildren<SSW.SakuraField>());Check(field.FindProperty("_petals").objectReferenceValue!=null,"Sakura particle clock binding");Check(root.GetComponentsInChildren<KDH_SakuraEffectPooling>(true).Length==0&&root.GetComponentsInChildren<KDH.Scripts.Objects.KDH_SakuraMode>(true).Length==0,"no stale global pool");var p=root.GetComponentInChildren<SSW.SakuraField>().GetComponent<UnityEngine.ParticleSystem>();Check(p.collision.enabled&&p.collision.sendCollisionMessages,"Sakura physical collision callbacks");}
if(path.EndsWith("SakuraFx.prefab")){Check(root.GetComponentsInChildren<KDH_SakuraEffect>(true).Length==0,"Sakura effect has no stale pool callback");Check(root.GetComponent<UnityEngine.ParticleSystem>().main.stopAction==UnityEngine.ParticleSystemStopAction.Destroy,"Sakura effect cleanup");}
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}}
int scenes=0;
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:Scene",new[]{"Assets/SSW"})){
var path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);if(path.EndsWith("/MainMenu.unity"))continue;var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
try{foreach(var root in scene.GetRootGameObjects())foreach(var camera in root.GetComponentsInChildren<UnityEngine.Camera>(true)){Check(camera.orthographic&&UnityEngine.Mathf.Approximately(camera.orthographicSize,9),path+" camera nine");scenes++;}}
finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}}
Check(scenes==8,"eight map scene cameras");
System.IO.File.WriteAllText("Logs/MapFeel25/AssetsChecks.json",Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented));return new{checks=checks.Count};

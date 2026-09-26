if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play first");
var profile=UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.MapSources>("Assets/SSW/Editor/Maps/MapSources.asset");
var game=UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.NetGame>("Assets/SSW/Resources/Network/NetGame.prefab");
var rotation=(SSW.MapRotation)new UnityEditor.SerializedObject(game).FindProperty("_maps").objectReferenceValue;
var before=new System.Collections.Generic.Dictionary<string,(float Damage,UnityEngine.Vector2 Force)[]>();
(float Damage,UnityEngine.Vector2 Force)[] Read(SSW.BattleMap map)
{
    using var settings=new UnityEditor.SerializedObject(map.GetComponent<SSW.MapEdges>());
    var edges=settings.FindProperty("_edges");
    var result=new (float Damage,UnityEngine.Vector2 Force)[edges.arraySize];
    for(int i=0;i<result.Length;i++)
    {
        var edge=edges.GetArrayElementAtIndex(i);
        result[i]=(edge.FindPropertyRelative("Damage").floatValue,edge.FindPropertyRelative("Force").vector2Value);
    }
    return result;
}
foreach(var map in rotation.Prefabs)before.Add(UnityEditor.AssetDatabase.GetAssetPath(map),Read(map));
var entries=profile.Entries;
for(int i=0;i<entries.Length;i++)entries[i].Hash=string.Empty;
profile.Replace(entries);
UnityEditor.EditorUtility.SetDirty(profile);
UnityEditor.AssetDatabase.SaveAssetIfDirty(profile);
string bake=SSW.MapBake.BakeAll();
int checks=0;
foreach(var pair in before)
{
    var after=Read(UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.BattleMap>(pair.Key));
    if(after.Length!=pair.Value.Length)throw new System.InvalidOperationException(pair.Key+" edge count changed");
    for(int i=0;i<after.Length;i++)
    {
        if(after[i]!=pair.Value[i])throw new System.InvalidOperationException(pair.Key+" edge tuning changed");
        checks++;
    }
}
string repeat=SSW.MapBake.BakeAll();
if(repeat!="맵 원본 변경 없음")throw new System.InvalidOperationException(repeat);
var result=new{checks,bake,repeat};
System.IO.File.WriteAllText("Logs/Swing/BakeCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;

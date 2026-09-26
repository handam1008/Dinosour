if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
const string folder = "Assets/SSW/Tests/BalanceFixture";
if (UnityEditor.AssetDatabase.IsValidFolder(folder)) throw new System.InvalidOperationException("BalanceFixture already exists");
const string log = "Logs/Balance26/BakeChecks.json";
var checks = new System.Collections.Generic.List<string>();
var observations = new System.Collections.Generic.List<object>();
var original = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.GunSources>("Assets/SSW/Editor/GunSources.asset");
var sourceObjects = new UnityEngine.Component[] { original.Fire, original.Gravity, original.Ice, original.Poison, original.Shuriken };
var sourceFields = new[] { "dotDamage", "forceAmount", "slowAmount", "dotDamage", "damage" };
var referenceFields = new[] { "_fire", "_gravity", "_ice", "_poison", "_shuriken" };
var expected = new[] { 1f, 10f, 0.3f, 2f, 1f };
var changed = new[] { 4f, 13f, 0.6f, 3f, 5f };
var sourceBytes = new System.Collections.Generic.Dictionary<string, byte[]>();
foreach (var source in sourceObjects)
{
    string path = UnityEditor.AssetDatabase.GetAssetPath(source);
    sourceBytes[path] = System.IO.File.ReadAllBytes(path);
}
string originalBalance = UnityEngine.JsonUtility.ToJson(original.Balance);
var previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string previousPath = previousScene.path;
bool previousDirty = previousScene.isDirty;
bool Near(float a, float b) => UnityEngine.Mathf.Abs(a - b) < 0.0001f;
float[] Values(SSW.GunBalance balance) => new[] { balance.FireDamage, balance.GravityForce, balance.IceSlow, balance.PoisonDamage, balance.ShurikenDamage };
void Check(bool value, string label)
{
    if (!value) throw new System.InvalidOperationException(label);
    checks.Add(label);
}
void Match(SSW.GunBalance balance, float[] target, string label)
{
    float[] values = Values(balance);
    for (int i = 0; i < values.Length; i++) Check(Near(values[i], target[i]), label + " " + referenceFields[i] + "=" + values[i]);
}
async System.Threading.Tasks.Task Run()
{
    string error = null;
    try
    {
        Match(original.Balance, expected, "production defaults");
        Check(UnityEditor.PrefabUtility.GetPrefabAssetType(original.Fire) == UnityEditor.PrefabAssetType.Variant, "fire uses SSW variant");
        var parentFire = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/KDH/GameModules/Prefabs/AbilityBullets/KDH_FireBullet.prefab").GetComponent<KDH.Scripts.Upgrade.Instances.Bullets.KDH_FireBullet>();
        Check(new UnityEditor.SerializedObject(parentFire).FindProperty("dotDamage").intValue == 2, "team fire original stays two");
        Check(!SSW.GunBake.Bake(original), "unchanged bake is idempotent");
        UnityEditor.AssetDatabase.CreateFolder("Assets/SSW/Tests", "BalanceFixture");
        var balance = UnityEngine.ScriptableObject.CreateInstance<SSW.GunBalance>();
        UnityEditor.AssetDatabase.CreateAsset(balance, folder + "/Balance.asset");
        var sources = UnityEngine.ScriptableObject.CreateInstance<SSW.GunSources>();
        using (var setup = new UnityEditor.SerializedObject(sources))
        {
            setup.FindProperty("_balance").objectReferenceValue = balance;
            for (int i = 0; i < sourceObjects.Length; i++)
            {
                string target = folder + "/Source" + i + ".prefab";
                Check(UnityEditor.AssetDatabase.CopyAsset(UnityEditor.AssetDatabase.GetAssetPath(sourceObjects[i]), target), "copy independent source " + i);
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(target);
                setup.FindProperty(referenceFields[i]).objectReferenceValue = prefab.GetComponent(sourceObjects[i].GetType());
            }
            setup.ApplyModifiedPropertiesWithoutUndo();
            UnityEditor.AssetDatabase.CreateAsset(sources, folder + "/GunSources.asset");
        }
        SSW.GunBake.BakeAll();
        Match(balance, expected, "fixture baseline");
        for (int i = 0; i < sourceObjects.Length; i++)
        {
            string path = folder + "/Source" + i + ".prefab";
            var instance = UnityEditor.PrefabUtility.LoadPrefabContents(path);
            try
            {
                var component = instance.GetComponent(sourceObjects[i].GetType());
                using var edit = new UnityEditor.SerializedObject(component);
                var field = edit.FindProperty(sourceFields[i]);
                if (field.propertyType == UnityEditor.SerializedPropertyType.Integer) field.intValue = (int)changed[i];
                else field.floatValue = changed[i];
                edit.ApplyModifiedPropertiesWithoutUndo();
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally { UnityEditor.PrefabUtility.UnloadPrefabContents(instance); }
        }
        double until = UnityEditor.EditorApplication.timeSinceStartup + 8d;
        while (true)
        {
            balance = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.GunBalance>(folder + "/Balance.asset");
            sources = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.GunSources>(folder + "/GunSources.asset");
            if (System.Linq.Enumerable.SequenceEqual(Values(balance), changed)) break;
            if (UnityEditor.EditorApplication.timeSinceStartup >= until)
            {
                var debug = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
                observations.Add(new { stage = "import timeout", values = Values(balance), sources = UnityEditor.EditorJsonUtility.ToJson(sources),
                    queued = typeof(SSW.GunBake).GetField("_queued", debug).GetValue(null), dependencies = typeof(SSW.GunBake).GetField("_dependencies", debug).GetValue(null),
                    updating = UnityEditor.EditorApplication.isUpdating, compiling = UnityEditor.EditorApplication.isCompiling });
                SSW.GunBake.Bake(sources);
                observations.Add(new { stage = "explicit bake diagnostic", values = Values(balance) });
                throw new System.TimeoutException("saved source import did not refresh balance");
            }
            await System.Threading.Tasks.Task.Delay(100);
        }
        Match(balance, changed, "automatic import update");
        observations.Add(new { stage = "import", values = Values(balance) });
        balance.Replace(99f, 99f, 99f, 99f, 99f);
        new SSW.GunBuild().OnPreprocessBuild(null);
        Match(balance, changed, "build callback refresh");
        balance.Replace(99f, 99f, 99f, 99f, 99f);
        typeof(SSW.GunBake).GetMethod("OnPlay", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Invoke(null, new object[] { UnityEditor.PlayModeStateChange.ExitingEditMode });
        Match(balance, changed, "play callback refresh");
        var invalidSource = UnityEngine.Object.Instantiate(sources);
        var invalidScene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var invalidGravity = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(sources.Gravity.gameObject, invalidScene);
        try
        {
            using (var field = new UnityEditor.SerializedObject(invalidGravity.GetComponent(sources.Gravity.GetType())))
            {
                field.FindProperty("forceAmount").floatValue = float.NaN;
                field.ApplyModifiedPropertiesWithoutUndo();
            }
            using (var field = new UnityEditor.SerializedObject(invalidSource))
            {
                field.FindProperty("_gravity").objectReferenceValue = invalidGravity.GetComponent(sources.Gravity.GetType());
                field.ApplyModifiedPropertiesWithoutUndo();
            }
            bool rejected = false;
            try { SSW.GunBake.Bake(invalidSource); }
            catch (System.InvalidOperationException) { rejected = true; }
            Check(rejected, "invalid source rejected before output changes");
            Match(balance, changed, "invalid bake preserves output");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(invalidSource);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(invalidScene);
        }
    }
    catch (System.Exception failure) { error = failure.ToString(); }
    finally
    {
        if (UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.DeleteAsset(folder);
        SSW.GunBake.BakeAll();
        original = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.GunSources>("Assets/SSW/Editor/GunSources.asset");
        foreach (var pair in sourceBytes) Check(System.Linq.Enumerable.SequenceEqual(pair.Value, System.IO.File.ReadAllBytes(pair.Key)), "preserved source " + pair.Key);
        Check(originalBalance == UnityEngine.JsonUtility.ToJson(original.Balance), "production balance preserved");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Check(scene.path == previousPath && scene.isDirty == previousDirty, "active scene preserved");
        System.IO.File.WriteAllText(log, Newtonsoft.Json.JsonConvert.SerializeObject(new { success = error == null, error, count = checks.Count, checks, observations }, Newtonsoft.Json.Formatting.Indented));
    }
}
_ = Run();
return new { running = true, log };

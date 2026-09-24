if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
var deck = Resources.Load<SSW.NetDeck>("Network/Deck");
var jobs = System.Enum.GetValues(typeof(SSW.PlayerJob)).Cast<SSW.PlayerJob>().Where(j => j != SSW.PlayerJob.None).ToArray();
foreach (var job in jobs)
{
    var pool = deck.Candidates(job, new int[0], false);
    if (pool.Count < 4) throw new System.InvalidOperationException(job + " needs four job cards");
    if (deck.Candidates(job, new[] { pool[0] }, false).Count < 3) throw new System.InvalidOperationException(job + " has insufficient choices after the first job pick");
}
var player = Resources.Load<GameObject>("Network/Player");
if (player.GetComponentsInChildren<MonoBehaviour>(true).Any(component => component == null)) throw new System.InvalidOperationException("Missing player script");
var cast = new SerializedObject(player.GetComponent<SSW.NetCast>());
if (cast.FindProperty("_jobs").arraySize != 4) throw new System.InvalidOperationException("Job adapters are incomplete");
if (cast.FindProperty("_boltPrefab").objectReferenceValue == null) throw new System.InvalidOperationException("Missing bolt prefab");
var fx = new SerializedObject(player.GetComponent<SSW.PlayerFx>());
if (fx.FindProperty("_blind").objectReferenceValue == null) throw new System.InvalidOperationException("Missing vision mask");
if (System.IO.Directory.Exists("Assets/Logs"))
{
    string assetRoot = System.IO.Path.GetFullPath("Assets");
    foreach (string file in System.IO.Directory.GetFiles("Assets/Logs", "*", System.IO.SearchOption.AllDirectories))
    {
        if (file.EndsWith(".meta")) continue;
        if (System.IO.Path.GetFileName(file) != "assassin.png") throw new System.InvalidOperationException("Unexpected evidence file: " + file);
        string relative = System.IO.Path.GetRelativePath(assetRoot, System.IO.Path.GetFullPath(file));
        string target = System.IO.Path.GetFullPath(relative);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target));
        System.IO.File.Copy(file, target, true);
    }
    AssetDatabase.DeleteAsset("Assets/Logs");
}
return new { deck.Count, jobs = jobs.Select(job => new { job = job.ToString(), cards = deck.Candidates(job, new int[0], false).Count }).ToArray(), missingScripts = 0 };

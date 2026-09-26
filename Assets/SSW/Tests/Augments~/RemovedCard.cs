if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first");
var deck = UnityEditor.AssetDatabase.LoadAssetAtPath<SSW.NetDeck>("Assets/SSW/Resources/Network/Deck.asset");
if (deck.At(85) != null) throw new System.InvalidOperationException("ID 85 is no longer empty; inspect new source before changing it");
using var settings = new UnityEditor.SerializedObject(deck);
settings.FindProperty("_augments").GetArrayElementAtIndex(85).objectReferenceValue = null;
settings.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(deck);
UnityEditor.AssetDatabase.SaveAssetIfDirty(deck);
var result = new System.Collections.Generic.List<object>();
var active = new System.Collections.Generic.HashSet<int>();
foreach (SSW.PlayerJob job in System.Enum.GetValues(typeof(SSW.PlayerJob)))
{
    if (job == SSW.PlayerJob.None) continue;
    var common = deck.Candidates(job, System.Array.Empty<int>(), true);
    var jobs = deck.Candidates(job, System.Array.Empty<int>(), false);
    foreach (int id in System.Linq.Enumerable.Concat(common, jobs))
    {
        if (deck.At(id) == null || id == 85) throw new System.InvalidOperationException("Removed card remains eligible");
        active.Add(id);
    }
    result.Add(new { job = job.ToString(), common = common.Count, jobs = jobs.Count });
}
return new { slots = deck.Count, valid = System.Linq.Enumerable.Count(System.Linq.Enumerable.Range(0, deck.Count), id => deck.At(id) != null), active = active.Count, jobs = result.ToArray() };

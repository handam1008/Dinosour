if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play required");
var info = UnityEngine.Object.FindFirstObjectByType<SSW.ProfileInfo>();
if (info == null || !info.isActiveAndEnabled) throw new System.InvalidOperationException("Active menu profile required");
using var settings = new UnityEditor.SerializedObject(info);
var text = (TMPro.TMP_Text)settings.FindProperty("_score").objectReferenceValue;
string original = text.text;
var legacy = info.GetComponent<RYU._01.Script.PlayerInfoPanel>();
if (legacy.enabled) throw new System.InvalidOperationException("Legacy score writer still enabled");
var field = typeof(RYU._01.Script.Leaderboard.MatchReporter).GetField("Reported", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
var callback = (System.Action<RYU._01.Script.Leaderboard.MatchReportResult>)field.GetValue(null);
if (callback == null) throw new System.InvalidOperationException("Result subscription missing");
try
{
    callback(new RYU._01.Script.Leaderboard.MatchReportResult { delta = 17, score = 1234 });
    if (text.text != "1234점") throw new System.InvalidOperationException("New score not shown: " + text.text);
    var result = new { status = "completed", displayed = text.text, legacyDisabled = !legacy.enabled, cloudWrite = false, scope = "Actual result event updates bound menu text; no ranked match submitted" };
    System.IO.File.WriteAllText("Logs/AugmentLink27/ProfileScore.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
    return result;
}
finally { text.SetText(original); }

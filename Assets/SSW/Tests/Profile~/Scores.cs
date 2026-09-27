var checks = new System.Collections.Generic.List<string>();
void Check(bool value, string name)
{
    if (!value) throw new System.InvalidOperationException(name);
    checks.Add(name);
}
var score = new SSW.ProfileScore();
Check(!score.HasValue, "missing score is not a zero score");
int first = score.BeginRead();
Check(score.CompleteRead(first, 0) && score.HasValue && score.Value == 0, "real zero score is displayed");
int beforeMatch = score.BeginRead();
score.Report(1017);
Check(!score.CompleteRead(beforeMatch, 1000) && score.Value == 1017, "late query cannot overwrite completed match");
int afterMatch = score.BeginRead();
Check(!score.CompleteRead(afterMatch, 1000) && score.Value == 1017, "menu reentry preserves result before leaderboard catches up");
Check(score.CompleteRead(score.BeginRead(), 1017), "matching leaderboard confirms result");
int older = score.BeginRead();
int newer = score.BeginRead();
Check(score.CompleteRead(newer, 1030), "newer refresh accepted");
Check(!score.CompleteRead(older, 1017) && score.Value == 1030, "older refresh cannot replace newer refresh");
score.Report(999);
Check(score.Value == 999 && !score.CompleteRead(score.BeginRead(), 1030), "loss lowers score and still rejects stale leaderboard");
Check(score.CompleteRead(score.BeginRead(), 999), "loss is confirmed by matching leaderboard");
var other = new SSW.ProfileScore();
Check(!other.HasValue && score.Value == 999, "account score records stay separate");
var result = new { status = "completed", checks, count = checks.Count, cloudWrite = false };
System.IO.Directory.CreateDirectory("Logs/Profile27");
System.IO.File.WriteAllText("Logs/Profile27/Scores.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return result;

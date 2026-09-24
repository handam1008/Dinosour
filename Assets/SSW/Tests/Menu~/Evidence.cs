if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop play mode first");
string source = System.IO.Path.GetFullPath("Assets/Logs/Jobs24/leaderboard.png");
string target = System.IO.Path.GetFullPath("Logs/Jobs24/leaderboard.png");
if (System.IO.File.Exists(source)) System.IO.File.Copy(source, target, true);
var files = System.IO.Directory.GetFiles("Assets/Logs", "*", System.IO.SearchOption.AllDirectories);
if (System.Array.Exists(files, x => !x.EndsWith(".meta") && System.IO.Path.GetFileName(x) != "leaderboard.png")) throw new System.InvalidOperationException("Unexpected files in screenshot directory");
AssetDatabase.DeleteAsset("Assets/Logs");
return target;

UnityEditor.SessionState.SetInt("practice.savedJob", (int)SSW.PlayerJobStorage.Load());
SSW.PlayerJobStorage.Save(SSW.PlayerJob.Swordsman);
UnityEditor.EditorApplication.isPlaying = true;
return "started";

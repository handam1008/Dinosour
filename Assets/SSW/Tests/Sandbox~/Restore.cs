var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
SSW.PlayerJobStorage.Save((SSW.PlayerJob)UnityEditor.SessionState.GetInt("practice.savedJob", (int)SSW.PlayerJob.Swordsman));
return new { scene = scene.path, dirty = scene.isDirty, job = SSW.PlayerJobStorage.Load().ToString() };

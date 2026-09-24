var muted = UnityEditor.EditorUtility.audioMasterMute;
UnityEditor.EditorUtility.audioMasterMute = false;
var audio = SSW.GameAudio.Current;
var path = "D:/unity_project/Mushrooms/Logs/Audio/smoke.txt";
var checks = new System.Collections.Generic.List<string>();
void Check(bool value, string text) { if (!value) throw new System.Exception(text); checks.Add("PASS " + text); System.IO.File.WriteAllLines(path, checks); }
var probe = audio.gameObject.AddComponent<SSW.SoundProbe>();
System.Collections.IEnumerator Run()
{
    try
    {
        Check(object.ReferenceEquals(DevLib.ServiceLocator.ServiceLocator.Get<DevLib.ServiceLocator.IAudioService>(), audio), "Existing audio interface uses the shared service");
        Check(probe.Read().sources == 25 && probe.Read().listeners == 1, "One listener and 24 pooled effects plus one music source");
        probe.Execute(1, 1f, 0.7f); probe.Execute(2, 1f, 0f); probe.Execute(0, 0f, 0f);
        probe.Execute(3, 0f, 0f);
        yield return new UnityEngine.WaitForSecondsRealtime(0.5f);
        var signal = probe.Read();
        System.IO.File.WriteAllText("D:/unity_project/Mushrooms/Logs/Audio/signal.json", UnityEngine.JsonUtility.ToJson(signal, true));
        Check(signal.peak > 0.001f && signal.musicPeak > 0.001f, "Music produces a real output signal");
        probe.Execute(3, 0f, 0f);
        Check(System.Linq.Enumerable.Single(probe.Read().cues, c => c.name == "TestMusic").count == 1, "Repeating the current music does not restart it");
        probe.Execute(2, 0f, 0f); probe.Execute(0, 0f, 0f);
        yield return new UnityEngine.WaitForSecondsRealtime(0.3f);
        Check(probe.Read().peak > 0.001f, "Muting SFX leaves music audible");
        probe.Execute(1, 1f, 0f);
        yield return new UnityEngine.WaitForSecondsRealtime(0.2f);
        probe.Execute(0, 0f, 0f);
        yield return new UnityEngine.WaitForSecondsRealtime(0.25f);
        Check(probe.Read().peak < 0.00001f, "BGM and SFX zero produce silence");
        probe.Execute(2, 1f, 0f); probe.Execute(0, 0f, 0f);
        audio.PlaySfx(audio.Bank.Cast(SSW.PlayerJob.Swordsman, SSW.CastKind.Press));
        yield return new UnityEngine.WaitForSecondsRealtime(0.5f);
        Check(probe.Read().effectsPeak > 0.001f && probe.Read().peak > 0.001f, "SFX remains audible with music muted");
        probe.Execute(4, 0f, 0f); probe.Execute(5, 0f, 0f);
        UnityEngine.Time.timeScale = 0f;
        probe.Execute(6, 0f, 0f);
        Check(probe.Read().sources == 25 && probe.Read().active <= 24, "80 effects reuse the fixed pool");
        float duration = audio.Bank.Cast(SSW.PlayerJob.Swordsman, SSW.CastKind.Press).clip.length / 1.45f;
        yield return new UnityEngine.WaitForSecondsRealtime(duration + 0.3f);
        Check(probe.Read().active == 0, "Paused gameplay still returns completed voices");
        UnityEngine.Time.timeScale = 1f;
        var legacy = UnityEngine.ScriptableObject.CreateInstance<DevLib.SoundSystem.Runtime.SoundClipSO>();
        legacy.name = "LegacyTest";
        legacy.clip = audio.Bank.Cast(SSW.PlayerJob.Swordsman, SSW.CastKind.Press).clip;
        legacy.volume = 0.2f;
        DevLib.ServiceLocator.ServiceLocator.Get<DevLib.ServiceLocator.IAudioService>().PlaySfx(legacy, 12);
        yield return new UnityEngine.WaitForSecondsRealtime(0.1f);
        Check(probe.Read().active == 1, "Legacy SoundClipSO with End Time zero plays normally");
        audio.StopSfx(12);
        Check(probe.Read().active == 0, "Stopping a named SFX channel releases its voice");
        UnityEngine.Object.Destroy(legacy);
        var cue = UnityEngine.Object.Instantiate(audio.Bank.Cast(SSW.PlayerJob.Swordsman, SSW.CastKind.Press));
        cue.name = "EmitterTest"; cue.isLoop = true;
        var obj = new UnityEngine.GameObject("Emitter Test");
        var emitter = obj.AddComponent<SSW.SoundEmitter>();
        var serial = new UnityEditor.SerializedObject(emitter);
        serial.FindProperty("_cue").objectReferenceValue = cue;
        serial.ApplyModifiedPropertiesWithoutUndo();
        emitter.Play(); emitter.Play();
        yield return new UnityEngine.WaitForSecondsRealtime(0.1f);
        Check(probe.Read().active == 1, "Emitter reuses its own channel");
        emitter.Stop();
        Check(probe.Read().active == 0, "Emitter Stop stops a looping sound");
        UnityEngine.Object.Destroy(obj); UnityEngine.Object.Destroy(cue);
        var menu = UnityEngine.Object.FindFirstObjectByType<SSW.MainMenuController>();
        menu.ShowSettings();
        yield return new UnityEngine.WaitForSecondsRealtime(0.5f);
        var data = new UnityEditor.SerializedObject(menu);
        foreach (string name in new [] { "_masterVolume", "_bgmVolume", "_sfxVolume" })
            Check(data.FindProperty(name).objectReferenceValue != null, "Menu binds " + name);
        checks.Add("DONE " + checks.Count); System.IO.File.WriteAllLines(path, checks);
    }
    finally
    {
        UnityEngine.Time.timeScale = 1f;
        probe.Execute(7, 0f, 0f);
        UnityEngine.Object.Destroy(probe);
        UnityEditor.EditorUtility.audioMasterMute = muted;
    }
}
audio.StartCoroutine(Run());
return path;

var menu = UnityEngine.Object.FindFirstObjectByType<SSW.MainMenuController>();
menu.ShowPlay();
menu.PlaySandbox();
var sandbox = UnityEngine.Object.FindFirstObjectByType<SSW.SandboxMapMenuUI>();
var field = typeof(SSW.SandboxMapMenuUI).GetField("_practiceButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
((UnityEngine.UI.Button)field.GetValue(sandbox)).onClick.Invoke();
return "menu sandbox invoked";

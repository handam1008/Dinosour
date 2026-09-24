
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path != "Assets/SSW/MainMenu.unity" || scene.isDirty || EditorApplication.isPlaying) throw new System.InvalidOperationException("MainMenu must be clean and stopped");
var roots = scene.GetRootGameObjects();
var canvas = roots.Single(x => x.name == "Canvas").transform;
var menu = roots.Single(x => x.name == "MainMenuController").GetComponent<SSW.MainMenuController>();
var main = canvas.Find("MainPanel");
if (canvas.Find("LeaderboardPanel") != null) throw new System.InvalidOperationException("Leaderboard already exists");
var source = main.Find("Button_Settings").GetComponent<UnityEngine.UI.Button>();
var open = UnityEngine.Object.Instantiate(source, main);
open.name = "Button_Leaderboard";
open.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
UnityEditor.Events.UnityEventTools.AddPersistentListener(open.onClick, menu.ShowLeaderboard);
open.GetComponentInChildren<UnityEngine.UI.Text>().text = "리더보드";
var buttons = new[] { main.Find("Button_Play"), main.Find("Button_Jobs"), open.transform, source.transform };
for (int i = 0; i < buttons.Length; i++)
{
    var rect = (RectTransform)buttons[i];
    rect.anchoredPosition = new Vector2((i - 1.5f) * 330f, -110f);
    rect.sizeDelta = new Vector2(306f, 230f);
}
RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
{
    var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
    rect.SetParent(parent, false);
    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
    rect.sizeDelta = size;
    rect.anchoredPosition = position;
    return rect;
}
var panel = Rect("LeaderboardPanel", canvas, Vector2.zero, Vector2.zero);
panel.anchorMin = Vector2.zero;
panel.anchorMax = Vector2.one;
var group = panel.gameObject.AddComponent<CanvasGroup>();
var shell = Rect("Shell", panel, new Vector2(1180f, 680f), Vector2.zero);
var background = shell.gameObject.AddComponent<UnityEngine.UI.Image>();
background.color = new Color(0.13f, 0.13f, 0.18f);
background.raycastTarget = false;
var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/RYU/서울사이버대학체B SDF.asset");
TMPro.TextMeshProUGUI Label(string name, string value, Transform parent, Vector2 size, Vector2 position, float fontSize, TMPro.TextAlignmentOptions align)
{
    var rect = Rect(name, parent, size, position);
    var label = rect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
    label.font = font;
    label.text = value;
    label.fontSize = fontSize;
    label.color = new Color(0.94f, 0.93f, 0.98f);
    label.alignment = align;
    label.raycastTarget = false;
    label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
    return label;
}
Label("Title", "리더보드", shell, new Vector2(1036f, 70f), new Vector2(0f, 250f), 48f, TMPro.TextAlignmentOptions.Left);
var header = Rect("Columns", shell, new Vector2(1036f, 64f), new Vector2(0f, 152f));
var fill = header.gameObject.AddComponent<UnityEngine.UI.Image>();
fill.color = new Color(0.20f, 0.17f, 0.30f);
fill.raycastTarget = false;
Label("Rank", "순위", header, new Vector2(120f, 48f), new Vector2(-422f, 0f), 28f, TMPro.TextAlignmentOptions.Center);
Label("Name", "닉네임", header, new Vector2(610f, 48f), new Vector2(-25f, 0f), 28f, TMPro.TextAlignmentOptions.Left);
Label("Score", "승점", header, new Vector2(170f, 48f), new Vector2(395f, 0f), 28f, TMPro.TextAlignmentOptions.Right);
Label("Empty", "아직 기록이 없습니다", shell, new Vector2(1000f, 64f), new Vector2(0f, -40f), 30f, TMPro.TextAlignmentOptions.Center).color = new Color(0.64f, 0.63f, 0.73f);
var back = UnityEngine.Object.Instantiate(canvas.Find("PlayPanel/Button_뒤로").GetComponent<UnityEngine.UI.Button>(), panel);
back.name = "BackButton";
((RectTransform)back.transform).anchoredPosition = new Vector2(0f, -410f);
back.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
UnityEditor.Events.UnityEventTools.AddPersistentListener(back.onClick, menu.ShowMain);
var serialized = new SerializedObject(menu);
serialized.FindProperty("_leaderboardPanel").objectReferenceValue = group;
serialized.ApplyModifiedPropertiesWithoutUndo();
panel.gameObject.SetActive(false);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new { panel = panel.name, button = open.name, font = font.name };
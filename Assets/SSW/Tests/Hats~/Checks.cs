using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SSW;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class HatChecks
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> Checks = new List<string>();
    static readonly List<object> Poses = new List<object>();
    static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
    static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        Checks.Add(name);
    }
    static Rect Bounds(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners.Min(p => p.x), corners.Min(p => p.y), corners.Max(p => p.x), corners.Max(p => p.y));
    }
    static void Layout(MenuDinosaurPreview preview, JobDefinition definition, SpriteRenderer sourceBody, SpriteRenderer sourceHat, string name)
    {
        var body = Get<Image>(preview, "_dinosaurImage");
        var hat = preview.HatImage;
        var rect = body.GetPixelAdjustedRect();
        float pixels = Mathf.Min(rect.width / body.sprite.rect.width, rect.height / body.sprite.rect.height);
        var drawn = body.sprite.rect.size * pixels;
        var bottom = rect.min + Vector2.Scale(rect.size - drawn, body.rectTransform.pivot);
        Vector2 point = body.transform.InverseTransformPoint(hat.transform.position);
        var actual = (point - bottom) / drawn;
        Vector2 attachment = sourceBody.transform.InverseTransformPoint(sourceHat.transform.parent.TransformPoint(definition.WorldOffset));
        Vector2 expected = (attachment - (Vector2)body.sprite.bounds.min) / (Vector2)body.sprite.bounds.size;
        Check(hat.enabled && hat.sprite == definition.HatSprite, name + " selected sprite");
        Check(hat.transform.parent == body.transform, name + " shared body parent");
        Check(Vector2.Distance(hat.rectTransform.pivot, definition.HatSprite.pivot / definition.HatSprite.rect.size) < 0.0001f,
            name + " imported sprite pivot");
        Check(Vector2.Distance(actual, expected) < 0.001f, name + " actual head anchor in sprite space");
        Check(Mathf.Abs(hat.rectTransform.rect.width / hat.rectTransform.rect.height
            - definition.HatSprite.rect.width / definition.HatSprite.rect.height) < 0.001f, name + " sprite aspect");
        Check(Mathf.Abs(hat.rectTransform.rect.width * Mathf.Abs(hat.transform.localScale.x) / drawn.x
            - 560f / 340f * definition.PreviewScale) < 0.001f, name + " existing relative preview size");
        Check(Quaternion.Angle(hat.transform.localRotation,
            Quaternion.Inverse(sourceBody.transform.rotation) * sourceHat.transform.rotation) < 0.001f, name + " source rotation");
        Poses.Add(new { name, job = definition.Job.ToString(), anchor = actual.ToString("F5"), expected = expected.ToString("F5"),
            pivot = hat.rectTransform.pivot.ToString("F5"), facing = Mathf.Sign(preview.DinosaurTransform.localScale.x) });
    }
    static void Space(JobSettingsPanel panel, MenuDinosaurPreview preview, string name)
    {
        var hat = Bounds(preview.HatImage.rectTransform);
        var body = Bounds(Get<Image>(preview, "_dinosaurImage").rectTransform);
        var tooltip = Bounds(Get<Text>(panel, "_tooltip").rectTransform);
        var button = Bounds(Get<Button>(panel, "_equipButton").GetComponent<RectTransform>());
        Check(hat.yMax + 4f < tooltip.yMin, name + " hat clears description");
        Check(body.yMin > button.yMax, name + " body clears equip button");
        Check(hat.xMin >= 0f && hat.xMax <= Screen.width && hat.yMin >= 0f && hat.yMax <= Screen.height,
            name + " hat within screen");
    }
    static async Task Capture(string name)
    {
        await Task.Delay(200);
        ScreenCapture.CaptureScreenshot("Logs/Hat26/" + name + ".png");
        await Task.Delay(150);
    }
    public static async Task RunAsync()
    {
        Directory.CreateDirectory("Logs/Hat26");
        var errors = new List<string>();
        void Log(string message, string stack, LogType kind)
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert) errors.Add(message + "\n" + stack);
        }
        Application.logMessageReceived += Log;
        string failure = null;
        Vs versus = null;
        DraftScreen draft = null;
        var menu = Object.FindObjectsByType<MainMenuController>().Single();
        var savedJob = (PlayerJob)SessionState.GetInt("augment.savedJob", (int)PlayerJobStorage.Load());
        var viewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var window = EditorWindow.GetWindow(viewType);
        var sizeProperty = viewType.GetProperty("selectedSizeIndex", Fields | BindingFlags.Public);
        int savedSize = (int)sizeProperty.GetValue(window);
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var sizesInstance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var group = sizesType.GetMethod("GetGroup").Invoke(sizesInstance, new[] {
            Enum.Parse(typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone") });
        int addedIndex = -1;
        try
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SSW/Resources/Network/Player.prefab");
            var sourceHat = Get<SpriteRenderer>(source.GetComponentInChildren<PlayerHatView>(true), "_renderer");
            var sourceBody = sourceHat.transform.parent.parent.GetComponent<SpriteRenderer>();
            var definitions = Resources.Load<JobCatalog>("JobCatalog").Definitions;
            menu.ShowJobs();
            await Task.Delay(1000);
            var panel = Object.FindObjectsByType<JobSettingsPanel>().Single();
            var preview = Get<MenuDinosaurPreview>(panel, "_preview");
            var slots = Get<JobSlotControl[]>(panel, "_slots");
            foreach (var definition in definitions)
            {
                var slot = slots.Single(s => s.Definition == definition);
                ExecuteEvents.Execute(slot.gameObject, new PointerEventData(EventSystem.current)
                    { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
                Check(panel.SelectedJob == definition.Job, definition.Job + " actual slot selects job");
                ExecuteEvents.Execute(Get<Button>(panel, "_equipButton").gameObject,
                    new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                Check(panel.EquippedJob == definition.Job && PlayerJobStorage.Load() == definition.Job, definition.Job + " equip persists");
                menu.ShowMain();
                await Task.Delay(350);
                menu.ShowJobs();
                await Task.Delay(900);
                Check(panel.SelectedJob == definition.Job && panel.EquippedJob == definition.Job, definition.Job + " reopen restores job");
                Layout(preview, definition, sourceBody, sourceHat, definition.Job + " menu");
                Space(panel, preview, definition.Job + " menu");
                await Capture("menu-" + definition.Job);
            }
            var selected = definitions.Last();
            var frames = new HashSet<int>();
            var heights = new List<float>();
            for (int i = 0; i < 12; i++)
            {
                await Task.Delay(65);
                frames.Add(preview.FrameIndex);
                heights.Add(preview.DinosaurTransform.anchoredPosition.y);
                Layout(preview, selected, sourceBody, sourceHat, "animated " + i);
            }
            Check(frames.Count == 4 && heights.Max() - heights.Min() > 1f, "all idle frames and bob preserve attachment");
            var bodyRect = Get<Image>(preview, "_dinosaurImage").rectTransform;
            var oldSize = bodyRect.sizeDelta;
            var oldPivot = bodyRect.pivot;
            bodyRect.sizeDelta = new Vector2(110f, -20f);
            bodyRect.pivot = new Vector2(0.3f, 0.6f);
            await Task.Delay(300);
            Layout(preview, selected, sourceBody, sourceHat, "non-square body with offset pivot");
            bodyRect.sizeDelta = oldSize;
            bodyRect.pivot = oldPivot;
            await Task.Delay(300);
            addedIndex = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            var custom = Activator.CreateInstance(typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize"),
                Enum.Parse(typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), 1366, 768, "HatChecks");
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { custom });
            sizeProperty.SetValue(window, addedIndex);
            await Task.Delay(500);
            Check(Screen.width == 1366 && Screen.height == 768, "actual 1366x768 Game View");
            foreach (var definition in definitions)
            {
                ExecuteEvents.Execute(slots.Single(s => s.Definition == definition).gameObject,
                    new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                await Task.Delay(150);
                Layout(preview, definition, sourceBody, sourceHat, definition.Job + " 1366x768");
                Space(panel, preview, definition.Job + " 1366x768");
            }
            await Capture("menu-1366");
            sizeProperty.SetValue(window, savedSize);
            await Task.Delay(400);
            foreach (var definition in definitions)
            {
                versus = Object.Instantiate(Resources.Load<Vs>("UI/Vs"));
                versus.Show(default, definition.Job, 1, default, definition.Job, 2, () => { });
                await Task.Delay(950);
                var left = Get<MenuDinosaurPreview>(Get<VsCard>(versus, "_left"), "_preview");
                var right = Get<MenuDinosaurPreview>(Get<VsCard>(versus, "_right"), "_preview");
                Layout(left, definition, sourceBody, sourceHat, definition.Job + " VS left");
                Layout(right, definition, sourceBody, sourceHat, definition.Job + " VS right");
                Check(left.DinosaurTransform.localScale.x > 0f && right.DinosaurTransform.localScale.x < 0f,
                    definition.Job + " VS opposite facing");
                Check(Mathf.Abs(Bounds(left.HatImage.rectTransform).width - Bounds(right.HatImage.rectTransform).width) < 0.1f,
                    definition.Job + " VS equal mirrored size");
                foreach (var side in new[] { left, right })
                {
                    var face = Bounds((RectTransform)side.transform.parent);
                    var hat = Bounds(side.HatImage.rectTransform);
                    Check(hat.xMin >= face.xMin && hat.xMax <= face.xMax && hat.yMin >= face.yMin && hat.yMax <= face.yMax,
                        definition.Job + " complete hat within VS mask " + side.name + side.DinosaurTransform.localScale.x);
                }
                await Capture("vs-" + definition.Job);
                versus.Close();
                await Task.Delay(650);
                Check(versus == null, definition.Job + " actual VS teardown");
            }
            draft = Object.Instantiate(Resources.Load<DraftScreen>("UI/Draft"));
            foreach (var watching in new[] { false, true })
            {
                foreach (var definition in definitions)
                {
                    draft.SetPlayer(definition.Job, watching, null);
                    await Task.Delay(80);
                    var portrait = Get<DraftPortrait>(draft, "_portrait");
                    Layout(Get<MenuDinosaurPreview>(portrait, "_preview"), definition, sourceBody, sourceHat,
                        definition.Job + " draft " + watching);
                    Check(portrait.OnRight == watching, definition.Job + " draft spectator side " + watching);
                }
            }
            Object.Destroy(draft.gameObject);
            draft = null;
            await Task.Delay(150);
            Check(errors.Count == 0, "no runtime errors");
        }
        catch (Exception ex) { failure = ex.ToString(); }
        finally
        {
            if (versus != null) Object.Destroy(versus.gameObject);
            if (draft != null) Object.Destroy(draft.gameObject);
            PlayerJobStorage.Save(savedJob);
            sizeProperty.SetValue(window, savedSize);
            if (addedIndex >= 0)
            {
                group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { addedIndex });
            }
            menu.ShowMain();
            Application.logMessageReceived -= Log;
            if (errors.Count != 0 && failure == null) failure = "Runtime or cleanup errors";
            File.WriteAllText("Logs/Hat26/checks.json", JsonConvert.SerializeObject(new
            {
                passed = failure == null, count = Checks.Count, failure, checks = Checks, poses = Poses, errors,
                savedJob = savedJob.ToString(), restoredSize = savedSize,
                coverage = "Editor MainMenu selection/equip/reopen, actual VS and draft UI methods, two Game View sizes; no separate process or two-PC match"
            }, Formatting.Indented));
        }
    }
}

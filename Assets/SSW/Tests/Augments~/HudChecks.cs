using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SSW;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class HudChecks
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> Checks = new List<string>();
    static readonly List<object> Jobs = new List<object>();
    static readonly List<object> Sizes = new List<object>();
    static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
    static Transform CanvasOf(JobAugmentHUD hud) => hud.transform.Find("JobAugmentHUDCanvas");
    static JobAugmentIconUI[] Icons(JobAugmentHUD hud) => hud.GetComponentsInChildren<JobAugmentIconUI>(true);

    sealed class Source : IAugmentSource
    {
        readonly List<Augment> _owned = new List<Augment>();
        public event Action<Augment> AugmentGranted;
        public IReadOnlyList<Augment> Owned => _owned;
        public int Subscribers => AugmentGranted?.GetInvocationList().Length ?? 0;
        public bool TryGrant(Augment augment)
        {
            if (_owned.Contains(augment)) return false;
            _owned.Add(augment);
            AugmentGranted?.Invoke(augment);
            return true;
        }
        public void Repeat(Augment augment) => AugmentGranted?.Invoke(augment);
    }

    static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        Checks.Add(name);
        File.WriteAllText("Logs/Augment26/progress.json", JsonConvert.SerializeObject(new { count = Checks.Count, last = name }));
    }

    static async Task Wait(Func<bool> ready, string name)
    {
        double end = Time.realtimeSinceStartupAsDouble + 15;
        while (!ready())
        {
            if (Time.realtimeSinceStartupAsDouble >= end) throw new TimeoutException(name);
            await Task.Delay(30);
        }
        await Task.Delay(200);
    }

    static bool Inside(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return corners.All(p => p.x >= -1 && p.y >= -1 && p.x <= Screen.width + 1 && p.y <= Screen.height + 1);
    }

    static void Hover(JobAugmentIconUI icon, bool enter)
    {
        var data = new PointerEventData(EventSystem.current);
        if (enter) ExecuteEvents.Execute(icon.gameObject, data, ExecuteEvents.pointerEnterHandler);
        else ExecuteEvents.Execute(icon.gameObject, data, ExecuteEvents.pointerExitHandler);
    }

    static void CheckTooltip(JobAugmentHUD hud, string name)
    {
        var icon = Icons(hud).First(x => x.Augment.icon != null);
        var tip = CanvasOf(hud).Find("JobAugmentTooltip");
        Hover(icon, true);
        Check(tip.gameObject.activeInHierarchy, name + " hover opens");
        Check(tip.Find("Title").GetComponent<Text>().text == icon.Augment.displayName, name + " real title");
        Check(tip.Find("Description").GetComponent<Text>().text == icon.Augment.description, name + " real description");
        Check(Inside((RectTransform)tip), name + " tooltip within screen");
        var corners = new Vector3[4];
        ((RectTransform)tip).GetWorldCorners(corners);
        var pointer = new PointerEventData(EventSystem.current) { position = (corners[0] + corners[2]) * 0.5f };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Check(hits.All(h => !h.gameObject.transform.IsChildOf(tip)), name + " tooltip passes raycasts");
        Hover(icon, false);
        Check(!tip.gameObject.activeSelf && icon.transform.localScale == Vector3.one, name + " exit hides");
    }

    static void CheckOwned(NetPlayer player, string name)
    {
        var owned = player.GetComponent<AugmentDrafter>().Owned;
        var hud = player.GetComponent<JobAugmentHUD>();
        var icons = Icons(hud);
        Check(hud.IconCount == owned.Count && icons.Length == owned.Count, name + " owned count");
        Check(icons.Select(i => i.Augment).SequenceEqual(owned), name + " exact acquired order");
        Check(icons.All(i => i.transform.Find("Icon").GetComponent<Image>().sprite == i.Augment.icon), name + " real sprite references");
    }

    public static async Task RunAsync()
    {
        string failure = null;
        var deck = AssetDatabase.LoadAssetAtPath<NetDeck>(AssetDatabase.GUIDToAssetPath("a6f7ec0ef4b66634db8915c29fba2521"));
        var items = Enumerable.Range(0, deck.Count).Select(deck.At).Distinct().ToArray();
        var savedJob = PlayerJobStorage.Load();
        var viewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var window = EditorWindow.GetWindow(viewType);
        var sizeProperty = viewType.GetProperty("selectedSizeIndex", Fields | BindingFlags.Public);
        int savedSize = (int)sizeProperty.GetValue(window);
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var sizesInstance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var groupKind = Enum.Parse(typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone");
        var group = sizesType.GetMethod("GetGroup").Invoke(sizesInstance, new[] { groupKind });
        var customSizes = new List<int>();
        int AddSize(int width, int height)
        {
            int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            var kind = Enum.Parse(typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
            var size = Activator.CreateInstance(typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize"), kind, width, height, "HudChecks");
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            customSizes.Add(index);
            return index;
        }
        var runtimeErrors = new List<string>();
        void Log(string message, string stack, LogType kind)
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
                runtimeErrors.Add(message + "\n" + stack);
        }
        Application.logMessageReceived += Log;
        GameObject fixture = null;
        try
        {
            foreach (var job in Enum.GetValues(typeof(PlayerJob)).Cast<PlayerJob>().Where(j => j != PlayerJob.None))
            {
                var old = NetGame.Current.Practice;
                if (old.Player.Job != job || old.Player.Draft.OwnedCount > 0
                    || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "SuperUltraLegendScene")
                {
                    PlayerJobStorage.Save(job);
                    NetGame.Current.OpenPracticeScene("SuperUltraLegendScene");
                    await Wait(() => NetGame.Current.Practice != null && !ReferenceEquals(NetGame.Current.Practice, old)
                        && NetGame.Current.Practice.Player != null && NetGame.Current.Practice.Player.CanAct, job + " reload");
                }
                var practice = NetGame.Current.Practice;
                var player = practice.Player;
                var hud = player.GetComponent<JobAugmentHUD>();
                Check(player.Job == job && player.IsOwner, job + " real local player");
                Check(hud.IconCount == 0 && CanvasOf(hud) == null, job + " new session empty");
                int jobCandidates = deck.Candidates(job, player.Draft.Owned, false).Count;
                practice.OpenDraft(true);
                await Wait(() => player.Draft.Offer.x >= 0, job + " common offer");
                player.Draft.Choose(0);
                await Task.Delay(250);
                if (jobCandidates > 0)
                {
                    Check(!player.Draft.Ready && !player.Draft.Common, job + " job offer");
                    player.Draft.Choose(0);
                }
                await Wait(() => !practice.Drafting && player.Draft.Ready, job + " selected");
                CheckOwned(player, job + " selected");
                Check(hud.IconCount == (jobCandidates > 0 ? 2 : 1), job + " choices only");
                var owned = player.Draft.Owned.ToArray();
                player.Draft.Restore(owned.Concat(owned));
                await Task.Delay(200);
                CheckOwned(player, job + " duplicate restore");
                if (Icons(hud).Any(i => i.Augment.icon != null)) CheckTooltip(hud, job.ToString());
                hud.SetJobVisible(false);
                Check(CanvasOf(hud).gameObject.activeInHierarchy, job + " inventory independent of magician visibility");
                hud.SetJobVisible(true);
                hud.enabled = false;
                Check(!CanvasOf(hud).gameObject.activeInHierarchy, job + " disable hides");
                hud.enabled = true;
                await Task.Delay(200);
                CheckOwned(player, job + " reenable");
                practice.ResetMap();
                await Wait(() => !ReferenceEquals(practice.Player, player), job + " respawn");
                Check(practice.Player.Draft.Owned.SequenceEqual(owned), job + " respawn retains ownership");
                CheckOwned(practice.Player, job + " respawn");
                Check(practice.Target.GetComponent<JobAugmentHUD>().IconCount == 0, job + " remote dummy hidden");
                Check(Object.FindObjectsByType<Canvas>().Count(c => c.name == "JobAugmentHUDCanvas") == 1, job + " one local HUD after respawn");
                Jobs.Add(new { job = job.ToString(), jobCandidates, owned = practice.Player.GetComponent<AugmentDrafter>().Owned.Select(a => new { a.name, a.displayName, icon = a.icon != null }).ToArray() });
            }

            var prefab = Resources.Load<NetPlayer>("Network/Player");
            int commonId = deck.Candidates(PlayerJob.Magician, Array.Empty<int>(), true).First();
            int magicId = deck.Candidates(PlayerJob.Magician, Array.Empty<int>(), false)
                .First(id => deck.At(id) is MagicianAugment augment && augment.type == MagicianAugmentType.EmergencyMagic);
            var remote = Object.Instantiate(prefab, new Vector3(0, 5, 0), Quaternion.identity);
            remote.Init(PlayerJob.Magician, -1);
            remote.NetworkObject.SpawnWithOwnership(1, true);
            remote.Draft.Restore(new[] { commonId, magicId });
            await Task.Delay(200);
            Check(remote.GetComponent<JobAugmentHUD>().IconCount == 0 && CanvasOf(remote.GetComponent<JobAugmentHUD>()) == null, "remote magician grants never create HUD");
            remote.NetworkObject.Despawn();

            var restored = Object.Instantiate(prefab, new Vector3(0, 5, 0), Quaternion.identity);
            restored.Init(PlayerJob.Magician, 1);
            var networkOwned = Get<NetworkList<int>>(restored.Draft, "_owned");
            networkOwned.Dispose();
            typeof(NetDraft).GetField("_owned", Fields).SetValue(restored.Draft, new NetworkList<int>(new[] { commonId, magicId }));
            var mainPlayer = NetGame.Current.Practice.Player;
            mainPlayer.Input.enabled = false;
            restored.NetworkObject.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId, true);
            await Task.Delay(200);
            CheckOwned(restored, "prefilled network list on spawn");
            Check(restored.GetComponent<JobAugmentHUD>().IconCount == 2, "initial replication restores icons");
            restored.GetComponent<MagicianAugmentController>().ConsumeEmergencyHealBonus();
            var cooldownIcon = Icons(restored.GetComponent<JobAugmentHUD>()).Single(i => i.Augment == deck.At(magicId));
            cooldownIcon.RefreshCooldown();
            Check(cooldownIcon.GetComponentInChildren<RadialCooldownUI>().Progress > 0, "existing magician cooldown overlay retained");
            restored.NetworkObject.Despawn();
            await Task.Delay(200);
            mainPlayer.Input.enabled = true;

            fixture = new GameObject("HudChecksFixture");
            var testHud = fixture.AddComponent<JobAugmentHUD>();
            var source = new Source();
            foreach (var item in items) source.TryGrant(item);
            testHud.Bind(source);
            await Task.Delay(200);
            Check(testHud.IconCount == items.Length && source.Subscribers == 1, "fixture preowned catalog renders unique items");
            source.Repeat(items[0]);
            Check(testHud.IconCount == items.Length, "fixture duplicate event adds no stack");
            testHud.Bind(source);
            await Task.Delay(200);
            Check(source.Subscribers == 1 && Icons(testHud).Length == items.Length, "fixture rebind keeps one subscription and no duplicate rows");

            foreach (int size in new[] { 3, 4, 5, AddSize(1024, 768), AddSize(600, 900), AddSize(1920, 600) })
            {
                sizeProperty.SetValue(window, size);
                window.Repaint();
                await Task.Delay(700);
                var canvas = CanvasOf(testHud);
                var viewport = (RectTransform)canvas.Find("JobAugmentViewport");
                var scroll = viewport.GetComponent<ScrollRect>();
                Check(Inside(viewport), Screen.width + " viewport within screen");
                Check(scroll.vertical && scroll.content.rect.height > viewport.rect.height, Screen.width + " overflow scroll enabled");
                scroll.verticalNormalizedPosition = 1f;
                await Task.Delay(150);
                CheckTooltip(testHud, Screen.width + " overflow");
                var last = Icons(testHud).Last();
                scroll.verticalNormalizedPosition = 0f;
                await Task.Delay(200);
                var center = ((RectTransform)last.transform).TransformPoint(((RectTransform)last.transform).rect.center);
                Check(RectTransformUtility.RectangleContainsScreenPoint(viewport, center), Screen.width + " final item reachable by scrolling");
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = center }, hits);
                Check(hits.Any(h => h.gameObject == last.gameObject), Screen.width + " scrolled item receives hover raycast");
                Hover(last, true);
                scroll.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = Vector2.up });
                await Task.Delay(100);
                Check(!canvas.Find("JobAugmentTooltip").gameObject.activeSelf, Screen.width + " scroll clears tooltip");
                Sizes.Add(new { Screen.width, Screen.height, icons = testHud.IconCount });
            }

            var longest = Icons(testHud).OrderByDescending(i => i.Augment.description?.Length ?? 0).First();
            Hover(longest, true);
            var tip = CanvasOf(testHud).Find("JobAugmentTooltip");
            Check(Inside((RectTransform)tip), "longest real description within screen");
            ScreenCapture.CaptureScreenshot("Logs/Augment26/overflow-tooltip.png");
            await Task.Delay(250);
            float scale = Time.timeScale;
            Time.timeScale = 0;
            Hover(longest, false);
            Hover(longest, true);
            Check(tip.gameObject.activeInHierarchy, "hover works while draft pauses time");
            Time.timeScale = scale;
            testHud.enabled = false;
            Check(source.Subscribers == 0 && !CanvasOf(testHud).gameObject.activeInHierarchy, "disable detaches inventory and hides tooltip");
            testHud.enabled = true;
            await Task.Delay(200);
            Check(source.Subscribers == 1 && Icons(testHud).Length == items.Length, "enable restores exact inventory");
            testHud.Unbind();
            Check(source.Subscribers == 0 && testHud.IconCount == 0 && !CanvasOf(testHud).gameObject.activeSelf, "unbind clears inventory and subscriptions");
            var fresh = new Source();
            testHud.Bind(fresh);
            source.Repeat(items[0]);
            Check(testHud.IconCount == 0, "old inventory events ignored after rebind");
            fresh.TryGrant(items.Last());
            Check(testHud.IconCount == 1 && Icons(testHud).Single().Augment == items.Last(), "new source event shows only newly owned item");
            Object.Destroy(fixture);
            await Task.Delay(200);
            Check(fresh.Subscribers == 0, "destroy detaches inventory subscription");
            fixture = null;
            Check(runtimeErrors.Count == 0, "no runtime errors during UI checks");
        }
        catch (Exception ex) { failure = ex.ToString(); }
        finally
        {
            if (fixture != null) Object.Destroy(fixture);
            PlayerJobStorage.Save(savedJob);
            Time.timeScale = 1;
            sizeProperty.SetValue(window, savedSize);
            foreach (int index in customSizes.AsEnumerable().Reverse())
                group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { index });
            window.Repaint();
            Application.logMessageReceived -= Log;
            File.WriteAllText("Logs/Augment26/checks.json", JsonConvert.SerializeObject(new
            {
                passed = failure == null, count = Checks.Count, failure, checks = Checks, jobs = Jobs, sizes = Sizes, runtimeErrors,
                catalog = new { count = items.Length, missingIcons = items.Where(i => i.icon == null).Select(i => new { path = AssetDatabase.GetAssetPath(i), i.name, i.displayName }).ToArray() },
                coverage = "Editor sandbox host, real grants and respawns; fixture ownership list, overflow and lifecycle; no separate client or two-PC rejoin"
            }, Formatting.Indented));
        }
    }
}

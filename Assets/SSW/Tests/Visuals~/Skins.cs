var game = SSW.NetGame.Current;
if (!UnityEditor.EditorApplication.isPlaying || game == null || !game.Manager.IsServer || !game.CanFight || game.Practice != null || game.Players.Count != 2 || !System.Linq.Enumerable.All(game.Players, p => p.CanAct))
    throw new System.InvalidOperationException("Requires a Playing editor host and one connected client.");
string root = System.IO.Path.GetFullPath("@ROOT@");
string mode = "@MODE@";
bool quick = !string.Equals(mode, "full", System.StringComparison.OrdinalIgnoreCase);
string allowed = System.IO.Path.GetFullPath(UnityEngine.Application.dataPath + "/../Logs/Boundary") + System.IO.Path.DirectorySeparatorChar;
if (!root.StartsWith(allowed, System.StringComparison.OrdinalIgnoreCase))
    throw new System.InvalidOperationException("Use a fresh Logs/Boundary run directory.");
string log = System.IO.Path.Combine(root, "Skins.json");
string clientPath = System.IO.Path.Combine(root, "client.json");
if (System.IO.File.Exists(log)) throw new System.InvalidOperationException("Skins.json already exists.");
if (!System.IO.File.Exists(clientPath)) throw new System.InvalidOperationException("The active client.json probe is required.");
System.IO.Directory.CreateDirectory(root);
var guards = new[] { "visuals.skins.running", "visuals.afterimages.running", "Augments.Effects.Running" };
foreach (string key in guards)
    if (UnityEditor.SessionState.GetBool(key, false)) throw new System.InvalidOperationException("Another player fixture is active: " + key);
const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
System.Reflection.FieldInfo Field(object item, string name)
{
    for (var type = item.GetType(); type != null; type = type.BaseType)
    {
        var found = type.GetField(name, flags);
        if (found != null) return found;
    }
    throw new System.MissingFieldException(item.GetType().Name, name);
}
T Read<T>(object item, string name) => (T)Field(item, name).GetValue(item);
var prefab = Read<SSW.NetPlayer>(game, "_playerPrefab");
var original = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.OrderBy(game.Players, p => p.OwnerClientId), p => new {
    p.OwnerClientId, p.Job, p.Side, p.Info, position = p.Body.position,
    cards = System.Linq.Enumerable.ToArray(p.Draft.Owned), health = p.Health.Current, maximum = p.Health.Max,
    blocked = Read<bool>(p, "_blocked"), progress = p.Cast.Weapon != null ? p.Cast.Weapon.Progress : 0
}));
var catalog = RYU._01.Script.Customize.DinoSkinCatalog.Load();
if (catalog == null) throw new System.InvalidOperationException("The native DinoSkinCatalog resource is missing.");
var skins = System.Linq.Enumerable.ToArray(catalog.Skins);
const string preference = "RYU.DinoSkin.v1";
bool hadPreference = UnityEngine.PlayerPrefs.HasKey(preference);
string rawPreference = UnityEngine.PlayerPrefs.GetString(preference, string.Empty);
string storedSkin = RYU._01.Script.Customize.DinoSkinStorage.LoadId();
var quickSkins = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Take(System.Linq.Enumerable.Where(skins, skin => skin != null && skin.library != null && !string.IsNullOrEmpty(skin.id) && skin.id != storedSkin), 2));
int expectedSkins = quick ? 2 : 24;
int expectedPairs = quick ? 1 : 12;
var jobs = new[] { SSW.PlayerJob.Witch, SSW.PlayerJob.Magician, SSW.PlayerJob.Swordsman, SSW.PlayerJob.Assassin, SSW.PlayerJob.Gambler, SSW.PlayerJob.Gunner };
var checks = new System.Collections.Generic.List<object>();
var cases = new System.Collections.Generic.List<object>();
var packets = new System.Collections.Generic.List<object>();
var failures = new System.Collections.Generic.List<object>();
var errors = new System.Collections.Generic.List<string>();
var covered = new System.Collections.Generic.HashSet<string>();
var hostJobs = new System.Collections.Generic.HashSet<string>();
var clientJobs = new System.Collections.Generic.HashSet<string>();
var basicFields = new[] { "skinId", "skinLibrary", "skinComponents", "trailComponents" };
var spriteFields = new[] { "skinCategory", "skinLabel", "skinSprite", "skinTexture", "skinResolved" };
var requiredFields = quick ? basicFields : System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(basicFields, spriteFields));
bool clientRendererObserved = false;
Newtonsoft.Json.Linq.JObject client = null;
SSW.NetPlayer[] actors = System.Array.Empty<SSW.NetPlayer>();
string current = "preflight";
string clientError = null;
bool replaced = false;
bool prefsUnchanged = true;
double began = UnityEngine.Time.realtimeSinceStartupAsDouble;
int count = 0;
void Check(bool condition, string label, object actual = null, object expected = null)
{
    count++;
    checks.Add(new { stage = current, pass = condition, label, actual, expected });
    if (!condition) throw new System.InvalidOperationException(current + ": " + label);
}
void Save(string status, string error = null)
{
    prefsUnchanged = hadPreference == UnityEngine.PlayerPrefs.HasKey(preference) && rawPreference == UnityEngine.PlayerPrefs.GetString(preference, string.Empty);
    System.IO.File.WriteAllText(log, Newtonsoft.Json.JsonConvert.SerializeObject(new {
        status, current, error, count, mode = quick ? "quick" : "full", expectedSkins, expectedPairs, clientRendererObserved, seconds = UnityEngine.Time.realtimeSinceStartupAsDouble - began,
        protocol = SSW.NetGame.Protocol, build = UnityEngine.Application.buildGUID,
        source = UnityEditor.AssetDatabase.GetAssetPath(catalog), storedSkin, prefsUnchanged,
        coveredIds = System.Linq.Enumerable.ToArray(covered),
        hostJobs = System.Linq.Enumerable.ToArray(hostJobs), clientJobs = System.Linq.Enumerable.ToArray(clientJobs),
        requiredFields, checks, cases, packets, failures, errors,
        dispatch = "Server injects only Fighter.Skin into cloned actors, then real NetworkObject.SpawnAsPlayerObject replicates Info. Not remote customization-button input or session Join validation.",
        fixture = "Caller supplies Map0 isolation, wideground(0,3;60,.6), lag60/jitter10 and idle input. No PlayerPrefs or source assets are changed. Original Info, jobs, cards, HP, positions, progress and blocked flags are restored.",
        scope = quick ? "Two distinct skins, existing jobs, both authoritative renderers and replicated client library/Info. Client rendered sprite is only claimed when its optional probe fields exist. No full catalog, six-job matrix or death test." : "Each of 24 catalog IDs spawns once, both processes inspect both owners, and each owner covers six jobs. Actual death/next-round restoration is delegated to the separate Afterimages test."
    }, Newtonsoft.Json.Formatting.Indented));
}
Newtonsoft.Json.Linq.JObject Remote()
{
    try { client = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(clientPath)); }
    catch (System.IO.IOException) { }
    catch (Newtonsoft.Json.JsonReaderException) { }
    return client ?? throw new System.InvalidOperationException("No complete client probe snapshot.");
}
Newtonsoft.Json.Linq.JObject RemotePlayer(SSW.NetPlayer p, Newtonsoft.Json.Linq.JObject snapshot)
{
    foreach (var item in snapshot["players"])
        if ((ulong)item["id"] == p.OwnerClientId && (ulong)item["objectId"] == p.NetworkObjectId) return (Newtonsoft.Json.Linq.JObject)item;
    return null;
}
async System.Threading.Tasks.Task Wait(float seconds)
{
    float end = UnityEngine.Time.time + seconds;
    double watchdog = UnityEngine.Time.realtimeSinceStartupAsDouble + seconds * 3d + 10d;
    do
    {
        if (!UnityEditor.EditorApplication.isPlaying || game == null || !game.Connected || !game.CanFight || UnityEngine.Time.realtimeSinceStartupAsDouble > watchdog)
            throw new System.TimeoutException(current + ": fixture disconnected, left Playing, or stopped advancing.");
        await System.Threading.Tasks.Task.Delay(16);
    } while (UnityEngine.Time.time < end);
}
async System.Threading.Tasks.Task Until(System.Func<bool> condition, float timeout, string label)
{
    double end = UnityEngine.Time.realtimeSinceStartupAsDouble + timeout;
    while (!condition())
    {
        if (UnityEngine.Time.realtimeSinceStartupAsDouble >= end) throw new System.TimeoutException(current + ": " + label);
        await Wait(0f);
    }
}
void Clear()
{
    typeof(SSW.NetGame).GetMethod("ClearShots", flags).Invoke(game, null);
    foreach (var p in System.Linq.Enumerable.ToArray(game.Players)) p.NetworkObject.Despawn();
}
async System.Threading.Tasks.Task Pair(int index)
{
    double after = game.ServerTime;
    replaced = true;
    Clear();
    var list = new System.Collections.Generic.List<SSW.NetPlayer>();
    for (int i = 0; i < original.Length; i++)
    {
        var saved = original[i];
        var skin = quick ? quickSkins[i] : skins[i == 0 ? index : skins.Length - index - 1];
        var job = quick && SSW.NetMath.Supported(saved.Job) ? saved.Job : jobs[(index + i * 3) % jobs.Length];
        var info = saved.Info;
        info.Skin = new Unity.Collections.FixedString128Bytes(skin.id);
        var p = UnityEngine.Object.Instantiate(prefab);
        p.Init(job, saved.Side, info);
        p.NetworkObject.SpawnAsPlayerObject(saved.OwnerClientId, true);
        p.Draft.Restore(System.Array.Empty<int>());
        p.Block(true);
        p.Drive.Teleport(new UnityEngine.Vector2(i == 0 ? -2f : 2f, 4f));
        list.Add(p);
    }
    actors = list.ToArray();
    UnityEngine.Physics2D.IgnoreCollision(actors[0].Collider, actors[1].Collider);
    await Wait(0.25f);
    await Until(() => {
        var snapshot = Remote();
        return (double)snapshot["serverTime"] > after && ((Newtonsoft.Json.Linq.JArray)snapshot["players"]).Count == 2
            && System.Linq.Enumerable.All(actors, p => RemotePlayer(p, snapshot) != null);
    }, 6f, "replacement actors did not replicate");
    Check(System.Linq.Enumerable.All(actors, p => p.CanAct), "both new actors remain alive and active");
}
async System.Threading.Tasks.Task ReadyFrame(double after)
{
    await Until(() => {
        var snapshot = Remote();
        if ((double)snapshot["serverTime"] <= after) return false;
        foreach (var p in actors)
        {
            var expected = catalog.Find(p.Info.Skin.ToString());
            var visual = Read<SSW.DinosaurVisualController>(p, "_animation");
            var resolver = visual.GetComponent<UnityEngine.U2D.Animation.SpriteResolver>();
            var renderer = visual.GetComponent<UnityEngine.SpriteRenderer>();
            if (expected == null || visual.CurrentSpriteLibrary != expected.library || renderer.sprite == null
                || renderer.sprite != expected.library.GetSprite(resolver.GetCategory(), resolver.GetLabel())) return false;
            var remote = RemotePlayer(p, snapshot);
            if (remote == null || (string)remote["skinId"] != expected.id || (string)remote["skinLibrary"] != expected.library.name) return false;
            if (clientRendererObserved)
            {
                var sprite = expected.library.GetSprite((string)remote["skinCategory"], (string)remote["skinLabel"]);
                if (!(bool)remote["skinResolved"] || sprite == null || (string)remote["skinSprite"] != sprite.name) return false;
            }
        }
        return true;
    }, 4f, "host and client renderers did not resolve their requested catalog sprites");
}
object Observe(SSW.NetPlayer p, string phase, Newtonsoft.Json.Linq.JObject snapshot)
{
    var expected = catalog.Find(p.Info.Skin.ToString());
    Check(expected != null && expected.library != null, "spawned Info.Skin resolves in original catalog");
    var appliers = p.GetComponentsInChildren<RYU._01.Script.Customize.DinoSkinApplier>(true);
    var trails = p.GetComponentsInChildren<RYU._01.Script.FeedBack.SpeedAfterimage>(true);
    Check(appliers.Length == 1 && appliers[0].gameObject == p.gameObject, "exactly one native skin applier exists on player root", appliers.Length, 1);
    Check(!Read<bool>(appliers[0], "applyOnStart"), "native Start cannot overwrite network skin from local storage");
    Check(trails.Length == 1, "skin spawn does not duplicate native trail component", trails.Length, 1);
    var visual = Read<SSW.DinosaurVisualController>(p, "_animation");
    var resolver = visual.GetComponent<UnityEngine.U2D.Animation.SpriteResolver>();
    var renderer = visual.GetComponent<UnityEngine.SpriteRenderer>();
    string category = resolver.GetCategory();
    string label = resolver.GetLabel();
    var expectedSprite = expected.library.GetSprite(category, label);
    Check(visual.CurrentSpriteLibrary == expected.library, "host actual library equals catalog asset", visual.CurrentSpriteLibrary != null ? visual.CurrentSpriteLibrary.name : "", expected.library.name);
    Check(expectedSprite != null && renderer.sprite == expectedSprite, "host renderer displays the catalog sprite selected by its current resolver", renderer.sprite != null ? renderer.sprite.name : "", expectedSprite != null ? expectedSprite.name : "");
    var remote = RemotePlayer(p, snapshot);
    Check(remote != null, "client snapshot contains current owner and object ID");
    foreach (string field in requiredFields) Check(remote[field] != null, "client probe exposes " + field);
    Check((string)remote["skinId"] == expected.id && (string)remote["skinLibrary"] == expected.library.name, "client actual Info and library equal requested catalog skin", new { id = (string)remote["skinId"], library = (string)remote["skinLibrary"] }, new { expected.id, library = expected.library.name });
    Check((int)remote["skinComponents"] == 1 && (int)remote["trailComponents"] == 1, "client has one native skin applier and one trail component");
    string remoteCategory = (string)remote["skinCategory"];
    string remoteLabel = (string)remote["skinLabel"];
    if (clientRendererObserved)
    {
        var remoteExpected = expected.library.GetSprite(remoteCategory, remoteLabel);
        Check((bool)remote["skinResolved"] && remoteExpected != null && (string)remote["skinSprite"] == remoteExpected.name, "client renderer actually resolves its requested library sprite", new { category = remoteCategory, label = remoteLabel, sprite = (string)remote["skinSprite"], resolved = (bool)remote["skinResolved"] }, remoteExpected != null ? remoteExpected.name : "");
    }
    Check((string)remote["job"] == p.Job.ToString(), "skin replication preserves selected job");
    return new {
        phase, owner = p.OwnerClientId, objectId = p.NetworkObjectId, clientOwned = p.OwnerClientId != game.Manager.LocalClientId,
        id = expected.id, displayName = expected.displayName, gender = expected.gender.ToString(), job = p.Job.ToString(),
        differsFromHostStorage = expected.id != storedSkin,
        library = expected.library.name, libraryPath = UnityEditor.AssetDatabase.GetAssetPath(expected.library),
        host = new { category, label, sprite = renderer.sprite.name, texture = renderer.sprite.texture.name, skinComponents = appliers.Length, trailComponents = trails.Length },
        client = new { serverTime = (double)snapshot["serverTime"], category = remoteCategory, label = remoteLabel, sprite = (string)remote["skinSprite"], texture = (string)remote["skinTexture"], resolved = clientRendererObserved ? (bool?)remote["skinResolved"] : null, skinComponents = (int)remote["skinComponents"], trailComponents = (int)remote["trailComponents"] }
    };
}
async System.Threading.Tasks.Task Restore()
{
    if (!replaced) return;
    Clear();
    var restored = new System.Collections.Generic.List<SSW.NetPlayer>();
    foreach (var saved in original)
    {
        var p = UnityEngine.Object.Instantiate(prefab);
        p.Init(saved.Job, saved.Side, saved.Info);
        p.NetworkObject.SpawnAsPlayerObject(saved.OwnerClientId, true);
        p.Draft.Restore(saved.cards);
        if (p.Cast.Weapon != null) p.Cast.Weapon.Progress = saved.progress;
        p.Block(true);
        p.Drive.Teleport(saved.position);
        restored.Add(p);
    }
    await Wait(0.1f);
    for (int i = 0; i < original.Length; i++)
    {
        var p = restored[i];
        var saved = original[i];
        typeof(SSW.Health).GetMethod("ApplyNetworkState", flags).Invoke(p.Health, new object[] { saved.health, saved.maximum });
        p.Drive.Teleport(saved.position);
        p.Block(saved.blocked);
        Check(p.Info.Equals(saved.Info) && p.Job == saved.Job && System.Linq.Enumerable.SequenceEqual(p.Draft.Owned, saved.cards), "cleanup restores Info, job and owned cards");
        Check(UnityEngine.Vector2.Distance(p.Body.position, saved.position) < 0.001f, "cleanup restores original position");
    }
    double after = game.ServerTime;
    await Until(() => {
        var snapshot = Remote();
        return (double)snapshot["serverTime"] > after && System.Linq.Enumerable.All(restored, p => RemotePlayer(p, snapshot) != null);
    }, 6f, "restored player objects did not reach client");
    foreach (var p in restored)
    {
        var remote = RemotePlayer(p, Remote());
        Check((string)remote["skinId"] == p.Info.Skin.ToString() && (string)remote["job"] == p.Job.ToString(), "cleanup restores client Info skin and job");
    }
}
UnityEngine.Application.LogCallback capture = (message, stack, kind) => {
    if (kind == UnityEngine.LogType.Exception || kind == UnityEngine.LogType.Error || kind == UnityEngine.LogType.Assert)
        errors.Add(message + "\n" + stack);
};
async System.Threading.Tasks.Task Run()
{
    string fatal = null;
    foreach (string key in guards) UnityEditor.SessionState.SetBool(key, true);
    UnityEngine.Application.logMessageReceived += capture;
    Save("running");
    try
    {
        var initialRemote = Remote();
        clientError = (string)initialRemote["error"];
        foreach (var item in initialRemote["players"])
            foreach (string field in requiredFields) Check(item[field] != null, "required client probe field " + field);
        clientRendererObserved = System.Linq.Enumerable.All(initialRemote["players"], item => System.Linq.Enumerable.All(spriteFields, field => item[field] != null));
        Check(quick ? quickSkins.Length == 2 : skins.Length == 24, quick ? "two nonlocal catalog skins are available" : "native catalog contains the requested twenty-four entries", quick ? quickSkins.Length : skins.Length, expectedSkins);
        var ids = new System.Collections.Generic.HashSet<string>();
        var libraries = new System.Collections.Generic.HashSet<UnityEngine.U2D.Animation.SpriteLibraryAsset>();
        var names = new System.Collections.Generic.HashSet<string>();
        foreach (var skin in quick ? quickSkins : skins)
        {
            Check(skin != null && !string.IsNullOrEmpty(skin.id) && skin.library != null, "catalog entry has an ID and sprite library");
            Check(ids.Add(skin.id) && libraries.Add(skin.library) && names.Add(skin.library.name), "catalog IDs, asset references and client library names are unique", skin.id);
            Check(System.Text.Encoding.UTF8.GetByteCount(skin.id) <= 125, "skin ID fits FixedString128Bytes", skin.id);
        }
        foreach (int index in quick ? System.Array.Empty<int>() : new[] { 0, 12, 23 })
        {
            var value = original[0].Info;
            value.Skin = new Unity.Collections.FixedString128Bytes(skins[index].id);
            using (var writer = new Unity.Netcode.FastBufferWriter(1024, Unity.Collections.Allocator.Temp))
            {
                writer.WriteNetworkSerializable(value);
                using (var reader = new Unity.Netcode.FastBufferReader(writer, Unity.Collections.Allocator.Temp))
                {
                    reader.ReadNetworkSerializable(out SSW.Fighter restored);
                    Check(restored.Equals(value) && restored.Name.Equals(value.Name) && restored.Tag.Equals(value.Tag) && restored.Skin.Equals(value.Skin), "Fighter packet round trip preserves name, tag and skin", skins[index].id);
                    var different = restored;
                    different.Skin = new Unity.Collections.FixedString128Bytes(skins[(index + 1) % skins.Length].id);
                    Check(!different.Equals(value), "Fighter equality notices a skin-only difference");
                    packets.Add(new { id = skins[index].id, bytes = writer.Length, preserved = restored.Skin.ToString(), name = restored.Name.ToString(), tag = restored.Tag.ToString() });
                }
            }
        }
        for (int pair = 0; pair < expectedPairs; pair++)
        {
            current = "pair-" + pair;
            Save("running");
            try
            {
                await Pair(pair);
                var observations = new System.Collections.Generic.List<object>();
                await ReadyFrame(game.ServerTime - 0.1d);
                var startFrame = client;
                foreach (var p in actors) observations.Add(Observe(p, "post-start", startFrame));
                await Until(() => System.Linq.Enumerable.All(actors, p => !Read<bool>(Read<SSW.DinosaurVisualController>(p, "_animation"), "_hatching")), 5f, "native hatch animation did not finish");
                await Wait(0.65f);
                double after = game.ServerTime;
                await ReadyFrame(after);
                var settledFrame = client;
                foreach (var p in actors)
                {
                    observations.Add(Observe(p, "settled", settledFrame));
                    covered.Add(p.Info.Skin.ToString());
                    (p.OwnerClientId == game.Manager.LocalClientId ? hostJobs : clientJobs).Add(p.Job.ToString());
                }
                Check(prefsUnchanged && hadPreference == UnityEngine.PlayerPrefs.HasKey(preference) && rawPreference == UnityEngine.PlayerPrefs.GetString(preference, string.Empty), "read-only local skin preference remains unchanged");
                cases.Add(new { pair, pass = true, observations });
            }
            catch (System.Exception failure)
            {
                failures.Add(new { current, error = failure.ToString() });
                cases.Add(new { pair, pass = false, error = failure.GetBaseException().Message });
            }
            Save("running");
        }
        current = "coverage";
        Check(covered.Count == expectedSkins, quick ? "both requested skins reached authoritative renderers and the client libraries" : "all catalog IDs were validated on both processes", covered.Count, expectedSkins);
        if (!quick) Check(hostJobs.Count == 6 && clientJobs.Count == 6, "both ownership directions cover all six jobs", new { host = hostJobs.Count, client = clientJobs.Count }, 6);
        Check(errors.Count == 0, "no new host runtime errors", errors.ToArray());
        Check((string)Remote()["error"] == clientError, "no new client probe error", (string)client["error"], clientError);
    }
    catch (System.Exception failure) { fatal = failure.ToString(); }
    finally
    {
        current = "restore";
        try
        {
            if (UnityEditor.EditorApplication.isPlaying && game != null && game.Manager.IsServer && game.Connected) await Restore();
            Check(hadPreference == UnityEngine.PlayerPrefs.HasKey(preference) && rawPreference == UnityEngine.PlayerPrefs.GetString(preference, string.Empty), "original local preference is preserved");
        }
        catch (System.Exception failure) { fatal = (fatal ?? "") + "\nRestore: " + failure; }
        UnityEngine.Application.logMessageReceived -= capture;
        foreach (string key in guards) UnityEditor.SessionState.SetBool(key, false);
        current = "finished";
        Save(fatal == null && failures.Count == 0 && errors.Count == 0 && covered.Count == expectedSkins ? "completed" : "failed", fatal ?? (failures.Count > 0 || errors.Count > 0 ? "Inspect pair failures and runtime errors." : null));
    }
}
_ = Run();
return new { running = true, log, mode = quick ? "quick" : "full", expectedSkins, expectedPairs, dispatch = "server Info injection and real network spawn", requiredFields, estimatedSeconds = quick ? 10 : 80 };

if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play mode required");
var game = SSW.NetGame.Current;
if (game == null || !game.Manager.IsServer || !game.CanFight || game.Practice != null || game.Players.Count != 2)
    throw new System.InvalidOperationException("A playing Editor host and one connected client are required");
string root = System.IO.Path.GetFullPath(@"@ROOT@");
bool quick = !bool.TryParse("@QUICK@", out bool selectedQuick) || selectedQuick;
int expectedCases = quick ? 3 : 10;
string allowed = System.IO.Path.GetFullPath(UnityEngine.Application.dataPath + "/../Logs/Boundary") + System.IO.Path.DirectorySeparatorChar;
if (!root.StartsWith(allowed, System.StringComparison.OrdinalIgnoreCase)) throw new System.InvalidOperationException("Use a fresh Logs/Boundary directory");
System.IO.Directory.CreateDirectory(root);
string log = System.IO.Path.Combine(root, "Afterimages.json");
string peerFile = System.IO.Path.Combine(root, "client.json");
if (System.IO.File.Exists(log)) throw new System.InvalidOperationException("Afterimages.json already exists");
if (!System.IO.File.Exists(peerFile)) throw new System.InvalidOperationException("client.json probe is required in the run directory");
const string runKey = "visuals.afterimages.running";
if (UnityEditor.SessionState.GetBool(runKey, false)) throw new System.InvalidOperationException("Afterimages already running");
const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
System.Reflection.FieldInfo Field(object item, string name)
{
    for (var type = item.GetType(); type != null; type = type.BaseType)
    {
        var field = type.GetField(name, flags);
        if (field != null) return field;
    }
    throw new System.MissingFieldException(item.GetType().Name, name);
}
T Read<T>(object item, string name) => (T)Field(item, name).GetValue(item);
void Set(object item, string name, object value) => Field(item, name).SetValue(item, value);
object Call(object item, string name, params object[] args) => item.GetType().GetMethod(name, flags).Invoke(item, args);
var saved = game.Players.OrderBy(p => p.OwnerClientId).Select(p => new
{
    p.OwnerClientId, p.Job, p.Side, p.Info, position = p.Body.position, owned = p.Draft.Owned.ToArray(),
    health = p.Health.Current, maximum = p.Health.Max, blocked = Read<bool>(p, "_blocked"),
    progress = p.Cast.Weapon != null ? p.Cast.Weapon.Progress : 0
}).ToArray();
var prefab = Read<SSW.NetPlayer>(game, "_playerPrefab");
var stock = UnityEngine.Resources.Load<SSW.NetStock>("Network/Potions");
var potions = Read<AbstractPotion[]>(stock, "_potions");
var speed = potions.Single(p => p is SpeedPotion);
var other = potions.First(p => p.GetType().Name == "HealthPotion");
var potionData = Newtonsoft.Json.Linq.JObject.Parse(UnityEngine.JsonUtility.ToJson(speed));
float duration = (float)potionData["duration"];
var apply = typeof(SSW.NetGame).Assembly.GetType("SSW.PotionUse").GetMethod("Apply", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
string originalPath = "Assets/RYU/02.Prefabs/Agent/Witch.prefab";
var original = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(originalPath)
    .GetComponentsInChildren<RYU._01.Script.FeedBack.SpeedAfterimage>(true).Single();
float interval = Read<float>(original, "spawnInterval"), fade = Read<float>(original, "fadeDuration");
var tint = Read<UnityEngine.Color>(original, "ghostColor");
int sorting = Read<int>(original, "sortingOffset");
var books = new System.Collections.Generic.List<SSW.StatsBook>();
var cases = new System.Collections.Generic.List<object>();
var checks = new System.Collections.Generic.List<object>();
var failures = new System.Collections.Generic.List<string>();
var errors = new System.Collections.Generic.List<string>();
var initialState = game.State;
SSW.NetPlayer[] actors = System.Array.Empty<SSW.NetPlayer>();
Newtonsoft.Json.Linq.JObject peer = null;
object detail = null;
string current = "";
int count = 0;
bool changedRound = false;
double started = UnityEngine.Time.realtimeSinceStartupAsDouble;
void Save(string status, string error = null)
{
    System.IO.File.WriteAllText(log, Newtonsoft.Json.JsonConvert.SerializeObject(new
    {
        status, error, count, current, quick, expectedCases, seconds = UnityEngine.Time.realtimeSinceStartupAsDouble - started,
        protocol = SSW.NetGame.Protocol, build = UnityEngine.Application.buildGUID, cases, failures, errors, checks = checks.ToArray(), detail,
        source = new { potion = UnityEditor.AssetDatabase.GetAssetPath(speed), data = potionData, originalPath, interval, fade, tint = new { tint.r, tint.g, tint.b, tint.a }, sorting },
        scope = "Original SpeedPotion.Use through authoritative PotionUse.Apply, native host roots and received client trails. Not a potion input, projectile flight, skin or speed-balance test.",
        fixture = "Caller parks existing actors safely before isolating both maps, adds wideground(0,3;60,.6), applies lag60/jitter10 and leaves input idle. Runtime player clones have gravity=0 and no augments. Original jobs, owned cards, HP and weapon progress are restored. "
            + (quick ? "Quick mode does not inject death or advance a round." : "Full mode advances one real round; its score/map are not restored."),
        remoteContract = "client.json: fresh serverTime, players id/objectId/trailActive/trailSources/trailComponents and top-level native trails array. Caller must not write probe commands during this run.",
        changedRound, initialRound = initialState.Round, finalRound = game != null ? game.State.Round : 0
    }, Newtonsoft.Json.Formatting.Indented));
}
void Check(bool condition, string label, object actual = null, object expected = null)
{
    checks.Add(new { pass = condition, label, actual, expected });
    count++;
    if (!condition) throw new System.InvalidOperationException(label);
}
bool Near(float a, float b, float tolerance = 0.01f) => UnityEngine.Mathf.Abs(a - b) <= tolerance;
Newtonsoft.Json.Linq.JObject Remote()
{
    try { peer = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(peerFile)); }
    catch (System.IO.IOException) { }
    catch (Newtonsoft.Json.JsonReaderException) { }
    return peer ?? throw new System.InvalidOperationException("No complete client probe snapshot");
}
Newtonsoft.Json.Linq.JObject RemotePlayer(SSW.NetPlayer player, Newtonsoft.Json.Linq.JObject snapshot) =>
    snapshot["players"].OfType<Newtonsoft.Json.Linq.JObject>().FirstOrDefault(p => (ulong)p["id"] == player.OwnerClientId && (ulong)p["objectId"] == player.NetworkObjectId);
Newtonsoft.Json.Linq.JArray RemoteGhosts(Newtonsoft.Json.Linq.JObject snapshot) =>
    snapshot["trails"] as Newtonsoft.Json.Linq.JArray ?? throw new System.InvalidOperationException("Client probe has no native trails array; rebuild the client with the current NetProbe");
UnityEngine.SpriteRenderer[] Ghosts() => UnityEngine.Object.FindObjectsByType<UnityEngine.SpriteRenderer>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)
    .Where(p => p.gameObject.scene.IsValid() && p.gameObject.name == "Afterimage" && p.transform.parent == null).ToArray();
async System.Threading.Tasks.Task Wait(float seconds)
{
    float end = UnityEngine.Time.time + seconds;
    double deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + seconds * 3d + 10d;
    do
    {
        if (!UnityEditor.EditorApplication.isPlaying || game == null || !game.Connected || UnityEngine.Time.realtimeSinceStartupAsDouble > deadline)
            throw new System.TimeoutException("Afterimage fixture disconnected or stopped");
        await System.Threading.Tasks.Task.Delay(10);
    } while (UnityEngine.Time.time < end);
}
async System.Threading.Tasks.Task Until(System.Func<bool> condition, float seconds, string label)
{
    double end = UnityEngine.Time.realtimeSinceStartupAsDouble + seconds;
    while (!condition())
    {
        if (UnityEngine.Time.realtimeSinceStartupAsDouble >= end) throw new System.TimeoutException(label);
        await Wait(0f);
    }
}
async System.Threading.Tasks.Task Fresh(double after)
{
    await Until(() => (double)Remote()["serverTime"] > after, 3f, "Client probe stopped advancing");
}
async System.Threading.Tasks.Task Command(string op, float x = 0f, float y = 0f)
{
    int seq = (int)Remote()["seq"] + 1;
    System.IO.File.WriteAllText(System.IO.Path.Combine(root, "client.cmd.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new { seq, op, x, y, value = 0 }));
    await Until(() => (int)Remote()["seq"] >= seq, 3f, "Client probe did not acknowledge " + op);
}
void Use(AbstractPotion potion, SSW.NetPlayer target, float ticks = 1f)
{
    var mods = new RYU._01.Script.Potions.PotionModifiers(1f, ticks, 1f, false, 0);
    apply.Invoke(null, new object[] { potion, target, actors.First(p => p != target), mods });
}
void Clear()
{
    Call(game, "ClearShots");
    foreach (var player in game.Players.ToArray()) player.NetworkObject.Despawn();
    foreach (var book in books) if (book != null) UnityEngine.Object.Destroy(book);
    books.Clear();
}
async System.Threading.Tasks.Task Pair(SSW.PlayerJob job)
{
    var before = new
    {
        phase = game.State.Phase.ToString(), game.CanFight, fallY = game.Arena.FallY,
        players = game.Players.Select(p => new { id = p.OwnerClientId, p.IsSpawned, hp = p.Health.Current, x = p.Body.position.x, y = p.Body.position.y }).ToArray()
    };
    Clear();
    var list = new System.Collections.Generic.List<SSW.NetPlayer>();
    foreach (var previous in saved)
    {
        var player = UnityEngine.Object.Instantiate(prefab);
        var book = UnityEngine.Object.Instantiate(Read<SSW.StatsBook>(player, "_statsBook"));
        var stats = book.At(job);
        stats.Gravity = 0f;
        book.Replace(new[] { stats });
        books.Add(book);
        Set(player, "_statsBook", book);
        player.Init(job, previous.Side, previous.Info);
        player.NetworkObject.SpawnAsPlayerObject(previous.OwnerClientId, true);
        player.Draft.Restore(System.Array.Empty<int>());
        player.Block(true);
        player.Drive.Teleport(new UnityEngine.Vector2(previous.OwnerClientId == game.LocalId ? -8f : 8f, 6f));
        list.Add(player);
    }
    actors = list.ToArray();
    UnityEngine.Physics2D.IgnoreCollision(actors[0].Collider, actors[1].Collider);
    await Wait(0.25f);
    await Until(() => actors.All(p => RemotePlayer(p, Remote()) != null), 4f, "Replacement actors missing on client");
    Check(actors.All(p => p.CanAct), "Both authoritative actors can act", new
    {
        before, phase = game.State.Phase.ToString(), game.CanFight, fallY = game.Arena.FallY,
        players = actors.Select(p => new { id = p.OwnerClientId, objectId = p.NetworkObjectId, p.IsSpawned, p.CanAct, hp = p.Health.Current, x = p.Body.position.x, y = p.Body.position.y }).ToArray()
    }, "Playing; both spawned and HP > 0");
}
async System.Threading.Tasks.Task Clean(string label, float timeout = 2f)
{
    double after = (double)Remote()["serverTime"];
    await Until(() => Ghosts().Length == 0 && (double)Remote()["serverTime"] > after && RemoteGhosts(peer).Count == 0, timeout, label);
    Check(Ghosts().Length == 0 && RemoteGhosts(Remote()).Count == 0, label, new { host = Ghosts().Length, client = RemoteGhosts(peer).Count }, 0);
}
async System.Threading.Tasks.Task Case(string name, System.Func<System.Threading.Tasks.Task> body)
{
    current = name;
    checks.Clear();
    detail = null;
    Save("running");
    float at = UnityEngine.Time.time;
    string error = null;
    try { await body(); }
    catch (System.Exception exception) { error = exception.ToString(); failures.Add(name + ": " + exception.GetBaseException().Message); }
    cases.Add(new { name, status = error == null ? "passed" : "failed", seconds = UnityEngine.Time.time - at, checks = checks.ToArray(), detail, error });
    Save("running");
    if (error != null) throw new System.InvalidOperationException(name + " failed; inspect saved case detail");
}
async System.Threading.Tasks.Task Observe(bool modified)
{
    await Clean("No old native roots before potion use");
    var effects = actors.Select(p => p.GetComponentsInChildren<RYU._01.Script.FeedBack.SpeedAfterimage>(true).Single()).ToArray();
    var renderers = actors.Select(p => p.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true)).ToArray();
    var factors = actors.Select(p => modified && !p.IsOwner ? 1.25f : 1f).ToArray();
    var periods = factors.Select(ticks => duration * ticks).ToArray();
    var hostBirths = actors.Select(p => new System.Collections.Generic.List<float>()).ToArray();
    var clientBirths = actors.Select(p => new System.Collections.Generic.List<double>()).ToArray();
    var sourceIds = actors.Select(p => new System.Collections.Generic.HashSet<string>()).ToArray();
    var inactiveIds = actors.Select(p => new System.Collections.Generic.HashSet<string>()).ToArray();
    var ghostSprites = actors.Select(p => new System.Collections.Generic.HashSet<string>()).ToArray();
    var sourceNames = actors.Select(p => new System.Collections.Generic.HashSet<string>()).ToArray();
    var inactiveNames = actors.Select(p => new System.Collections.Generic.HashSet<string>()).ToArray();
    var clientSprites = actors.Select(p => new System.Collections.Generic.HashSet<string>()).ToArray();
    var seen = new System.Collections.Generic.Dictionary<string, (UnityEngine.SpriteRenderer view, int actor, float time, float alpha)>();
    var remoteSeen = new System.Collections.Generic.Dictionary<string, (int actor, double time, float alpha)>();
    var faded = new System.Collections.Generic.HashSet<string>();
    var remoteFaded = new System.Collections.Generic.HashSet<string>();
    var hostDone = new float[actors.Length];
    var hostLast = new float[actors.Length];
    var remoteStart = new double[actors.Length];
    var remoteDone = new double[actors.Length];
    var remoteLast = new double[actors.Length];
    var sourceCount = new int[actors.Length];
    bool nativeCacheSafe = true, hostColor = true, clientColor = true, hostOrder = true, remoteOrder = true;
    bool hostFadeOrder = true, clientFadeOrder = true, lateRoots = false, remoteLateRoots = false;
    float maxFrame = 0f, maxPoll = 0f;
    double previousRemote = (double)Remote()["serverTime"];
    float at = UnityEngine.Time.time, previousPoll = at;
    for (int i = 0; i < actors.Length; i++)
    {
        Check(Read<float>(effects[i], "spawnInterval") == interval && Read<float>(effects[i], "fadeDuration") == fade
            && Read<UnityEngine.Color>(effects[i], "ghostColor") == tint && Read<int>(effects[i], "sortingOffset") == sorting,
            actors[i].OwnerClientId + " uses original afterimage settings");
        Use(speed, actors[i], factors[i]);
        Check(actors[i].Effects.TrailActive, actors[i].OwnerClientId + " server trail starts from original Use");
        Check(Near(Read<float>(effects[i], "_until") - UnityEngine.Time.time, periods[i], 0.025f),
            actors[i].OwnerClientId + " original Play remaining equals asset duration times TickCount",
            Read<float>(effects[i], "_until") - UnityEngine.Time.time, periods[i]);
    }
    while (UnityEngine.Time.time < at + periods.Max() + fade + 0.8f)
    {
        await Wait(0f);
        float now = UnityEngine.Time.time;
        maxFrame = UnityEngine.Mathf.Max(maxFrame, UnityEngine.Time.deltaTime);
        maxPoll = UnityEngine.Mathf.Max(maxPoll, now - previousPoll);
        previousPoll = now;
        for (int i = 0; i < actors.Length; i++)
        {
            if (actors[i].Effects.TrailActive) hostLast[i] = now - at;
            else if (hostDone[i] == 0f) hostDone[i] = now - at;
            foreach (var renderer in renderers[i])
            {
                if (renderer == null || renderer.sprite == null) continue;
                bool active = renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.color.a > 0f;
                (active ? sourceIds[i] : inactiveIds[i]).Add(renderer.sprite.GetEntityId().ToString());
                (active ? sourceNames[i] : inactiveNames[i]).Add(renderer.sprite.name);
            }
            var cache = Read<UnityEngine.SpriteRenderer[]>(effects[i], "_renderers");
            sourceCount[i] = System.Math.Max(sourceCount[i], cache.Count(p => p != null));
            nativeCacheSafe &= cache.All(p => p == null || p.enabled && p.gameObject.activeInHierarchy && p.sprite != null && p.color.a > 0f);
        }
        var births = new int[actors.Length];
        foreach (var ghost in Ghosts())
        {
            string id = ghost.gameObject.GetEntityId().ToString();
            if (!seen.TryGetValue(id, out var sample))
            {
                int index = System.Linq.Enumerable.Range(0, actors.Length).OrderBy(i => UnityEngine.Mathf.Abs(ghost.transform.position.x - actors[i].Body.position.x)).First();
                sample = (ghost, index, now, ghost.color.a);
                births[index]++;
                ghostSprites[index].Add(ghost.sprite.GetEntityId().ToString());
                hostColor &= Near(ghost.color.r, tint.r) && Near(ghost.color.g, tint.g) && Near(ghost.color.b, tint.b) && ghost.color.a > 0f && ghost.color.a <= tint.a + 0.001f;
                hostOrder &= renderers[index].Any(p => p != null && p.sortingLayerID == ghost.sortingLayerID && p.sortingOrder + sorting == ghost.sortingOrder);
            }
            else
            {
                hostFadeOrder &= ghost.color.a <= sample.alpha + 0.001f;
                if (ghost.color.a < sample.alpha - 0.01f) faded.Add(id);
                lateRoots |= now - sample.time > fade + maxFrame * 2f + 0.04f;
            }
            seen[id] = (sample.view, sample.actor, sample.time, ghost.color.a);
        }
        for (int i = 0; i < actors.Length; i++) if (births[i] > 0) hostBirths[i].Add(now - at);
        var snapshot = Remote();
        double remoteNow = (double)snapshot["serverTime"];
        if (remoteNow > previousRemote)
        {
            previousRemote = remoteNow;
            var remoteBirths = new int[actors.Length];
            for (int i = 0; i < actors.Length; i++)
            {
                var player = RemotePlayer(actors[i], snapshot);
                if (player == null) throw new System.InvalidOperationException("An observed client actor disappeared");
                if ((bool)player["trailActive"])
                {
                    if (remoteStart[i] == 0d) remoteStart[i] = remoteNow;
                    remoteLast[i] = remoteNow;
                }
                else if (remoteStart[i] > 0d && remoteDone[i] == 0d) remoteDone[i] = remoteNow;
            }
            foreach (var ghost in RemoteGhosts(snapshot).OfType<Newtonsoft.Json.Linq.JObject>())
            {
                string id = (string)ghost["id"];
                float alpha = (float)ghost["color"]["a"];
                if (!remoteSeen.TryGetValue(id, out var sample))
                {
                    float x = (float)ghost["position"]["x"];
                    int index = System.Linq.Enumerable.Range(0, actors.Length).OrderBy(i => UnityEngine.Mathf.Abs(x - actors[i].Body.position.x)).First();
                    sample = (index, remoteNow, alpha);
                    remoteBirths[index]++;
                    string sprite = (string)ghost["sprite"];
                    clientSprites[index].Add(sprite);
                    clientColor &= Near((float)ghost["color"]["r"], tint.r) && Near((float)ghost["color"]["g"], tint.g)
                        && Near((float)ghost["color"]["b"], tint.b) && alpha >= 0f && alpha <= tint.a + 0.001f;
                    remoteOrder &= renderers[index].Any(p => p != null && p.sortingOrder + sorting == (int)ghost["sorting"]);
                }
                else
                {
                    clientFadeOrder &= alpha <= sample.alpha + 0.001f;
                    if (alpha < sample.alpha - 0.01f) remoteFaded.Add(id);
                    remoteLateRoots |= remoteNow - sample.time > fade + 0.2f;
                }
                remoteSeen[id] = (sample.actor, sample.time, alpha);
            }
            for (int i = 0; i < actors.Length; i++) if (remoteBirths[i] > 0) clientBirths[i].Add(remoteNow);
        }
    }
    detail = new
    {
        job = actors[0].Job.ToString(), maxFrame, maxPoll, hostColor, clientColor, hostOrder, remoteOrder, nativeCacheSafe,
        hostFadeOrder, clientFadeOrder, lateRoots, remoteLateRoots,
        owners = actors.Select((p, i) => new
        {
            owner = p.OwnerClientId, p.IsOwner, objectId = p.NetworkObjectId, tickCount = factors[i], expectedDuration = periods[i],
            hostActiveUntil = hostLast[i], hostInactiveAt = hostDone[i], remoteActiveFrom = remoteStart[i], remoteActiveUntil = remoteLast[i], remoteInactiveAt = remoteDone[i],
            sourceCount = sourceCount[i], hostBirths = hostBirths[i].ToArray(), clientBirths = clientBirths[i].ToArray(),
            hostRoots = seen.Count(g => g.Value.actor == i), clientRoots = remoteSeen.Count(g => g.Value.actor == i),
            hostFadeSamples = faded.Count(id => seen[id].actor == i), clientFadeSamples = remoteFaded.Count(id => remoteSeen[id].actor == i),
            inactiveOnlySpriteIds = inactiveIds[i].Except(sourceIds[i]).ToArray(), unexpectedSpriteIds = ghostSprites[i].Intersect(inactiveIds[i].Except(sourceIds[i])).ToArray(),
            inactiveOnlySpriteNames = inactiveNames[i].Except(sourceNames[i]).ToArray(), unexpectedClientSprites = clientSprites[i].Intersect(inactiveNames[i].Except(sourceNames[i])).ToArray()
        }).ToArray()
    };
    Check(nativeCacheSafe, "Native cache excludes inactive or transparent weapon sprites");
    Check(hostColor && clientColor, "Native host and client roots use original RGB and alpha range");
    Check(hostOrder && remoteOrder, "Native roots retain source sorting order plus original offset");
    Check(hostFadeOrder && clientFadeOrder && !lateRoots && !remoteLateRoots, "Same root IDs fade monotonically and do not outlive native fade");
    for (int i = 0; i < actors.Length; i++)
    {
        string label = actors[i].OwnerClientId + " " + actors[i].Job;
        var remote = RemotePlayer(actors[i], Remote());
        Check(actors[i].GetComponentsInChildren<RYU._01.Script.FeedBack.SpeedAfterimage>(true).Length == 1 && (int)remote["trailComponents"] == 1,
            label + " has one native emitter on both peers");
        Check(sourceCount[i] > 0 && (int)remote["trailSources"] > 0 && hostBirths[i].Count >= 3 && clientBirths[i].Count >= 3,
            label + " actually creates repeated native roots on both peers");
        var gaps = hostBirths[i].Skip(1).Zip(hostBirths[i], (next, previous) => next - previous).OrderBy(t => t).ToArray();
        Check(gaps.Length >= 2 && gaps.All(gap => gap >= interval - maxPoll - 0.012f)
            && gaps[gaps.Length / 2] <= interval + 2f * maxFrame + maxPoll + 0.02f,
            label + " cadence follows original interval within measured frame and sampling tolerance", gaps, interval);
        Check(hostLast[i] >= periods[i] - maxPoll - 0.12f && hostDone[i] >= periods[i] - 0.04f && hostDone[i] <= periods[i] + maxPoll + 0.15f,
            label + " natural server duration follows original duration times TickCount", new { hostLast = hostLast[i], hostDone = hostDone[i] }, periods[i]);
        Check(remoteStart[i] > 0d && remoteLast[i] - remoteStart[i] >= periods[i] - 0.4f
            && remoteDone[i] - remoteStart[i] >= periods[i] - 0.25f && remoteDone[i] - remoteStart[i] <= periods[i] + 0.35f,
            label + " client RPC starts and naturally ends the modified duration", new { start = remoteStart[i], last = remoteLast[i], done = remoteDone[i] }, periods[i]);
        Check(faded.Any(id => seen[id].actor == i) && remoteFaded.Any(id => remoteSeen[id].actor == i), label + " same native root visibly fades on both peers");
        Check(!ghostSprites[i].Intersect(inactiveIds[i].Except(sourceIds[i])).Any()
            && !clientSprites[i].Intersect(inactiveNames[i].Except(sourceNames[i])).Any(), label + " no inactive-only weapon sprite appears in host or client roots");
        Check(!actors[i].Effects.TrailActive && !(bool)remote["trailActive"], label + " trail stops without forced cleanup");
    }
    await Clean("All original host and client roots naturally destroyed");
}
UnityEngine.Application.LogCallback capture = (message, stack, type) =>
{
    if (type == UnityEngine.LogType.Error || type == UnityEngine.LogType.Exception || type == UnityEngine.LogType.Assert) errors.Add(message + "\n" + stack);
};
async System.Threading.Tasks.Task Run()
{
    UnityEditor.SessionState.SetBool(runKey, true);
    UnityEngine.Application.logMessageReceived += capture;
    string fatal = null;
    Save("running");
    try
    {
        Check(duration > 0f && interval > 0f && fade > 0f, "Original timing values are positive");
        if (!quick) Check(initialState.FirstWins == 0 && initialState.SecondWins == 0, "Start at a fresh set so one death produces a small-round respawn");
        Check(Ghosts().Length == 0 && RemoteGhosts(Remote()).Count == 0, "No pre-existing roots at test entry");
        string initialPeerError = (string)peer["error"];
        await Fresh((double)peer["serverTime"]);
        await Case("non-speed-negative", async () =>
        {
            await Pair(SSW.PlayerJob.Witch);
            double after = (double)Remote()["serverTime"];
            foreach (var player in actors) Use(other, player);
            await Wait(0.45f);
            await Fresh(after);
            Check(actors.All(p => !p.Effects.TrailActive) && actors.All(p => !(bool)RemotePlayer(p, Remote())["trailActive"]), "HealthPotion never starts speed trail on either peer");
            await Clean("HealthPotion creates no native roots");
            detail = new { potion = UnityEditor.AssetDatabase.GetAssetPath(other), method = "Same PotionUse.Apply hook, original non-SpeedPotion, no direct Trail call" };
        });
        var jobs = quick ? new[] { SSW.PlayerJob.Witch }
            : new[] { SSW.PlayerJob.Witch, SSW.PlayerJob.Magician, SSW.PlayerJob.Swordsman, SSW.PlayerJob.Assassin, SSW.PlayerJob.Gambler, SSW.PlayerJob.Gunner };
        foreach (var job in jobs)
            await Case("both-owners-" + job, async () => { if (!quick) await Pair(job); await Observe(!quick); });
        await Case("active-job-replacement", async () =>
        {
            string from = actors[0].Job.ToString();
            foreach (var player in actors) Use(speed, player);
            await Until(() => Ghosts().Length > 0 && RemoteGhosts(Remote()).Count > 0, 2f, "No active roots before job replacement");
            var oldRoots = Ghosts();
            var oldEmitters = actors.SelectMany(p => p.GetComponentsInChildren<RYU._01.Script.FeedBack.SpeedAfterimage>(true)).ToArray();
            var oldObjects = actors.Select(p => p.NetworkObjectId).ToArray();
            await Pair(SSW.PlayerJob.Swordsman);
            await Wait(fade + 0.15f);
            await Clean("Active job replacement releases old host and client roots");
            Check(oldRoots.All(p => p == null) && oldEmitters.All(p => p == null), "Old native emitters survive only long enough to complete fade");
            Check(actors.All(p => !oldObjects.Contains(p.NetworkObjectId) && !p.Effects.TrailActive)
                && actors.All(p => !(bool)RemotePlayer(p, Remote())["trailActive"]), "Old duration and roots do not leak into replacement jobs");
            Check(actors.All(p => p.GetComponentsInChildren<RYU._01.Script.FeedBack.SpeedAfterimage>(true).Length == 1)
                && actors.All(p => (int)RemotePlayer(p, Remote())["trailComponents"] == 1), "Replacement jobs retain exactly one native emitter per peer");
            detail = new { method = "Runtime network despawn and Init/SpawnAsPlayerObject with new job; not a lobby selection input test", from, to = "Swordsman", oldObjects, objects = actors.Select(p => p.NetworkObjectId).ToArray() };
        });
        if (!quick)
        {
        await Case("native-death-and-round-respawn", async () =>
        {
            foreach (var player in actors) Use(speed, player);
            await Until(() => Ghosts().Length > 0 && RemoteGhosts(Remote()).Count > 0, 2f, "No native roots before actual death");
            var oldObjects = actors.Select(p => p.NetworkObjectId).ToArray();
            var victim = actors.Single(p => !p.IsOwner);
            var winner = actors.Single(p => p.IsOwner);
            var result = victim.Health.ReceiveDamage(new SSW.DamageRequest(winner, victim.Health.Max * 10f, SSW.DamageTag.Environment | SSW.DamageTag.IgnoreDefense));
            Check(result.WasLethal && victim.Health.Current <= 0f, "Native authoritative lethal damage actually kills client-owned player");
            await Until(() => game.State.Phase == SSW.MatchPhase.RoundEnd, 2f, "Native death did not enter RoundEnd");
            changedRound = true;
            await Wait(fade + 0.2f);
            await Clean("Death/round transition stops emission and finishes all native roots", 1.2f);
            await Until(() => game.Players.Count == 2 && game.Players.All(p => !oldObjects.Contains(p.NetworkObjectId)), 10f, "Native ResetRound did not replace both player objects");
            actors = game.Players.OrderBy(p => p.OwnerClientId).ToArray();
            foreach (var shape in game.Arena.Map.GetComponentsInChildren<UnityEngine.Collider2D>(true)) shape.enabled = false;
            foreach (var effect in game.Arena.Map.GetComponentsInChildren<UnityEngine.ParticleSystem>()) effect.Stop(true, UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var player in actors)
            {
                player.Block(true);
                player.Drive.Teleport(new UnityEngine.Vector2(player.IsOwner ? -8f : 8f, 6f));
            }
            await Command("isolate");
            await Command("wideground", 0f, 3f);
            await Until(() => game.CanFight && actors.All(p => RemotePlayer(p, Remote()) != null), 10f, "Respawn did not return to Playing on both peers");
            Check(actors.All(p => p.Job == SSW.PlayerJob.Swordsman && p.Health.Current > 0f && !p.Effects.TrailActive), "Native respawn retains job and resets the expired trail");
            Check(actors.All(p => p.GetComponentsInChildren<RYU._01.Script.FeedBack.SpeedAfterimage>(true).Length == 1)
                && actors.All(p => (int)RemotePlayer(p, Remote())["trailComponents"] == 1 && !(bool)RemotePlayer(p, Remote())["trailActive"]), "Native respawn has one inactive emitter on both peers");
            await Clean("Native respawn leaves zero old root ghosts");
            detail = new { method = "Health.ReceiveDamage -> NetMatch.RoundEnd -> native NetGame.ResetRound; round/score are not injected", oldObjects, objects = actors.Select(p => p.NetworkObjectId).ToArray(), round = game.State.Round };
        });
        await Case("post-respawn-play-and-cleanup", async () =>
        {
            await Wait(0.3f);
            await Observe(false);
        });
        }
        Check(errors.Count == 0, "No new host errors", errors.ToArray());
        Check((string)Remote()["error"] == initialPeerError, "No new client probe error", (string)peer["error"], initialPeerError);
    }
    catch (System.Exception failure) { fatal = failure.ToString(); failures.Add(failure.GetBaseException().Message); }
    finally
    {
        try
        {
            if (game != null && game.Connected && game.Manager.IsServer)
            {
                Clear();
                foreach (var previous in saved)
                {
                    var player = UnityEngine.Object.Instantiate(prefab);
                    player.Init(previous.Job, previous.Side, previous.Info);
                    player.NetworkObject.SpawnAsPlayerObject(previous.OwnerClientId, true);
                    player.Draft.Restore(previous.owned);
                    if (player.Cast.Weapon != null) player.Cast.Weapon.Progress = previous.progress;
                    player.Drive.Teleport(previous.position);
                    Call(player.Health, "ApplyNetworkState", previous.health, previous.maximum);
                    player.Block(previous.blocked);
                }
                await Wait(fade + 0.2f);
            }
        }
        catch (System.Exception failure) { fatal = (fatal ?? "") + "\nRestore: " + failure; failures.Add("fixture restoration failed"); }
        UnityEngine.Application.logMessageReceived -= capture;
        UnityEditor.SessionState.SetBool(runKey, false);
        current = "";
        Save(failures.Count == 0 && cases.Count == expectedCases ? "completed" : "failed", fatal);
    }
}
_ = Run();
return new { running = true, log, quick, expectedCases, potionInputValidation = false, nativeHostAndClientGhostValidation = true, advancesOneRound = !quick };

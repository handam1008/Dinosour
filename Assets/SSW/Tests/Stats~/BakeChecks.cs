if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.BuildPipeline.isBuildingPlayer)
    throw new System.InvalidOperationException("Play와 빌드를 멈춘 뒤 실행하세요.");
const string temporary = "Assets/SSW/Tests/StatBakeCheck";
const string configuration = "Assets/SSW/Editor/StatSources.asset";
const string networkPrefabs = "Assets/DefaultNetworkPrefabs.asset";
var checks = new System.Collections.Generic.List<string>();
var created = new System.Collections.Generic.List<string>();
var memory = new System.Collections.Generic.List<UnityEngine.Object>();
var originalRegistrations = new System.Collections.Generic.List<string>();
Unity.Netcode.NetworkPrefabsList registrations = null;
var ownedScenes = new System.Collections.Generic.HashSet<ulong>();
var savedFiles = new System.Collections.Generic.Dictionary<string, string>();
var savedObjects = new System.Collections.Generic.Dictionary<UnityEngine.Object, (string Json, bool Dirty)>();
var scenes = new System.Collections.Generic.List<(UnityEngine.SceneManagement.Scene Scene, bool Loaded, bool Dirty)>();
var expected = new System.Collections.Generic.Dictionary<SSW.PlayerJob, SSW.FighterStats>();
var summary = new System.Collections.Generic.List<string>();
var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
int previewCount = UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
{
    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
    scenes.Add((scene, scene.isLoaded, scene.isDirty));
}
bool folderCreated = false;
int packetBytes = 0;
void Check(bool valid, string name)
{
    if (!valid) throw new System.InvalidOperationException(name);
    checks.Add(name);
}
void Near(float actual, float value, string name)
    => Check(UnityEngine.Mathf.Abs(actual - value) <= 0.0001f * UnityEngine.Mathf.Max(1f, UnityEngine.Mathf.Abs(value)), name + ": " + actual + " / " + value);
void Reject(System.Action action, string name)
{
    try { action(); }
    catch (System.InvalidOperationException) { checks.Add(name); return; }
    throw new System.InvalidOperationException("거부되지 않음: " + name);
}
T Asset<T>(string path) where T : UnityEngine.Object
    => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new System.InvalidOperationException("에셋 없음: " + path);
T One<T>(UnityEngine.GameObject root) where T : UnityEngine.Component
{
    var values = root.GetComponentsInChildren<T>(true);
    if (values.Length != 1) throw new System.InvalidOperationException(root.name + "/" + typeof(T).Name + " 개수: " + values.Length);
    return values[0];
}
float Number(UnityEngine.Object target, string field)
{
    using var data = new UnityEditor.SerializedObject(target);
    return (data.FindProperty(field) ?? throw new System.InvalidOperationException("필드 없음: " + field)).floatValue;
}
int Integer(UnityEngine.Object target, string field)
{
    using var data = new UnityEditor.SerializedObject(target);
    return (data.FindProperty(field) ?? throw new System.InvalidOperationException("필드 없음: " + field)).intValue;
}
T Reference<T>(UnityEngine.Object target, string field) where T : UnityEngine.Object
{
    using var data = new UnityEditor.SerializedObject(target);
    return data.FindProperty(field).objectReferenceValue as T ?? throw new System.InvalidOperationException("참조 없음: " + field);
}
T[] References<T>(UnityEngine.Object target, string field) where T : UnityEngine.Object
{
    using var data = new UnityEditor.SerializedObject(target);
    var array = data.FindProperty(field);
    var values = new T[array.arraySize];
    for (int i = 0; i < values.Length; i++) values[i] = (T)array.GetArrayElementAtIndex(i).objectReferenceValue;
    return values;
}
string Hash(string path)
{
    using var digest = System.Security.Cryptography.SHA256.Create();
    return System.Convert.ToBase64String(digest.ComputeHash(System.IO.File.ReadAllBytes(path)));
}
void Protect(UnityEngine.Object value)
    => savedObjects.Add(value, (UnityEditor.EditorJsonUtility.ToJson(value), UnityEditor.EditorUtility.IsDirty(value)));
bool OwnedRegistration(Unity.Netcode.NetworkPrefab item)
    => item != null && item.Override == Unity.Netcode.NetworkPrefabOverride.None
        && created.Contains(UnityEditor.AssetDatabase.GetAssetPath(item.Prefab));
void CheckRegistrations(System.Collections.Generic.List<string> expected, string label)
{
    Check(registrations.PrefabList.Count == expected.Count, label + " 등록 개수 보존");
    for (int i = 0; i < expected.Count; i++)
        Check(UnityEngine.JsonUtility.ToJson(registrations.PrefabList[i]) == expected[i], label + " 등록 값·순서 보존: " + i);
}
void Set(UnityEngine.Object target, string field, object value)
{
    string path = UnityEditor.AssetDatabase.GetAssetPath(target);
    if (path.Length > 0 && !path.StartsWith(temporary + "/", System.StringComparison.Ordinal))
        throw new System.InvalidOperationException("테스트 소유 에셋 밖의 변경: " + path);
    if (path.Length == 0 && target is UnityEngine.Component component && !ownedScenes.Contains(component.gameObject.scene.handle.GetRawData()))
        throw new System.InvalidOperationException("테스트 소유 씬 밖의 변경: " + target.name);
    using var data = new UnityEditor.SerializedObject(target);
    var property = data.FindProperty(field) ?? throw new System.InvalidOperationException("필드 없음: " + field);
    if (value is float number) property.floatValue = number;
    else if (value is int integer) property.intValue = integer;
    else if (value is UnityEngine.Vector3 vector) property.vector3Value = vector;
    else property.objectReferenceValue = (UnityEngine.Object)value;
    data.ApplyModifiedPropertiesWithoutUndo();
    if (target is UnityEngine.Component member && UnityEditor.PrefabUtility.IsPartOfPrefabInstance(member))
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(member);
}
void Reserve(string path)
{
    if (!path.StartsWith(temporary + "/", System.StringComparison.Ordinal)
        || UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) != null || System.IO.File.Exists(path)
        || System.IO.File.Exists(path + ".meta") || System.IO.Directory.Exists(path))
        throw new System.InvalidOperationException("임시 경로 충돌: " + path);
    created.Add(path);
}
T Copy<T>(string source, string path) where T : UnityEngine.Object
{
    Reserve(path);
    if (!UnityEditor.AssetDatabase.CopyAsset(source, path)) throw new System.InvalidOperationException("복사 실패: " + path);
    return Asset<T>(path);
}
T Create<T>(string path) where T : UnityEngine.ScriptableObject
{
    Reserve(path);
    var value = UnityEngine.ScriptableObject.CreateInstance<T>();
    memory.Add(value);
    UnityEditor.AssetDatabase.CreateAsset(value, path);
    return value;
}
void EditPrefab(string path, System.Action<UnityEngine.GameObject> change)
{
    if (!created.Contains(path)) throw new System.InvalidOperationException("테스트가 생성하지 않은 프리팹: " + path);
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    ownedScenes.Add(root.scene.handle.GetRawData());
    ulong handle = root.scene.handle.GetRawData();
    try
    {
        change(root);
        if (UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path) == null)
            throw new System.InvalidOperationException("임시 프리팹 저장 실패: " + path);
    }
    finally
    {
        UnityEditor.PrefabUtility.UnloadPrefabContents(root);
        ownedScenes.Remove(handle);
    }
}
UnityEngine.GameObject SceneRoot(UnityEngine.SceneManagement.Scene scene, string path)
{
    string[] parts = path.Split('/');
    UnityEngine.GameObject current = null;
    foreach (var root in scene.GetRootGameObjects())
    {
        if (root.name != parts[0]) continue;
        if (current != null) throw new System.InvalidOperationException("중복 씬 루트: " + path);
        current = root;
    }
    if (current == null) throw new System.InvalidOperationException("씬 루트 없음: " + path);
    for (int i = 1; i < parts.Length; i++)
    {
        UnityEngine.Transform found = null;
        foreach (UnityEngine.Transform child in current.transform)
        {
            if (child.name != parts[i]) continue;
            if (found != null) throw new System.InvalidOperationException("중복 자식 경로: " + path);
            found = child;
        }
        if (found == null) throw new System.InvalidOperationException("자식 경로 없음: " + path);
        current = found.gameObject;
    }
    return current;
}
void WithRoot(SSW.StatSources.Entry entry, System.Action<UnityEngine.GameObject> action)
{
    if (entry.Prefab != null) { action(entry.Prefab); return; }
    var preview = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(UnityEditor.AssetDatabase.GetAssetPath(entry.Scene));
    try { action(SceneRoot(preview, entry.Root)); }
    finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview); }
}
void SameSceneState()
{
    Check(UnityEngine.SceneManagement.SceneManager.sceneCount == scenes.Count, "열린 씬 개수 보존");
    Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle == active.handle, "활성 씬 보존");
    Check(UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount == previewCount, "preview 씬 누수 없음");
    foreach (var before in scenes)
    {
        Check(before.Scene.IsValid() && before.Scene.isLoaded == before.Loaded, "기존 씬 로드 상태 보존: " + before.Scene.path);
        Check(before.Scene.isDirty == before.Dirty, "기존 씬 dirty 상태 보존: " + before.Scene.path);
    }
}
void FlightScale(SSW.FlightStats flight, UnityEngine.GameObject source, string label)
{
    var scale = source.transform.localScale;
    Near(flight.Scale, UnityEngine.Mathf.Abs(scale.x), label + " 가로 크기");
    Near(flight.Aspect, UnityEngine.Mathf.Abs(scale.y / scale.x), label + " 세로 비율");
}
void CompareSource(UnityEngine.GameObject root, SSW.FighterStats stats)
{
    string name = stats.Job.ToString();
    var health = root.GetComponent<SSW.Health>();
    var motion = root.GetComponent<SSW.PlayerController>();
    Near(stats.Health, Number(health, "maxHealth"), name + " 체력");
    Near(stats.MoveSpeed, Number(motion, "_moveSpeed"), name + " 이동");
    Near(stats.JumpSpeed, Number(motion, "_jumpForce"), name + " 점프");
    Near(stats.Gravity, root.GetComponent<UnityEngine.Rigidbody2D>().gravityScale, name + " 중력");
    Near(stats.Coyote, Number(motion, "_coyoteTime"), name + " 코요테");
    Near(stats.Decay, Number(motion, "_externalVelocityDecay"), name + " 외력 감쇠");
    Near(stats.Brake, Number(motion, "_counterMoveBrake"), name + " 반대 이동 제동");
    if (stats.Job == SSW.PlayerJob.Magician)
    {
        var source = One<SSW.NumberRoller>(root);
        Near(stats.AttackInterval, Number(source, "_cooldownDuration"), "마술사 발사 대기");
        Near(stats.RollInterval, Number(source, "_tickInterval"), "마술사 숫자 변경");
        Near(stats.MirrorScale, Number(source, "_mirrorScaleMultiplier"), "마술사 복제 비율");
        Near(stats.Flight.Speed, Number(source, "_flySpeed"), "마술사 카드 속도");
        Near(stats.Flight.Gravity, Number(source, "_flyGravityScale"), "마술사 카드 중력");
        Near(stats.Flight.Life, Number(source, "_flyMaxLifetime"), "마술사 카드 수명");
        Near(stats.Flight.Spin, Number(source, "_flySpin"), "마술사 카드 회전");
        Near(stats.Flight.Scale, Number(source, "_flyScale"), "마술사 카드 크기");
        Near(stats.Flight.Aspect, 1f, "마술사 균일 크기");
    }
    else if (stats.Job == SSW.PlayerJob.Witch)
    {
        var source = One<RandomPotion>(root);
        Near(stats.BrewTime, Number(source, "cycleTime"), "마녀 생성 시간");
        Near(stats.Spread, Number(source, "spreadAngle"), "마녀 분산 각도");
        Near(stats.Flight.Speed, Number(source, "potionSpeed"), "마녀 포션 속도");
        Near(stats.ThrowLift, Number(source, "Angle") * Number(source, "potionSpeed"), "마녀 원본 발사 식");
        foreach (var potion in References<AbstractPotion>(source, "potions"))
        {
            var prefab = Reference<UnityEngine.GameObject>(potion, "PotionPrefab");
            Near(stats.Splash, Number(One<RYU._01.Script.Potions.Potion>(prefab), "splashRadious"), potion.name + " 폭발 반경");
            FlightScale(stats.Flight, prefab, potion.name);
        }
    }
    else if (stats.Job == SSW.PlayerJob.Gunner)
    {
        var gun = One<KDH.Scripts.Gun.KDH_Gun>(root);
        var prefab = Reference<UnityEngine.GameObject>(gun, "<BulletPrefab>k__BackingField");
        var bullet = One<KDH.Scripts.Bullet.KDH_Bullet>(prefab);
        Check(stats.Capacity == Integer(gun, "<MaxAmmo>k__BackingField"), "총잡이 탄수");
        Near(stats.Reload, Number(gun, "<ChargeSpeed>k__BackingField"), "총잡이 한 발 충전");
        Near(stats.AttackInterval, 0f, "총잡이 미사용 AttackSpeed 제외");
        Near(stats.Damage, Number(bullet, "<Damage>k__BackingField"), "총잡이 탄환 피해");
        Near(stats.ChargeDamage, Number(bullet, "<UpgradValue>k__BackingField"), "총잡이 강화 배율");
        Near(stats.Flight.Speed, Number(bullet, "<Speed>k__BackingField"), "총잡이 탄환 속도");
        Near(stats.Flight.Gravity, prefab.GetComponent<UnityEngine.Rigidbody2D>().gravityScale, "총잡이 탄환 중력");
        FlightScale(stats.Flight, prefab, "총잡이 탄환");
    }
    else if (stats.Job == SSW.PlayerJob.Gambler)
    {
        var shooter = One<GamblerCoinShooter>(root);
        var magazine = Reference<GamblerMagazine>(shooter, "magazine");
        var pool = Reference<GamblerCoinPool>(shooter, "coinPool");
        Near(stats.Damage, Number(shooter, "coinDamage") * Number(shooter, "damageMultiplier"), "도박사 발사기 피해");
        Near(stats.AttackInterval, Number(shooter, "fireCooldown"), "도박사 발사 간격");
        Near(stats.Reload, Number(magazine, "reloadTime"), "도박사 재장전");
        Check(stats.Capacity == Integer(magazine, "magazineSize"), "도박사 탄수");
        foreach (string field in new[] { "normalCoinPrefab", "rouletteCoinPrefab" })
        {
            var coin = Reference<GamblerCoinProjectile>(pool, field);
            var body = Reference<UnityEngine.Rigidbody2D>(coin, "body");
            var gravity = One<CoinGravity>(coin.gameObject);
            Near(stats.Flight.Speed, Number(coin, "speed"), field + " 속도");
            Near(stats.Flight.Life, Number(coin, "lifetime"), field + " 수명");
            Near(stats.Flight.Gravity, body.gravityScale, field + " 중력");
            Near(stats.GravityDelay, Number(gravity, "gravityDelay"), field + " 중력 지연");
            Near(stats.ExtraGravity, Number(gravity, "extraGravity") / body.mass, field + " 힘/질량 가속도");
            FlightScale(stats.Flight, coin.gameObject, field);
        }
    }
    else if (stats.Job == SSW.PlayerJob.Assassin)
    {
        var attack = One<NKY.Scripts.AssassinMeleeAttack>(root);
        var module = One<NKY.Scripts.Skill.PlayerSkillModule>(root);
        var skill = Reference<NKY.Scripts.Skill.AssassinNormalSkillSo>(module, "_skillData");
        var projectile = Reference<NKY.Scripts.Skill.AssassinNormalSkill>(skill, "_skillPrefab");
        Near(stats.Damage, Number(attack, "damage") * Number(attack, "damageMultiplier"), "암살자 프리팹 피해");
        Near(stats.AttackInterval, Number(attack, "attackCooldown"), "암살자 프리팹 공격 간격");
        Near(stats.SkillCooldown, Number(skill, "<SkillCooldown>k__BackingField"), "암살자 연결 SO 재사용 시간");
        Near(stats.SkillDamage, Number(skill, "<Damage>k__BackingField"), "암살자 연결 SO 피해");
        Near(stats.Flight.Speed, Number(skill, "<ThrowSpeed>k__BackingField"), "암살자 연결 SO 속도");
        Near(stats.Flight.Life, Number(skill, "<DestroyTime>k__BackingField"), "암살자 연결 SO 수명");
        Near(stats.Flight.Gravity, projectile.GetComponent<UnityEngine.Rigidbody2D>().gravityScale, "암살자 투척 중력");
        FlightScale(stats.Flight, projectile.gameObject, "암살자 비균일 투사체");
    }
    else if (stats.Job == SSW.PlayerJob.Swordsman)
    {
        var attack = One<NKY.Scripts.KHG_nomalAttack>(root);
        var dash = One<KHG_Dash>(root);
        var parry = One<KHG_Paring>(root);
        Near(stats.Damage, Number(attack, "damage"), "칼잡이 피해");
        Near(stats.AttackInterval, Number(attack, "attackCooldown"), "칼잡이 간격");
        Near(stats.AttackTime, Number(attack, "attackDuration"), "칼잡이 공격 유지");
        Near(stats.DashSpeed, Number(dash, "_dashSpeed"), "칼잡이 대쉬 속도");
        Near(stats.DashTime, Number(dash, "_dashDuration"), "칼잡이 대쉬 시간");
        Near(stats.DashCooldown, Number(dash, "_dashCooldown"), "칼잡이 대쉬 대기");
        Near(stats.DashDamage, Number(dash, "damage"), "칼잡이 대쉬 피해");
        Near(stats.DashRadius, Number(dash, "hitRadius"), "칼잡이 대쉬 반경");
        Near(stats.ParryTime, Number(parry, "parryTime"), "칼잡이 패링 시간");
        Near(stats.ParryCooldown, Number(parry, "parryCooldown"), "칼잡이 패링 대기");
        Near(stats.ReflectSpeed, Number(parry, "reflectSpeed"), "칼잡이 반사 속도");
        var collider = Reference<UnityEngine.BoxCollider2D>(attack, "attackCollider");
        var bounds = new UnityEngine.Bounds(root.transform.InverseTransformPoint(collider.transform.TransformPoint(collider.offset)), UnityEngine.Vector3.zero);
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                bounds.Encapsulate(root.transform.InverseTransformPoint(collider.transform.TransformPoint(collider.offset + UnityEngine.Vector2.Scale(collider.size * 0.5f, new UnityEngine.Vector2(x, y)))));
        Near(stats.HitSize.x, bounds.size.x, "칼잡이 플레이어 기준 AABB 너비");
        Near(stats.HitSize.y, bounds.size.y, "칼잡이 플레이어 기준 AABB 높이");
        Near(stats.HitOffset.x, bounds.center.x, "칼잡이 플레이어 기준 AABB 중심 X");
        Near(stats.HitOffset.y, bounds.center.y, "칼잡이 플레이어 기준 AABB 중심 Y");
    }
}
bool Refers(System.Reflection.MethodBase method, System.Reflection.MethodBase target)
{
    if (method == null || target == null) return false;
    var bytes = method.GetMethodBody()?.GetILAsByteArray();
    if (bytes == null) return false;
    var codes = new System.Collections.Generic.Dictionary<short, System.Reflection.Emit.OpCode>();
    foreach (var field in typeof(System.Reflection.Emit.OpCodes).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
    {
        if (field.FieldType != typeof(System.Reflection.Emit.OpCode)) continue;
        var op = (System.Reflection.Emit.OpCode)field.GetValue(null);
        codes[op.Value] = op;
    }
    int at = 0;
    while (at < bytes.Length)
    {
        short code = bytes[at++];
        if (code == 0xfe) code = unchecked((short)(0xfe00 | bytes[at++]));
        var operand = codes[code].OperandType;
        if (operand == System.Reflection.Emit.OperandType.InlineMethod)
        {
            var called = method.Module.ResolveMethod(System.BitConverter.ToInt32(bytes, at));
            if (called.Module == target.Module && called.MetadataToken == target.MetadataToken) return true;
        }
        if (operand == System.Reflection.Emit.OperandType.InlineNone) continue;
        if (operand == System.Reflection.Emit.OperandType.ShortInlineBrTarget || operand == System.Reflection.Emit.OperandType.ShortInlineI || operand == System.Reflection.Emit.OperandType.ShortInlineVar) at++;
        else if (operand == System.Reflection.Emit.OperandType.InlineVar) at += 2;
        else if (operand == System.Reflection.Emit.OperandType.InlineI8 || operand == System.Reflection.Emit.OperandType.InlineR) at += 8;
        else if (operand == System.Reflection.Emit.OperandType.InlineSwitch) at += 4 + System.BitConverter.ToInt32(bytes, at) * 4;
        else at += 4;
    }
    return false;
}
void RoundTrip(SSW.FighterStats value, string label)
{
    using var writer = new Unity.Netcode.FastBufferWriter(2048, Unity.Collections.Allocator.Temp);
    writer.WriteNetworkSerializable(value);
    packetBytes = writer.Length;
    using var reader = new Unity.Netcode.FastBufferReader(writer, Unity.Collections.Allocator.Temp);
    reader.ReadNetworkSerializable(out SSW.FighterStats restored);
    Check(value.Equals(restored), label + " 네트워크 왕복 동등성");
    foreach (var field in typeof(SSW.FighterStats).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        Check(object.Equals(field.GetValue(value), field.GetValue(restored)), label + " 왕복 필드 " + field.Name);
    foreach (var field in typeof(SSW.FlightStats).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        Check(object.Equals(field.GetValue(value.Flight), field.GetValue(restored.Flight)), label + " 왕복 Flight." + field.Name);
}
SSW.StatSources Configure(SSW.StatsBook book, SSW.NetStock stock, SSW.StatSources.Entry[] entries)
{
    var source = UnityEngine.ScriptableObject.CreateInstance<SSW.StatSources>();
    source.name = "BakeChecks";
    memory.Add(source);
    using var data = new UnityEditor.SerializedObject(source);
    data.FindProperty("_book").objectReferenceValue = book;
    data.FindProperty("_stock").objectReferenceValue = stock;
    var array = data.FindProperty("_entries");
    array.arraySize = entries.Length;
    for (int i = 0; i < entries.Length; i++)
    {
        var entry = array.GetArrayElementAtIndex(i);
        entry.FindPropertyRelative("Job").intValue = (int)entries[i].Job;
        entry.FindPropertyRelative("Prefab").objectReferenceValue = entries[i].Prefab;
        entry.FindPropertyRelative("Scene").objectReferenceValue = entries[i].Scene;
        entry.FindPropertyRelative("Root").stringValue = entries[i].Root ?? string.Empty;
    }
    data.ApplyModifiedPropertiesWithoutUndo();
    return source;
}
try
{
    Check(!UnityEditor.AssetDatabase.IsValidFolder(temporary) && !System.IO.Directory.Exists(temporary)
        && !System.IO.File.Exists(temporary) && !System.IO.File.Exists(temporary + ".meta"), "임시 경로가 비어 있음");
    registrations = Asset<Unity.Netcode.NetworkPrefabsList>(networkPrefabs);
    foreach (var item in registrations.PrefabList) originalRegistrations.Add(UnityEngine.JsonUtility.ToJson(item));
    var source = Asset<SSW.StatSources>(configuration);
    var entries = source.Entries;
    Check(entries.Length == 6 && source.Book.Count == 6, "저장된 직업 설정과 책자 6개");
    Protect(source);
    Protect(source.Book);
    Protect(source.Stock);
    foreach (string path in UnityEditor.AssetDatabase.GetDependencies(configuration, true))
    {
        if (!(path.EndsWith(".asset") || path.EndsWith(".prefab") || path.EndsWith(".unity")) || !System.IO.File.Exists(path)) continue;
        savedFiles[path] = Hash(path);
    }
    foreach (var potion in References<AbstractPotion>(source.Stock, "_potions")) Protect(potion);
    SSW.StatSources.Entry magician = default;
    SSW.StatSources.Entry assassin = default;
    SSW.StatSources.Entry gambler = default;
    SSW.StatSources.Entry witch = default;
    SSW.StatSources.Entry gunner = default;
    foreach (var entry in entries)
    {
        var read = SSW.StatRead.Read(entry);
        Check(!expected.ContainsKey(entry.Job), "원본 직업 중복 없음: " + entry.Job);
        expected.Add(entry.Job, read.Stats);
        SSW.StatCheck.Validate(read.Stats);
        Check(source.Book.At(entry.Job).Equals(read.Stats), "저장된 책자와 원본 일치: " + entry.Job);
        WithRoot(entry, root => CompareSource(root, read.Stats));
        RoundTrip(read.Stats, entry.Job.ToString());
        summary.Add(entry.Job + ": HP=" + read.Stats.Health + ", damage=" + read.Stats.Damage + ", interval=" + read.Stats.AttackInterval + ", scale=" + read.Stats.Flight.Scale + ", aspect=" + read.Stats.Flight.Aspect);
        if (entry.Job == SSW.PlayerJob.Magician) magician = entry;
        if (entry.Job == SSW.PlayerJob.Assassin) assassin = entry;
        if (entry.Job == SSW.PlayerJob.Gambler) gambler = entry;
        if (entry.Job == SSW.PlayerJob.Witch) witch = entry;
        if (entry.Job == SSW.PlayerJob.Gunner) gunner = entry;
    }
    SameSceneState();
    var bake = typeof(SSW.StatBake);
    var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
    var all = bake.GetMethod("BakeAll", flags);
    var play = bake.GetMethod("OnPlay", flags);
    Check(System.Attribute.IsDefined(bake, typeof(UnityEditor.InitializeOnLoadAttribute)), "베이크 Editor 로드 등록");
    Check(Refers(bake.TypeInitializer, typeof(UnityEditor.EditorApplication).GetEvent("playModeStateChanged").GetAddMethod()), "Play 상태 이벤트 구독 연결");
    Check(Refers(bake.TypeInitializer, play) && Refers(play, all), "Play 콜백에서 전체 베이크 호출 연결");
    Check(typeof(UnityEditor.Build.IPreprocessBuildWithReport).IsAssignableFrom(typeof(SSW.StatBuild)), "빌드 전처리 인터페이스 연결");
    Check(new SSW.StatBuild().callbackOrder < 0 && Refers(typeof(SSW.StatBuild).GetMethod("OnPreprocessBuild"), all), "빌드 전 베이크 순서와 호출 연결");
    Check(Refers(typeof(SSW.StatImports).GetMethod("OnPostprocessAllAssets", flags), bake.GetMethod("Imported", flags)), "import 의존성 검사 연결");
    object populated = expected[SSW.PlayerJob.Gunner];
    float marker = 1.125f;
    foreach (var field in typeof(SSW.FighterStats).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
    {
        if (field.FieldType == typeof(float)) { field.SetValue(populated, marker); marker += 1.125f; }
        if (field.FieldType == typeof(UnityEngine.Vector2)) { field.SetValue(populated, new UnityEngine.Vector2(marker, -marker - 0.375f)); marker += 1.125f; }
    }
    object flight = SSW.FlightStats.Default;
    foreach (var field in typeof(SSW.FlightStats).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
    {
        field.SetValue(flight, marker);
        marker += 1.125f;
    }
    typeof(SSW.FighterStats).GetField("Flight").SetValue(populated, flight);
    RoundTrip((SSW.FighterStats)populated, "모든 필드가 다른 패킷");
    foreach (var field in typeof(SSW.FighterStats).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
    {
        if (field.FieldType != typeof(float)) continue;
        object invalid = expected[SSW.PlayerJob.Gunner];
        field.SetValue(invalid, float.NaN);
        Reject(() => SSW.StatCheck.Validate((SSW.FighterStats)invalid), "NaN 거부: " + field.Name);
    }
    foreach (var field in typeof(SSW.FlightStats).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
    {
        var invalid = expected[SSW.PlayerJob.Gunner];
        object badFlight = invalid.Flight;
        field.SetValue(badFlight, float.NaN);
        invalid.Flight = (SSW.FlightStats)badFlight;
        Reject(() => SSW.StatCheck.Validate(invalid), "NaN 거부: Flight." + field.Name);
    }
    foreach (int capacity in new[] { -1, 0, 16 })
    {
        var invalid = expected[SSW.PlayerJob.Gunner];
        invalid.Capacity = capacity;
        Reject(() => SSW.StatCheck.Validate(invalid), "탄창 범위 거부: " + capacity);
    }
    var bad = expected[SSW.PlayerJob.Gunner];
    bad.Flight.Aspect = 0f;
    Reject(() => SSW.StatCheck.Validate(bad), "0 Aspect 거부");
    bad = expected[SSW.PlayerJob.Gunner];
    bad.HitOffset.x = float.PositiveInfinity;
    Reject(() => SSW.StatCheck.Validate(bad), "무한 좌표 거부");
    bad = expected[SSW.PlayerJob.Gunner];
    bad.Health = 0f;
    Reject(() => SSW.StatCheck.Validate(bad), "0 기본 체력 거부");
    Reject(() => source.Book.At(SSW.PlayerJob.None), "None 기본값을 임의 생성하지 않음");
    Reject(() => source.Book.At((SSW.PlayerJob)999), "등록되지 않은 직업 거부");
    Check(UnityEditor.AssetDatabase.CreateFolder("Assets/SSW/Tests", "StatBakeCheck").Length > 0, "테스트 소유 폴더 생성");
    folderCreated = true;
    string basePath = temporary + "/Base.prefab";
    string variantPath = temporary + "/Variant.prefab";
    var baseCopy = Copy<UnityEngine.GameObject>(UnityEditor.AssetDatabase.GetAssetPath(magician.Prefab), basePath);
    Reserve(variantPath);
    var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
    ownedScenes.Add(preview.handle.GetRawData());
    try
    {
        var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(baseCopy, preview);
        Set(instance.GetComponent<SSW.Health>(), "maxHealth", 333f);
        Set(instance.GetComponent<SSW.PlayerController>(), "_moveSpeed", 8.75f);
        Set(instance.GetComponent<SSW.PlayerController>(), "_jumpForce", 18.25f);
        Set(instance.GetComponent<UnityEngine.Rigidbody2D>(), "m_GravityScale", 2.125f);
        Set(One<SSW.NumberRoller>(instance), "_cooldownDuration", 2.75f);
        Set(One<SSW.NumberRoller>(instance), "_flySpeed", 42f);
        if (UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance, variantPath) == null)
            throw new System.InvalidOperationException("variant 생성 실패");
    }
    finally
    {
        UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
        ownedScenes.Remove(preview.handle.GetRawData());
    }
    var variant = Asset<UnityEngine.GameObject>(variantPath);
    Check(UnityEditor.PrefabUtility.GetPrefabAssetType(variant) == UnityEditor.PrefabAssetType.Variant, "실제 prefab variant 생성");
    EditPrefab(basePath, root => { Set(root.GetComponent<SSW.Health>(), "maxHealth", 222f); Set(root.GetComponent<SSW.PlayerController>(), "_coyoteTime", 0.23f); });
    UnityEditor.AssetDatabase.ImportAsset(variantPath, UnityEditor.ImportAssetOptions.ForceUpdate);
    var changed = magician;
    changed.Prefab = Asset<UnityEngine.GameObject>(variantPath);
    var overridden = SSW.StatRead.Read(changed).Stats;
    Near(overridden.Health, 333f, "variant 체력 override가 부모 222보다 우선");
    Near(overridden.MoveSpeed, 8.75f, "variant 이동 override");
    Near(overridden.JumpSpeed, 18.25f, "variant 점프 override");
    Near(overridden.Gravity, 2.125f, "variant 물리 override");
    Near(overridden.Coyote, 0.23f, "override 없는 필드는 최신 부모 값 상속");
    Near(overridden.AttackInterval, 2.75f, "variant 직업 능력 override");
    Near(overridden.Flight.Speed, 42f, "variant 투사체 override");
    var book = Create<SSW.StatsBook>(temporary + "/Book.asset");
    var stock = Copy<SSW.NetStock>(UnityEditor.AssetDatabase.GetAssetPath(source.Stock), temporary + "/Stock.asset");
    var testEntries = (SSW.StatSources.Entry[])entries.Clone();
    for (int i = 0; i < testEntries.Length; i++) if (testEntries[i].Job == changed.Job) testEntries[i] = changed;
    var settings = Configure(book, stock, testEntries);
    SSW.StatBake.Bake(settings);
    Check(book.Count == 6 && book.At(SSW.PlayerJob.Magician).Equals(overridden), "임시 설정의 여섯 직업 전체 베이크");
    string baked = UnityEditor.EditorJsonUtility.ToJson(book);
    string bookHash = Hash(UnityEditor.AssetDatabase.GetAssetPath(book));
    var modified = System.IO.File.GetLastWriteTimeUtc(UnityEditor.AssetDatabase.GetAssetPath(book));
    SSW.StatBake.Bake(settings);
    Check(UnityEditor.EditorJsonUtility.ToJson(book) == baked && Hash(UnityEditor.AssetDatabase.GetAssetPath(book)) == bookHash,
        "동일 원본 재베이크에 값·버프 누적 없음");
    Check(System.IO.File.GetLastWriteTimeUtc(UnityEditor.AssetDatabase.GetAssetPath(book)) == modified, "동일 원본 재베이크는 책자를 다시 저장하지 않음");
    var duplicate = (SSW.StatSources.Entry[])testEntries.Clone();
    duplicate[1] = duplicate[0];
    Reject(() => SSW.StatBake.Bake(Configure(book, stock, duplicate)), "중복 직업 설정 거부");
    var missing = new System.Collections.Generic.List<SSW.StatSources.Entry>();
    foreach (var entry in testEntries) if (entry.Job != SSW.PlayerJob.Gambler) missing.Add(entry);
    Reject(() => SSW.StatBake.Bake(Configure(book, stock, missing.ToArray())), "필수 직업 누락 거부");
    Reject(() => SSW.StatBake.Bake(Configure(null, stock, testEntries)), "필수 책자 참조 누락 거부");
    Check(UnityEditor.EditorJsonUtility.ToJson(book) == baked, "설정 검증 실패는 기존 임시 베이크 결과 보존");
    string gunPath = temporary + "/Gun.prefab";
    Copy<UnityEngine.GameObject>(UnityEditor.AssetDatabase.GetAssetPath(gunner.Prefab), gunPath);
    EditPrefab(gunPath, root => Set(One<KDH.Scripts.Gun.KDH_Gun>(root), "<BulletPrefab>k__BackingField", null));
    var noBullet = gunner;
    noBullet.Prefab = Asset<UnityEngine.GameObject>(gunPath);
    Reject(() => SSW.StatRead.Read(noBullet), "필수 투사체 참조 누락 거부");
    string skillPath = temporary + "/Skill.asset";
    string projectilePath = temporary + "/Knife.prefab";
    string playerPath = temporary + "/Assassin.prefab";
    var originalSkill = Reference<NKY.Scripts.Skill.AssassinNormalSkillSo>(One<NKY.Scripts.Skill.PlayerSkillModule>(assassin.Prefab), "_skillData");
    var originalShot = Reference<NKY.Scripts.Skill.AssassinNormalSkill>(originalSkill, "_skillPrefab");
    var skillCopy = Copy<NKY.Scripts.Skill.AssassinNormalSkillSo>(UnityEditor.AssetDatabase.GetAssetPath(originalSkill), skillPath);
    var shotCopy = Copy<UnityEngine.GameObject>(UnityEditor.AssetDatabase.GetAssetPath(originalShot), projectilePath);
    Copy<UnityEngine.GameObject>(UnityEditor.AssetDatabase.GetAssetPath(assassin.Prefab), playerPath);
    Set(skillCopy, "_skillPrefab", shotCopy.GetComponent<NKY.Scripts.Skill.AssassinNormalSkill>());
    UnityEditor.AssetDatabase.SaveAssetIfDirty(skillCopy);
    EditPrefab(playerPath, root => Set(One<NKY.Scripts.Skill.PlayerSkillModule>(root), "_skillData", skillCopy));
    var linked = assassin;
    linked.Prefab = Asset<UnityEngine.GameObject>(playerPath);
    EditPrefab(projectilePath, root => Set(root.transform, "m_LocalScale", new UnityEngine.Vector3(0.8f, 1.4f, 1f)));
    var resized = SSW.StatRead.Read(linked).Stats;
    Near(resized.Flight.Scale, 0.8f, "연결된 투사체 원본 X 크기 반영");
    Near(resized.Flight.Aspect, 1.75f, "연결된 투사체 원본 Y/X 비율 반영");
    EditPrefab(projectilePath, root => Set(root.transform, "m_LocalScale", new UnityEngine.Vector3(0f, 1.4f, 1f)));
    Reject(() => SSW.StatRead.Read(linked), "원본 투사체 0 크기 거부");
    var originalCatalog = References<AbstractPotion>(source.Stock, "_potions");
    var bakedCatalog = References<AbstractPotion>(stock, "_potions");
    Check(bakedCatalog.Length >= originalCatalog.Length, "포션 catalog 항목 보존");
    for (int i = 0; i < originalCatalog.Length; i++) Check(originalCatalog[i] == bakedCatalog[i], "포션 원본 SO와 기존 ID 보존: " + i);
    var basePotions = SSW.StatRead.Read(witch).Potions;
    Check(stock.BaseCount == basePotions.Length, "원본 기본 포션 목록 개수 반영");
    for (int i = 0; i < basePotions.Length; i++) Check(stock.At(stock.BaseAt(i)) == basePotions[i], "원본 기본 포션 순서 연결: " + i);
    var extra = Copy<AbstractPotion>(UnityEditor.AssetDatabase.GetAssetPath(basePotions[0]), temporary + "/ExtraPotion.asset");
    var normal = Copy<AbstractPotion>(UnityEditor.AssetDatabase.GetAssetPath(basePotions[0]), temporary + "/NormalPotion.asset");
    Set(normal, "weight", 1f);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(normal);
    Set(extra, "weight", 100f);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(extra);
    new SSW.StatPotions(stock, new[] { normal, extra }).Apply();
    int extraId = stock.IndexOf(extra);
    Check(stock.IndexOf(normal) == bakedCatalog.Length && extraId == bakedCatalog.Length + 1, "새 포션 참조는 기존 ID 뒤에 append");
    for (int i = 0; i < originalCatalog.Length; i++) Check(stock.At(i) == originalCatalog[i], "append 뒤 기존 포션 ID 보존: " + i);
    Check(stock.Pick(System.Array.Empty<AbstractPotion>(), 1f) == extraId, "확률은 연결된 SO weight 사용");
    Set(extra, "weight", 0f);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(extra);
    Check(stock.Pick(System.Array.Empty<AbstractPotion>(), 1f) == stock.IndexOf(normal), "SO weight 변경은 재베이크 없이 추첨에 반영");
    var wrongRoot = gambler;
    wrongRoot.Root = "__MissingStatsRoot__";
    Reject(() => SSW.StatRead.Read(wrongRoot), "잘못된 씬 원본 루트 거부");
    SameSceneState();
    string scenePath = temporary + "/Gambler.unity";
    var sceneAsset = Copy<UnityEditor.SceneAsset>(UnityEditor.AssetDatabase.GetAssetPath(gambler.Scene), scenePath);
    var dirty = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
    try
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(dirty);
        var dirtyEntry = gambler;
        dirtyEntry.Scene = sceneAsset;
        Reject(() => SSW.StatRead.Read(dirtyEntry), "저장하지 않은 원본 씬은 자동 저장하지 않고 거부");
        Check(dirty.IsValid() && dirty.isLoaded && dirty.isDirty, "거부 후 테스트 원본 씬 dirty 보존");
    }
    finally
    {
        UnityEditor.SceneManagement.EditorSceneManager.CloseScene(dirty, true);
        if (active.IsValid() && active.isLoaded) UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
    }
}
finally
{
    var cleanupErrors = new System.Collections.Generic.List<string>();
    var keptRegistrations = new System.Collections.Generic.List<string>();
    bool unregistered = registrations == null;
    if (registrations != null)
    {
        try
        {
            var remove = new System.Collections.Generic.List<Unity.Netcode.NetworkPrefab>();
            foreach (var item in registrations.PrefabList)
            {
                if (OwnedRegistration(item)) remove.Add(item);
                else keptRegistrations.Add(UnityEngine.JsonUtility.ToJson(item));
            }
            foreach (var item in remove) registrations.Remove(item);
            if (remove.Count > 0)
            {
                UnityEditor.EditorUtility.SetDirty(registrations);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(registrations);
            }
            CheckRegistrations(keptRegistrations, "임시 프리팹 등록 제거 후");
            int next = 0;
            foreach (string item in originalRegistrations)
            {
                while (next < keptRegistrations.Count && keptRegistrations[next] != item) next++;
                Check(next < keptRegistrations.Count, "기존 NGO 등록 항목 보존: " + next);
                next++;
            }
            unregistered = true;
        }
        catch (System.Exception error) { cleanupErrors.Add(error.Message); }
    }
    foreach (var value in memory)
    {
        try { if (value != null && !UnityEditor.EditorUtility.IsPersistent(value)) UnityEngine.Object.DestroyImmediate(value); }
        catch (System.Exception error) { cleanupErrors.Add(error.Message); }
    }
    for (int i = created.Count - 1; i >= 0; i--)
    {
        string path = created[i];
        try
        {
            if (!path.StartsWith(temporary + "/", System.StringComparison.Ordinal)) throw new System.InvalidOperationException("정리 범위 오류: " + path);
            if (!unregistered && path.EndsWith(".prefab", System.StringComparison.Ordinal))
                throw new System.InvalidOperationException("NGO 등록 정리 실패로 프리팹 삭제를 멈췄습니다: " + path);
            if ((UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) != null || System.IO.File.Exists(path)) && !UnityEditor.AssetDatabase.DeleteAsset(path))
                cleanupErrors.Add("임시 에셋 정리 실패: " + path);
        }
        catch (System.Exception error) { cleanupErrors.Add(error.Message); }
    }
    if (folderCreated)
    {
        try
        {
            if (System.IO.Directory.Exists(temporary) && System.IO.Directory.GetFileSystemEntries(temporary).Length != 0)
                cleanupErrors.Add("생성하지 않은 파일이 남아 폴더 정리를 멈췄습니다: " + temporary);
            else if (!UnityEditor.AssetDatabase.DeleteAsset(temporary)) cleanupErrors.Add("임시 폴더 정리 실패");
        }
        catch (System.Exception error) { cleanupErrors.Add(error.Message); }
    }
    if (registrations != null && unregistered)
    {
        try { CheckRegistrations(keptRegistrations, "임시 에셋 삭제 후"); }
        catch (System.Exception error) { cleanupErrors.Add(error.Message); }
    }
    try { SameSceneState(); }
    catch (System.Exception error) { cleanupErrors.Add(error.Message); }
    foreach (var value in savedObjects)
    {
        try
        {
            Check(UnityEditor.EditorJsonUtility.ToJson(value.Key) == value.Value.Json, "운영 에셋 값 보존: " + value.Key.name);
            Check(UnityEditor.EditorUtility.IsDirty(value.Key) == value.Value.Dirty, "운영 에셋 dirty 보존: " + value.Key.name);
        }
        catch (System.Exception error) { cleanupErrors.Add(error.Message); }
    }
    foreach (var file in savedFiles)
    {
        try { Check(System.IO.File.Exists(file.Key) && Hash(file.Key) == file.Value, "원본 파일 보존: " + file.Key); }
        catch (System.Exception error) { cleanupErrors.Add(error.Message); }
    }
    if (cleanupErrors.Count > 0) throw new System.InvalidOperationException(string.Join("\n", cleanupErrors));
}
if (checks.Count > 0)
{
    var result = new { passed = checks.Count, jobs = summary.ToArray(), packetBytes,
        hooks = "Play/build/import 호출 연결 구조 확인. 실제 Play 진입과 빌드는 별도 검증 필요.",
        cleanup = "테스트 프리팹의 NGO 등록 제거 후 생성한 정확한 경로만 삭제. 기존 등록·운영 에셋·열린 씬 상태 보존." };
    System.IO.Directory.CreateDirectory("Logs/Stats");
    System.IO.File.WriteAllText("Logs/Stats/BakeResult.json", Newtonsoft.Json.JsonConvert.SerializeObject(result));
    return result;
}

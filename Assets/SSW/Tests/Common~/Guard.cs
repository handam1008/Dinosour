using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SSW;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GuardChecks
{
    static readonly List<string> Checks = new List<string>();
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const string Output = "Logs/Common/Guard.txt";

    static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
    static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
    static void Check(bool condition, string name)
    {
        File.AppendAllText(Output, (condition ? "PASS " : "FAIL ") + name + "\n");
        if (!condition) throw new InvalidOperationException(name);
        Checks.Add(name);
    }

    static GameObject Box(string name, Vector2 position, Vector2 size, float angle = 0f, bool platform = false)
    {
        var obj = new GameObject(name);
        obj.layer = 8;
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        BoxCollider2D shape = obj.AddComponent<BoxCollider2D>();
        shape.size = size;
        if (platform)
        {
            shape.usedByEffector = true;
            obj.AddComponent<PlatformEffector2D>().useOneWay = true;
        }
        Physics2D.SyncTransforms();
        return obj;
    }

    public static void Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first");
        Directory.CreateDirectory("Logs/Common");
        File.WriteAllText(Output, "");
        Checks.Clear();
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
        CommonAugment leap = null;
        try
        {
            var obj = new GameObject("Guard Test");
            obj.SetActive(false);
            var player = obj.AddComponent<NetPlayer>();
            var motion = obj.AddComponent<PlayerController>();
            var buffs = obj.AddComponent<NetBuff>();
            var guard = obj.AddComponent<BuffGuard>();
            var area = obj.AddComponent<BuffArea>();
            Set(player, "_motion", motion);
            Set(guard, "_player", player);
            Set(guard, "_buffs", buffs);
            Set(area, "_player", player);
            using (var data = new SerializedObject(motion))
            {
                data.FindProperty("_whatIsGround").intValue = 1 << 8;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            var owned = Get<HashSet<CommonAugmentType>>(buffs, "_owned");
            Check(Mathf.Approximately(guard.Cooldown, 3f) && Mathf.Approximately(guard.Duration, 0.5f), "base guard duration and cooldown");
            var multipliers = new Dictionary<CommonAugmentType, float>
            {
                [CommonAugmentType.GuardMastery] = 0.7f,
                [CommonAugmentType.Recharge] = 1.4f,
                [CommonAugmentType.Blink] = 1.2f,
                [CommonAugmentType.IceAge] = 1.15f,
                [CommonAugmentType.BestOffense] = 1.5f,
                [CommonAugmentType.Nuclear] = 2.5f,
                [CommonAugmentType.Versatile] = 0.9f
            };
            foreach (var pair in multipliers)
            {
                owned.Clear();
                owned.Add(pair.Key);
                Check(Mathf.Approximately(guard.Cooldown, 3f * pair.Value), pair.Key + " cooldown modifier");
            }
            owned.Clear();
            owned.Add(CommonAugmentType.Invincible);
            Check(Mathf.Approximately(guard.Duration, 1.5f) && Mathf.Approximately(guard.Cooldown, 3f), "invincible extends guard without cooldown penalty");
            float expected = 3f;
            foreach (var pair in multipliers) { owned.Add(pair.Key); expected *= pair.Value; }
            Check(Mathf.Approximately(guard.Cooldown, expected), "cooldown modifiers multiply without overwriting each other");
            leap = ScriptableObject.CreateInstance<CommonAugment>();
            leap.type = (CommonAugmentType)100;
            Set(guard, "_leapBombAugment", leap);
            Get<HashSet<Augment>>(buffs, "_assets").Add(leap);
            Check(Mathf.Approximately(guard.Cooldown, expected * 1.2f), "authored leap bomb reference adds cooldown penalty");
            Check(!guard.Ready && !guard.Guarding && !guard.Empowered, "unspawned player cannot guard or empower");
            guard.Guard();
            Check(guard.ReadyAt == 0d && guard.GuardUntil == 0d, "unspawned guard command has no effect");
            Check(guard.ModifyIncomingDamage(new DamageRequest(null, 12f), 12f) == 12f, "non-authority guard cannot block damage");
            Check(guard.ModifyOutgoingDamage(12f) == 12f, "non-authority guard cannot multiply damage");
            area.Heal(Vector2.zero);
            area.Mine(Vector2.zero);
            area.Bomb(Vector2.zero, Vector2.one);
            Check(area.Count == 0 && Get<System.Collections.IList>(area, "_heals").Count == 0, "non-authority cannot create healing fields, mines or bombs");
            Check(Get<float>(area, "_healRadius") == 1.3f && Get<float>(area, "_healRatio") == 0.2f && Get<float>(area, "_healChargeTime") == 1f, "heal field uses authored prefab values");
            Check(Get<float>(area, "_mineRadius") == 2f && Get<float>(area, "_mineDamage") == 10f && Get<float>(area, "_mineKnockback") == 6f, "mine uses authored prefab values");
            Check(Get<float>(area, "_bombRadius") == 1.5f && Get<float>(area, "_bombFuse") == 0.45f && Get<float>(area, "_bombArm") == 0.3f, "bomb uses authored radius and fuse");
            var targetObject = new GameObject("Area Target");
            targetObject.transform.position = new Vector2(1060f, 2f);
            var targetShape = targetObject.AddComponent<CapsuleCollider2D>();
            targetShape.size = new Vector2(1f, 2f);
            Set(player, "_collider", targetShape);
            Physics2D.SyncTransforms();
            Check(BuffArea.InRange(player, new Vector2(1061.7f, 2f), 1.3f), "area includes collider edge when center is outside");
            Check(!BuffArea.InRange(player, new Vector2(1062f, 2f), 1.3f), "area rejects collider outside radius");
            Type bodyType = typeof(BuffArea).GetNestedType("Body", BindingFlags.NonPublic);
            MethodInfo advance = typeof(BuffArea).GetMethod("Advance", Fields);
            object Body(Vector2 position, Vector2 velocity)
            {
                object body = Activator.CreateInstance(bodyType, true);
                Set(body, "State", new BuffAreaState { Kind = BuffAreaKind.Bomb, Position = position, Velocity = velocity });
                return body;
            }
            BuffAreaState Step(object body, int steps)
            {
                for (int i = 0; i < steps; i++) advance.Invoke(area, new object[] { body, 1f / 60f });
                return Get<BuffAreaState>(body, "State");
            }
            GameObject floor = Box("Area Floor", new Vector2(1000f, -0.5f), new Vector2(20f, 1f));
            object falling = Body(new Vector2(1000f, 4f), new Vector2(3f, 0f));
            BuffAreaState state = Step(falling, 180);
            Check(state.Settled && Mathf.Abs(state.Position.y - 0.227f) < 0.01f && state.Velocity == Vector2.zero, "falling bomb settles at collider bottom on floor");
            Vector2 before = state.Position;
            floor.transform.position += Vector3.up;
            Physics2D.SyncTransforms();
            state = Step(falling, 1);
            Check(Vector2.Distance(state.Position, before + Vector2.up) < 0.001f, "settled mine and bomb follow moving ground");
            UnityEngine.Object.DestroyImmediate(floor);
            state = Step(falling, 1);
            Check(!state.Settled && state.Velocity.y < 0f, "removed ground releases settled bomb");
            GameObject slope = Box("Area Slope", new Vector2(1010f, 0f), new Vector2(8f, 0.5f), 45f);
            state = Step(Body(new Vector2(1010f, 4f), Vector2.zero), 180);
            Check(state.Settled && state.Position.y > 0.5f && state.Position.y < 1f, "45 degree slope supports mine and bomb");
            Box("Area Thin Wall", new Vector2(1023f, 2f), new Vector2(0.05f, 8f));
            state = Step(Body(new Vector2(1020f, 2f), new Vector2(300f, 0f)), 1);
            Check(state.Position.x < 1022.7f && Mathf.Abs(state.Velocity.x) < 0.001f, "fast bomb sweep cannot tunnel through thin wall");
            Box("Area One Way", new Vector2(1040f, 0f), new Vector2(8f, 0.2f), 0f, true);
            state = Step(Body(new Vector2(1040f, -1f), Vector2.up * 100f), 1);
            Check(!state.Settled && state.Position.y > 0.6f, "bomb passes upward through one-way platform");
            state = Step(Body(new Vector2(1040f, 3f), Vector2.down * 100f), 2);
            Check(state.Settled && Mathf.Abs(state.Position.y - 0.327f) < 0.015f, "bomb lands on one-way platform from above");
            GameObject trigger = Box("Area Trigger", new Vector2(1080f, 1f), new Vector2(8f, 1f));
            trigger.GetComponent<BoxCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms();
            state = Step(Body(new Vector2(1080f, 4f), Vector2.down * 100f), 3);
            Check(!state.Settled && state.Position.y < 0f, "trigger volume does not stop falling bomb");
            File.AppendAllText(Output, "TOTAL " + Checks.Count + "\n");
            Debug.Log("Guard checks passed: " + Checks.Count);
        }
        finally
        {
            if (leap != null) UnityEngine.Object.DestroyImmediate(leap);
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }
}

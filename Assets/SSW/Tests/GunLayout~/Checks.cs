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
using Object = UnityEngine.Object;

public static class GunLayoutChecks
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> Checks = new List<string>();
    static readonly List<object> Shots = new List<object>();
    static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
    static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        Checks.Add(name);
    }
    static bool Same(Transform a, Transform b) => Vector3.Distance(a.localPosition, b.localPosition) < 0.001f
        && Quaternion.Angle(a.localRotation, b.localRotation) < 0.001f && Vector3.Distance(a.localScale, b.localScale) < 0.001f;
    static void Pose(NetPlayer player, KDH.Scripts.Gun.KDH_Gun source, string name)
    {
        var view = ((GunCast)player.Cast.Weapon).View;
        var muzzle = Get<Transform>(view, "_muzzle");
        var holder = view.transform.Find("Tanchang");
        Check(Same(muzzle, source.GunPos), name + " exact original muzzle local pose");
        Check(Same(holder, source.Tanchang.transform), name + " exact original ammo holder local pose");
        Check(holder.childCount == source.AmmoPrefabs.Length, name + " original ammo slot count");
        for (int i = 0; i < holder.childCount; i++)
            Check(Same(holder.GetChild(i), source.AmmoPrefabs[i].transform), name + " ammo local pose " + i);
    }
    static void Aim(NetPlayer player, Vector2 direction)
    {
        typeof(NetPlayer).GetField("_aim", Fields).SetValue(player, direction);
        ((GunCast)player.Cast.Weapon).View.Render(true);
    }
    static async Task Wait(Func<bool> condition, string name)
    {
        double end = Time.realtimeSinceStartupAsDouble + 12;
        while (!condition())
        {
            if (Time.realtimeSinceStartupAsDouble > end) throw new TimeoutException(name);
            await Task.Delay(30);
        }
    }
    public static async Task RunAsync()
    {
        Directory.CreateDirectory("Logs/GunLayout26");
        var errors = new List<string>();
        void Log(string message, string stack, LogType kind)
        {
            if (kind == LogType.Error || kind == LogType.Exception) errors.Add(message + "\n" + stack);
        }
        Application.logMessageReceived += Log;
        string failure = null;
        NetPlayer remote = null;
        try
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KDH/GameModules/KDH_Player Variant.prefab").GetComponent<KDH.Scripts.Gun.KDH_Gun>();
            var practice = NetGame.Current.Practice;
            var player = practice.Player;
            var gun = (GunCast)player.Cast.Weapon;
            var view = gun.View;
            Check(player.IsOwner && player.Job == PlayerJob.Gunner && player.CanAct, "real sandbox gunner owner");
            await Wait(() => player.Drive.Grounded && gun.Status.Ammo >= 4, "ammo recharge and grounded");
            Pose(player, source, "owner");
            Check(gun.Status.Ammo <= source.MaxAmmo && view.VisibleAmmo == gun.Status.Ammo, "visible ammo matches authoritative stock");
            foreach (var direction in new[] { Vector2.right, Vector2.left, new Vector2(1, 1).normalized, new Vector2(-1, 1).normalized })
            {
                Aim(player, direction);
                var muzzle = Get<Transform>(view, "_muzzle");
                Vector2 shown = muzzle.position;
                Vector2 computed = view.Muzzle(player.View.position, direction, player.Drive.Scale);
                Check(Vector2.Distance(shown, computed) < 0.001f, direction + " displayed muzzle matches launch calculation");
                int count = gun.Status.Ammo;
                int shotCount = player.Cast.Shots;
                player.Cast.Attack(true, direction);
                await Wait(() => player.Cast.Shots > shotCount, "real fire " + direction);
                var bolt = Object.FindObjectsByType<NetBolt>().Where(b => b.Caster == player.NetworkObjectId).OrderByDescending(b => b.Action).First();
                Check(Vector2.Distance(player.Cast.Origin, shown) < 0.03f, direction + " actual network bolt starts at displayed muzzle");
                Check(Vector2.Distance(bolt.Position, player.Cast.Origin) < 0.03f, direction + " first projectile position uses muzzle");
                Check(gun.Status.Ammo == count - 1, direction + " actual fire consumes one round");
                view.Render(true);
                Check(view.VisibleAmmo == gun.Status.Ammo, direction + " ammo display follows consumption");
                Shots.Add(new { direction = direction.ToString(), shown = shown.ToString("F4"), spawned = player.Cast.Origin.ToString("F4"), ammo = gun.Status.Ammo });
                player.Cast.Attack(false, direction);
                await Task.Delay(100);
            }
            int remaining = gun.Status.Ammo;
            await Wait(() => gun.Status.Ammo > remaining, "real recharge after shots");
            view.Render(true);
            Check(view.VisibleAmmo == gun.Status.Ammo, "recharge updates visible ammo");
            Pose(player, source, "after firing and recharge");

            var prefab = Resources.Load<NetPlayer>("Network/Player");
            remote = Object.Instantiate(prefab, new Vector3(0, 5, 0), Quaternion.identity);
            remote.Init(PlayerJob.Gunner, -1);
            remote.NetworkObject.SpawnWithOwnership(1, true);
            await Task.Delay(100);
            Pose(remote, source, "nonowner proxy");
            foreach (var direction in new[] { Vector2.right, Vector2.left, new Vector2(-1, 1).normalized })
            {
                Aim(remote, direction);
                var remoteView = ((GunCast)remote.Cast.Weapon).View;
                var muzzle = Get<Transform>(remoteView, "_muzzle");
                Check(Vector2.Distance(muzzle.position, remoteView.Muzzle(remote.View.position, direction, remote.Drive.Scale)) < 0.001f,
                    direction + " nonowner displayed muzzle matches launch calculation");
            }
            remote.NetworkObject.Despawn();
            remote = null;
            practice.ResetMap();
            await Task.Delay(250);
            Pose(practice.Player, source, "respawn");
            Check(!ReferenceEquals(player, practice.Player) && practice.Player.IsOwner, "actual respawn replaced owner");
            await Wait(() => ((GunCast)practice.Player.Cast.Weapon).Status.Ammo > 0, "respawn recharge");
            var restoredGun = (GunCast)practice.Player.Cast.Weapon;
            restoredGun.View.Render(true);
            Check(restoredGun.View.VisibleAmmo == restoredGun.Status.Ammo, "respawn preserves ammo display binding");
            Aim(practice.Player, new Vector2(1, 0.3f).normalized);
            ScreenCapture.CaptureScreenshot("Logs/GunLayout26/layout.png");
            await Task.Delay(150);
            Check(errors.Count == 0, "no runtime errors");
        }
        catch (Exception ex) { failure = ex.ToString(); }
        finally
        {
            if (remote != null && remote.IsSpawned) remote.NetworkObject.Despawn();
            Application.logMessageReceived -= Log;
            File.WriteAllText("Logs/GunLayout26/checks.json", JsonConvert.SerializeObject(new
            {
                passed = failure == null, count = Checks.Count, failure, checks = Checks, shots = Shots, errors,
                coverage = "Editor sandbox host and nonowner proxy; no separate client process or two-PC session"
            }, Formatting.Indented));
        }
    }
}

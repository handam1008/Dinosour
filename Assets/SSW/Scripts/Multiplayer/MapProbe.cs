#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public static class MapProbe
    {
        [Serializable] public sealed class State
        {
            public ulong[] ids;
            public Vector3[] moving;
            public Flood[] floods;
            public Breath[] breaths;
            public Flash[] flashes;
            public Sakura[] sakura;
        }

        [Serializable] public sealed class Animator
        {
            public bool enabled;
            public float time;
        }

        [Serializable] public sealed class Flood
        {
            public float height;
            public float target;
            public double startedAt;
            public float duration;
            public Animator[] animators;
        }

        [Serializable] public sealed class Breath
        {
            public bool active;
            public double startedAt;
            public float delay;
            public float duration;
            public int particles;
            public bool playing;
            public Color first;
            public Color second;
        }

        [Serializable] public sealed class Flash
        {
            public double at;
            public Color color;
        }

        [Serializable] public sealed class Sakura
        {
            public int children;
            public int particles;
            public uint seed;
            public double startedAt;
            public bool playing;
            public string culling;
        }

        public static State Read(NetGame game, BattleMap map)
        {
            if (map == null || !game.Connected) return null;
            return new State
            {
                ids = game.Manager.SpawnManager.SpawnedObjectsList.Where(value => value.TryGetComponent<BattleMap>(out _))
                    .Select(value => value.NetworkObjectId).ToArray(),
                moving = Field<Transform[]>(map.GetComponent<MapMotion>(), "_moving").Select(value => value.position).ToArray(),
                floods = map.GetComponentsInChildren<MapFlood>(true).Select(value =>
                {
                    var water = Field<Transform>(value, "_water");
                    return new Flood
                    {
                        height = water.position.y, target = Field<Transform>(value, "_target").position.y,
                        startedAt = Clock(value, "_startedAt"), duration = Field<float>(value, "_duration"),
                        animators = water.GetComponentsInChildren<UnityEngine.Animator>(true).Select(animation =>
                            new Animator { enabled = animation.isActiveAndEnabled && animation.runtimeAnimatorController != null,
                                time = animation.GetCurrentAnimatorStateInfo(0).normalizedTime }).ToArray()
                    };
                }).ToArray(),
                breaths = map.GetComponentsInChildren<MapBreath>(true).Select(value =>
                {
                    var effect = Field<ParticleSystem>(value, "_effect");
                    return new Breath { active = value.Active, startedAt = Clock(value, "_startedAt"),
                        delay = Field<float>(value, "_delay"), duration = effect.main.duration,
                        particles = effect.particleCount, playing = effect.isPlaying,
                        first = Field<SpriteRenderer>(value, "_first").color,
                        second = Field<SpriteRenderer>(value, "_second").color };
                }).ToArray(),
                flashes = map.GetComponentsInChildren<MapFlash>(true).Select(value => new Flash
                {
                    at = Clock(value, "_at"), color = Field<SpriteRenderer>(Field<MapTint>(value, "_tint"), "_sprite").color
                }).ToArray(),
                sakura = map.GetComponentsInChildren<SakuraField>(true).Select(value => new Sakura
                {
                    children = value.transform.childCount, particles = Field<ParticleSystem>(value, "_petals").particleCount,
                    seed = Field<ParticleSystem>(value, "_petals").randomSeed,
                    startedAt = Clock(value, "_startedAt"), playing = Field<ParticleSystem>(value, "_petals").isPlaying,
                    culling = Field<ParticleSystem>(value, "_petals").main.cullingMode.ToString()
                }).ToArray()
            };
        }

        static double Clock(object target, string field) => Field<NetworkVariable<double>>(target, field).Value;
        static T Field<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SSW
{
    public sealed class NetTrace
    {
        [Serializable] sealed class Shot
        {
            public ulong caster;
            public uint action;
            public int part;
            public bool preview;
            public bool ending;
            public bool blocked;
            public float age;
            public uint turn;
            public Vector2 position;
            public Vector2 velocity;
            public double sampleTime;
            public double viewTime;
            public Vector2 samplePosition;
            public Vector2 gravity;
        }

        [Serializable] sealed class Frame
        {
            public double time;
            public long utc;
            public double serverTime;
            public double localTime;
            public double physicsTime;
            public double otherTime;
            public float hp;
            public float otherHp;
            public float delta;
            public Vector2 velocity;
            public uint pulse;
            public uint gust;
            public bool gustEffect;
            public float gustTime;
            public int sakuraEffects;
            public uint petalSeed;
            public float petalTime;
            public float speed;
            public float timeScale;
            public Vector2 body;
            public Vector2 view;
            public Vector2 otherView;
            public float correction;
            public uint epoch;
            public uint tick;
            public uint processed;
            public Shot[] shots;
        }

        [Serializable] sealed class Trace
        {
            public string build;
            public ulong owner;
            public List<Frame> frames = new List<Frame>(2400);
        }

        Trace _trace;
        string _path;
        double _until;

        public void Begin(string path, float seconds)
        {
            _path = path;
            _trace = new Trace { build = Application.buildGUID };
            _until = Time.unscaledTimeAsDouble + Mathf.Clamp(seconds, 1f, 30f);
        }

        public void Sample(NetGame game)
        {
            if (_trace == null || !game.Connected || game.Local == null) return;
            NetPlayer player = game.Local;
            MotionView motion = player.GetComponent<MotionView>();
            Vector2 otherView = default;
            double otherTime = 0d;
            float otherHp = 0f;
            foreach (NetPlayer other in game.Players)
                if (other != player)
                {
                    otherView = other.View.position;
                    otherTime = other.ViewTime;
                    otherHp = other.Health.Current;
                }
            var shots = new List<Shot>();
            player.Cast.ReadPreviews((action, part, position, blocked) => shots.Add(new Shot
            {
                caster = player.NetworkObjectId, action = action, part = part, preview = true, blocked = blocked, position = position
            }));
            foreach (var entry in game.Manager.SpawnManager.SpawnedObjects)
                if (entry.Value.TryGetComponent(out ShotSync shot) && shot.Sprite.enabled)
                    shots.Add(new Shot
                    {
                        caster = shot.Caster, action = shot.Action, part = shot.Part,
                        position = shot.transform.position, velocity = shot.Velocity, blocked = shot.Blocked, age = shot.Age, turn = shot.Turn,
                        sampleTime = shot.Pose.Time, viewTime = shot.ViewTime,
                        samplePosition = shot.Pose.Position, gravity = shot.Pose.Gravity
                    });
            foreach (NetPlayer caster in game.Players)
                foreach (ShotTail tail in caster.Cast.Finishes)
                    shots.Add(new Shot
                    {
                        caster = caster.NetworkObjectId, action = tail.Action, part = tail.Part,
                        ending = true, position = tail.transform.position
                    });
            var map = game.Arena.Map;
            var gust = map == null ? null : map.GetComponentInChildren<MapGust>();
            var effect = gust == null ? null : map.transform.Find("KDH_Map 4/ForceEffect").GetComponent<ParticleSystem>();
            var sakura = map == null ? null : map.GetComponentInChildren<SakuraField>();
            _trace.owner = player.NetworkObjectId;
            _trace.frames.Add(new Frame
            {
                time = Time.unscaledTimeAsDouble, delta = Time.unscaledDeltaTime,
                utc = DateTime.UtcNow.Ticks, serverTime = game.Manager.ServerTime.Time,
                localTime = game.Manager.LocalTime.Time, physicsTime = game.PhysicsTime,
                otherTime = otherTime, hp = player.Health.Current, otherHp = otherHp,
                velocity = motion.Velocity, pulse = motion.PulseSequence, gust = gust == null ? 0 : gust.PulseCount,
                gustEffect = effect != null && effect.isPlaying, gustTime = effect == null ? 0 : effect.time,
                sakuraEffects = sakura == null ? 0 : sakura.GetComponentsInChildren<ParticleSystem>().Length - 1,
                petalSeed = sakura == null ? 0 : sakura.GetComponent<ParticleSystem>().randomSeed,
                petalTime = sakura == null ? 0 : sakura.GetComponent<ParticleSystem>().time,
                speed = motion.Speed, timeScale = Time.timeScale,
                body = player.Body.position, view = player.View.position, otherView = otherView, correction = motion.Correction,
                epoch = motion.Epoch, tick = motion.Tick, processed = motion.Processed, shots = shots.ToArray()
            });
            if (Time.unscaledTimeAsDouble < _until) return;
            File.WriteAllText(_path, JsonUtility.ToJson(_trace));
            _trace = null;
        }
    }
}
#endif

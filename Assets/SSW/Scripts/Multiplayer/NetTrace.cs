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
            public bool blocked;
            public float age;
            public uint turn;
            public Vector2 position;
            public Vector2 velocity;
        }

        [Serializable] sealed class Frame
        {
            public double time;
            public float delta;
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
            foreach (NetPlayer other in game.Players)
                if (other != player) otherView = other.View.position;
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
                        position = shot.transform.position, velocity = shot.Velocity, blocked = shot.Blocked, age = shot.Age, turn = shot.Turn
                    });
            _trace.owner = player.NetworkObjectId;
            _trace.frames.Add(new Frame
            {
                time = Time.unscaledTimeAsDouble, delta = Time.unscaledDeltaTime,
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

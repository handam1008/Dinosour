using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    public sealed class ShotContact
    {
        readonly NetPlayer _caster;
        readonly IReadOnlyList<NetPlayer> _players;
        NetPlayer _target;
        NetPlayer _ignored;
        Vector2 _direction;
        uint _epoch;
        uint _turn;
        float _age;
        float _clearance;

        public bool Blocked => _target != null;
        public Vector2 Point { get; private set; }

        public ShotContact(NetPlayer caster, IReadOnlyList<NetPlayer> players)
        {
            _caster = caster;
            _players = players;
        }

        public bool TryBlock(Vector2 from, Vector2 to, float radius, Vector2 size, float age, float speed)
        {
            if ((to - from).sqrMagnitude < 0.000001f) return false;
            float nearest = float.PositiveInfinity;
            float half = Mathf.Max(0f, (size.y - size.x) * 0.5f);
            foreach (NetPlayer player in _players)
            {
                if (player == _caster || player == _ignored || !player.CanAct) continue;
                if (!player.SweepView(from, to, radius, half, out float fraction) || fraction >= nearest) continue;
                nearest = fraction;
                _target = player;
            }
            if (nearest > 1f) return false;
            Point = Vector2.Lerp(from, to, nearest);
            _direction = (to - from).normalized;
            _epoch = _target.Epoch;
            _age = age;
            _clearance = radius + speed * Time.fixedDeltaTime * 2f;
            return true;
        }

        public bool Advance(ShotPose pose, double born)
        {
            bool turned = pose.Turn != _turn;
            _turn = pose.Turn;
            if (turned) _ignored = null;
            if (!Blocked) return false;
            bool moved = !_target.IsSpawned || _target.Epoch != _epoch;
            bool passed = pose.Time >= born + _age
                && Vector2.Dot(pose.Position - Point, _direction) > _clearance;
            if (!turned && !moved && !passed) return false;
            if (!turned) _ignored = _target;
            _target = null;
            return true;
        }
    }
}

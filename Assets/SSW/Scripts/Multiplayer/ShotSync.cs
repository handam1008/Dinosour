using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(150)]
    public sealed class ShotSync : NetworkBehaviour
    {
        [SerializeField] Rigidbody2D _body;
        [SerializeField] SpriteRenderer _sprite;
        [SerializeField] MagicianCardFeedback _feedback;
        [SerializeField] LayerMask _ground;
        readonly Queue<ShotPose> _pending = new Queue<ShotPose>();
        readonly NetworkVariable<ShotPose> _seed = new NetworkVariable<ShotPose>();
        NetPlayer _caster;
        uint _action;
        uint _turn;
        int _part;
        ShotPose _pose;
        Vector2 _offset;
        Vector2 _normal;
        Vector2 _stop;
        float _angleOffset;
        ViewClock _clock;
        double _sentAt;
        double _receivedAt;
        bool _ready;
        bool _local;
        bool _caught;
        bool _ending;
        bool _released;
        bool _held;
        Collider2D _contact;
        double _blockedAt;
        double _caughtAt;
        float _radius;
        Vector2 _size;
        ShotContact _target;

        public float Age => IsServer ? 0f : (float)(_clock.Time - _pose.Time);
        public uint Turn => _pose.Turn;
        public bool Terrain { get; set; } = true;
        public bool AlignVelocity { get; set; }
        public SpriteRenderer Sprite => _sprite;
        public bool Blocked => _normal.sqrMagnitude > 0f || _caught || _target != null && _target.Blocked;
        public ulong Caster => _caster.NetworkObjectId;
        public uint Action => _action;
        public int Part => _part;
        public Vector2 Velocity => _pose.Velocity;
        internal ShotPose Pose => _pose;
        internal double ViewTime => IsServer ? NetGame.Current.PhysicsTime : _clock.Time;
        double Now => _local ? NetworkManager.LocalTime.Time : NetGame.Current.ServerTime;

        public void Redirect() => _turn++;
        public void Hold(bool hold) => _held = hold;

        public void Deflect(NetPlayer caster)
        {
            _held = false;
            _caster = caster;
            _target = new ShotContact(caster, NetGame.Current.Players);
            _normal = Vector2.zero;
            _contact = null;
            _caught = false;
            if (IsServer) Redirect();
        }

        public void Bind(NetPlayer caster, uint action, int part, float radius = 0f, Vector2 size = default)
        {
            _caster = caster;
            _action = action;
            _part = part;
            _local = caster.IsOwner;
            _radius = radius;
            _size = size;
            _target = new ShotContact(caster, NetGame.Current.Players);
        }

        protected override void OnNetworkPostSpawn()
        {
            if (IsServer)
            {
                _body.interpolation = RigidbodyInterpolation2D.Interpolate;
                _pose = _seed.Value = Capture(NetGame.Current.PhysicsTime);
                return;
            }
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.interpolation = RigidbodyInterpolation2D.None;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _pose = _seed.Value;
            _receivedAt = _pose.Time;
            _ready = true;
            _clock.Reset(System.Math.Max(Now, _pose.Time));
            transform.SetPositionAndRotation(_pose.Point(_clock.Time), Quaternion.Euler(0f, 0f, Angle(_pose, _clock.Time)));
            bool matched = _local && _caster.Cast.MatchShot(_action, _part, this);
            if (_feedback != null && !matched) _feedback.PlayLaunch();
        }

        float Angle(ShotPose pose, double time)
        {
            if (!AlignVelocity) return pose.Rotation(time);
            Vector2 velocity = pose.Velocity + pose.Gravity * Mathf.Clamp((float)(time - pose.Time), 0f, 1f);
            return velocity.sqrMagnitude > 0.000001f ? Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg : pose.Angle;
        }

        ShotPose Capture(double time)
        {
            return new ShotPose
            {
                Time = time, Position = _body.position, Velocity = _body.linearVelocity,
                Gravity = Physics2D.gravity * _body.gravityScale,
                Angle = _body.rotation, Spin = _body.angularVelocity, Terrain = Terrain, Turn = _turn
            };
        }

        public void Adopt(Transform view, Vector2 normal, Collider2D contact, ShotContact target)
        {
            double time = _clock.Time;
            _normal = normal;
            _contact = contact;
            _blockedAt = _clock.Time;
            _stop = view.position;
            _target = target;
            _offset = (Vector2)view.position - _pose.Point(time);
            _angleOffset = Mathf.DeltaAngle(Angle(_pose, time), view.eulerAngles.z);
            transform.SetPositionAndRotation(view.position, view.rotation);
        }

        public void Receive(ShotPose pose)
        {
            if (!_ready || pose.Time <= _receivedAt) return;
            _receivedAt = pose.Time;
            _pending.Enqueue(pose);
            if (_pending.Count > 32) _pending.Dequeue();
        }

        void Accept(ShotPose pose, double time, double previous)
        {
            _offset = pose.Turn != _pose.Turn
                ? (Vector2)transform.position - pose.Point(previous)
                : _offset + _pose.Point(time) - pose.Point(time);
            _angleOffset = Mathf.DeltaAngle(Angle(pose, time), Angle(_pose, time) + _angleOffset);
            bool cleared = _normal.sqrMagnitude > 0f && !ShotQuery.Touches(_contact, _stop, _radius, _size);
            bool escaped = pose.Time >= _blockedAt && Vector2.Dot(pose.Velocity, _normal) > 0.01f
                && Vector2.Dot(pose.Position - _stop, _normal) > _radius
                && Vector2.Dot(pose.Position - _stop, _pose.Velocity) >= 0f;
            if (_normal.sqrMagnitude > 0f && (!pose.Terrain || pose.Turn != _pose.Turn || cleared || escaped))
                Release(pose, time);
            if (_caught && pose.Time >= _caughtAt && Vector2.Distance(pose.Position, _caster.View.position) > 0.5f) _caught = false;
            if (_target != null && _target.Advance(pose, _seed.Value.Time))
            {
                _released = true;
                _offset = _target.Point - pose.Point(time);
                transform.position = _target.Point;
            }
            _pose = pose;
        }

        void Release(ShotPose pose, double time)
        {
            _released = true;
            Vector2 point = _stop + _normal * 0.02f;
            _offset = point - pose.Point(time);
            transform.position = point;
            _normal = Vector2.zero;
            _contact = null;
        }
        Vector2 StopAtCaster(Vector2 point)
        {
            if (_caster == null) return point;
            Vector2 target = _caster.View.position;
            Vector2 before = transform.position;
            Vector2 travel = point - before;
            if (!_caught && travel.sqrMagnitude > 0.000001f)
            {
                float along = Mathf.Clamp01(Vector2.Dot(target - before, travel) / travel.sqrMagnitude);
                _caught = Vector2.Distance(before + travel * along, target) < 0.35f;
                if (_caught) _caughtAt = _clock.Time;
            }
            return _caught ? target : point;
        }

        public void Complete(Vector2 point, float radius, bool hit)
        {
            if (IsServer || _ending || !_ready) return;
            _ending = true;
            ShotTail.Create(_caster.Cast, _action, _part, _sprite, _feedback, _pose.Velocity, point, radius, hit);
            _sprite.enabled = false;
        }

        void LateUpdate()
        {
            if (!IsSpawned || _ending) return;
            if (IsServer)
            {
                double time = NetGame.Current.PhysicsTime;
                if (time - _sentAt < 0.019d) return;
                _sentAt = time;
                _pose = Capture(time);
                NetGame.Current.Shots.Send(NetworkObjectId, _pose);
                return;
            }
            if (!_ready || _held) return;
            _released = false;
            double previous = _clock.Time;
            double now = _clock.Step(Now, Time.deltaTime);
            while (_pending.Count > 0 && _pending.Peek().Time <= now) Accept(_pending.Dequeue(), now, previous);
            if (_normal.sqrMagnitude > 0f && !ShotQuery.Touches(_contact, _stop, _radius, _size)) Release(_pose, now);
            float age = Mathf.Clamp((float)(now - _pose.Time), 0f, 1f);
            float elapsed = _released ? 0f : age - Mathf.Clamp((float)(previous - _pose.Time), 0f, 1f);
            float speed = (_pose.Velocity + _pose.Gravity * age).magnitude;
            _offset = Vector2.MoveTowards(_offset, Vector2.zero, speed * 0.25f * elapsed);
            _angleOffset = Mathf.MoveTowards(_angleOffset, 0f, 180f * elapsed);
            Vector2 point = _pose.Point(now) + _offset;
            if (_target.Blocked) point = _target.Point;
            else if (_normal.sqrMagnitude > 0f) point = _stop;
            else
            {
                if (_pose.Terrain && ShotQuery.Ground(transform.position, point, _radius, _size, _ground, out RaycastHit2D hit))
                {
                    _normal = hit.normal;
                    _contact = hit.collider;
                    _blockedAt = now;
                    point = _stop = hit.centroid;
                }
                if (_target.TryBlock(transform.position, point, _radius, _size, (float)(now - _seed.Value.Time), speed))
                {
                    point = _target.Point;
                    _normal = Vector2.zero;
                    _contact = null;
                }
            }
            if (!_pose.Terrain && !_target.Blocked) point = StopAtCaster(point);
            transform.SetPositionAndRotation(point, Quaternion.Euler(0f, 0f, Angle(_pose, now) + _angleOffset));
        }
    }
}

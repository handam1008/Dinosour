using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(100)]
    public sealed class MotionView : NetworkBehaviour
    {
        struct InputFrame
        {
            public uint Sequence;
            public double Time;
            public Vector2 Move;
        }

        struct JumpFrame
        {
            public uint Sequence;
            public double Time;
        }

        [SerializeField] NetPlayer _player;
        [SerializeField] PlayerController _motion;
        [SerializeField] Rigidbody2D _body;
        [SerializeField] CapsuleCollider2D _shape;
        [SerializeField] Transform _view;
        [SerializeField] Transform[] _parts;
        [SerializeField] DinosaurVisualController _animation;
        readonly List<InputFrame> _inputs = new List<InputFrame>(64);
        readonly List<JumpFrame> _jumps = new List<JumpFrame>(8);
        MotionCast _cast;
        Vector2 _position;
        Vector2 _velocity;
        Vector2 _move;
        Vector2 _offset;
        float _speed;
        float _external;
        double _dropUntil;
        double _nextState;
        double _predictedAt;
        double _lastStateAt;
        bool _active;
        uint _sentState;
        uint _receivedState;

        public Transform View => _view;
        public Vector2 Position => IsOwner && !IsServer && _active ? (Vector2)_view.position : _body.position;

        void Awake()
        {
            foreach (Transform part in _parts) part.SetParent(_view, true);
        }

        public override void OnNetworkSpawn()
        {
            _cast = new MotionCast(_shape, _motion.GroundMask);
            _position = _body.position;
            _speed = _motion.MoveSpeed;
        }

        public void Move(Vector2 value) => _move = value;

        public void Record(uint sequence)
        {
            if (IsServer) return;
            _inputs.Add(new InputFrame { Sequence = sequence, Time = Time.unscaledTimeAsDouble, Move = _move });
            if (_inputs.Count > 120) _inputs.RemoveAt(0);
        }

        public void Jump(uint sequence)
        {
            if (IsServer || !_active) return;
            _jumps.Add(new JumpFrame { Sequence = sequence, Time = Time.unscaledTimeAsDouble });
            if (_jumps.Count > 32) _jumps.RemoveAt(0);
            ApplyJump(Time.unscaledTimeAsDouble, _move);
        }

        void LateUpdate()
        {
            if (!IsSpawned) return;
            if (IsServer && !IsOwner && Time.unscaledTimeAsDouble >= _nextState)
            {
                _nextState = Time.unscaledTimeAsDouble + 1d / 30d;
                StateRpc(++_sentState, _player.InputSequence, _player.JumpSequence, _body.position, _body.linearVelocity,
                    _motion.MoveSpeed, _player.CanAct);
            }
            if (!IsOwner || IsServer) return;
            if (!_player.CanAct)
            {
                _active = false;
                _view.localPosition = Vector3.zero;
                _offset = Vector2.zero;
                _inputs.Clear();
                _jumps.Clear();
                _animation.ClearMotionView();
                return;
            }
            if (!_active)
            {
                _active = true;
                _position = _body.position;
                _velocity = Vector2.zero;
                _speed = _motion.MoveSpeed;
                _predictedAt = _lastStateAt = Time.unscaledTimeAsDouble;
            }
            double now = Time.unscaledTimeAsDouble;
            if (now - _lastStateAt > 0.75d)
            {
                _position = _body.position;
                _velocity = _offset = Vector2.zero;
                _predictedAt = now;
                _view.localPosition = Vector3.zero;
                _animation.ClearMotionView();
                return;
            }
            Advance(_move, Mathf.Clamp((float)(now - _predictedAt), 0f, 0.1f), now);
            _predictedAt = now;
            _offset *= Mathf.Exp(-18f * Time.unscaledDeltaTime);
            _view.position = new Vector3(_position.x + _offset.x, _position.y + _offset.y, transform.position.z);
            _animation.SetMotionView(_velocity, _cast.Grounded(_position, Time.unscaledTimeAsDouble < _dropUntil));
        }

        void Advance(Vector2 move, float duration, double endTime)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(duration / 0.01f));
            float delta = duration / steps;
            for (int i = 0; i < steps; i++)
            {
                float decay = _motion.KnockbackDecay;
                if (Mathf.Abs(move.x) > 0.01f && Mathf.Sign(move.x) != Mathf.Sign(_external)) decay += _motion.CounterBrake;
                _external = Mathf.MoveTowards(_external, 0f, decay * delta);
                _velocity.x = move.x * _speed + _external;
                _velocity.y += Physics2D.gravity.y * _body.gravityScale * delta;
                _cast.Move(ref _position, ref _velocity, delta, endTime - duration + (i + 1) * delta < _dropUntil);
            }
        }

        void ApplyJump(double time, Vector2 move)
        {
            if (!_cast.Grounded(_position, time < _dropUntil)) return;
            if (move.y < -0.5f && _cast.Platform(_position))
            {
                _dropUntil = time + 0.5d;
                _velocity.y = -2f;
            }
            else _velocity.y = _motion.JumpSpeed;
        }

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server, Delivery = RpcDelivery.Unreliable)]
        void StateRpc(uint state, uint sequence, uint jump, Vector2 position, Vector2 velocity, float speed, bool playing)
        {
            if (IsServer || !playing || !_active || state <= _receivedState) return;
            _receivedState = state;
            int start = _inputs.FindIndex(frame => frame.Sequence == sequence);
            if (start < 0) return;
            double now = Time.unscaledTimeAsDouble;
            if (now - _inputs[start].Time > 0.6d) return;
            _lastStateAt = _predictedAt = now;
            Vector2 previous = _position;
            _position = position;
            _velocity = velocity;
            _speed = speed;
            _external = velocity.x - _inputs[start].Move.x * speed;
            _jumps.RemoveAll(frame => frame.Sequence <= jump);
            int pending = 0;
            for (int i = start; i < _inputs.Count; i++)
            {
                double time = _inputs[i].Time;
                double end = i + 1 < _inputs.Count ? _inputs[i + 1].Time : now;
                while (pending < _jumps.Count && _jumps[pending].Time <= end)
                {
                    double at = System.Math.Max(time, _jumps[pending].Time);
                    Advance(_inputs[i].Move, (float)(at - time), at);
                    ApplyJump(at, _inputs[i].Move);
                    time = at;
                    pending++;
                }
                Advance(_inputs[i].Move, (float)(end - time), end);
            }
            Vector2 correction = previous - _position;
            _offset = correction.sqrMagnitude < 4f ? Vector2.ClampMagnitude(_offset + correction, 1f) : Vector2.zero;
            if (start > 0) _inputs.RemoveRange(0, start);
        }

        public override void OnNetworkDespawn()
        {
            _animation.ClearMotionView();
            _view.localPosition = Vector3.zero;
        }
    }
}

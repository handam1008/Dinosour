using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(100)]
    public sealed class MotionView : NetworkBehaviour, IForceReceiver, IMotionSource
    {
        struct Snapshot
        {
            public double Time;
            public MotionState State;
        }

        [SerializeField] NetPlayer _player;
        [SerializeField] PlayerController _motion;
        [SerializeField] Rigidbody2D _body;
        [SerializeField] CapsuleCollider2D _shape;
        [SerializeField] Transform _view;
        [SerializeField] Transform[] _parts;
        [SerializeField] DinosaurVisualController _animation;
        readonly MotionHistory _history = new MotionHistory();
        readonly List<Snapshot> _snapshots = new List<Snapshot>(32);
        MotionMotor _motor;
        MotionState _state;
        MotionFrame _last;
        Vector2 _previous;
        Vector2 _offset;
        float _speed;
        MotionRate _rate;
        double _lastInputAt;
        double _lastStateAt;
        double _lastAckAt;
        double _startAt;
        bool _started;
        uint _tick;
        uint _processed;
        uint _receivedInput;
        uint _sentState;
        uint _receivedState;

        public Transform View => _view;
        public Vector2 Position => _body.position;
        public Vector2 Velocity => _state.Velocity;
        public uint Epoch => _state.Epoch;
        public uint Processed => _processed;
        public uint JumpSequence => _state.Jump;
        public uint Tick => _tick;
        public float Correction { get; private set; }
        public float MaxCorrection { get; private set; }
        public bool Grounded => _state.Grounded;
        public float Silence => (float)(Time.unscaledTimeAsDouble - _lastStateAt);
        public float AckSilence => (float)(Time.unscaledTimeAsDouble - _lastAckAt);
        public uint Buffered => IsServer && _receivedInput > _processed ? _receivedInput - _processed : 0;
        public int Resyncs { get; private set; }
        public float Speed => _speed;

        void Awake()
        {
            foreach (Transform part in _parts) part.SetParent(_view, true);
        }

        public override void OnNetworkSpawn()
        {
            _motor = new MotionMotor(_shape, _motion, Physics2D.gravity.y * _body.gravityScale);
            _state.Position = _previous = _body.position;
            _view.position = transform.position;
            _rate = _motion.Rate;
            _speed = _rate.Value;
            _lastStateAt = _lastAckAt = Time.unscaledTimeAsDouble;
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.useFullKinematicContacts = true;
            _body.interpolation = RigidbodyInterpolation2D.None;
            _body.linearVelocity = Vector2.zero;
        }

        void FixedUpdate()
        {
            if (!IsSpawned || (!IsOwner && !IsServer)) return;
            _previous = _state.Position;
            if (IsOwner)
            {
                MotionFrame input = _player.ReadInput(++_tick);
                _history.Store(input);
                if (IsServer)
                {
                    _rate = _motion.Rate;
                    _speed = _rate.Value;
                    _player.ApplyInput(input);
                    _motor.Step(ref _state, input, _speed, Time.fixedDeltaTime, _player.CanAct);
                    _processed = input.Tick;
                }
                else
                {
                    InputRpc(new MotionPacket(_history, input.Tick, _state.Epoch));
                    Predict(input, _player.CanAct && Silence < 0.75f && AckSilence < 0.75f);
                }
            }
            else if (_started && Time.unscaledTimeAsDouble >= _startAt)
            {
                StepRemote();
            }
            else if (IsServer)
            {
                StepIdle();
            }
            Commit();
            if (IsServer)
                StateRpc(++_sentState, _processed, _state, _rate, _player.CanAct, NetworkManager.ServerTime.Time);
        }

        void Predict(MotionFrame input, bool playing)
        {
            _rate.Advance(Time.fixedDeltaTime);
            _speed = _rate.Value;
            _motor.Step(ref _state, input, _speed, Time.fixedDeltaTime, playing);
        }

        void StepRemote()
        {
            uint pending = Buffered;
            if (pending == 0)
            {
                StepIdle();
                return;
            }
            uint tick = pending > 3 ? _receivedInput - 2 : _processed + 1;
            MotionFrame input = _history.TryGet(tick, out MotionFrame received) ? received : _last;
            input.Tick = tick;
            _last = input;
            _rate = _motion.Rate;
            _speed = _rate.Value;
            _player.ApplyInput(input);
            _motor.Step(ref _state, input, _speed, Time.fixedDeltaTime, _player.CanAct);
            _processed = tick;
        }

        void StepIdle()
        {
            _rate = _motion.Rate;
            _speed = _rate.Value;
            MotionFrame input = _last;
            if (Time.unscaledTimeAsDouble - _lastInputAt > 0.12d) input.Move = Vector2.zero;
            input.Jump = _state.Jump;
            _motor.Step(ref _state, input, _speed, Time.fixedDeltaTime, _player.CanAct);
        }

        void Commit()
        {
            _body.position = _state.Position;
            _body.linearVelocity = Vector2.zero;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
        void InputRpc(MotionPacket packet)
        {
            if (packet.Epoch != _state.Epoch) return;
            MotionFrame newest = packet[0];
            if (!newest.Valid || newest.Tick <= _processed) return;
            bool overflow = newest.Tick - _processed >= MotionHistory.Capacity;
            if (overflow && _started && Time.unscaledTimeAsDouble - _lastInputAt < 0.75d) return;
            if (!_started || overflow)
            {
                uint first = newest.Tick;
                for (int i = 1; i < 6; i++)
                    if (packet[i].Valid && packet[i].Tick + i == newest.Tick) first = packet[i].Tick;
                if (_started) Resyncs++;
                _history.Clear();
                _processed = first - 1;
                _startAt = Time.unscaledTimeAsDouble + Time.fixedDeltaTime * 2d;
                _started = true;
            }
            if (newest.Tick > _receivedInput)
            {
                _receivedInput = newest.Tick;
                _lastInputAt = Time.unscaledTimeAsDouble;
            }
            for (int i = 0; i < 6; i++)
            {
                MotionFrame frame = packet[i];
                if (frame.Valid && frame.Tick + i == newest.Tick && frame.Tick > _processed)
                    _history.Store(frame);
            }
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server, Delivery = RpcDelivery.Unreliable)]
        void StateRpc(uint serial, uint processed, MotionState state, MotionRate rate, bool playing, double time)
        {
            if (serial <= _receivedState) return;
            _receivedState = serial;
            _lastStateAt = Time.unscaledTimeAsDouble;
            if (processed > _processed) _lastAckAt = Time.unscaledTimeAsDouble;
            _processed = processed;
            _rate = rate;
            _speed = rate.Value;
            bool relocated = state.Epoch != _state.Epoch;
            if (!IsOwner)
            {
                if (relocated)
                {
                    _snapshots.Clear();
                    _state = state;
                    Commit();
                    _view.localPosition = Vector3.zero;
                }
                _snapshots.Add(new Snapshot { Time = time, State = state });
                if (_snapshots.Count > 32) _snapshots.RemoveAt(0);
                return;
            }

            Vector2 before = _state.Position;
            _state = state;
            if (relocated)
            {
                _player.ResetJump(state.Jump);
                _history.Clear();
                _tick = processed;
                _offset = Vector2.zero;
                _lastAckAt = Time.unscaledTimeAsDouble;
            }
            if (processed > _tick || _tick - processed >= MotionHistory.Capacity)
            {
                _tick = processed;
                _history.Clear();
            }
            for (uint tick = processed + 1; tick <= _tick; tick++)
            {
                if (!_history.TryGet(tick, out MotionFrame input))
                {
                    _state = state;
                    _tick = processed;
                    _history.Clear();
                    break;
                }
                Predict(input, playing && _player.CanAct && AckSilence < 0.75f);
            }
            Vector2 correction = before - _state.Position;
            Correction = correction.magnitude;
            MaxCorrection = Mathf.Max(MaxCorrection, Correction);
            _previous -= correction;
            _offset = playing && !relocated && Correction < 0.5f ? Vector2.ClampMagnitude(_offset + correction, 0.35f) : Vector2.zero;
            if (!playing || relocated || Correction >= 0.5f) _previous = _state.Position;
            Commit();
            if (relocated || !playing) _view.position = transform.position;
        }

        void LateUpdate()
        {
            if (!IsSpawned) return;
            if (!IsServer && !IsOwner)
            {
                Interpolate();
                _view.localPosition = Vector3.zero;
            }
            else
            {
                float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
                _offset *= Mathf.Exp(-24f * Time.unscaledDeltaTime);
                Vector2 point = Vector2.Lerp(_previous, _state.Position, alpha) + _offset;
                _view.position = new Vector3(point.x, point.y, transform.position.z);
            }
            _animation.SetMotionView(_state.Velocity, _state.Grounded);
        }

        void Interpolate()
        {
            if (_snapshots.Count == 0) return;
            double time = NetworkManager.ServerTime.Time - 0.04d;
            while (_snapshots.Count > 2 && _snapshots[1].Time <= time) _snapshots.RemoveAt(0);
            Snapshot first = _snapshots[0];
            Snapshot last = _snapshots.Count > 1 ? _snapshots[1] : first;
            float alpha = last.Time > first.Time ? Mathf.Clamp01((float)((time - first.Time) / (last.Time - first.Time))) : 1f;
            _state = last.State;
            _state.Position = Vector2.Lerp(first.State.Position, last.State.Position, alpha);
            _state.Velocity = Vector2.Lerp(first.State.Velocity, last.State.Velocity, alpha);
            Commit();
        }

        public void ApplyForce(Vector2 force, ForceMode2D mode)
        {
            if (!IsServer || !_player.CanAct || !NetMath.Finite(force)) return;
            float scale = (mode == ForceMode2D.Impulse ? 1f : Time.fixedDeltaTime) / Mathf.Max(0.0001f, _body.mass);
            if (Mathf.Abs(force.x) > 0.0001f) _state.External = force.x * scale;
            _state.Velocity.y += force.y * scale;
        }

        public void Teleport(Vector2 position)
        {
            if (!IsServer || !NetMath.Finite(position)) return;
            _state = new MotionState { Epoch = _state.Epoch + 1, Position = position, Jump = _state.Jump };
            if (IsOwner) _player.ResetJump(_state.Jump);
            _previous = position;
            _offset = Vector2.zero;
            _processed = _receivedInput;
            _history.Clear();
            _snapshots.Clear();
            _last = default;
            _started = false;
            Commit();
            _view.position = transform.position;
        }

        public void ClearMetrics() => Correction = MaxCorrection = 0f;

        public override void OnNetworkDespawn()
        {
            _animation.ClearMotionView();
            _view.localPosition = Vector3.zero;
            _history.Clear();
            _snapshots.Clear();
        }
    }
}

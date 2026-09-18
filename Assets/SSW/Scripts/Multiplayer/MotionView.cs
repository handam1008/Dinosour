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

        struct CastSample
        {
            public uint Tick;
            public uint Epoch;
            public Vector2 Position;
        }

        readonly CastSample[] _positions = new CastSample[256];
        [SerializeField] NetPlayer _player;
        [SerializeField] PlayerController _motion;
        [SerializeField] Rigidbody2D _body;
        [SerializeField] CapsuleCollider2D _shape;
        [SerializeField] Transform _view;
        [SerializeField] Transform[] _parts;
        [SerializeField] DinosaurVisualController _animation;
        readonly HitTrack _hits = new HitTrack();
        readonly MotionHistory _history = new MotionHistory();
        readonly List<Snapshot> _snapshots = new List<Snapshot>(32);
        MotionMotor _motor;
        MotionState _state;
        MotionFrame _last;
        Vector2 _previous;
        Vector2 _offset;
        Vector2 _shown;
        float _speed;
        float _inputCredit;
        MotionRate _rate;
        double _lastInputAt;
        double _lastStateAt;
        double _lastAckAt;
        double _startAt;
        ViewClock _clock;
        double _viewTime;
        bool _started;
        uint _tick;
        uint _processed;
        uint _receivedInput;
        uint _sentState;
        uint _receivedState;

        public double ViewTime => _viewTime;
        public bool ReadHit(double time, out Vector2 position) => _hits.Read(time, _state.Epoch, out position);
        public Vector2 HitOffset => transform.TransformVector(_shape.offset);
        public Vector2 HitAxis => _shape.direction == CapsuleDirection2D.Vertical ? transform.up : transform.right;
        public Vector2 HitSize => Vector2.Scale(_shape.size, new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y)));
        public bool Vertical => _shape.direction == CapsuleDirection2D.Vertical;
        public MotionPacket Packet => new MotionPacket(_history, _tick, _state.Epoch);

        public bool ReadPosition(uint epoch, uint tick, out Vector2 position)
        {
            CastSample sample = _positions[tick % _positions.Length];
            position = sample.Position;
            return sample.Epoch == epoch && sample.Tick == tick;
        }

        public Transform View => _view;
        public Vector2 Position => _body.position;
        public Vector2 Velocity => _state.Velocity;
        public uint Epoch => _state.Epoch;
        public uint Processed => _processed;
        public uint JumpSequence => _state.Jump;
        public uint Tick => _tick;
        public float InputDelay => IsOwner && !IsServer ? Mathf.Max(0f, (long)_tick - _processed) * Time.fixedDeltaTime : 0f;
        public float Correction { get; private set; }
        public float MaxCorrection { get; private set; }
        public bool Grounded => _state.Grounded;
        public float Silence => (float)(Time.realtimeSinceStartupAsDouble - _lastStateAt);
        public float AckSilence => (float)(Time.realtimeSinceStartupAsDouble - _lastAckAt);
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
            _shown = _view.position = transform.position;
            _rate = _motion.Rate;
            _speed = _rate.Value;
            _lastStateAt = _lastAckAt = Time.realtimeSinceStartupAsDouble;
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.useFullKinematicContacts = true;
            _body.interpolation = RigidbodyInterpolation2D.None;
            _body.linearVelocity = Vector2.zero;
        }

        void FixedUpdate()
        {
            if (!IsSpawned || (!IsOwner && !IsServer)) return;
            _previous = _state.Position;
            if (IsServer && !IsOwner)
                _inputCredit = Mathf.Min(_inputCredit + Time.fixedDeltaTime, MotionHistory.Capacity * Time.fixedDeltaTime);
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
                    float wait = _player.ResponseTime;
                    Predict(input, _player.CanAct && Silence < wait && AckSilence < wait);
                }
            }
            else if (_started && Time.realtimeSinceStartupAsDouble >= _startAt)
            {
                StepRemote();
            }
            else if (IsServer)
            {
                StepIdle();
            }
            Commit();
            if (IsServer) _hits.Store(NetGame.Current.PhysicsTime, _state.Epoch, _state.Position);
            if (IsServer)
                StateRpc(++_sentState, _processed, _state, _rate, _player.CanAct, NetGame.Current.PhysicsTime);
        }

        void Predict(MotionFrame input, bool playing)
        {
            _rate.Advance(Time.fixedDeltaTime);
            _speed = _rate.Value;
            _motor.Step(ref _state, input, _speed, Time.fixedDeltaTime, playing);
        }

        void StepRemote()
        {
            int steps = _receivedInput > _processed + 3 ? 2 : 1;
            for (int i = 0; i < steps; i++)
            {
                if (_inputCredit + 0.000001f < Time.fixedDeltaTime) break;
                uint tick = _processed + 1;
                if (!_history.TryGet(tick, out MotionFrame input))
                {
                    if (_receivedInput < tick + 6) break;
                    input = _last;
                    input.Tick = tick;
                }
                _last = input;
                _rate = _motion.Rate;
                _speed = _rate.Value;
                _player.ApplyInput(input);
                _motor.Step(ref _state, input, _speed, Time.fixedDeltaTime, _player.CanAct);
                _processed = tick;
                _inputCredit = Mathf.Max(0f, _inputCredit - Time.fixedDeltaTime);
                Commit();
            }
        }

        void StepIdle()
        {
            _inputCredit = Mathf.Max(0f, _inputCredit - Time.fixedDeltaTime);
            _rate = _motion.Rate;
            _speed = _rate.Value;
            MotionFrame input = _last;
            if (Time.realtimeSinceStartupAsDouble - _lastInputAt > 0.12d) input.Move = Vector2.zero;
            input.Jump = _state.Jump;
            _motor.Step(ref _state, input, _speed, Time.fixedDeltaTime, _player.CanAct);
        }

        void Commit()
        {
            _body.position = _state.Position;
            _body.linearVelocity = Vector2.zero;
            if (IsServer)
                _positions[_processed % _positions.Length] = new CastSample
                {
                    Tick = _processed, Epoch = _state.Epoch, Position = _state.Position
                };
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
        void InputRpc(MotionPacket packet) => ReceiveInput(packet);

        public void ReceiveInput(MotionPacket packet)
        {
            if (packet.Epoch != _state.Epoch) return;
            MotionFrame newest = packet[0];
            if (!newest.Valid || newest.Tick <= _processed) return;
            bool overflow = newest.Tick - _processed >= MotionHistory.Capacity;
            if (overflow && _started && Time.realtimeSinceStartupAsDouble - _lastInputAt < 0.75d) return;
            if (!_started || overflow)
            {
                uint first = newest.Tick;
                for (int i = 1; i < 6; i++)
                    if (packet[i].Valid && packet[i].Tick + i == newest.Tick) first = packet[i].Tick;
                if (_started) Resyncs++;
                _history.Clear();
                _processed = first - 1;
                _inputCredit = Mathf.Max(_inputCredit, (newest.Tick - first + 1) * Time.fixedDeltaTime);
                _startAt = Time.realtimeSinceStartupAsDouble + Time.fixedDeltaTime * 2d;
                _started = true;
            }
            if (newest.Tick > _receivedInput)
            {
                _receivedInput = newest.Tick;
                _lastInputAt = Time.realtimeSinceStartupAsDouble;
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
            _lastStateAt = Time.realtimeSinceStartupAsDouble;
            if (processed > _processed) _lastAckAt = Time.realtimeSinceStartupAsDouble;
            _processed = processed;
            _rate = rate;
            _speed = rate.Value;
            bool relocated = state.Epoch != _state.Epoch;
            if (!IsOwner)
            {
                if (relocated)
                {
                    _snapshots.Clear();
                    _clock = default;
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
                _lastAckAt = Time.realtimeSinceStartupAsDouble;
            }
            if (processed > _tick || _tick - processed >= MotionHistory.Capacity)
            {
                _tick = processed;
                _history.Clear();
            }
            bool predict = playing && _player.CanAct && AckSilence < _player.ResponseTime;
            for (uint tick = processed + 1; tick <= _tick; tick++)
            {
                if (!_history.TryGet(tick, out MotionFrame input))
                {
                    _state = state;
                    _tick = processed;
                    _history.Clear();
                    break;
                }
                Predict(input, predict);
            }
            Vector2 correction = before - _state.Position;
            Correction = correction.magnitude;
            MaxCorrection = Mathf.Max(MaxCorrection, Correction);
            _previous -= correction;
            _offset = playing && !relocated ? _offset + correction : Vector2.zero;
            if (!playing || relocated) _previous = _state.Position;
            Commit();
            if (relocated || !playing) _shown = _view.position = transform.position;
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
                Vector2 point = Vector2.Lerp(_previous, _state.Position, alpha);
                Vector2 movement = point + _offset - _shown;
                _offset = Ease(_offset, _state.Velocity, _speed, Time.unscaledDeltaTime, movement);
                point += _offset;
                _shown = point;
                _view.position = new Vector3(point.x, point.y, transform.position.z);
                _viewTime = NetGame.Current.PhysicsTime - (1f - alpha) * Time.fixedDeltaTime;
            }
            _animation.SetMotionView(_state.Velocity, _state.Grounded);
        }

        static Vector2 Ease(Vector2 offset, Vector2 velocity, float speed, float delta, Vector2 movement)
        {
            Vector2 shift = Vector2.ClampMagnitude(-offset, Mathf.Max(8f, speed * 2f) * delta);
            if (shift.x * velocity.x < 0f)
                shift.x = Mathf.Sign(shift.x) * Mathf.Min(Mathf.Abs(shift.x), Mathf.Max(0f, movement.x * Mathf.Sign(velocity.x)) * 0.8f);
            if (shift.y * velocity.y < 0f)
                shift.y = Mathf.Sign(shift.y) * Mathf.Min(Mathf.Abs(shift.y), Mathf.Max(0f, movement.y * Mathf.Sign(velocity.y)) * 0.8f);
            return offset + shift;
        }

        void Interpolate()
        {
            if (_snapshots.Count == 0) return;
            double time = _clock.Step(NetworkManager.ServerTime.Time, Time.unscaledDeltaTime);
            while (_snapshots.Count > 2 && _snapshots[1].Time <= time) _snapshots.RemoveAt(0);
            Snapshot first = _snapshots[0];
            Snapshot last = _snapshots.Count > 1 ? _snapshots[1] : first;
            float alpha = last.Time > first.Time ? Mathf.Clamp01((float)((time - first.Time) / (last.Time - first.Time))) : 1f;
            _viewTime = first.Time + (last.Time - first.Time) * alpha;
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
            _processed = IsOwner ? _tick : _receivedInput;
            _history.Clear();
            _snapshots.Clear();
            _inputCredit = 0f;
            _last = default;
            _started = false;
            _clock = default;
            Commit();
            _shown = _view.position = transform.position;
            _hits.Store(NetGame.Current.PhysicsTime, _state.Epoch, _state.Position);
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

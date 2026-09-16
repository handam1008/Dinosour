using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class NetPlayer : NetworkBehaviour, IPlayerDrive
    {
        [SerializeField] PlayerController _motion;
        [SerializeField] PlayerInput _input;
        [SerializeField] PlayerIdentity _identity;
        [SerializeField] Health _health;
        [SerializeField] Transform _visual;
        [SerializeField] DinosaurVisualController _animation;
        [SerializeField] NetCast _cast;
        [SerializeField] NetDraft _draft;
        [SerializeField] Rigidbody2D _body;
        [SerializeField] Collider2D _collider;
        [SerializeField] MotionView _prediction;
        readonly NetworkVariable<Fighter> _info = new NetworkVariable<Fighter>();
        Fighter _startInfo;
        readonly NetworkVariable<PlayerJob> _job = new NetworkVariable<PlayerJob>();
        readonly NetworkVariable<int> _side = new NetworkVariable<int>(1);
        readonly NetworkVariable<bool> _left = new NetworkVariable<bool>();
        Vector2 _move;
        Vector2 _aim = Vector2.right;
        float _lastInput;
        float _nextInput;
        uint _sentInput;
        uint _receivedInput;
        uint _sentJump;
        uint _receivedJump;
        bool _blocked;
        PlayerJob _startJob;
        int _startSide;
        InputAction _attack;
        InputAction _cycle;

        public Fighter Info => _info.Value;
        public Transform View => _prediction.View;
        public Vector2 ViewPosition => _prediction.Position;
        public uint InputSequence => _receivedInput;
        public uint JumpSequence => _receivedJump;
        public Health Health => _health;
        public NetCast Cast => _cast;
        public NetDraft Draft => _draft;
        public PlayerJob Job => _job.Value;
        public Collider2D Collider => _collider;
        public Rigidbody2D Body => _body;
        public Vector2 Aim => _aim;
        public int Side => _side.Value;
        public bool CanAct => IsSpawned && _health.Current > 0f && NetGame.Current.CanFight;

        public void Init(PlayerJob job, int side, Fighter info = default)
        {
            _startInfo = info;
            _startJob = job;
            _startSide = side;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _info.Value = _startInfo;
                _job.Value = _startJob;
                _side.Value = _startSide;
                _left.Value = _startSide < 0;
            }
            _identity.SetJob(_job.Value);
            _aim = new Vector2(Side, 0f);
            _motion.Bind(this, IsServer);
            _input.enabled = IsOwner;
            if (IsOwner)
            {
                _attack = _input.actions["Attack"];
                _cycle = _input.actions["CycleSuit"];
                _attack.performed += Attack;
                _attack.canceled += Attack;
                _cycle.performed += Cycle;
                _cycle.canceled += Cycle;
            }
            _animation.enabled = IsServer || IsOwner;
            _left.OnValueChanged += Face;
            Face(false, _left.Value);
            _health.OnDied += Die;
            NetGame.Current.Register(this);
        }

        void Update()
        {
            if (!IsSpawned) return;
            _motion.Simulated = IsServer && CanAct;
            if (IsServer && (!CanAct || Time.unscaledTime - _lastInput > 0.5f))
                _motion.Move(Vector2.zero);

            if (!IsOwner) return;
            if (Mouse.current != null && NetGame.Current.Arena != null)
            {
                Vector3 cursor = NetGame.Current.Arena.View.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Vector2 aim = (Vector2)cursor - ViewPosition;
                if (aim.sqrMagnitude > 0.001f) _aim = aim.normalized;
            }
            Vector2 move = new Vector2(_move.x * Side, _move.y);
            if (_blocked || !_input.inputIsActive || !CanAct) move = Vector2.zero;
            _prediction.Move(move);
            if (IsServer) _motion.Move(move);
            Face(false, _aim.x < 0f);
            if (Time.unscaledTime < _nextInput) return;
            _nextInput = Time.unscaledTime + 1f / 30f;
            _prediction.Record(++_sentInput);
            InputRpc(move, _aim, _sentInput);
        }

        void Attack(InputAction.CallbackContext context)
        {
            if (_blocked || !_input.inputIsActive) return;
            _cast.Attack(context.ReadValueAsButton(), _aim);
        }

        void Cycle(InputAction.CallbackContext context)
        {
            if (_blocked || !_input.inputIsActive) return;
            _cast.Cycle(context.ReadValueAsButton());
        }

        public void Move(Vector2 value)
        {
            _move = value;
            _nextInput = 0f;
            if (IsOwner) _prediction.Move(CanAct && !_blocked && _input.inputIsActive ? new Vector2(value.x * Side, value.y) : Vector2.zero);
        }

        public void Jump()
        {
            if (!IsOwner || _blocked || !_input.inputIsActive || !CanAct) return;
            _prediction.Jump(++_sentJump);
            JumpRpc(_sentJump);
        }

        public void Block(bool value)
        {
            _blocked = value;
            _move = Vector2.zero;
            if (IsOwner)
            {
                if (value)
                {
                    _cast.Cancel();
                    _input.DeactivateInput();
                }
                else _input.ActivateInput();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
        void InputRpc(Vector2 move, Vector2 aim, uint sequence)
        {
            if (sequence <= _receivedInput || !NetMath.Finite(move) || !NetMath.Finite(aim)) return;
            _receivedInput = sequence;
            _lastInput = Time.unscaledTime;
            _aim = aim.sqrMagnitude > 0.001f ? aim.normalized : _aim;
            _left.Value = _aim.x < 0f;
            _motion.Move(CanAct ? Vector2.ClampMagnitude(move, 1f) : Vector2.zero);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void JumpRpc(uint sequence)
        {
            if (sequence <= _receivedJump) return;
            _receivedJump = sequence;
            if (CanAct) _motion.Jump();
        }

        void Face(bool previous, bool left)
        {
            Vector3 scale = _visual.localScale;
            scale.x = Mathf.Abs(scale.x) * ((IsOwner ? _aim.x < 0f : left) ? -1f : 1f);
            _visual.localScale = scale;
        }

        void Die()
        {
            if (IsServer) NetGame.Current.Match.CheckDeath();
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                _attack.performed -= Attack;
                _attack.canceled -= Attack;
                _cycle.performed -= Cycle;
                _cycle.canceled -= Cycle;
            }
            _left.OnValueChanged -= Face;
            _health.OnDied -= Die;
            if (NetGame.Current != null) NetGame.Current.Unregister(this);
        }
    }
}

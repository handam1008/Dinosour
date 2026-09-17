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
        uint _sentJump;
        bool _blocked;
        PlayerJob _startJob;
        int _startSide;
        InputAction _attack;
        InputAction _cycle;

        public Fighter Info => _info.Value;
        public Transform View => _prediction.View;
        public Vector2 ViewPosition => _prediction.Position;
        public uint InputSequence => _prediction.Processed;
        public uint JumpSequence => _prediction.JumpSequence;
        public Health Health => _health;
        public NetCast Cast => _cast;
        public NetDraft Draft => _draft;
        public PlayerJob Job => _job.Value;
        public Collider2D Collider => _collider;
        public Rigidbody2D Body => _body;
        public LayerMask GroundMask => _motion.GroundMask;
        public uint Epoch => _prediction.Epoch;
        public Vector2 Velocity => _prediction.Velocity;
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
            _motion.Bind(this, false, _prediction);
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

            if (!IsOwner) return;
            if (Mouse.current != null && NetGame.Current.Arena != null)
            {
                Vector3 cursor = NetGame.Current.Arena.View.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Vector2 aim = (Vector2)cursor - ViewPosition;
                if (aim.sqrMagnitude > 0.001f) _aim = aim.normalized;
            }
            Face(false, _aim.x < 0f);
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
        }

        public void Jump()
        {
            if (!IsOwner || _blocked || !_input.inputIsActive || !CanAct) return;
            _sentJump++;
        }

        internal void ResetJump(uint jump) => _sentJump = jump;

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

        public MotionFrame ReadInput(uint tick)
        {
            Vector2 move = new Vector2(_move.x * Side, _move.y);
            if (_blocked || !_input.inputIsActive || !CanAct) move = Vector2.zero;
            return new MotionFrame
            {
                Tick = tick, Jump = _sentJump, Move = Vector2.ClampMagnitude(move, 1f), Aim = _aim
            };
        }

        public void ApplyInput(MotionFrame input)
        {
            if (!IsServer) return;
            if (input.Aim.sqrMagnitude > 0.001f) _aim = input.Aim.normalized;
            _left.Value = _aim.x < 0f;
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

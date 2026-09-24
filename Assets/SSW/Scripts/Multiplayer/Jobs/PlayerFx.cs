using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(1100)]
    public sealed class PlayerFx : NetworkBehaviour, IIncomingDamageModifier
    {
        [SerializeField] NetPlayer _player;
        [SerializeField] SpriteRenderer[] _sprites;
        [SerializeField] UnityEngine.UI.RawImage _blind;
        readonly NetworkVariable<double> _hide = new NetworkVariable<double>();
        readonly NetworkVariable<double> _immune = new NetworkVariable<double>();
        readonly NetworkVariable<double> _dark = new NetworkVariable<double>();
        Vector3 _baseScale;
        Material _mask;
        float[] _alpha;
        public int Priority => -200;
        public bool Hidden => IsSpawned && NetworkManager.ServerTime.Time < _hide.Value;
        public bool Shrunk => IsSpawned && _player.Drive.Scale < 1f;
        public bool BlindActive => IsSpawned && Now < _dark.Value;
        public bool ImmuneActive => IsSpawned && Now < _immune.Value;
        double Now => NetworkManager.ServerTime.Time;

        void Awake()
        {
            _baseScale = transform.localScale;
            _alpha = new float[_sprites.Length];
            for (int i = 0; i < _sprites.Length; i++) _alpha[i] = _sprites[i].color.a;
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _mask = new Material(_blind.material);
                _blind.material = _mask;
            }
        }

        public void Shrink(float duration) => _player.Drive.Shrink(duration);
        public void Hide(float duration) { if (IsServer) _hide.Value = Now + duration; }
        public void Immune(float duration) { if (IsServer) _immune.Value = Now + duration; }
        public void Blind(float duration) { if (IsServer) _dark.Value = Now + duration; }

        public float ModifyIncomingDamage(DamageRequest request, float amount) =>
            IsSpawned && Now < _immune.Value && !request.HasTag(DamageTag.IgnoreDefense) ? 0f : amount;

        void LateUpdate()
        {
            if (!IsSpawned) return;
            bool playing = _player.CanAct;
            float alpha = playing && Hidden ? IsOwner ? 0.4f : 0f : 1f;
            for (int i = 0; i < _sprites.Length; i++)
            {
                Color color = _sprites[i].color;
                color.a = _alpha[i] * alpha;
                _sprites[i].color = color;
            }
            _blind.enabled = IsOwner && playing && Now < _dark.Value;
            if (!_blind.enabled) return;
            Camera camera = NetGame.Current.Arena.View;
            Vector3 point = camera.WorldToViewportPoint(_player.View.position);
            _mask.SetVector("_Center", new Vector4(point.x, point.y, camera.aspect, 0f));
        }

        public override void OnNetworkDespawn()
        {
            transform.localScale = _baseScale;
            _blind.enabled = false;
            if (_mask != null) Destroy(_mask);
        }
    }
}

using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public enum BuffEffect : byte
    {
        Guard, HealCharge, HealBurst, IceBlast, Freeze, NuclearCharge, NuclearBlast,
        Slam, Mine, Bomb, Explosion, Revive
    }

    public sealed class BuffFx : NetworkBehaviour
    {
        [Serializable] struct Entry
        {
            public BuffEffect Kind;
            public GameObject Prefab;
        }

        sealed class AreaView
        {
            public GameObject Object;
            public Vector2 Target;
            public SpriteRenderer Sprite;
        }

        [SerializeField] NetPlayer _player;
        [SerializeField] NetBuff _buffs;
        [SerializeField] BuffGuard _guard;
        [SerializeField] DinosaurVisualController _animation;
        [SerializeField] Entry[] _effects;
        [SerializeField] GameObject[] _auraPrefabs;
        [SerializeField] LineRenderer _tetherPrefab;
        [SerializeField] float _swiftRange = 30f;
        readonly Dictionary<int, AreaView> _areas = new Dictionary<int, AreaView>();
        readonly HashSet<int> _closed = new HashSet<int>();
        readonly Queue<int> _ends = new Queue<int>();
        readonly List<GameObject> _transients = new List<GameObject>();
        readonly GameObject[] _auras = new GameObject[3];
        GameObject _barrier;
        LineRenderer _tether;
        static readonly CommonAugmentType[] AuraTypes = { CommonAugmentType.SlowAura, CommonAugmentType.Magnet, CommonAugmentType.NomNom };

        public int AreaCount => _areas.Count;

        public override void OnNetworkSpawn()
        {
            _barrier = Create(BuffEffect.Guard, _player.View.position, 1f);
            _barrier.transform.SetParent(_player.View, true);
            _barrier.SetActive(false);
            _tether = Instantiate(_tetherPrefab, _player.View);
            _tether.enabled = false;
        }

        GameObject Create(BuffEffect kind, Vector2 point, float size)
        {
            foreach (Entry entry in _effects)
            {
                if (entry.Kind != kind) continue;
                GameObject effect = Instantiate(entry.Prefab, point, Quaternion.identity);
                effect.transform.localScale *= size;
                effect.SetActive(true);
                return effect;
            }
            throw new InvalidOperationException("Missing common effect: " + kind);
        }

        public void Play(BuffEffect kind, Vector2 point, float size = 1f, float duration = 1f)
        {
            if (IsSpawned && IsServer) PlayRpc(kind, point, size, duration);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        void PlayRpc(BuffEffect kind, Vector2 point, float size, float duration)
        {
            if (kind == BuffEffect.Revive) { _animation.PlayRevive(); return; }
            GameObject effect = Create(kind, point, kind == BuffEffect.HealCharge || kind == BuffEffect.NuclearBlast || kind == BuffEffect.IceBlast || kind == BuffEffect.Explosion ? 1f : size);
            _transients.RemoveAll(item => item == null);
            _transients.Add(effect);
            if (kind == BuffEffect.HealCharge) effect.GetComponentInChildren<HealDial>().Play(duration, size);
            if (kind == BuffEffect.NuclearCharge)
            {
                effect.transform.SetParent(_player.View, true);
                effect.GetComponent<NuclearCharge>().Play(duration);
            }
            if (kind == BuffEffect.NuclearBlast) effect.GetComponent<NuclearBlast>().Play(size, _player.GroundMask);
            bool timed = kind == BuffEffect.HealCharge || kind == BuffEffect.NuclearCharge || kind == BuffEffect.Freeze;
            if (!timed)
            {
                foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = system.main;
                    duration = Mathf.Max(duration, main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
                }
            }
            Destroy(effect, Mathf.Max(0.05f, duration));
        }

        public void Revive() => Play(BuffEffect.Revive, _player.Drive.Position);

        public void Area(int id, BuffEffect kind, Vector2 point, float size = 1f, bool end = false, bool armed = false)
        {
            if (!IsSpawned || !IsServer) return;
            if (end) EndAreaRpc(id);
            else AreaRpc(id, kind, point, size, armed);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server, Delivery = RpcDelivery.Unreliable)]
        void AreaRpc(int id, BuffEffect kind, Vector2 point, float size, bool armed)
        {
            if (_closed.Contains(id)) return;
            if (!_areas.TryGetValue(id, out AreaView view))
            {
                view = new AreaView { Object = Create(kind, point, size) };
                if (kind == BuffEffect.Bomb) view.Sprite = view.Object.GetComponentInChildren<SpriteRenderer>();
                _areas.Add(id, view);
            }
            view.Target = point;
            if (armed && view.Sprite != null) view.Sprite.color = Color.red;
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        void EndAreaRpc(int id)
        {
            if (_areas.Remove(id, out AreaView view)) Destroy(view.Object);
            if (!_closed.Add(id)) return;
            _ends.Enqueue(id);
            if (_ends.Count > 256) _closed.Remove(_ends.Dequeue());
        }

        void LateUpdate()
        {
            if (!IsSpawned) return;
            bool playing = NetGame.Current.CanFight && _player.Health.Current > 0f;
            _barrier.SetActive(playing && _guard.Guarding);
            for (int i = 0; i < _auras.Length; i++)
            {
                bool show = playing && _buffs.Has(AuraTypes[i]);
                if (show && _auras[i] == null) _auras[i] = Instantiate(_auraPrefabs[i], _player.View);
                if (_auras[i] != null) _auras[i].SetActive(show);
            }
            foreach (AreaView view in _areas.Values)
                view.Object.transform.position = Vector2.Lerp(view.Object.transform.position, view.Target, 1f - Mathf.Exp(-25f * Time.deltaTime));
            UpdateTether(playing);
        }

        void UpdateTether(bool playing)
        {
            _tether.enabled = false;
            if (!playing || !_buffs.Has(CommonAugmentType.SwiftApproach)) return;
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (target == _player || target.Health.Current <= 0f) continue;
                Vector2 a = _player.View.position;
                Vector2 b = target.View.position;
                if ((b - a).sqrMagnitude > _swiftRange * _swiftRange || Physics2D.Linecast(a, b, _player.GroundMask)) continue;
                _tether.enabled = true;
                _tether.positionCount = 2;
                _tether.SetPosition(0, a);
                _tether.SetPosition(1, b);
                return;
            }
        }

        public override void OnNetworkDespawn()
        {
            foreach (AreaView view in _areas.Values) Destroy(view.Object);
            foreach (GameObject effect in _transients) if (effect != null) Destroy(effect);
            foreach (GameObject aura in _auras) if (aura != null) Destroy(aura);
            if (_barrier != null) Destroy(_barrier);
            if (_tether != null) Destroy(_tether.gameObject);
            _areas.Clear();
            _closed.Clear();
            _ends.Clear();
            _transients.Clear();
        }
    }
}

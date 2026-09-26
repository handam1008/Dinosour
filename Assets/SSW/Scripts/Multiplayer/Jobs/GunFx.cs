using System;
using KDH.Scripts.Arguments;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [Flags]
    public enum GunProc : byte
    {
        None = 0, Haste = 1, Shrink = 2, Evolve = 4
    }

    public sealed class GunFx : NetworkBehaviour
    {
        [Serializable] struct Entry
        {
            public GunnerAugmentType Type;
            public GameObject Normal;
            public GameObject Charged;
            public GameObject Trail;
        }

        [SerializeField] NetPlayer _player;
        [SerializeField] GunCast _gun;
        [SerializeField] EffectPool _pool;
        [SerializeField] Entry[] _effects;
        [SerializeField] GameObject _flagPrefab;
        [SerializeField] GameObject _lightningPrefab;
        [SerializeField] GameObject _shrinkPrefab;
        [SerializeField] GameObject _evolvePrefab;
        [SerializeField] GameObject _ghostPrefab;
        [SerializeField] SpriteRenderer[] _sprites;
        GameObject _flag;
        float _flagUntil;
        float _hasteUntil;
        float _ghostAt;
        bool _playing;
        public Vector2 Aim => _player.Aim;
        public EffectPool Pool => _pool;

        public override void OnNetworkSpawn()
        {
            if (_player.Job != PlayerJob.Gunner) return;
            foreach (Entry entry in _effects)
            {
                _pool.Warm(entry.Normal, 1);
                _pool.Warm(entry.Charged, 1);
                _pool.Warm(entry.Trail, 2);
            }
            _pool.Warm(_flagPrefab, 1);
            _pool.Warm(_lightningPrefab, 1);
            _pool.Warm(_shrinkPrefab, 1);
            _pool.Warm(_evolvePrefab, 1);
        }

        public void Attach(SpriteRenderer source, bool charged)
        {
            foreach (Entry entry in _effects)
                if (_gun.Has(entry.Type)) _pool.Trail(entry.Trail, source);
        }

        public void Hit(NetPlayer target, int mask, bool charged, GunProc proc)
        {
            if (IsSpawned && IsServer) HitRpc(target.NetworkObjectId, target.View.position, mask, charged, proc);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        void HitRpc(ulong targetId, Vector2 point, int mask, bool charged, GunProc proc)
        {
            Transform target = Target(targetId);
            if (target != null) point = target.position;
            foreach (Entry entry in _effects)
            {
                if ((mask & (1 << (int)entry.Type)) == 0) continue;
                bool follow = entry.Type == GunnerAugmentType.FireBullet || entry.Type == GunnerAugmentType.PoisonBullet;
                float duration = entry.Type == GunnerAugmentType.FireBullet ? 3f : entry.Type == GunnerAugmentType.PoisonBullet ? 4f : 1.5f;
                _pool.Play(charged ? entry.Charged : entry.Normal, point, duration, follow ? target : null);
            }
            if ((proc & GunProc.Haste) != 0)
            {
                _hasteUntil = Time.time + 1f;
                _ghostAt = Time.time;
            }
            if ((proc & GunProc.Shrink) != 0) _pool.Play(_shrinkPrefab, _player.View.position, 2.5f, _player.View);
            if ((proc & GunProc.Evolve) != 0) _pool.Play(_evolvePrefab, _player.View.position, 3f, _player.View);
        }

        public void Mark(NetPlayer target, int count, float duration)
        {
            if (IsSpawned && IsServer) MarkRpc(target.NetworkObjectId, count, duration);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        void MarkRpc(ulong targetId, int count, float duration)
        {
            if (_flag != null) _pool.Release(_flag);
            _flag = null;
            Transform target = Target(targetId);
            if (count == 0 || target == null) return;
            _flag = _pool.Play(_flagPrefab, target.position, duration, target);
            Transform flags = _flag.transform.GetChild(0);
            for (int i = 0; i < flags.childCount; i++) flags.GetChild(i).gameObject.SetActive(i < count);
            _flagUntil = Time.time + duration;
        }

        public void Strike(NetPlayer target)
        {
            if (IsSpawned && IsServer) StrikeRpc(target.NetworkObjectId, target.View.position);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        void StrikeRpc(ulong targetId, Vector2 point)
        {
            Transform target = Target(targetId);
            if (target != null) point = target.position;
            _pool.Play(_lightningPrefab, point - Vector2.up * 0.8f, 0.4f);
            if (_flag != null) _pool.Release(_flag);
            _flag = null;
        }

        Transform Target(ulong id) => NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject obj)
            ? obj.GetComponent<NetPlayer>().View : null;

        void Update()
        {
            if (!IsSpawned || _player.Job != PlayerJob.Gunner) return;
            bool playing = _player.CanAct;
            if (_playing && !playing) Clear();
            _playing = playing;
            if (!playing) return;
            if (_flag != null && Time.time < _flagUntil)
            {
                Transform flags = _flag.transform.GetChild(0);
                Vector3 position = flags.localPosition;
                position.y = 1.5f + Mathf.Sin(Time.time * 5.2f) * 0.125f;
                flags.localPosition = position;
            }
            if (Time.time >= _hasteUntil || Time.time < _ghostAt) return;
            _ghostAt = Time.time + 0.25f;
            foreach (SpriteRenderer sprite in _sprites)
                if (sprite.enabled && sprite.gameObject.activeInHierarchy)
                    _pool.Image(_ghostPrefab, sprite, 1f, new Color(1f, 1f, 1f, 0.5f));
        }

        void Clear()
        {
            _pool.Clear();
            _flag = null;
            _hasteUntil = 0f;
            _playing = false;
        }

        public override void OnNetworkDespawn() => Clear();
    }
}

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class Practice : MonoBehaviour
    {
        [SerializeField] NetArena _arena;
        [SerializeField] MapHost _mapHost;
        [SerializeField] MapSelector _selector;
        readonly NetPlayer[] _players = new NetPlayer[2];
        readonly float[] _respawn = new float[2];
        NetGame _game;
        float _timeScale;
        bool _drafting;

        public NetArena Arena => _arena;
        public NetPlayer Player => _players[0];
        public NetPlayer Target => _players[1];
        public bool Drafting => _drafting;
        MapLayout Map => _mapHost != null ? _mapHost.Map : null;

        public Vector3 Spawn(int slot) => Map != null
            ? slot == 0 ? Map.Spawn : Map.Target
            : _arena.Spawn(slot);

        void Start() => Begin();

        public void Begin()
        {
            if (_mapHost != null) _mapHost.Initialize();
            _selector.enabled = true;
            _game = NetGame.GetOrCreate();
            _game.StartPractice(this);
            _players[0] = _game.SpawnPractice(PlayerJobStorage.Load(), 0, Array.Empty<int>(), 0);
            _players[1] = _game.SpawnPractice(PlayerJob.None, 1, Array.Empty<int>(), 0);
            Target.gameObject.AddComponent<DummyRegen>();
            Bind();
        }

        void Bind()
        {
            Physics2D.IgnoreCollision(Player.Collider, Target.Collider);
            _selector.Bind(Player.Input);
        }

        void Update()
        {
            if (_game == null || !_game.Connected || Time.timeScale <= 0f) return;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.rKey.wasPressedThisFrame) ResetMap();
                if (Keyboard.current.pKey.wasPressedThisFrame) OpenDraft(false);
                else if (Keyboard.current.oKey.wasPressedThisFrame) OpenDraft(true);
            }
            for (int slot = 0; slot < _players.Length; slot++)
            {
                NetPlayer player = _players[slot];
                if (player.Body.position.y < _arena.FallY)
                {
                    if (Map != null && Map.ResetOnFall) { ResetMap(); break; }
                    Respawn(slot);
                }
                else if (player.Health.Current <= 0f)
                {
                    if (_respawn[slot] <= 0f) _respawn[slot] = Time.time + 0.7f;
                    else if (Time.time >= _respawn[slot]) Respawn(slot);
                }
            }
        }

        public void OpenDraft(bool withJob)
        {
            if (_drafting || Time.timeScale <= 0f || !Player.CanAct) return;
            _drafting = true;
            _timeScale = Time.timeScale;
            Player.Block(true);
            Time.timeScale = 0f;
            Player.Draft.Deal(withJob);
        }

        public void FinishDraft()
        {
            if (!_drafting) return;
            _drafting = false;
            Time.timeScale = _timeScale;
            Player.Block(false);
        }

        public void ResetMap()
        {
            if (_drafting) return;
            Respawn(0);
            Respawn(1);
            if (Map != null) Map.ResetMap();
        }

        void Respawn(int slot)
        {
            if (slot == 0) _game.ClearShots();
            NetPlayer previous = _players[slot];
            int[] owned = previous.Draft.Owned.ToArray();
            int progress = previous.Cast.Weapon != null ? previous.Cast.Weapon.Progress : 0;
            PlayerJob job = previous.Job;
            previous.NetworkObject.Despawn();
            _players[slot] = _game.SpawnPractice(job, slot, owned, progress);
            if (slot == 1) Target.gameObject.AddComponent<DummyRegen>();
            _respawn[slot] = 0f;
            Bind();
        }

        void OnDestroy()
        {
            if (_drafting) Time.timeScale = _timeScale;
        }
    }
}

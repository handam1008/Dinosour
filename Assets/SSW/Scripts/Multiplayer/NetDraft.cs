using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class NetDraft : NetworkBehaviour
    {
        [SerializeField] NetPlayer _player;
        [SerializeField] AugmentDrafter _drafter;
        [SerializeField] NetDeck _deck;
        [SerializeField] AugmentDraftUIBase _viewPrefab;
        NetworkList<int> _owned;
        readonly NetworkVariable<Vector3Int> _offer = new NetworkVariable<Vector3Int>(
            new Vector3Int(-1, -1, -1), NetworkVariableReadPermission.Owner);
        readonly NetworkVariable<bool> _ready = new NetworkVariable<bool>();
        AugmentDraftUIBase _view;

        public bool HasView => _view != null;
        public bool Ready => _ready.Value;
        public IEnumerable<int> Owned { get { foreach (int id in _owned) yield return id; } }
        public Vector3Int Offer => _offer.Value;
        public int OwnedCount => _owned.Count;

        void Awake()
        {
            _owned = new NetworkList<int>();
            _drafter.BindNetwork();
        }

        public override void OnNetworkSpawn()
        {
            _owned.OnListChanged += Granted;
            _offer.OnValueChanged += Offered;
            _ready.OnValueChanged += Selected;
            foreach (int id in _owned) _drafter.ApplyGrant(_deck.At(id));
            Offered(default, _offer.Value);
        }

        public void Deal()
        {
            if (!IsServer) return;
            List<int> job = _deck.Candidates(_player.Job, Owned, false);
            if (job.Count > 0) Grant(job[Random.Range(0, job.Count)]);
            List<int> common = _deck.Candidates(_player.Job, Owned, true);
            Vector3Int offer = new Vector3Int(-1, -1, -1);
            for (int slot = 0; slot < 3 && common.Count > 0; slot++)
            {
                int index = Random.Range(0, common.Count);
                offer[slot] = common[index];
                common.RemoveAt(index);
            }
            _ready.Value = offer.x < 0;
            _offer.Value = offer;
            if (_ready.Value) NetGame.Current.Match.Picked();
        }

        void Grant(int id)
        {
            if (!_owned.Contains(id)) _owned.Add(id);
        }

        void Granted(NetworkListEvent<int> change)
        {
            if (change.Type == NetworkListEvent<int>.EventType.Add)
                _drafter.ApplyGrant(_deck.At(change.Value));
        }

        void Offered(Vector3Int previous, Vector3Int current)
        {
            if (!IsOwner || current.x < 0 || _ready.Value) return;
            Close();
            Augment[] choices = new Augment[3];
            for (int i = 0; i < 3; i++)
                choices[i] = current[i] >= 0 ? _deck.At(current[i]) : null;
            _view = Instantiate(_viewPrefab);
            _view.PausesGame = false;
            _view.Show(choices, selected =>
            {
                for (int i = 0; i < choices.Length; i++)
                    if (choices[i] == selected) { Choose(i); break; }
                _view = null;
            });
            foreach (int id in _owned)
                if (_deck.At(id) is IJobRestrictedAugment) _view.ShowJobReward(_deck.At(id));
        }

        public void Choose(int slot)
        {
            if (IsOwner && IsSpawned) ChooseRpc(slot);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void ChooseRpc(int slot)
        {
            if (_ready.Value || NetGame.Current.Match.State.Phase != MatchPhase.Draft) return;
            if (slot < 0 || slot > 2 || _offer.Value[slot] < 0) return;
            Grant(_offer.Value[slot]);
            _ready.Value = true;
            NetGame.Current.Match.Picked();
        }

        void Selected(bool previous, bool current)
        {
            if (current) Close();
        }

        public void Close()
        {
            if (_view != null) Destroy(_view.gameObject);
            _view = null;
        }

        public override void OnNetworkDespawn()
        {
            _owned.OnListChanged -= Granted;
            _offer.OnValueChanged -= Offered;
            _ready.OnValueChanged -= Selected;
            Close();
        }
    }
}
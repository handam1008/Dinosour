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
        bool _jobPending;

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

        public void Deal(bool withJob = false)
        {
            if (!IsServer) return;
            _ready.Value = false;
            _jobPending = withJob;
            OfferChoices(true);
        }

        void OfferChoices(bool common)
        {
            List<int> candidates = _deck.Candidates(_player.Job, Owned, common);
            Vector3Int offer = new Vector3Int(-1, -1, -1);
            for (int slot = 0; slot < 3 && candidates.Count > 0; slot++)
            {
                int index = Random.Range(0, candidates.Count);
                offer[slot] = candidates[index];
                candidates.RemoveAt(index);
            }
            _offer.Value = offer;
            if (offer.x < 0) NextChoice();
        }

        void NextChoice()
        {
            if (_jobPending)
            {
                _jobPending = false;
                OfferChoices(false);
                return;
            }
            _ready.Value = true;
            NetGame.Current.Match.Picked();
        }

        void Grant(int id)
        {
            if (!_owned.Contains(id)) _owned.Add(id);
        }

        public void Restore(IEnumerable<int> owned)
        {
            if (!IsServer) return;
            foreach (int id in owned) Grant(id);
            _ready.Value = true;
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
                _view = null;
                for (int i = 0; i < choices.Length; i++)
                    if (choices[i] == selected) { Choose(i); break; }
            });
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
            NextChoice();
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

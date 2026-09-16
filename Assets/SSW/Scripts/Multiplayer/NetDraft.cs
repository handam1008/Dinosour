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
        [SerializeField] DraftScreen _viewPrefab;
        [SerializeField] DraftWatch _watch;
        NetworkList<int> _owned;
        readonly NetworkVariable<Vector3Int> _offer = new NetworkVariable<Vector3Int>(
            new Vector3Int(-1, -1, -1), NetworkVariableReadPermission.Owner);
        readonly NetworkVariable<bool> _ready = new NetworkVariable<bool>();
        DraftScreen _view;
        Vector3Int _viewOffer;
        bool _jobPending;
        int _reserved = -1;

        public bool Common => _offer.Value.x >= 0 && _deck.At(_offer.Value.x) is CommonAugment;

        public bool HasView => _view != null;
        public DraftScreen View => _view;
        public DraftPose WatchPose => _watch.Pose;
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
            _reserved = -1;
            List<int> candidates = _deck.Candidates(_player.Job, Owned, common);
            Vector3Int offer = new Vector3Int(-1, -1, -1);
            for (int slot = 0; slot < 3 && candidates.Count > 0; slot++)
            {
                int index = Random.Range(0, candidates.Count);
                offer[slot] = candidates[index];
                candidates.RemoveAt(index);
            }
            _offer.Value = offer;
            _watch.Open(offer, NetGame.Current.State.Set > 1);
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
            _watch.Close();
            NetGame.Current.Match.Picked();
        }

        void Update()
        {
            if (IsSpawned && !IsOwner) Watch();
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
            Show(current, false);
        }

        void Watch()
        {
            if (!_watch.Active || _ready.Value || NetGame.Current.State.Phase != MatchPhase.Draft)
            {
                if (_view != null) Close();
                return;
            }
            DraftPose pose = _watch.Pose;
            if (_view == null && _viewOffer == pose.Offer && pose.Pick >= 0) return;
            if (_view == null || _viewOffer != pose.Offer) Show(pose.Offer, true);
            _view.Apply(pose);
        }

        void Show(Vector3Int current, bool spectator)
        {
            Close();
            _viewOffer = current;
            Augment[] choices = new Augment[3];
            for (int i = 0; i < 3; i++)
                choices[i] = current[i] >= 0 ? _deck.At(current[i]) : null;
            _view = Instantiate(_viewPrefab);
            _view.PausesGame = false;
            _view.SetPlayer(_player.Job, spectator, NetGame.Current.Menu);
            if (spectator)
            {
                _view.Show(choices, _ => { });
                return;
            }
            _view.Pointed += (cursor, hover) => _watch.Point(cursor, hover, current);
            AugmentDraftUIBase view = _view;
            _view.Picked += selected =>
            {
                for (int i = 0; i < choices.Length; i++)
                    if (choices[i] == selected) { ReserveRpc(i, current); break; }
            };
            _view.Show(choices, selected =>
            {
                if (_view != view) return;
                _view = null;
                for (int i = 0; i < choices.Length; i++)
                    if (choices[i] == selected) { ChooseRpc(i, current); break; }
            });
        }

        public void Choose(int slot)
        {
            if (IsOwner && IsSpawned) ChooseRpc(slot, _offer.Value);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void ReserveRpc(int slot, Vector3Int offer)
        {
            if (!Valid(slot, offer) || _reserved >= 0) return;
            _reserved = slot;
            _watch.Pick(slot);
        }

        bool Valid(int slot, Vector3Int offer) =>
            !_ready.Value && NetGame.Current.Match.State.Phase == MatchPhase.Draft
            && slot >= 0 && slot < 3 && offer.Equals(_offer.Value) && _offer.Value[slot] >= 0;

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void ChooseRpc(int slot, Vector3Int offer)
        {
            if (!Valid(slot, offer)) return;
            if (_reserved >= 0 && _reserved != slot) return;
            Grant(_offer.Value[slot]);
            NextChoice();
        }

        void Selected(bool previous, bool current)
        {
            if (current) Close();
        }

        public void Close()
        {
            if (_view != null) _view.Close();
            _view = null;
        }

        public override void OnNetworkDespawn()
        {
            _owned.OnListChanged -= Granted;
            _offer.OnValueChanged -= Offered;
            _ready.OnValueChanged -= Selected;
            if (_view != null) Destroy(_view.gameObject);
            _view = null;
        }
    }
}

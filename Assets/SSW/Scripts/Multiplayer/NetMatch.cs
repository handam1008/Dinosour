using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class NetMatch : NetworkBehaviour
    {
        readonly NetworkVariable<MatchState> _state = new NetworkVariable<MatchState>();
        readonly HashSet<ulong> _shown = new HashSet<ulong>();
        double _introEnd;
        bool _checkDeath;
        public MatchState State => _state.Value;
        public bool Playing => State.Phase == MatchPhase.Playing;

        public override void OnNetworkSpawn()
        {
            NetGame.Current.SetMatch(this);
            _state.OnValueChanged += Changed;
            Changed(default, State);
        }

        public void Begin()
        {
            if (!IsServer || State.Phase != MatchPhase.Waiting) return;
            _shown.Clear();
            _introEnd = Time.unscaledTimeAsDouble + NetGame.Current.IntroDuration;
            _state.Value = new MatchState { Phase = MatchPhase.Intro };
        }

        public void IntroShown()
        {
            if (IsSpawned) ShownRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void ShownRpc(RpcParams rpc = default)
        {
            if (State.Phase != MatchPhase.Intro) return;
            ulong sender = rpc.Receive.SenderClientId;
            foreach (NetPlayer player in NetGame.Current.Players)
                if (player.OwnerClientId == sender) _shown.Add(sender);
        }

        void Update()
        {
            if (!IsServer || State.Phase != MatchPhase.Intro || _shown.Count != 2) return;
            if (Time.unscaledTimeAsDouble < _introEnd) return;
            _state.Value = new MatchState { Phase = MatchPhase.Draft };
            foreach (NetPlayer player in NetGame.Current.Players) player.Draft.Deal();
        }

        public void Picked()
        {
            if (!IsServer || State.Phase != MatchPhase.Draft) return;
            foreach (NetPlayer player in NetGame.Current.Players)
                if (!player.Draft.Ready) return;
            _state.Value = new MatchState { Phase = MatchPhase.Playing };
        }

        public void CheckDeath()
        {
            _checkDeath = true;
        }

        void LateUpdate()
        {
            if (!IsServer || !Playing) return;
            foreach (NetPlayer player in NetGame.Current.Players)
                if (player.transform.position.y < NetGame.Current.Arena.FallY && player.Health.Current > 0f)
                    player.Health.TakeDamage(player.Health.Max * 10f);
            if (!_checkDeath) return;
            _checkDeath = false;
            int alive = 0;
            ulong winner = 0;
            foreach (NetPlayer player in NetGame.Current.Players)
            {
                if (player.Health.Current <= 0f) continue;
                alive++;
                winner = player.OwnerClientId;
            }
            if (alive < 2) Finish(winner, alive == 0 ? MatchEnd.Draw : MatchEnd.Knockout);
        }

        public void Forfeit(ulong client)
        {
            if (!IsServer || State.Phase == MatchPhase.Finished) return;
            foreach (NetPlayer player in NetGame.Current.Players)
                if (player.OwnerClientId != client)
                {
                    Finish(player.OwnerClientId, MatchEnd.Left);
                    return;
                }
        }

        void Finish(ulong winner, MatchEnd reason)
        {
            _state.Value = new MatchState { Phase = MatchPhase.Finished, Winner = winner, Reason = reason };
        }

        void Changed(MatchState previous, MatchState current)
        {
            NetGame.Current.MatchChanged(current);
        }

        public override void OnNetworkDespawn()
        {
            _state.OnValueChanged -= Changed;
        }
    }
}
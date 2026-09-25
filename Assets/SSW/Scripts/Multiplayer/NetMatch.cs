using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class NetMatch : NetworkBehaviour
    {
        [SerializeField, Min(0.5f)] float _roundPause = 2f;
        [SerializeField, Min(0.5f)] float _readyPause = 1.5f;
        readonly NetworkVariable<MatchState> _state = new NetworkVariable<MatchState>();
        readonly HashSet<ulong> _shown = new HashSet<ulong>();
        double _introEnd;
        bool _checkDeath;
        double _nextRound;
        IRoundField _field;
        public MatchState State => _state.Value;
        public bool Playing => State.Phase == MatchPhase.Playing;

        public void Bind(IRoundField field) => _field = field;

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
            _state.Value = new MatchState
            {
                Phase = MatchPhase.Intro, Round = 1, Set = 1,
                First = NetGame.Current.Players[0].OwnerClientId,
                Second = NetGame.Current.Players[1].OwnerClientId
            };
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
            if (!IsServer) return;
            if (State.Phase == MatchPhase.RoundEnd || State.Phase == MatchPhase.SetEnd)
            {
                if (Time.unscaledTimeAsDouble < _nextRound) return;
                if (Rounds.Complete(State)) Finish(State.Winner, State.Reason);
                else
                {
                    bool setEnded = State.Phase == MatchPhase.SetEnd;
                    _checkDeath = false;
                    _field.ResetRound();
                    if (setEnded)
                    {
                        MatchState state = Rounds.NextSet(State);
                        _state.Value = state;
                        foreach (NetPlayer player in NetGame.Current.Players)
                        {
                            if (player.OwnerClientId == state.Winner) continue;
                            int losses = player.OwnerClientId == state.First ? state.SecondSets : state.FirstSets;
                            player.Draft.Deal(Rounds.JobReward(losses));
                        }
                    }
                    else Ready();
                }
                return;
            }
            if (State.Phase == MatchPhase.Draft)
            {
                Picked();
                return;
            }
            if (State.Phase == MatchPhase.Countdown)
            {
                if (!_field.MapReady) _nextRound = Time.unscaledTimeAsDouble + _readyPause;
                else if (Time.unscaledTimeAsDouble >= _nextRound) SetPhase(MatchPhase.Playing);
                return;
            }
            if (State.Phase != MatchPhase.Intro || _shown.Count != 2) return;
            if (Time.unscaledTimeAsDouble < _introEnd) return;
            SetPhase(MatchPhase.Draft);
            foreach (NetPlayer player in NetGame.Current.Players) player.Draft.Deal();
        }

        public void Picked()
        {
            if (!IsServer || State.Phase != MatchPhase.Draft || !_field.MapReady) return;
            foreach (NetPlayer player in NetGame.Current.Players)
                if (!player.Draft.Ready) return;
            Ready();
        }

        void Ready()
        {
            MatchState state = State;
            state.Round = (byte)(state.FirstWins + state.SecondWins + 1);
            state.Phase = MatchPhase.Countdown;
            _nextRound = Time.unscaledTimeAsDouble + _readyPause;
            _state.Value = state;
        }

        void SetPhase(MatchPhase phase)
        {
            MatchState state = State;
            state.Phase = phase;
            _state.Value = state;
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
            if (alive >= 2) return;
            _nextRound = Time.unscaledTimeAsDouble + _roundPause;
            _state.Value = Rounds.Award(State, winner, alive == 0);
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
            MatchState state = State;
            state.Phase = MatchPhase.Finished;
            state.Winner = winner;
            state.Reason = reason;
            _state.Value = state;
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

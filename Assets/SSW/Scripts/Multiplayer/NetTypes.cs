using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public interface IPlayerDrive
    {
        void Move(Vector2 value);
        void Jump();
    }

    public interface IHealthAuthority
    {
        bool CanChange { get; }
    }

    public interface IShotLife
    {
        void Impact(Vector2 point, float radius);
        void Finish();
    }

    public interface IRoundField
    {
        void ResetRound();
    }

    public enum MatchPhase : byte { Waiting, Draft, Playing, Finished, Intro, RoundEnd, Countdown, SetEnd }
    public enum MatchEnd : byte { Knockout, Left, Draw }

    public struct MatchState : INetworkSerializable, System.IEquatable<MatchState>
    {
        public MatchPhase Phase;
        public MatchEnd Reason;
        public ulong Winner;
        public ulong First;
        public ulong Second;
        public byte FirstWins;
        public byte SecondWins;
        public byte Round;
        public byte Set;
        public byte FirstSets;
        public byte SecondSets;
        public byte FirstMarks;
        public byte SecondMarks;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Phase);
            serializer.SerializeValue(ref Reason);
            serializer.SerializeValue(ref Winner);
            serializer.SerializeValue(ref First);
            serializer.SerializeValue(ref Second);
            serializer.SerializeValue(ref FirstWins);
            serializer.SerializeValue(ref SecondWins);
            serializer.SerializeValue(ref Round);
            serializer.SerializeValue(ref Set);
            serializer.SerializeValue(ref FirstSets);
            serializer.SerializeValue(ref SecondSets);
            serializer.SerializeValue(ref FirstMarks);
            serializer.SerializeValue(ref SecondMarks);
        }

        public bool Equals(MatchState other) =>
            Phase == other.Phase && Reason == other.Reason && Winner == other.Winner
            && First == other.First && Second == other.Second && FirstWins == other.FirstWins
            && SecondWins == other.SecondWins && Round == other.Round && Set == other.Set
            && FirstSets == other.FirstSets && SecondSets == other.SecondSets
            && FirstMarks == other.FirstMarks && SecondMarks == other.SecondMarks;
    }

    public static class NetMath
    {
        public static bool Finite(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y);

        public static bool Supported(PlayerJob job) =>
            job == PlayerJob.Witch || job == PlayerJob.Magician;
    }
}

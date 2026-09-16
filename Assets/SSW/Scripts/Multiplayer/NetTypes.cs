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

    public enum MatchPhase : byte { Waiting, Draft, Playing, Finished, Intro }
    public enum MatchEnd : byte { Knockout, Left, Draw }

    public struct MatchState : INetworkSerializable, System.IEquatable<MatchState>
    {
        public MatchPhase Phase;
        public MatchEnd Reason;
        public ulong Winner;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Phase);
            serializer.SerializeValue(ref Reason);
            serializer.SerializeValue(ref Winner);
        }

        public bool Equals(MatchState other) =>
            Phase == other.Phase && Reason == other.Reason && Winner == other.Winner;
    }

    public static class NetMath
    {
        public static bool Finite(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y);

        public static bool Supported(PlayerJob job) =>
            job == PlayerJob.Witch || job == PlayerJob.Magician;
    }
}
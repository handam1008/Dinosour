using System;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public struct DraftPose : INetworkSerializable, IEquatable<DraftPose>
    {
        public Vector3Int Offer;
        public Vector2 Cursor;
        public int Hover;
        public int Pick;

        public static DraftPose Empty => new DraftPose
        {
            Offer = new Vector3Int(-1, -1, -1), Cursor = -Vector2.one, Hover = -1, Pick = -1
        };

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Offer);
            serializer.SerializeValue(ref Cursor);
            serializer.SerializeValue(ref Hover);
            serializer.SerializeValue(ref Pick);
        }

        public bool Equals(DraftPose other) => Offer == other.Offer && Cursor == other.Cursor
            && Hover == other.Hover && Pick == other.Pick;
    }

    public sealed class DraftWatch : NetworkBehaviour
    {
        [SerializeField] NetDraft _draft;
        readonly NetworkVariable<DraftPose> _pose = new NetworkVariable<DraftPose>(DraftPose.Empty);
        double _lastPoint;

        public DraftPose Pose => _pose.Value;
        public bool Active => Pose.Offer.x >= 0;

        public void Open(Vector3Int offer, bool shared)
        {
            if (!IsServer) return;
            DraftPose pose = DraftPose.Empty;
            if (shared) pose.Offer = offer;
            _pose.Value = pose;
            _lastPoint = 0d;
        }

        public void Pick(int slot)
        {
            if (!IsServer || !Active) return;
            DraftPose pose = Pose;
            pose.Pick = slot;
            pose.Hover = slot;
            _pose.Value = pose;
        }

        public void Close()
        {
            if (IsServer) _pose.Value = DraftPose.Empty;
        }

        public void Point(Vector2 cursor, int hover, Vector3Int offer)
        {
            if (IsOwner && IsSpawned && Active) PointRpc(cursor, hover, offer);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
        void PointRpc(Vector2 cursor, int hover, Vector3Int offer)
        {
            if (!Active || _draft.Ready || NetGame.Current.State.Phase != MatchPhase.Draft) return;
            if (offer != Pose.Offer || !NetMath.Finite(cursor) || hover < -1 || hover > 2) return;
            double now = NetworkManager.ServerTime.Time;
            if (now - _lastPoint < 0.035d) return;
            _lastPoint = now;
            DraftPose pose = Pose;
            pose.Cursor = cursor == -Vector2.one ? cursor : new Vector2(Mathf.Clamp01(cursor.x), Mathf.Clamp01(cursor.y));
            if (pose.Pick < 0) pose.Hover = hover;
            _pose.Value = pose;
        }
    }
}

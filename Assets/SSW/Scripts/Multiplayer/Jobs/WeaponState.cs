using System;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

namespace SSW
{
    public struct WeaponState : INetworkSerializable, IEquatable<WeaponState>
    {
        public uint Action;
        public uint Ready;
        public uint Skill;
        public uint Parry;
        public uint Reload;
        public uint Filled;
        public int Ammo;
        public int Progress;
        public FixedList64Bytes<uint> Loaded;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Action);
            serializer.SerializeValue(ref Ready);
            serializer.SerializeValue(ref Skill);
            serializer.SerializeValue(ref Parry);
            serializer.SerializeValue(ref Reload);
            serializer.SerializeValue(ref Filled);
            serializer.SerializeValue(ref Ammo);
            serializer.SerializeValue(ref Progress);
            int count = Loaded.Length;
            serializer.SerializeValue(ref count);
            if (serializer.IsReader) Loaded.Clear();
            for (int i = 0; i < count; i++)
            {
                uint tick = serializer.IsReader ? 0u : Loaded[i];
                serializer.SerializeValue(ref tick);
                if (serializer.IsReader) Loaded.Add(tick);
            }
        }

        public bool Equals(WeaponState other) => Action == other.Action && Ready == other.Ready
            && Skill == other.Skill && Parry == other.Parry && Reload == other.Reload
            && Filled == other.Filled && Ammo == other.Ammo && Progress == other.Progress && Loaded.Equals(other.Loaded);
    }

    public struct BoltSpec : INetworkSerializable, IEquatable<BoltSpec>
    {
        public int Style;
        public float Speed;
        public float Gravity;
        public float Damage;
        public float Life;
        public float Radius;
        public float Scale;
        public int Bounce;
        public bool Stick;
        public bool Charged;
        public float Spin;
        public bool CanPenetrate;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Style);
            serializer.SerializeValue(ref Speed);
            serializer.SerializeValue(ref Gravity);
            serializer.SerializeValue(ref Damage);
            serializer.SerializeValue(ref Life);
            serializer.SerializeValue(ref Radius);
            serializer.SerializeValue(ref Scale);
            serializer.SerializeValue(ref Bounce);
            serializer.SerializeValue(ref Stick);
            serializer.SerializeValue(ref Charged);
            serializer.SerializeValue(ref Spin);
            serializer.SerializeValue(ref CanPenetrate);
        }

        public bool Equals(BoltSpec other) => Style == other.Style && Speed == other.Speed && Gravity == other.Gravity && Damage == other.Damage
            && Life == other.Life && Radius == other.Radius && Scale == other.Scale && Bounce == other.Bounce
            && Stick == other.Stick && Charged == other.Charged && Spin == other.Spin && CanPenetrate == other.CanPenetrate;
    }

    public interface IBoltReceiver
    {
        void Hit(NetPlayer target, NetBolt bolt);
    }
}

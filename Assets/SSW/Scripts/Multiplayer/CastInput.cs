using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public enum CastKind : byte { Press, Release, Cycle, StopCycle, Cancel, Parry }

    public struct CastInput : INetworkSerializable
    {
        public uint Action;
        public uint Epoch;
        public uint Tick;
        public uint Stock;
        public CastKind Kind;
        public Vector2 Direction;
        public double ViewTime;
        public uint Recall;
        public Vector2 Point;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Action);
            serializer.SerializeValue(ref Epoch);
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref Stock);
            serializer.SerializeValue(ref Kind);
            serializer.SerializeValue(ref Direction);
            serializer.SerializeValue(ref ViewTime);
            serializer.SerializeValue(ref Recall);
            serializer.SerializeValue(ref Point);
        }
    }

    public sealed class CastStock
    {
        public readonly struct Item
        {
            public readonly uint Id;
            readonly int _kind;
            public int Kind => Id == 0 ? -1 : _kind;

            public Item(uint id, int kind)
            {
                Id = id;
                _kind = kind;
            }
        }

        struct Retired
        {
            public Item Item;
            public double Until;
        }

        readonly List<Retired> _retired = new List<Retired>(16);
        uint _serial;
        uint _epoch;

        public Item Held { get; private set; }
        public Item Next { get; private set; }
        public Item Pocket { get; private set; }
        public bool PocketUsed { get; private set; }

        public void Reset(uint epoch)
        {
            _epoch = epoch;
            _retired.Clear();
            if (Held.Id != 0) Held = Create(Held.Kind);
            if (Next.Id != 0) Next = Create(Next.Kind);
            if (Pocket.Id != 0) Pocket = Create(Pocket.Kind);
        }

        public void Add(int kind, double now, double grace)
        {
            Prune(now);
            Item item = Create(kind);
            if (Held.Id == 0) Held = item;
            else
            {
                if (Next.Id != 0)
                {
                    if (_retired.Count == 64) _retired.RemoveAt(0);
                    _retired.Add(new Retired { Item = Held, Until = now + Math.Clamp(grace, 0d, 1.25d) });
                    Held = Next;
                }
                Next = item;
            }
        }

        public bool Take(uint id, uint epoch, double now, out int kind)
        {
            kind = -1;
            if (id == 0 || epoch != _epoch) return false;
            Prune(now);
            if (Held.Id == id)
            {
                kind = Held.Kind;
                Held = Next;
                Next = default;
                PocketUsed = false;
                return true;
            }
            for (int i = 0; i < _retired.Count; i++)
            {
                if (_retired[i].Item.Id != id) continue;
                kind = _retired[i].Item.Kind;
                _retired.RemoveAt(i);
                PocketUsed = false;
                return true;
            }
            return false;
        }

        public bool Swap(uint id, uint epoch, double now)
        {
            if (PocketUsed || epoch != _epoch) return false;
            Prune(now);
            if (id == Held.Id)
            {
                if (Held.Id == 0 && Pocket.Id == 0) return false;
                Item saved = Held;
                Held = Pocket;
                Pocket = saved;
                if (Held.Id == 0)
                {
                    Held = Next;
                    Next = default;
                }
                PocketUsed = true;
                return true;
            }
            for (int i = 0; i < _retired.Count; i++)
            {
                Retired retired = _retired[i];
                if (retired.Item.Id != id) continue;
                Item saved = Pocket;
                Pocket = retired.Item;
                if (saved.Id == 0) _retired.RemoveAt(i);
                else _retired[i] = new Retired { Item = saved, Until = retired.Until };
                PocketUsed = true;
                return true;
            }
            return false;
        }

        Item Create(int kind)
        {
            if (++_serial == 0) _serial++;
            return new Item(_serial, kind);
        }

        void Prune(double now)
        {
            for (int i = _retired.Count - 1; i >= 0; i--)
                if (_retired[i].Until <= now) _retired.RemoveAt(i);
        }
    }
}

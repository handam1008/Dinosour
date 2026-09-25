using System;
using Unity.Collections;
using Unity.Netcode;

namespace SSW
{
    public struct Fighter : INetworkSerializable, IEquatable<Fighter>
    {
        public FixedString128Bytes Name;
        public FixedString128Bytes Tag;

        public static Fighter Create(string name, string tag = "") => new Fighter
        {
            Name = new FixedString128Bytes(Clip(name)),
            Tag = new FixedString128Bytes(Clip(tag))
        };

        public string DisplayName(int slot)
        {
            string value = Name.ToString();
            int separator = value.LastIndexOf('#');
            if (separator >= 0) value = value.Substring(0, separator).TrimEnd();
            return string.IsNullOrEmpty(value) ? $"플레이어 {slot}" : value;
        }

        static string Clip(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            value = value.Trim().Replace('\n', ' ').Replace('\r', ' ');
            int length = Math.Min(value.Length, 30);
            if (char.IsHighSurrogate(value[length - 1])) length--;
            return value.Substring(0, length);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Tag);
        }

        public bool Equals(Fighter other) => Name.Equals(other.Name) && Tag.Equals(other.Tag);
    }
}
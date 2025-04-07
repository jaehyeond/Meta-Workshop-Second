using System;
using Unity.Netcode;

public struct NetworkString : INetworkSerializable, IEquatable<NetworkString>
    {
        private Unity.Collections.FixedString64Bytes _value;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _value);
        }

        public override string ToString() => _value.ToString();
        public static implicit operator string(NetworkString s) => s.ToString();
        public static implicit operator NetworkString(string s) => new NetworkString { _value = new Unity.Collections.FixedString64Bytes(s) };

        public bool Equals(NetworkString other) => _value.Equals(other._value);
    }
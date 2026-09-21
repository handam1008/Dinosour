using Unity.Collections;
using Unity.Netcode;

namespace SSW
{
    public sealed class ShotChannel
    {
        const string Message = "mushrooms.shot";
        NetworkManager _manager;

        public void Open(NetworkManager manager)
        {
            _manager = manager;
            manager.CustomMessagingManager.RegisterNamedMessageHandler(Message, Receive);
        }

        public void Send(ulong id, ShotPose pose)
        {
            using var writer = new FastBufferWriter(96, Allocator.Temp);
            writer.WriteValueSafe(id);
            writer.WriteNetworkSerializable(pose);
            _manager.CustomMessagingManager.SendNamedMessageToAll(Message, writer, NetworkDelivery.Unreliable);
        }

        void Receive(ulong sender, FastBufferReader reader)
        {
            if (_manager.IsServer || sender != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out ulong id);
            reader.ReadNetworkSerializable(out ShotPose pose);
            if (_manager.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject obj)
                && obj.TryGetComponent(out ShotSync shot)) shot.Receive(pose);
        }

        public void Close()
        {
            _manager?.CustomMessagingManager?.UnregisterNamedMessageHandler(Message);
            _manager = null;
        }
    }
}

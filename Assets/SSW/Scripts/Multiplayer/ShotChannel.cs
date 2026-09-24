using Unity.Collections;
using Unity.Netcode;

namespace SSW
{
    public sealed class ShotChannel
    {
        const string Message = "mushrooms.shot";
        NetworkManager _manager;
        FastBufferWriter _writer;

        public void Open(NetworkManager manager)
        {
            Close();
            _manager = manager;
            if (manager.IsServer) _writer = new FastBufferWriter(96, Allocator.Persistent);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(Message, Receive);
        }

        public void Send(ulong id, ShotPose pose)
        {
            if (_manager == null || !_manager.IsServer || !_manager.IsListening || _manager.ShutdownInProgress) return;
            if (_manager.IsHost && _manager.ConnectedClientsIds.Count == 1) return;
            _writer.Truncate(0);
            _writer.WriteValueSafe(id);
            _writer.WriteNetworkSerializable(pose);
            foreach (ulong client in _manager.ConnectedClientsIds)
                if (client != NetworkManager.ServerClientId)
                    _manager.CustomMessagingManager.SendNamedMessage(Message, client, _writer, NetworkDelivery.Unreliable);
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
            if (_writer.IsInitialized) _writer.Dispose();
        }
    }
}

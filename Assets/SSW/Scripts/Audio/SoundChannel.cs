using System;
using System.Collections.Generic;
using DevLib.SoundSystem.Runtime;
using Unity.Collections;
using Unity.Netcode;

namespace SSW
{
    public sealed class SoundChannel
    {
        const string Message = "mushrooms.sound";
        readonly HashSet<(ulong, uint, string)> _heard = new HashSet<(ulong, uint, string)>();
        readonly Queue<(ulong, uint, string)> _history = new Queue<(ulong, uint, string)>();
        NetworkManager _manager;
        FastBufferWriter _writer;
        GameAudio _audio;

        internal void Open(NetworkManager manager, GameAudio audio)
        {
            Close();
            _manager = manager;
            _audio = audio;
            if (manager.IsServer) _writer = new FastBufferWriter(96, Allocator.Persistent);
            _manager.CustomMessagingManager.RegisterNamedMessageHandler(Message, Receive);
        }

        internal void Close()
        {
            if (_manager != null && _manager.CustomMessagingManager != null)
                _manager.CustomMessagingManager.UnregisterNamedMessageHandler(Message);
            _manager = null;
            _audio = null;
            if (_writer.IsInitialized) _writer.Dispose();
            _heard.Clear();
            _history.Clear();
        }

        bool Valid(SoundCue cue) => _manager != null && _manager.IsListening && !_manager.ShutdownInProgress
            && _audio != null && _audio.Bank.Contains(cue)
            && cue.audioType == AudioType.Sfx && !cue.isLoop;

        internal void Predict(SoundCue cue, ulong source, uint action)
        {
            if (action != 0 && Valid(cue)) Hear(cue, source, action);
        }

        public bool Play(SoundCue cue, ulong source = 0, uint action = 0)
        {
            if (!Valid(cue) || !_manager.IsServer) return false;
            Hear(cue, source, action);
            if (_manager.IsHost && _manager.ConnectedClientsIds.Count == 1) return true;
            _writer.Truncate(0);
            _writer.WriteValueSafe(new FixedString64Bytes(cue.Id));
            _writer.WriteValueSafe(source);
            _writer.WriteValueSafe(action);
            foreach (ulong client in _manager.ConnectedClientsIds)
                if (client != NetworkManager.ServerClientId)
                    _manager.CustomMessagingManager.SendNamedMessage(Message, client, _writer, NetworkDelivery.ReliableSequenced);
            return true;
        }

        void Receive(ulong sender, FastBufferReader reader)
        {
            if (sender != NetworkManager.ServerClientId || _manager.IsServer) return;
            try
            {
                reader.ReadValueSafe(out FixedString64Bytes id);
                reader.ReadValueSafe(out ulong source);
                reader.ReadValueSafe(out uint action);
                if (_audio.Bank.TryGet(id.ToString(), out SoundCue cue) && Valid(cue)) Hear(cue, source, action);
            }
            catch (Exception error) when (error is OverflowException || error is ArgumentException) { }
        }

        void Hear(SoundCue cue, ulong source, uint action)
        {
            if (action != 0)
            {
                var key = (source, action, cue.Id);
                if (!_heard.Add(key)) return;
                _history.Enqueue(key);
                if (_history.Count > 256) _heard.Remove(_history.Dequeue());
            }
            if (_manager.IsClient) _audio.PlaySfx(cue);
        }
    }
}

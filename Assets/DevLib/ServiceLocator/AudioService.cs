using System;
using System.Collections.Generic;
using System.Linq;
using DevLib.SoundSystem.Runtime;
using UnityEngine;

namespace DevLib.ServiceLocator
{
    [Serializable]
    public struct NamedClip
    {
        public string name;
        public AudioClip clip;
    }
    public class AudioService : MonoBehaviour, IAudioService
    {
        [SerializeField] private GameObject soundPlayerPrefab;

        private Dictionary<int, SoundPlayer> _playerDict = new Dictionary<int, SoundPlayer>();


        private SoundPlayer _bgmPlayer;
        
        private void Awake()
        {
            ServiceLocator.Register<IAudioService>(this);
            GameObject bgmObject = Instantiate(soundPlayerPrefab,transform);
            _bgmPlayer = bgmObject.GetComponent<SoundPlayer>();
        }

        private void OnDestroy()
        {
            ServiceLocator.Register<IAudioService>(new NullAudioService());
        }

        public void PlaySfx(SoundClipSO clipData, int channel = 0)
        {
            GameObject playerObj = Instantiate(soundPlayerPrefab, transform);
            SoundPlayer player = playerObj.GetComponent<SoundPlayer>();
            player.PlaySound(clipData);
            player.OnSoundFinished += HandleSoundFinish;

            if (channel > 0)
            {
                if (_playerDict.TryGetValue(channel, out SoundPlayer oldPlayer))
                {
                    oldPlayer.ForceStopSound();
                    SetDisableSoundPlayer(oldPlayer);
                    _playerDict.Remove(channel);
                }
                
                _playerDict[channel] = player;
            }
        }

        private void HandleSoundFinish(SoundPlayer player)
        {
            player.OnSoundFinished -= HandleSoundFinish;
            //나ㅏㅏㅏㅏㅏ중에 풀매니저로 변경
            SetDisableSoundPlayer(player);
        }

        private void SetDisableSoundPlayer(SoundPlayer player)
        {
            Destroy(player.gameObject);
        }

        public void StopSfx(int channel)
        {
            if (_playerDict.TryGetValue(channel, out SoundPlayer player))
            {
                player.ForceStopSound();
                SetDisableSoundPlayer(player);
            }
        }

        public void PlayBgm(SoundClipSO bgmSound)
        {
            _bgmPlayer.ForceStopSound();
            _bgmPlayer.PlaySound(bgmSound);
        }

        public void StopBgm()
        {
            _bgmPlayer.ForceStopSound();
        }
    }
}
using System;
using KDH.Scripts.Sounds;
using UnityEngine;

namespace SSW
{
    public enum GunSound
    {
        Evolution, Lightning, Reload, Shrink, Mark, Damage, Air, Gravity, Ice, Poison, Shuriken
    }

    [Serializable]
    public sealed class GunAudio
    {
        [SerializeField] KDH_SoundCueListSO _source;

        public bool Play(GunSound sound) => NetGame.Current.Sounds.Play(_source.list[(int)sound]);
    }
}

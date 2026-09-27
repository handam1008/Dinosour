using System.Collections.Generic;
using SSW;
using UnityEngine;

namespace KDH.Scripts.Sounds
{
    [CreateAssetMenu(fileName = "SoundCueList", menuName = "KDH/SO/SoundCueList")]
    public class KDH_SoundCueListSO : ScriptableObject
    {
        public List<SoundCue> list = new List<SoundCue>();
    }
}

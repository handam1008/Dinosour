using DevLib.SoundSystem.Runtime;
using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(menuName = "Sound/Cue", fileName = "Sound")]
    public sealed class SoundCue : SoundClipSO
    {
        [SerializeField, HideInInspector] string _id;
        public string Id => _id;

        void OnValidate()
        {
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(path)) _id = UnityEditor.AssetDatabase.AssetPathToGUID(path);
#endif
            if (string.IsNullOrEmpty(_id)) _id = System.Guid.NewGuid().ToString("N");
        }
    }
}

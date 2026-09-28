using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    public sealed class VolumePanel : MonoBehaviour
    {
        [SerializeField] Slider _bgm;
        [SerializeField] Slider _sfx;
        GameAudio _audio;

        void Awake() => _audio = GameAudio.GetOrCreate();

        void OnEnable()
        {
            _bgm.SetValueWithoutNotify(_audio.BgmVolume);
            _sfx.SetValueWithoutNotify(_audio.SfxVolume);
            _bgm.onValueChanged.AddListener(_audio.SetBgmVolume);
            _sfx.onValueChanged.AddListener(_audio.SetSfxVolume);
        }

        void OnDisable()
        {
            _bgm.onValueChanged.RemoveListener(_audio.SetBgmVolume);
            _sfx.onValueChanged.RemoveListener(_audio.SetSfxVolume);
            PlayerPrefs.Save();
        }
    }
}

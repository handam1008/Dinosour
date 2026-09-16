using UnityEngine;

namespace SSW
{
    public sealed class DraftFx : MonoBehaviour
    {
        [SerializeField] AudioSource _audio;
        [SerializeField] AudioClip _deal;
        [SerializeField] AudioClip _pick;
        [SerializeField] CardBurst _burst;

        public void Deal() => _audio.PlayOneShot(_deal, 0.3f);

        public void Pick(RectTransform card)
        {
            _burst.Play(card);
            _audio.PlayOneShot(_pick, 0.28f);
        }
    }
}

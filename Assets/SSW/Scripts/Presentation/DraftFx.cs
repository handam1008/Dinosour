using UnityEngine;

namespace SSW
{
    public sealed class DraftFx : MonoBehaviour
    {
        [SerializeField] AudioClip _deal;
        [SerializeField] AudioClip _pick;
        [SerializeField] CardBurst _burst;

        public void Deal() => GameAudio.GetOrCreate().PlaySfx(_deal, 0.3f);

        public void Pick(RectTransform card)
        {
            _burst.Play(card);
            GameAudio.GetOrCreate().PlaySfx(_pick, 0.28f);
        }
    }
}

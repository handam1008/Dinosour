using UnityEngine;

namespace SSW
{
    public sealed class DraftFx : MonoBehaviour
    {
        [SerializeField] SoundCue _deal;
        [SerializeField] SoundCue _pick;
        [SerializeField] CardBurst _burst;

        public void Deal() => GameAudio.GetOrCreate().PlaySfx(_deal);

        public void Pick(RectTransform card)
        {
            _burst.Play(card);
            GameAudio.GetOrCreate().PlaySfx(_pick);
        }
    }
}

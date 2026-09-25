using HitEffect = NKY.Scripts.FeedBack.AbstractFeedBack;
using UnityEngine;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class HitFeedback : MonoBehaviour
    {
        [SerializeField] Health _health;
        [SerializeField] HitEffect[] _effects;

        void OnEnable() => _health.OnDamaged += Play;

        void OnDisable() => _health.OnDamaged -= Play;

        void Play()
        {
            foreach (HitEffect effect in _effects) effect.OnFeedBack();
        }
    }
}

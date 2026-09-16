using UnityEngine;

namespace RYU._01.Script.FeedBack
{
    public class EffectPlayer : AbstractFeedBack
    {
        [SerializeField] private GameObject Effect;

        public override void CreateFeedBack(Vector3 pos, float scale)
        {
            if (Effect == null) return;

            GameObject effectGo = Instantiate(Effect, pos, Quaternion.identity);

            effectGo.transform.localScale *= scale;

            ParticleSystem effect = effectGo.GetComponent<ParticleSystem>();
            if (effect != null) effect.Play();
        }

        public override void StopFeedBack()
        {
        }
    }
}

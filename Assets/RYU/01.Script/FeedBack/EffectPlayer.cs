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

            // 넓은 살포면 이펙트도 같이 커진다.
            // 파티클 Main > Scaling Mode 가 Local 또는 Hierarchy 여야 적용된다.
            effectGo.transform.localScale *= scale;

            ParticleSystem effect = effectGo.GetComponent<ParticleSystem>();
            if (effect != null) effect.Play();
        }

        public override void StopFeedBack()
        {
        }
    }
}

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NKY.Scripts.FeedBack
{
    public class EffectParticleFeedBack : AbstractFeedBack
    {
        [SerializeField] private GameObject effectPrefab;

        private CancellationToken ctx;

        private void Awake()
        {
            ctx = CancellationToken.None;
        }

        public override void OnFeedBack()
        {
            EffectPlay();
        }
        
        private void EffectPlay()
        {
            if(effectPrefab == null) return;
            
            GameObject effect = Instantiate(effectPrefab, transform.root);
            if (!effect.TryGetComponent(out ParticleSystem anim))
            {
                Destroy(effect);
                return;
            }
            
            effect.transform.position = transform.root.position;
            effect.SetActive(true);

            var main = anim.main;
            
            main.loop = false; // 루프 해제
        
            // 파티클 재생 완료 시 오브젝트 자동 삭제 설정
            main.stopAction = ParticleSystemStopAction.Destroy;
        }
    }
}
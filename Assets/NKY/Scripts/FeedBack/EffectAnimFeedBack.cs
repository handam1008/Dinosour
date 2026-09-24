using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NKY.Scripts.FeedBack
{
    public class EffectAnimFeedBack : AbstractFeedBack
    {
        private static readonly int Play = Animator.StringToHash("Play");
        
        [SerializeField] private GameObject effectPrefab;

        private CancellationToken ctx;

        private void Awake()
        {
            ctx = CancellationToken.None;
        }

        public  override void OnFeedBack()
        {
            _ = EffectPlay();
        }
        
        private async UniTaskVoid EffectPlay()
        {
            if(effectPrefab == null) return;
            
            GameObject effect = Instantiate(effectPrefab, transform.parent);
            if (!effect.TryGetComponent(out Animator anim))
            {
                Destroy(effect);
                return;
            }
            
            effect.transform.position = transform.root.position;
            effect.SetActive(true);
            anim.SetTrigger(Play);

            int delayMliSec = (int)(anim.GetCurrentAnimatorStateInfo(0).length * 1000);
            
            await UniTask.Delay(delayMliSec, false, PlayerLoopTiming.Update, ctx);
            
            Destroy(effect);
        }
    }
}
using SSW;
using UnityEngine;

namespace RYU._01.Script.FeedBack
{
   
    public class BoneDeathFX : MonoBehaviour, IDamageReceivedListener
    {
        [SerializeField] private FeedBackPlayer _deathFeedBack;

        public void OnDamageReceived(DamageRequest request, DamageResult result)
        {
            if (!result.WasLethal) return;
            if (_deathFeedBack == null) return;

            _deathFeedBack.PlayAllFeedBacks();
        }
    }
}

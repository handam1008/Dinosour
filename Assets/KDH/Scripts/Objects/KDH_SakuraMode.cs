using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_SakuraMode : MonoBehaviour
    {
        [SerializeField] private float timeApplySpeed;
        [SerializeField] private float speedAmount;

        [SerializeField] private SoundCue sakuraSound;
        
        void OnParticleCollision(GameObject go)
        {
            if (go.TryGetComponent(out ISpeedable speedable))
            {
                if (NetGame.Current != null && sakuraSound != null)
                    NetGame.Current.Sounds.Play(sakuraSound); // 사운드
                    
                speedable.ApplySpeed(speedAmount, timeApplySpeed);

                GameObject entity = KDH_SakuraEffectPooling.Instance.effects.Pop();
                entity.transform.position = go.transform.position;
                entity.SetActive(true);
            }
        }
    }
}

using DevLib.ServiceLocator;
using KDH.Scripts.Bullet;
using KDH.Scripts.Gun;
using SSW;
using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Upgrade.Instances.Bullets
{
    public class KDH_Quest_EvolutionBullet : KDH_AbstractBulletAbility
    {
        [field: SerializeField] public KDH_BulletAbilityDataSO BulletAbilityData { get; private set; }

        [field: SerializeField] public int HitCount  { get; private set; }
        [SerializeField] private int questClearCount = 2;
        [SerializeField] private GameObject questUIPrefab;
        public GameObject QuestUI { get; private set; }
        
        [SerializeField] private UnityEvent onHitPlayer;
        [SerializeField] private UnityEvent onQuestCleared;
        
        public Transform HitPoint { get; private set; }
        public KDH_Bullet Bullet { get; private set; }
        
        // private SoundCue clearedSound;
        
        private KDH_Gun _gun;
        private KDH_Quest_EvolutioinBulletUI ui; // 구조 망함
        private bool _isCleared;
        private bool _isStarted;
        private string _questStr;
        
        private void Awake()
        {
            _gun = GetComponentInParent<KDH_Gun>();
        }
        
        private void PlayQuest()
        {
            ++HitCount;
            
            if (_isCleared) return;
            
            if (HitCount >= questClearCount)
            {
                _gun.ChargeSpeed *= 0.5f;
                onQuestCleared?.Invoke();
                StartCoroutine(ui.RemoveUI());
                _isCleared = true;
                
                onQuestCleared?.RemoveAllListeners();
            }
        }

        public override void BulletAbility(Collider2D collision, KDH_Bullet bullet)
        {
            if (_isCleared) return;
            
            HitPoint = collision.transform;
            Bullet = bullet;
            
            if (!_isStarted)
            {
                QuestUI = Instantiate(questUIPrefab, Bullet.PlayerGun.transform);
                ui = QuestUI.GetComponentInChildren<KDH_Quest_EvolutioinBulletUI>();
                _isStarted = true;
                _questStr = $"퀘스트: 진화 진행률{HitCount} / {questClearCount}";
                ui.OnChangeText(_questStr);
            }

            if (bullet.IsUpgraded)
            {
                PlayQuest();
                
                _questStr = $"퀘스트: 진화 진행률{HitCount} / {questClearCount}";
                if (HitCount >= questClearCount)
                {
                    // if (clearedSound == null)
                    //     clearedSound = bullet.PlayerGun.SoundCues.list[0];
                    //
                    // if (clearedSound != null)
                    //     ServiceLocator.Get<IAudioService>().PlaySfx(clearedSound);
                            
                    _questStr = "퀘스트: 진화 클리어!";
                    
                }

                ui.OnChangeText(_questStr);
                
                onHitPlayer?.Invoke();
            }
        }
    }
}

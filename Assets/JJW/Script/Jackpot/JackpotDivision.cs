using System;
using SSW;
using UnityEngine;
using Random = UnityEngine.Random;

namespace JJW.Script.Jackpot
{
    public class JackpotDivision : MonoBehaviour
    {
        public event Action<float> DamageJackpot;
        public event Action<float> HealJackpot;
        public event Action<float, float> SpeedJackpot;
        public event Action Jackpot777;
        public event Action<float> Jackpot444;
        public event Action StarJackpot;
        
        [SerializeField] private GamblerCoinShooter coinShooter;
        private void Awake()
        {
            if (coinShooter == null)
            {
                coinShooter = GetComponentInChildren<GamblerCoinShooter>();
            }
        }

        private void OnEnable()
        {
            coinShooter.RouletteCoinFired += Roulette;
        }

        private void OnDisable()
        {
            coinShooter.RouletteCoinFired -= Roulette;
        }

        private void Roulette()
        {
            int boll = Random.Range(0, 100);
            Debug.Log("룰렛 돌아감!!");

             if (0 <= boll && boll < 7)
            {
                Debug.Log("공격력 증가");
                DamageJackpot?.Invoke(1.3f);
            }
            else if (7 <= boll && boll < 14)
            {
                Debug.Log("체력 회복");
                HealJackpot?.Invoke(50f);
            }
            else if (14 <= boll && boll < 21)
            {
                Debug.Log("무적");
                StarJackpot?.Invoke();
            }
            else if (21 <= boll && boll < 28)
            {
                Debug.Log("스피드 증가");
                SpeedJackpot?.Invoke(1.523f,7.4f);
            }
            else if (boll == 28)
            {
                Debug.Log("즉사");
                Jackpot444?.Invoke(100000000f);
            }
            else if (29 <= boll && boll < 34)
            {
                Debug.Log("잭팟");
                Jackpot777?.Invoke();
            }
        }
    }
}


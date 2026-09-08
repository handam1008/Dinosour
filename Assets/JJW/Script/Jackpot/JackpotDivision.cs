using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace JJW.Script.Jackpot
{
    public class JackpotDivision : MonoBehaviour
    {
        public event Action DamageJackpot;
        public event Action HealJackpot;
        public event Action SpeedJackpot;
        public event Action Jackpot777;
        public event Action Jackpot444;
        public event Action StartJackpot;
        
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
            
        }
    }
}


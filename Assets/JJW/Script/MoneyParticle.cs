using UnityEngine;

public class MoneyParticle : MonoBehaviour
{
   [SerializeField] private ParticleSystem Moneyparticle;
   private Gambler gambler;

    private void Awake()
    {
        Moneyparticle = new ParticleSystem();
        ParticleSystem.MainModule main = Moneyparticle.main;
        main.playOnAwake = false;
        main.loop = false;
        gambler.OnJackpot += OnMoneyParticle;
    }
    private void OnMoneyParticle()
    {
        //Moneyparticle.main.loop = true;
        Moneyparticle.Play();
    }
    
}

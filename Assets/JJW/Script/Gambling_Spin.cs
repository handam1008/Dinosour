using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class GamblingSpin : MonoBehaviour
{
    private bool isJackpot;
    private int jackpotLuck = 100;
    private bool isSpinning;

   [SerializeField] private GameObject jackpotparticlePre;
    

    private void Update()
    {
        Spining();
    }


    private void Spining()
    {
        if (Keyboard.current.shiftKey.wasPressedThisFrame)
        {
            if (isJackpot) return;

            StartCoroutine(SpinRoutine());
        }
    }

    private bool Roulette()
    {
        int roll = Random.Range(0,100);

        if (roll < jackpotLuck)
        {
            Debug.Log("잭팟 터짐");
           return true; 
        }
        else
        {
            Debug.Log("잭팟 실패");
            return false;
        }
    }

    
    private IEnumerator JackpotCooldown()
    {
        isJackpot = true;
        GameObject effectGO = Instantiate(jackpotparticlePre, transform);
        ParticleSystem effect = effectGO.GetComponent<ParticleSystem>();
        effect.Play();
        Debug.Log("잭팟 능력 발동!");
        
        yield return new WaitForSeconds(7.7f);
        
        effect.Stop();
        Destroy(jackpotparticlePre);
        Debug.Log("잭팟 능력 꺼짐");
    }

    private IEnumerator SpinRoutine()
    {
        Debug.Log("룰렛 도는 중...");

        yield return new WaitForSeconds(2f);

        bool result = Roulette();

        if (result)
        {
            Debug.Log("잭팟!!!");
            StartCoroutine(JackpotCooldown());
        }
    }
}

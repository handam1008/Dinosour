using System.Collections;
using SSW;
using UnityEngine;

public class KHG_DashSpeed : MonoBehaviour
{
   [Header("대쉬 후 이동속도 증가")]
   [SerializeField] private float speedIncrease = 1f;   // 10%
   [SerializeField] private float duration = 3f;           

   private PlayerController playerController;
   private Coroutine boostCoroutine;

   private void Awake()
   {
      playerController = GetComponent<PlayerController>();
   }

   public void ActivateSpeedBoost()
   {
      if (boostCoroutine != null)
         StopCoroutine(boostCoroutine);

      //boostCoroutine = StartCoroutine(SpeedBoostRoutine());
   }

   /*private IEnumerator SpeedBoostRoutine()
   {
      float originalSpeed = playerController._moveSpeed;

      playerController._moveSpeed = originalSpeed * (1f + speedIncrease);

      yield return new WaitForSeconds(duration);

      playerController._moveSpeed = originalSpeed;
   }*/
}

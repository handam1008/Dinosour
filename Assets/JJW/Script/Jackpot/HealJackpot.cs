using System;
using JJW.Script.Jackpot;
using SSW;
using UnityEngine;

public class HealJackpot : MonoBehaviour
{
   private JackpotDivision division;

   private void Awake()
   {
      division = GetComponent<JackpotDivision>();
      division.HealJackpot += ApplayHeal;
   }

   private void ApplayHeal(PlayerController playerController, float amount)
   {
      //playerController. 힐하는 함수 넣어 줘야함;;
   }

}

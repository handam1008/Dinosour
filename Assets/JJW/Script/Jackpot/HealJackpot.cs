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

   private void ApplayHeal(float amount)
   {
      if (TryGetComponent(out IHealable healable))
      {
         healable.Heal(amount);
      }
   }

}

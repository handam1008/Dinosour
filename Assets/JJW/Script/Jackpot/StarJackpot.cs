using System;
using JJW.Script.Jackpot;
using UnityEngine;

public class StarJackpot : MonoBehaviour
{
   private JackpotDivision division;

   private void Awake()
   {
      division = GetComponent<JackpotDivision>();
   }

   private void OnEnable()
   {
      //division.StarJackpot += 
   }

   private void OnDisable()
   {
      //division.StarJackpot -=
   }
}

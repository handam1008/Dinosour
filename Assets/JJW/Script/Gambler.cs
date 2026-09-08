using System;
using UnityEngine;

public class Gambler : MonoBehaviour
{
   public bool IsJackpot { get; private set; }
   public event Action OnJackpot;
   

   private void Update()
   {
      if (IsJackpot)
      {
         OnJackpot?.Invoke();
         //쿨타임 넣을거임
         IsJackpot = false;
      }
   }
}

using System.Collections;
using System.Collections.Generic;
using SSW;
using UnityEngine;

namespace RYU._01.Script.Potions
{
    public class OverTimeRunner : MonoBehaviour
    {
        public void Run(ITickEffect effect, int count, float interval)
        {
            StartCoroutine(TickCO(effect, count, interval));
        }

        IEnumerator TickCO(ITickEffect effect, int count, float interval)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new WaitForSeconds(interval);
                effect.Tick(gameObject);
            }
        }
    }
}
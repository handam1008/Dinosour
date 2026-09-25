using System;
using UnityEngine;

public class KDH_SakuraEffect : MonoBehaviour
{
    private ParticleSystem particle;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (particle.isStopped)
        { 
            KDH_SakuraEffectPooling.Instance.effects.Push(gameObject);
            gameObject.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        particle.Stop();
        particle.Play();
    }
}

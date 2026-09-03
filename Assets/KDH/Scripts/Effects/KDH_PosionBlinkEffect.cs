using UnityEngine;

public class KDH_PosionBlinkEffect : MonoBehaviour
{
    private ParticleSystem ps;
    
    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    public void SetBlink(float duration, float startLifeTime)
    {
        ps.Stop();
        var main = ps.main;
        main.duration = duration;
        main.startLifetime = startLifeTime;
        ps.Clear();
        ps.Play();
    }
}

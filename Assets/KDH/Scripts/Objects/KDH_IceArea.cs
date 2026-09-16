using System.Collections;
using SSW;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KDH_IceArea : MonoBehaviour
{
    [SerializeField] private Volume volume;
    [SerializeField, Range(0f, 1f)] private float targetIntensity = 0.4f;
    [SerializeField] private float fadeSpeed = 2f;

    [SerializeField] private float damage;
    [SerializeField] private float slowAmount;
    
    private Vignette vignette;
    private bool playerInside;

    private Collider2D _collider;
    private float _tick = 1f;
    private float _timer;
    
    void Awake()
    {
        volume.profile.TryGet(out vignette);
        vignette.intensity.value = 0f;
    }
    
    void Update()
    {
        float target = playerInside ? targetIntensity : 0f;
        vignette.intensity.value = Mathf.MoveTowards(
            vignette.intensity.value, target, fadeSpeed * Time.deltaTime);
        
        if (playerInside)
        {
            _timer += Time.deltaTime;

            if (_timer >= _tick)
            {
                if (_collider != null)
                {
                    ApplyDamage(_collider, damage);
                    ApplySlow(_collider, slowAmount);
                    _timer = 0;
                }
            }
        }
    }

    private void ApplyDamage(Collider2D player, float damage)
    {
        if (player.TryGetComponent(out IDamageable playerController))
            playerController.TakeDamage(damage);
    }

    private void ApplySlow(Collider2D player, float slowAmount)
    {
        if (player.TryGetComponent(out ISlowable slowable))
            slowable.ApplySlow(slowAmount, 1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerController _))
        {
            _collider = other;
            playerInside = false;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerController _))
        {           
            _collider = other;
            playerInside = true;
        }
    }
}
using RYU._01.Script.Potions;
using SSW;
using UnityEngine;

public class KHG_DashHPdown : MonoBehaviour
{
    [Header("지속 데미지 (DOT) 설정")]
    [SerializeField] private float _dotDamage = 3f;
    [SerializeField] private int _tickCount = 2;
    [SerializeField] private float _tickInterval = 0.7f;

    private KHG_Dash _dashScript;

    private void Awake()
    {
        _dashScript = GetComponent<KHG_Dash>();
    }

    private void OnEnable()
    {
        if (_dashScript != null)
        {
            _dashScript.OnDashHitEnemy += ApplyDotDamage;
        }
    }

    private void OnDisable()
    {
        if (_dashScript != null)
        {
            _dashScript.OnDashHitEnemy -= ApplyDotDamage;
        }
    }

    private void ApplyDotDamage(GameObject target, float dashDamage)
    {
        if (target == null)
            return;


        StartCoroutine(ApplyDamageOverTime(target));
    }

    private System.Collections.IEnumerator ApplyDamageOverTime(GameObject target)
    {
        for (int i = 0; i < _tickCount; i++)
        {
            if (target == null)
                yield break;

            IDamageable damageable =
                target.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                yield break;
            }

            damageable.TakeDamage(_dotDamage);


            yield return new WaitForSeconds(_tickInterval);
        }
    }
}
using UnityEngine;
using SSW;

public class KHG_DashStop : MonoBehaviour
{
    [Header("속박 설정")]
    [SerializeField] private float rootDuration = 1f;
    [SerializeField] private float rootCooldown = 1f;

    private KHG_Dash dash;

    private Rigidbody2D _rb;
    private float nextRootTime = 0f;

    private void Awake()
    {
        dash = GetComponent<KHG_Dash>();
    }

    private void OnEnable()
    {
        if (dash != null)
        {
            dash.OnDashHitEnemy += OnDashHit;
        }
    }

    private void OnDisable()
    {
        if (dash != null)
        {
            dash.OnDashHitEnemy -= OnDashHit;
        }

        _rb = null;
    }

    private void OnDashHit(GameObject target, float damage)
    {
        if (Time.time < nextRootTime)
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        ISlowable slowable =
            target.GetComponentInParent<ISlowable>();

        if (slowable == null)
        {
            Debug.Log("ISlowable을 찾지 못함");
            return;
        }

        Debug.Log("속박 대상: " + target.name);
        Debug.Log("실제 ISlowable: " + slowable);

        slowable.ApplySlow(1f, rootDuration);

        nextRootTime = Time.time + rootCooldown;
    }
}
using SSW;
using UnityEngine;

public class KHG_StandingHeal : MonoBehaviour
{
    [Header("회복 설정")]
    [SerializeField] private float healAmount = 1f;       // 한 번에 회복량
    [SerializeField] private float healInterval = 0.5f;   // 회복 간격

    [Header("가만히 있어야 하는 시간")]
    [SerializeField] private float standTime = 1f;       

    private IHealable healable;
    private Rigidbody2D rb;

    private float standTimer;
    private float healTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        healable = GetComponent<IHealable>();

        if (healable == null)
            healable = GetComponentInParent<IHealable>();

        if (healable == null)
            healable = GetComponentInChildren<IHealable>();
    }

    private void Update()
    {
        if (healable == null || rb == null)
            return;

        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            standTimer = 0f;
            healTimer = 0f;
            return;
        }

        standTimer += Time.deltaTime;

        if (standTimer < standTime)
            return;

        healTimer += Time.deltaTime;

        if (healTimer >= healInterval)
        {
            healable.Heal(healAmount);

            healTimer = 0f;

            Debug.Log($"[가만히 있기 회복] +{healAmount} HP");
        }
    }
}

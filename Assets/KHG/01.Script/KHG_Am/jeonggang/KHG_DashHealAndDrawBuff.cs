using SSW;
using System.Collections;
using UnityEngine;

public class KHG_DashHealAndDrawBuff : MonoBehaviour
{
    [Header("평타 적중 시 회복")]
    [SerializeField] private float healPercent = 0.3f;

    [Header("대쉬 적중 후 대쉬 데미지 증가")]
    [SerializeField] private float damagePercent = 0.15f;

    [Header("대쉬 데미지 증가 지속시간")]
    [SerializeField] private float buffDuration = 4f;

    private KHG_Dash dash;
    private KHG_nomalAttack normalAttack;
    private IHealable healable;

    private bool dashHitReady = false;

    private Coroutine damageBuffCoroutine;

    private void Awake()
    {
        dash = GetComponent<KHG_Dash>();

        normalAttack = GetComponentInChildren<KHG_nomalAttack>();

        healable = GetComponent<IHealable>();

        if (dash == null)

            if (normalAttack == null)

                if (healable == null) ;
    }

    private void OnEnable()
    {
        if (dash != null)
            dash.OnDashHitEnemy += OnDashHit;

        if (normalAttack != null)
            normalAttack.OnNormalAttackHit += OnNormalAttackHit;
    }

    private void OnDisable()
    {
        if (dash != null)
            dash.OnDashHitEnemy -= OnDashHit;

        if (normalAttack != null)
            normalAttack.OnNormalAttackHit -= OnNormalAttackHit;

        if (damageBuffCoroutine != null)
        {
            StopCoroutine(damageBuffCoroutine);
            damageBuffCoroutine = null;
        }
    }
    private void OnDashHit(GameObject target, float dashDamage)
    {
        Debug.Log("대쉬 적중! → 평타 적중 시 회복 가능");

        dashHitReady = true;

        if (damageBuffCoroutine != null)
        {
            StopCoroutine(damageBuffCoroutine);
        }

        damageBuffCoroutine = StartCoroutine(DamageBuffRoutine());
    }

    private void OnNormalAttackHit(GameObject target, float normalAttackDamage)
    {
        if (!dashHitReady)
            return;

        float healAmount = normalAttackDamage * healPercent;

        if (healable != null)
        {
            healable.Heal(healAmount);

            Debug.Log(
                "대쉬 적중 후 평타 적중 → 회복량: " +
                healAmount
            );
        }

        dashHitReady = false;
    }
    private IEnumerator DamageBuffRoutine()
    {
        while (dash != null && dash.IsDashing)
        {
            yield return null;
        }

        if (dash != null)
        {
            dash.SetDamageMultiplier(
                1f + damagePercent
            );

            Debug.Log("대쉬 데미지 +15% 적용");
        }

        yield return new WaitForSeconds(buffDuration);

        if (dash != null)
        {
            dash.SetDamageMultiplier(1f);

            Debug.Log("대쉬 데미지 증가 종료");
        }

        damageBuffCoroutine = null;
    }
}
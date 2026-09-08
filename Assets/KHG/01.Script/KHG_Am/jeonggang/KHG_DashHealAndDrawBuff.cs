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

    // 대쉬를 적중했는지
    private bool dashHitReady = false;

    private Coroutine damageBuffCoroutine;

    private void Awake()
    {
        dash = GetComponent<KHG_Dash>();

        normalAttack = GetComponentInChildren<KHG_nomalAttack>();

        healable = GetComponent<IHealable>();

        if (dash == null)
            Debug.LogError("KHG_Dash를 찾을 수 없습니다.");

        if (normalAttack == null)
            Debug.LogError("KHG_nomalAttack를 찾을 수 없습니다.");

        if (healable == null)
            Debug.LogError("IHealable을 찾을 수 없습니다.");
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

    // =========================
    // 대쉬 적중
    // =========================
    private void OnDashHit(GameObject target, float dashDamage)
    {
        Debug.Log("대쉬 적중! → 평타 적중 시 회복 가능");

        // 평타로 회복할 수 있도록 활성화
        dashHitReady = true;

        // 대쉬 데미지 +15%
        if (damageBuffCoroutine != null)
        {
            StopCoroutine(damageBuffCoroutine);
        }

        damageBuffCoroutine = StartCoroutine(DamageBuffRoutine());
    }

    // =========================
    // 평타 적중
    // =========================
    private void OnNormalAttackHit(GameObject target, float normalAttackDamage)
    {
        // 대쉬를 먼저 적중하지 않았다면 회복 X
        if (!dashHitReady)
            return;

        // 평타 피해량의 30%
        float healAmount = normalAttackDamage * healPercent;

        if (healable != null)
        {
            healable.Heal(healAmount);

            Debug.Log(
                "대쉬 적중 후 평타 적중 → 회복량: " +
                healAmount
            );
        }

        // 한 번 사용하면 효과 제거
        dashHitReady = false;
    }

    // =========================
    // 대쉬 데미지 +15%
    // =========================
    private IEnumerator DamageBuffRoutine()
    {
        // 현재 대쉬가 끝날 때까지 기다림
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
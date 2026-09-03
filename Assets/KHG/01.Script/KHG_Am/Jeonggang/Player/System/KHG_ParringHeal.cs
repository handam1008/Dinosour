using System.Collections;
using SSW;
using UnityEngine;

public class KHG_ParringHeal : MonoBehaviour
{
    [SerializeField] private float totalHealAmount = 10f; // 총 회복량
    [SerializeField] private float healDuration = 1f;    // 회복 지속 시간 (초)
    [SerializeField] private MonoBehaviour healableComponent;

    private IHealable healable;
    private KHG_Paring parry;
    private Coroutine healCoroutine;

    private void Awake()
    {
        parry = GetComponent<KHG_Paring>();

        if (healableComponent != null)
        {
            healable = healableComponent as IHealable;
        }

        if (healable == null) healable = GetComponent<IHealable>();
        if (healable == null) healable = GetComponentInParent<IHealable>();
        if (healable == null) healable = GetComponentInChildren<IHealable>();
    }

    private void OnEnable()
    {
        if (parry != null)
        {
            parry.OnParrySuccess -= HealOnParry;
            parry.OnParrySuccess += HealOnParry;
        }
    }

    private void OnDisable()
    {
        if (parry != null)
        {
            parry.OnParrySuccess -= HealOnParry;
        }
    }

    private void HealOnParry()
    {
        if (healable == null)
        {
            Debug.LogWarning("[KHG_ParringHeal] IHealable 컴포넌트를 찾지 못했습니다.");
            return;
        }

        if (healCoroutine != null)
        {
            StopCoroutine(healCoroutine);
            Debug.Log("[KHG_ParringHeal] 기존 회복을 취소하고 새로운 회복을 시작합니다.");
        }

        healCoroutine = StartCoroutine(HealOverTimeRoutine());
    }

    private IEnumerator HealOverTimeRoutine()
    {
        float timer = 0f;
        float healRate = totalHealAmount / healDuration; // 초당 회복 속도
        float logTimer = 0f;

        Debug.Log($"★ 패링 성공! 지속 회복 시작 (목표: {healDuration}초 동안 총 {totalHealAmount} 회복)");

        while (timer < healDuration)
        {
            float deltaTime = Time.deltaTime;
            float amountToHeal = healRate * deltaTime;

            healable.Heal(amountToHeal);

            timer += deltaTime;
            logTimer += deltaTime;

            if (logTimer >= 0.5f)
            {
                Debug.Log($"[회복 중...] 진행 시간: {timer:F1}s / {healDuration}s | 현재 체력: {healable.Current:F1}/{healable.Max}");
                logTimer = 0f;
            }

            yield return null;
        }

        Debug.Log($"★ 패링 회복 완료! (최종 체력: {healable.Current:F1}/{healable.Max})");
        healCoroutine = null;
    }
}
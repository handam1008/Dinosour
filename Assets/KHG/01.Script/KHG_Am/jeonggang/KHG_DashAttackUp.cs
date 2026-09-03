using UnityEngine;

public class KHG_DashAttackUp : MonoBehaviour
{
    [Header("연결할 스크립트 참조")]
    [SerializeField] private KHG_Paring paringScript;
    [SerializeField] private KHG_Dash dashScript; // 기존 대시 스크립트 타입으로 변경

    [Header("버프 설정")]
    [SerializeField] private float damageMultiplier = 2f;

    private bool isBuffActive = false;

    private void Awake()
    {
        if (paringScript == null) paringScript = GetComponent<KHG_Paring>();
        if (dashScript == null) dashScript = GetComponent<KHG_Dash>();
    }

    private void OnEnable()
    {
        if (paringScript != null)
        {
            paringScript.OnParrySuccess += ApplyBuff;
        }
    }

    private void OnDisable()
    {
        if (paringScript != null)
        {
            paringScript.OnParrySuccess -= ApplyBuff;
        }
    }

    private void ApplyBuff()
    {
        isBuffActive = true;

        if (dashScript != null)
        {
            dashScript.SetDamageMultiplier(damageMultiplier);
        }

        Debug.Log("패링 성공: 다음 대시 데미지 2배 적용");
    }

    public void ConsumeBuff()
    {
        if (!isBuffActive) return;

        isBuffActive = false;
        if (dashScript != null)
        {
            dashScript.SetDamageMultiplier(1f);
        }

        Debug.Log("대시 버프 소모 완료");
    }

}

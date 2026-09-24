using System.Collections.Generic;
using SSW;
using UnityEngine;

public class HealField : MonoBehaviour
{
    [SerializeField] float _radius = 1.5f;
    [SerializeField] float _chargeTime = 1f;     // 설치 후 터지기까지 시간
    [SerializeField] float _healRatio = 0.20f;   // 최대 체력의 20%
    [SerializeField] GameObject _healEffect;
    [SerializeField] HealDial _dial;

    void Start()
    {
        _dial.Play(_chargeTime, _radius);
        Invoke(nameof(Burst), _chargeTime);
    }

    void Burst()
    {
        // 콜라이더가 여러 개인 캐릭터도 한 번만 회복되게
        var targets = new HashSet<IHealable>();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, _radius))
        {
            IHealable healable = hit.GetComponentInParent<IHealable>();
            if (healable != null) targets.Add(healable);
        }

        foreach (IHealable healable in targets)
            healable.Heal(healable.Max * _healRatio);

        Instantiate(_healEffect, transform.position, Quaternion.identity).transform.localScale = Vector3.one * _radius;

        Destroy(gameObject);   // 아무도 없어도 사라짐
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
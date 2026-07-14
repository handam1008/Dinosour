using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class KDH_Ammo : MonoBehaviour
{
    [SerializeField] private KDH_Tanchang tanchang;
    [SerializeField] private GameObject ammoPivot;
    private float minGageValue = 0;
    private float maxGageValue = 3;
    private float _time = 0f;

    private void Update()
    {
        _time += Time.deltaTime;

        if (_time > 3f)
        {
            StartCoroutine(ChargeGage());
            return;
        }
    }

    private void Gage()
    {
        ammoPivot.transform.DOScaleX(GagePer(), 1f).SetEase(Ease.InOutCubic);
    }

    private float GagePer()
    {
        return minGageValue / maxGageValue;
    }
    
    private IEnumerator ChargeGage()
    {
        yield return new WaitForSeconds(1f);
        Gage();
    }
}

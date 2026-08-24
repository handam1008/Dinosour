using System;
using System.Collections;
using UnityEngine;

public class KHG_Invincible : MonoBehaviour
{
    private KHG_Dash Khg_Dash;

    private void Awake()
    {
        Khg_Dash = GetComponent<KHG_Dash>();
    }
    private void Invinchible()
    {
        StartCoroutine(Invcroutine());
    }

    private IEnumerator Invcroutine()
    {
        if(Khg_Dash != null)

            yield return new WaitForSeconds(Khg_Dash._dashDuration);

    }

}

using System;
using UnityEngine;

public class KHG_Invincible : MonoBehaviour
{
    private KHG_Dash Khg_Dash;

    private void Awake()
    {
        Khg_Dash = GetComponent<KHG_Dash>();
    }
}

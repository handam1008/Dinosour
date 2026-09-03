using UnityEngine;

public class KHG_DashAttackUp : MonoBehaviour
{
    private KHG_Dash _dash;
    private KHG_Paring _paring;

    private void Awake()
    {
        _dash = GetComponent<KHG_Dash>();
        _paring = GetComponent<KHG_Paring>();
    }

}

using UnityEngine;

public class KDH_GameManager : MonoBehaviour
{
    public static KDH_GameManager instanec;

    public GameObject player;

    private void Awake()
    {
        instanec = this;
    }
}

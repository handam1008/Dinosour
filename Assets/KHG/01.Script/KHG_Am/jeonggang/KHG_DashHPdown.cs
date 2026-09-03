using SSW;
using UnityEngine;

public class KHG_DashHPdown : MonoBehaviour
{
    [SerializeField] private float dashspeed;
    [SerializeField] private float speed;

    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }
    private void Update()
    {
        if (playerController != null)
        {
            
        }
    }
}

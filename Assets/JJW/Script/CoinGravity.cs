using UnityEngine;
public class CoinGravity : MonoBehaviour
{
    private float timeInAir = 0f;
   [SerializeField] private float gravityDelay = 0.15f;
   [SerializeField] private float extraGravity = 30f;
   private Rigidbody2D rb;

   private void Awake()
   {
       rb = GetComponent<Rigidbody2D>();
   }

   private void Update()
   {
       timeInAir += Time.deltaTime;
       OnGravity();
   }

   private void OnDisable()
   {
       timeInAir = 0f;
   }

   private void OnGravity()
   {
       if (timeInAir > gravityDelay)
       {
           rb.AddForceY(-extraGravity);
       }
   }
}

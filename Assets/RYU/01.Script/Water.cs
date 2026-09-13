using SSW;
using UnityEngine;

namespace RYU._01.Script
{
    public class Water : MonoBehaviour
    {
       
        [SerializeField] private float upTimer = 2f;        
        [SerializeField] private float intervalStep = 0.1f; 
        [SerializeField] private float minInterval = 0.5f; 
        [SerializeField] private float riseAmount = 1f;    

   
        [SerializeField] private float slowAmount = 2f;  

        private float timer;

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < upTimer) return;
            UpWater();
            upTimer = Mathf.Max(minInterval, upTimer - intervalStep);
            timer = 0f;
        }

        private void UpWater()
        {
            transform.position += new Vector3(0f, riseAmount, 0f);
        }

        
        private void OnTriggerStay2D(Collider2D other)
        {
            ISlowable target = other.GetComponentInParent<ISlowable>();
            if (target == null) return; 

            target.ApplySlow(slowAmount, 0.1f);
        }
    }
}

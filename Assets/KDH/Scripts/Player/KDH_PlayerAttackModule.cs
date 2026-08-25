using KDH.Scripts.Bullet;
using KDH.Scripts.Gun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KDH.Scripts.Player
{
    public class KDH_PlayerAttackModule : MonoBehaviour
    {
        public void FollowMouse(Camera cam, Transform visual)
        {
            Vector3 mouseWorldPos = FindMousePosition(cam, visual);
            Vector3 dir = (mouseWorldPos - transform.position).normalized;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private Vector3 FindMousePosition(Camera cam, Transform visual)
        {
            Vector3 world = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        
            Vector3 scale = visual.localScale;
        
            scale.y = world.x < transform.position.x ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            visual.localScale = scale;

            return world;
        }

        public Vector2 GetFireDirection(Camera cam, Transform visual, Transform gunPos)
        {
            return ((Vector2)(FindMousePosition(cam, visual) - gunPos.position)).normalized;
        }
        
        public void Shoot(KDH_SpawnBullet spawnBullet, KDH_Gun gun, Camera cam, Transform visual, Transform gunPos, bool  isUpgraded)
        {
            if (spawnBullet.bullets.Count <= 0) return;
        
            GameObject bullet = spawnBullet.bullets.Pop();
            bullet.transform.position = gun.GunPos.position;
        
            KDH_Bullet bulletScript = bullet.GetComponent<KDH_Bullet>();
            bulletScript.IsUpgraded = isUpgraded;
            bulletScript.Init(GetFireDirection(cam, visual, gunPos));
            
            bullet.SetActive(true);
        }
    }
}
using UnityEngine;

namespace RYU._01.Script
{
    public class PotionHUD : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer potionIcon;
        

        private void Start()
        {
            Camera view = Camera.main;
            if (view == null) return;

            transform.SetParent(view.transform, false);
            transform.position = new Vector3(transform.position.x, transform.position.y, 1);
        }

        public void SetPocket(AbstractPotion potion)
        {
            if (potionIcon == null) return;

            potionIcon.sprite = potion != null ? potion.sprite : null;
            potionIcon.enabled = potion != null;
        }
    }
}

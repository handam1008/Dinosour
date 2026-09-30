using SSW;
using UnityEngine;

namespace RYU._01.Script
{
    // 온라인 마녀: 서버가 동기화한 주머니 포션을 PotionHUD에 보여준다 (내 캐릭터만)
    public class NetPocketHUD : MonoBehaviour
    {
        [SerializeField] private NetPlayer player;
        [SerializeField] private NetStock stock;
        [SerializeField] private PotionHUD hud;
        private int _shown = -2;

        private void Update()
        {
            bool mine = player.IsSpawned && player.IsOwner && player.Job == PlayerJob.Witch;
            if (hud.gameObject.activeSelf != mine) hud.gameObject.SetActive(mine);
            if (!mine) return;

            int kind = player.Cast.Pocket;
            if (kind == _shown) return;
            _shown = kind;
            hud.SetPocket(kind >= 0 ? stock.At(kind) : null);
        }

        // PotionHUD는 카메라 밑으로 옮겨가서, 캐릭터가 사라져도 남는다 → 같이 지워준다
        private void OnDestroy()
        {
            if (hud != null) Destroy(hud.gameObject);
        }
    }
}

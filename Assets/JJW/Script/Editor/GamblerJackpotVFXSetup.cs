using JJW.Script.Jackpot;
using UnityEditor;
using UnityEngine;

namespace JJW.Script.Editor
{
    public static class GamblerJackpotVFXSetup
    {
        [MenuItem("Tools/JJW/Gambler/Setup Jackpot VFX")]
        private static void SetupJackpotVFX()
        {
            GameObject selected = Selection.activeGameObject;

            if (selected == null)
            {
                Debug.LogError("Hierarchy에서 Player 오브젝트를 선택해 주세요.");
                return;
            }

            JackpotDivision division =
                selected.GetComponent<JackpotDivision>();

            if (division == null)
            {
                division = selected.GetComponentInParent<JackpotDivision>();
            }

            if (division == null)
            {
                Debug.LogError(
                    "선택한 오브젝트 또는 부모에서 JackpotDivision을 찾지 못했습니다.",
                    selected);
                return;
            }

            GameObject player = division.gameObject;
            GamblerJackpotVFX controller =
                player.GetComponent<GamblerJackpotVFX>();

            if (controller == null)
            {
                controller = Undo.AddComponent<GamblerJackpotVFX>(player);
            }

            EditorUtility.SetDirty(player);
            Selection.activeGameObject = player;

            Debug.Log(
                "GamblerJackpotVFX 연결 완료. " +
                "InvincibleShieldEffect 이름의 자식 오브젝트도 자동 연결됩니다.",
                controller);
        }
    }
}

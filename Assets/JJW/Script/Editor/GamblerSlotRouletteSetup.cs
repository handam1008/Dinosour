using JJW.Script.Jackpot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JJW.Script.Editor
{
    public static class GamblerSlotRouletteSetup
    {
        [MenuItem("JJW/Gambler/Create Slot Roulette On Selected Player")]
        private static void CreateSlotRoulette()
        {
            GameObject selected = Selection.activeGameObject;

            if (selected == null)
            {
                EditorUtility.DisplayDialog(
                    "Gambler Slot Roulette",
                    "Hierarchy에서 도박사 Player 오브젝트를 먼저 선택하세요.",
                    "확인");
                return;
            }

            JackpotDivision division =
                selected.GetComponentInChildren<JackpotDivision>(true);

            if (division == null)
            {
                division = selected.GetComponentInParent<JackpotDivision>(true);
            }

            if (division == null)
            {
                EditorUtility.DisplayDialog(
                    "Gambler Slot Roulette",
                    "선택한 오브젝트 주변에서 JackpotDivision을 찾지 못했습니다.",
                    "확인");
                return;
            }

            Transform rouletteTransform = FindChildByName(
                division.transform,
                "Roulette");

            if (rouletteTransform == null)
            {
                GameObject rouletteObject = new GameObject("Roulette");
                Undo.RegisterCreatedObjectUndo(
                    rouletteObject,
                    "Create Gambler Roulette");

                rouletteTransform = rouletteObject.transform;
                rouletteTransform.SetParent(division.transform, false);
                rouletteTransform.localPosition = new Vector3(0f, 1.65f, 0f);
            }

            GamblerSlotRoulette slotRoulette =
                rouletteTransform.GetComponent<GamblerSlotRoulette>();

            if (slotRoulette == null)
            {
                slotRoulette = Undo.AddComponent<GamblerSlotRoulette>(
                    rouletteTransform.gameObject);
            }

            SerializedObject serializedDivision =
                new SerializedObject(division);

            SerializedProperty slotProperty =
                serializedDivision.FindProperty("slotRoulette");

            if (slotProperty != null)
            {
                slotProperty.objectReferenceValue = slotRoulette;
                serializedDivision.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(division);
            EditorUtility.SetDirty(slotRoulette);
            EditorSceneManager.MarkSceneDirty(division.gameObject.scene);

            Selection.activeGameObject = rouletteTransform.gameObject;

            EditorUtility.DisplayDialog(
                "Gambler Slot Roulette",
                "슬롯 룰렛 연결이 완료되었습니다. Play Mode에서 룰렛 동전을 발사해 확인하세요.",
                "확인");
        }

        [MenuItem("JJW/Gambler/Create Slot Roulette On Selected Player", true)]
        private static bool ValidateCreateSlotRoulette()
        {
            return Selection.activeGameObject != null;
        }

        private static Transform FindChildByName(
            Transform root,
            string targetName)
        {
            Transform[] children =
                root.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child.name == targetName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}

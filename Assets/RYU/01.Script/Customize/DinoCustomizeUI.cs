using UnityEngine;
using UnityEngine.UI;

namespace RYU._01.Script.Customize
{
    public class DinoCustomizeUI : MonoBehaviour
    {
        [Header("탭 버튼")]
        [SerializeField] private Button boyTab;
        [SerializeField] private Button girlTab;

        [Header("패널")]
        [SerializeField] private GameObject boyPanel;
        [SerializeField] private GameObject girlPanel;

        private void Awake()
        {
            Bind(boyPanel);
            Bind(girlPanel);

            if (boyTab != null) boyTab.onClick.AddListener(() => ShowPanel(DinoGender.Male));
            if (girlTab != null) girlTab.onClick.AddListener(() => ShowPanel(DinoGender.Female));
        }

        private void OnEnable()
        {
            DinoSkin saved = DinoSkinStorage.Load();
            ShowPanel(saved != null ? saved.gender : DinoGender.Male);
        }

        private static void Bind(GameObject panel)
        {
            if (panel == null) return;

            foreach (DinoSkinTag tag in panel.GetComponentsInChildren<DinoSkinTag>(true))
            {
                Button button = tag.GetComponent<Button>();
                if (button == null || string.IsNullOrEmpty(tag.id)) continue;

                string id = tag.id;
                button.onClick.AddListener(() => DinoSkinStorage.Save(id));
            }
        }

        private void ShowPanel(DinoGender gender)
        {
            if (boyPanel != null) boyPanel.SetActive(gender == DinoGender.Male);
            if (girlPanel != null) girlPanel.SetActive(gender == DinoGender.Female);
        }
    }
}

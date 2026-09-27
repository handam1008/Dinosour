using UnityEngine;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    [Header("칭호 버튼")]
    [SerializeField] private Button[] titleButtons;

    [Header("칭호 이름")]
    [SerializeField] private string[] titleNames;

    private const string TitleKey = "SelectedTitle";

    private void Awake()
    {
        BindButtons();
    }

    private void OnEnable()
    {
        RefreshSelectedTitle();
    }

    private void BindButtons()
    {
        if (titleButtons == null) return;

        for (int i = 0; i < titleButtons.Length; i++)
        {
            if (titleButtons[i] == null) continue;

            int index = i;

            titleButtons[i].onClick.AddListener(() =>
            {
                SelectTitle(index);
            });
        }
    }

    private void SelectTitle(int index)
    {
        if (titleNames == null || index < 0 || index >= titleNames.Length)
            return;

        PlayerPrefs.SetString(TitleKey, titleNames[index]);
        PlayerPrefs.Save();

        Debug.Log($"칭호 선택: {titleNames[index]}");

        RefreshSelectedTitle();
    }

    private void RefreshSelectedTitle()
    {
        string selectedTitle = PlayerPrefs.GetString(TitleKey, "");

        for (int i = 0; i < titleButtons.Length; i++)
        {
            if (titleButtons[i] == null) continue;

            bool selected =
                i < titleNames.Length &&
                titleNames[i] == selectedTitle;

            titleButtons[i].interactable = !selected;
        }
    }

    public string GetSelectedTitle()
    {
        return PlayerPrefs.GetString(TitleKey, "");
    }
}

using UnityEngine;

public class KHG_TitleManager : MonoBehaviour
{
    public KHG_TitleData equippedTitle;

    [Header("기본 칭호")]
    public KHG_TitleData eggTitle;

    [Header("티어별 칭호 SO")]
    public KHG_TitleData extinctTitle;
    public KHG_TitleData iceAgeTitle;
    public KHG_TitleData meteorTitle;
    public KHG_TitleData magmaTitle;
    public KHG_TitleData dinoTitle;
    public KHG_TitleData monkeyTitle;

    private void Start()
    {
        GiveDefaultTitle();
    }

    private void GiveDefaultTitle()
    {
        if (eggTitle == null)
        {
            Debug.LogWarning("기본 칭호 [알] SO가 연결되지 않았습니다.");
            return;
        }

        equippedTitle = eggTitle;

        Debug.Log("기본 칭호: " + equippedTitle.titleName);
    }

    public void ChangeTierTitle(string tier)
    {
        KHG_TitleData newTitle = null;

        switch (tier)
        {
            case "Extinct":
                newTitle = extinctTitle;
                break;

            case "IceAge":
                newTitle = iceAgeTitle;
                break;

            case "Meteor":
                newTitle = meteorTitle;
                break;

            case "Magma":
                newTitle = magmaTitle;
                break;

            case "Dino":
                newTitle = dinoTitle;
                break;

            case "Monkey":
                newTitle = monkeyTitle;
                break;

            default:
                Debug.LogWarning("존재하지 않는 티어입니다: " + tier);
                return;
        }

        if (newTitle == null)
        {
            Debug.LogWarning(
                "해당 티어의 칭호 SO가 연결되지 않았습니다: " + tier
            );

            return;
        }

        equippedTitle = newTitle;

        Debug.Log(
            "티어 변경: " + tier +
            " / 현재 칭호: " + equippedTitle.titleName
        );
    }

    public string GetCurrentTitle()
    {
        if (equippedTitle == null)
            return "";

        return equippedTitle.titleName;
    }
}
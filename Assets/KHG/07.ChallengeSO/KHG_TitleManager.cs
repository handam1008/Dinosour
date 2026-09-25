using System.Collections.Generic;
using UnityEngine;

public class KHG_TitleManager : MonoBehaviour
{
    public List<KHG_TitleData> unlockedTitles = new List<KHG_TitleData>();
    public KHG_TitleData equippedTitle;

    [Header("티어별 칭호 SO")]
    public KHG_TitleData extinctTitle;
    public KHG_TitleData iceAgeTitle;
    public KHG_TitleData meteorTitle;
    public KHG_TitleData magmaTitle;
    public KHG_TitleData dinoTitle;
    public KHG_TitleData monkeyTitle;

    public void ChangeTierTitle(string tier)
    {
        // 이전 티어 칭호 삭제
        unlockedTitles.Clear();
        equippedTitle = null;

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
        }

        if (newTitle == null)
        {
            Debug.LogWarning("해당 티어의 칭호 SO가 연결되지 않았습니다: " + tier);
            return;
        }

        unlockedTitles.Add(newTitle);
        equippedTitle = newTitle;

        Debug.Log(newTitle.titleName + " 칭호로 변경!");
    } // ChangeTierTitle 끝

    [ContextMenu("테스트 - 빙하기급")]
    private void TestIceAge()
    {
        ChangeTierTitle("IceAge");
    }

    [ContextMenu("테스트 - 마그마급")]
    private void TestMagma()
    {
        ChangeTierTitle("Magma");
    }
}
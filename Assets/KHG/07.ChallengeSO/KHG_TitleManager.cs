using System.Collections.Generic;
using UnityEngine;

public class KHG_TitleManager : MonoBehaviour
{
    public List<KHG_TitleData> unlockedTitles = new List<KHG_TitleData>();
    public KHG_TitleData equippedTitle;

    public KHG_TitleData iceTierTitle;
    public KHG_TitleData volcanoTierTitle;
    public void Unlock(KHG_TitleData title)
    {
        if (unlockedTitles.Contains(title))
            return;

        unlockedTitles.Add(title);
        Debug.Log(title.titleName + " 칭호 획득!");
    }

    public void TierUpToIce()
    {
        Unlock(iceTierTitle);
    }
    public void CheckTierAndUnlock(string currentTier)
    {
        switch (currentTier)
        {
            case "빙하기":
                Unlock(iceTierTitle);
                break;

            case "화산급":
                Unlock(volcanoTierTitle);
                break;
        }
    }
    
}
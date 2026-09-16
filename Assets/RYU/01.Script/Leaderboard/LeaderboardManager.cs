using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RYU._01.Script.Leaderboard;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button saveScoreBtn;
    [SerializeField] private Button loadScoreBtn;
    [SerializeField] private TMP_InputField scoreIf;
    [SerializeField] private Button fetchAllScoreBtn;
    [SerializeField] private Transform rankUserInfoParent;
    [SerializeField] private GameObject rankUserInfoPrefab;
    
    private const string LeaderboardId = "Ranking";


    private void Start()
    {
        BindingUIEvents();
    }

    private void BindingUIEvents()
    {
        saveScoreBtn.onClick.AddListener(async () => await SaveScore(int.Parse(scoreIf.text)));
        fetchAllScoreBtn.onClick.AddListener(async () => await LoadAllScore());
    }

    private async Task LoadAllScore()
    {
        RemoveScores();
        
        var options = new GetScoresOptions() { Limit = 1000 };
        var response = await UnityServices.Instance.GetLeaderboardsService().GetScoresAsync(LeaderboardId, options);

        foreach (var entry in response.Results)
        {
            var userInfo= Instantiate(rankUserInfoPrefab, rankUserInfoParent);
            userInfo.GetComponent<RankUserInfo>().SetUserInfo(entry.Rank, entry.Score, entry.Tier, entry.PlayerName);
        }
        
    }
    
    public void RemoveScores()
    {
        for (int i = 0; i < rankUserInfoParent.childCount; i++)
        {
            Destroy(rankUserInfoParent.GetChild(i).gameObject);
        }
    }

    private async Task SaveScore(int score)
    {
        var response = await UnityServices.Instance.GetLeaderboardsService().AddPlayerScoreAsync(LeaderboardId, score);
        Debug.Log(JsonConvert.SerializeObject(response));
    }
}

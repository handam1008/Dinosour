using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RYU._01.Script.Leaderboard
{
    
   
    public class LeaderboardManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button saveScoreBtn;
        [SerializeField] private Button logoutBtn;
        [SerializeField] private TMP_InputField scoreIf;
        [SerializeField] private Button fetchAllScoreBtn;
        [SerializeField] private Transform rankUserInfoParent;
        [SerializeField] private GameObject rankUserInfoPrefab;

        private readonly Dictionary<string, string> _tierName =  new Dictionary<string, string>
        {
            {"Extinct" , "멸종급"},
            {"IceAge" , "빙하기급" },
            {"Meteor" , "메테오급"},
            {"Magma" , "마그마급" },
            {"Dino" , "공룡급"},
            {"Monkey" , "원숭이급" },
        };
    
        private const string LeaderboardId = "Ranking";


        private void Start()
        {
            BindingUIEvents();
        }

        private void BindingUIEvents()
        {
            saveScoreBtn.onClick.AddListener(async () => await SaveScore(int.Parse(scoreIf.text)));
            fetchAllScoreBtn.onClick.AddListener(async () => await LoadAllScore());
            logoutBtn.onClick.AddListener(async () => await SignOut());
        }

        private async Task SignOut()
        {
            // true = 저장된 로그인 기록도 지움. 안 지우면 로그아웃해도 자동 로그인됨
            AuthenticationService.Instance.SignOut(true);
            SceneManager.LoadScene("Login");
        }

        public async Task LoadAllScore()
        {
            RemoveScores();
        
            var options = new GetScoresOptions { Limit = 1000 };
            var response = await UnityServices.Instance.GetLeaderboardsService().GetScoresAsync(LeaderboardId, options);

            foreach (var entry in response.Results)
            {
               // entry.Tier == "빙하기급" -> 원래 있던 창호 자우고 새로운 창호 지급
                
                var userInfo = Instantiate(rankUserInfoPrefab, rankUserInfoParent);
                var go = userInfo.GetComponent<RankUserInfo>();
                
                string tier = TierByRank(entry.Rank);
                go.SetUserInfo(entry.Rank + 1, entry.Score, _tierName[tier], entry.PlayerName.Split("#")[0]);
            
            
                //Monkey = 0, Dino = 1, Magma = 2, Meteor = 3, IceAge = 4, Extinct = 5
                   
                switch (tier)
                {
                    case "Extinct":
                        go.SetTierImage(go.tier[5]);
                        break;
                    case  "IceAge":
                        go.SetTierImage(go.tier[4]);
                        break;
                    case "Meteor":
                        go.SetTierImage(go.tier[3]);
                        break;
                    case  "Magma":
                        go.SetTierImage(go.tier[2]);
                        break;
                    case "Dino":
                        go.SetTierImage(go.tier[1]);
                        break;
                    case  "Monkey":
                        go.SetTierImage(go.tier[0]);
                        break;
               
                }
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
        
        private static string TierByRank(int rank)   // entry.Rank 그대로 (0부터)
        {
            if (rank < 1)  return "Extinct";   // 1위
            if (rank < 6)  return "IceAge";    // 2~6위
            if (rank < 16) return "Meteor";    // 7~16위
            if (rank < 31) return "Magma";     // 17~31위
            if (rank < 48) return "Dino";      // 32~48위
            return "Monkey";                   // 나머지
        }
        
        [ContextMenu("Test Win 4:1")]
        private void TestWin() => TestReport(true, 4, 1);

        [ContextMenu("Test Lose 1:4")]
        private void TestLose() => TestReport(false, 1, 4);

        private async void TestReport(bool isWin, int mySets, int opponentSets)
        {
            try
            {
                var result = await MatchReporter.ReportAsync(isWin, mySets, opponentSets, null);
                Debug.Log($"Cloud Code 결과: {result.delta:+#;-#;0}점, 현재 {result.score}점");
                await LoadAllScore();
            }
            catch (CloudCodeException e)
            {
                Debug.LogError(e.ToString());
            }
        }
    }
}

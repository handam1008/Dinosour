using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UI;

namespace RYU._01.Script.Leaderboard
{
    public class LeaderboardManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button saveScoreBtn;
        [SerializeField] private TMP_InputField scoreIf;
        [SerializeField] private Button fetchAllScoreBtn;
        [SerializeField] private Transform rankUserInfoParent;
        [SerializeField] private GameObject rankUserInfoPrefab;

        private Dictionary<string, string> tierName =  new Dictionary<string, string>
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
        }

        private async Task LoadAllScore()
        {
            RemoveScores();
        
            var options = new GetScoresOptions { Limit = 1000 };
            var response = await UnityServices.Instance.GetLeaderboardsService().GetScoresAsync(LeaderboardId, options);

            foreach (var entry in response.Results)
            {
                var userInfo = Instantiate(rankUserInfoPrefab, rankUserInfoParent);
                var go = userInfo.GetComponent<RankUserInfo>();
                go.SetUserInfo(entry.Rank + 1, entry.Score, tierName[entry.Tier], entry.PlayerName.Split("#")[0]);
            
            
                //Monkey = 0, Dino = 1, Magma = 2, Meteor = 3, IceAge = 4, Extinct = 5
                   
                switch (entry.Tier)
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
    }
}

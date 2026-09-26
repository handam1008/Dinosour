using System;
using System.Threading.Tasks;
using RYU._01.Script.Leaderboard;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RYU._01.Script
{
    public class PlayerInfoPanel : MonoBehaviour
    {
        [SerializeField] private Image dinoSourImage;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI jobText;
        [SerializeField] private TextMeshProUGUI scoreText;
        
        private string leaderboardId = "Ranking";


        private void Start()
        {
            GetPlayerName();
            GetJobNmae();
            _ = GetScoreText();
        }

        private async Task GetScoreText()
        {
            try
            {
                var entry  = await UnityServices.Instance.GetLeaderboardsService().GetPlayerScoreAsync(leaderboardId);
                scoreText.SetText(entry.Score.ToString());
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void GetJobNmae()
        {
            //여기 코드 추가좀
        }

        public void GetPlayerName()
        {
            playerNameText.SetText(AuthenticationService.Instance.PlayerName.Split('#')[0]);
        }
    }
}
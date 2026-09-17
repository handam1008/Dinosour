using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RYU._01.Script.Leaderboard
{
    public class RankUserInfo : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _rank, _playerName, _score, _tier;
        public Image _tierImage;
        
        
        [Header("TimerImage")]
        public Sprite[] tier;
           
        

        public void SetUserInfo(int rank,  int score, string tier, string playerName)
        {
            _rank.text = rank.ToString();
            _playerName.text = playerName;
            _score.text = score.ToString();
            _tier.text = tier;
        }

        public void SetTierImage(Sprite tier)
        {
            _tierImage.sprite = tier;
        }

    }
}
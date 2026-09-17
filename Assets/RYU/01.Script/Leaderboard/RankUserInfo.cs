using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RYU._01.Script.Leaderboard
{
    public class RankUserInfo : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _rank, _playerName, _score, _tier;
        [SerializeField] private Image _tierImage;

        public void SetUserInfo(int rank,  int score, string tier, string playerName)
        {
            _rank.text = rank.ToString();
            _playerName.text = playerName;
            _score.text = score.ToString();
            _tier.text = tier;
        }

    }
}
using System;
using System.Collections;
using System.Threading.Tasks;
using SSW;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.UI;

namespace RYU._01.Script.Leaderboard
{
    public class MatchResultView : MonoBehaviour
    {
        private const string LeaderboardId = "Ranking";

        private struct Record
        {
            public bool Ranked;  
            public double Score;
            public int Tier;
        }

        private GameObject root;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text roundScoreText;
        [SerializeField] private Image tierIcon;
        [SerializeField] private TMP_Text tierText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Button mainMenuButton;

        [Tooltip("0 원숭이, 1 공룡, 2 마그마, 3 메테오, 4 빙하기, 5 멸종")]
        [SerializeField] private Sprite[] tierSprites = new Sprite[6];
        [SerializeField] private Sprite unrankedSprite;
        [SerializeField] private float timeout = 8f;

        [Header("로딩 (점수가 올 때까지 결과 대신 보여줌)")]
        [SerializeField] private GameObject content;   // 승패·스코어·티어·점수를 묶은 부모
        [SerializeField] private GameObject loading;   // "점수 계산 중..." 표시

        [Header("연출")]
        [SerializeField] private float holdBeforeCount = 0.5f;   // 경기 전 점수를 보여주는 시간
        [SerializeField] private float countDuration = 1f;
        [SerializeField] private SoundCue countSound;
        [SerializeField] private SoundCue tierUpSound;
        [SerializeField] private SoundCue tierDownSound;

        private Task<Record?> _before;
        private float _shownAt;
        private bool _waiting;
        private bool _animating;
        private bool _shown;
        private Vector3 _iconScale = Vector3.one;

        private void Awake()
        {
            if (root == null) root = gameObject;
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(() => NetGame.Current?.Exit());
            if (tierIcon != null) _iconScale = tierIcon.transform.localScale;

            if (_before == null) _before = LoadRecordAsync();

            if (!_shown) root.SetActive(false);
        }

        private void OnDestroy()
        {
            MatchReporter.Reported -= OnReported;
        }

        public void Show(MatchState state, NetGame game)
        {
            _shown = true;
            if (root == null) root = gameObject;
            if (_before == null) _before = LoadRecordAsync();
            root.SetActive(true);
            _shownAt = Time.unscaledTime;

            bool won = state.Winner == game.LocalId;
            bool first = game.LocalId == state.First;
            int mine = first ? state.FirstSets : state.SecondSets;
            int theirs = first ? state.SecondSets : state.FirstSets;

            resultText.SetText(state.Reason switch
            {
                MatchEnd.Draw => "무승부",
                MatchEnd.Left => "상대가 나갔습니다",
                MatchEnd.Surrender => won ? "상대가 항복했습니다" : "항복",
                _ => won ? "승리!" : "패배",
            });
            roundScoreText.SetText($"{mine} : {theirs}");

          
            if (state.Reason == MatchEnd.Knockout)
            {
                SetLoading(true);
                _waiting = true;
                MatchReporter.Reported += OnReported;
                StartCoroutine(Timeout());
            }
            else Reveal();

            _ = ShowBeforeAsync();
        }

        private void SetLoading(bool value)
        {
            if (loading != null) loading.SetActive(value);
            if (content != null) content.SetActive(!value);
        }

        private void Reveal()
        {
            SetLoading(false);
            _shownAt = Time.unscaledTime;
        }

        private async Task ShowBeforeAsync()
        {
            Record? before = await _before;
            if (this == null || _animating) return;

            if (before.HasValue) ShowRecord(before.Value, null);
            else scoreText.SetText(_waiting ? "점수 계산 중..." : "점수를 불러오지 못했습니다");
        }

        private void OnReported(MatchReportResult result)
        {
            MatchReporter.Reported -= OnReported;
            _waiting = false;
            _ = AnimateAsync(result.delta);
        }

        private IEnumerator Timeout()
        {
            yield return new WaitForSecondsRealtime(timeout);
            if (!_waiting) yield break;

            MatchReporter.Reported -= OnReported;
            _waiting = false;
            Reveal();
            scoreText.SetText("점수를 불러오지 못했습니다");
        }

        private async Task AnimateAsync(int delta)
        {
            Record? before = await _before;
            Record? after = await LoadRecordAsync();
            if (this == null) return;

            if (!after.HasValue || !after.Value.Ranked)
            {
                Reveal();
                scoreText.SetText("점수를 불러오지 못했습니다");
                return;
            }

            Record start = before ?? new Record { Ranked = true, Score = after.Value.Score - delta, Tier = after.Value.Tier };
            _animating = true;
            Reveal();
            StartCoroutine(Play(start, after.Value, delta));
        }

        private IEnumerator Play(Record from, Record to, int delta)
        {
            ShowRecord(from, null);
            float wait = holdBeforeCount - (Time.unscaledTime - _shownAt);
            if (wait > 0f) yield return new WaitForSecondsRealtime(wait);

            if (countSound != null && GameAudio.Current != null) GameAudio.Current.PlaySfx(countSound);

            double start = from.Ranked ? from.Score : 0d;
            float elapsed = 0f;
            while (elapsed < countDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / countDuration), 3f);   // 끝에서 천천히
                scoreText.SetText(FormatScore(start + (to.Score - start) * t, delta));
                yield return null;
            }
            scoreText.SetText(FormatScore(to.Score, delta));

            if (from.Ranked != to.Ranked || from.Tier != to.Tier) yield return ChangeTier(from, to);
        }

        private IEnumerator ChangeTier(Record from, Record to)
        {
            bool up = !from.Ranked || to.Tier > from.Tier;
            SoundCue cue = up ? tierUpSound : tierDownSound;
            if (cue != null && GameAudio.Current != null) GameAudio.Current.PlaySfx(cue);

            Transform icon = tierIcon != null ? tierIcon.transform : tierText.transform;
            Vector3 baseScale = tierIcon != null ? _iconScale : Vector3.one;

            yield return Scale(icon, baseScale, Vector3.zero, 0.15f);

            SetTierVisual(to);
            tierText.SetText($"{TierName(to)} {(up ? "승급!" : "강등")}");

            yield return Scale(icon, Vector3.zero, baseScale * 1.35f, 0.18f);
            yield return Scale(icon, baseScale * 1.35f, baseScale, 0.12f);

            yield return new WaitForSecondsRealtime(1.2f);
            tierText.SetText(TierName(to));
        }

        private static IEnumerator Scale(Transform target, Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.LerpUnclamped(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            target.localScale = to;
        }

        private static async Task<Record?> LoadRecordAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
                    return null;

                var entry = await UnityServices.Instance.GetLeaderboardsService().GetPlayerScoreAsync(LeaderboardId);
                return new Record { Ranked = true, Score = entry.Score, Tier = RankTier.FromRank(entry.Rank) };
            }
            catch (Exception e)
            {
                bool unranked = e.Message.Contains("404") || e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase);
                return unranked ? new Record { Ranked = false } : (Record?)null;
            }
        }

        private void ShowRecord(Record record, int? delta)
        {
            SetTierVisual(record);
            scoreText.SetText(record.Ranked ? FormatScore(record.Score, delta) : "-");
        }

        private void SetTierVisual(Record record)
        {
            tierText.SetText(TierName(record));
            if (tierIcon == null) return;

            Sprite sprite = record.Ranked
                ? (record.Tier >= 0 && record.Tier < tierSprites.Length ? tierSprites[record.Tier] : null)
                : unrankedSprite;
            tierIcon.sprite = sprite;
            tierIcon.enabled = sprite != null;
        }

      

        private static Record Ranked(double score, int tier) => new Record { Ranked = true, Score = score, Tier = tier };

        private void RunTest(bool won, Record from, Record to, int delta)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[MatchResultView] 플레이 모드에서만 테스트할 수 있습니다.");
                return;
            }

            _shown = true;
            if (root == null) root = gameObject;
            root.SetActive(true);
            StopAllCoroutines();
            if (tierIcon != null) tierIcon.transform.localScale = _iconScale;

            _animating = true;
            Reveal();
            resultText.SetText(won ? "승리!" : "패배");
            roundScoreText.SetText(won ? "4 : 1" : "1 : 4");
            StartCoroutine(Play(from, to, delta));
        }

        private static string TierName(Record record) => record.Ranked ? RankTier.Names[record.Tier] : RankTier.Unranked;

        private static string FormatScore(double score, int? delta)
        {
            string value = $"{score:0}";
            return delta.HasValue ? $"{value} <size=55%>({delta.Value:+#;-#;0})</size>" : value;
        }
    }
}

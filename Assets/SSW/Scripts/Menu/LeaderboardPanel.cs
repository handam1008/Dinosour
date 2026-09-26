using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace SSW
{
    public sealed class LeaderboardPanel : MonoBehaviour
    {
        [SerializeField] RankList _rows;
        [SerializeField] UnityEngine.UI.Button _refresh;
        [SerializeField] UnityEngine.UI.ScrollRect _scroll;
        [SerializeField] TMP_Text _status;
        [SerializeField] float _timeout = 15f;
        IRankSource _source = new RankSource();
        CancellationTokenSource _request;

        public async void Refresh()
        {
            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            if (_request != null || !isActiveAndEnabled) return;
            var request = new CancellationTokenSource();
            _request = request;
            _refresh.interactable = false;
            _scroll.content.gameObject.SetActive(false);
            ShowStatus("불러오는 중...");
            try
            {
                var entries = await _source.LoadAsync(request.Token).AsUniTask()
                    .AttachExternalCancellation(request.Token)
                    .Timeout(TimeSpan.FromSeconds(_timeout), DelayType.Realtime);
                if (this == null || _request != request || !isActiveAndEnabled) return;
                _rows.Show(entries);
                _scroll.content.gameObject.SetActive(true);
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
                _scroll.verticalNormalizedPosition = 1f;
                ShowStatus(_rows.Count == 0 ? "아직 기록이 없습니다" : string.Empty);
            }
            catch (OperationCanceledException) when (request.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                if (this == null || _request != request || !isActiveAndEnabled) return;
                ShowStatus(RankStatus.Message(exception));
                Debug.LogWarning($"리더보드 조회 실패: {exception.Message}", this);
            }
            finally
            {
                request.Cancel();
                request.Dispose();
                if (_request == request)
                {
                    _request = null;
                    if (this != null) _refresh.interactable = true;
                }
            }
        }

        void OnDisable()
        {
            var request = _request;
            _request = null;
            request?.Cancel();
            _refresh.interactable = true;
        }

        void ShowStatus(string value)
        {
            _status.text = value;
            _status.gameObject.SetActive(value.Length > 0);
        }
    }
}

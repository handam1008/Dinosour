using SSW;
using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Objects
{
    public class KDH_TimeLineController : MonoBehaviour
    {
        [SerializeField] private float maxRange;
        [SerializeField] private float minRange;
        [SerializeField] private float relationTime;
        [SerializeField] private float duration;

        private KDH_TimeLineEffect _timeLineEffect;
        private float _timer;
        private float _applyTime;
        private bool _started;
        
        [SerializeField] private SoundCue clockSound;

        [SerializeField] private UnityEvent onTimeLineStart;
        [SerializeField] private UnityEvent onTimeLineEnd;

        
        private void Awake()
        {
            _timeLineEffect = GetComponent<KDH_TimeLineEffect>();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (!_started)
            {
                _timer += dt;
                if (_timer >= relationTime)
                {
                    Debug.Log("시작");
                    _started = true;
                    _applyTime = 0f;
                    
                    if (NetGame.Current != null && clockSound != null)
                        NetGame.Current.Sounds.Play(clockSound); // 사운드
                    
                    onTimeLineStart?.Invoke();
                }
            }
            else
            {
                _applyTime += dt;
                float t = Mathf.Clamp01(_applyTime / duration);
                Time.timeScale = Mathf.Lerp(minRange, maxRange, t);
                _timeLineEffect.StartEffect(t);

                if (t >= 1f)
                {
                    Debug.Log("끝");
                    _timeLineEffect.StopEffect();
                    _started = false;
                    _timer = 0f;
                    Time.timeScale = 1f;
                    
                    onTimeLineEnd?.Invoke();
                }
            }
        }
    }
}
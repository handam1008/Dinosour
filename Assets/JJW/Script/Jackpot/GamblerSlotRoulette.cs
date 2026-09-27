using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace JJW.Script.Jackpot
{
    [DisallowMultipleComponent]
    public class GamblerSlotRoulette : MonoBehaviour
    {
        [Header("Spin Timing")]
        [SerializeField, Min(0.1f)] private float leftStopTime = 1.8f;
        [SerializeField, Min(0.1f)] private float middleStopTime = 2.15f;
        [SerializeField, Min(0.1f)] private float rightStopTime = 2.5f;
        [SerializeField, Min(0.02f)] private float symbolStepDuration = 0.075f;
        [SerializeField, Min(0f)] private float resultHoldDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.12f;

        [Header("World Space Layout")]
        [SerializeField] private Vector3 visualLocalPosition = Vector3.zero;
        [SerializeField, Min(0.0001f)] private float visualScale = 0.0035f;
        [SerializeField] private int sortingOrder = 50;

        [Header("Optional Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip reelStopSound;
        [SerializeField, Range(0f, 1f)] private float reelStopVolume = 0.7f;

        private const string SymbolSheetPath = "GamblerSlot/SlotSymbols";
        private const string FramePath = "GamblerSlot/SlotFrame";
        private const string PayLinePath = "GamblerSlot/SlotPayLine";

        private readonly Queue<SpinRequest> spinQueue = new Queue<SpinRequest>();
        private readonly List<Sprite> runtimeSprites = new List<Sprite>();

        private CanvasGroup canvasGroup;
        private ReelView[] reels;
        private Sprite[] symbols;
        private Coroutine queueCoroutine;
        private AudioClip generatedReelStopSound;

        public bool IsSpinning => queueCoroutine != null;

        public event Action<int> ReelStopped;
        public event Action<JackpotResultType> SpinCompleted;

        private sealed class SpinRequest
        {
            public JackpotResultType Result;
            public Action OnComplete;
        }

        private sealed class ReelView
        {
            public Image Current;
            public Image Next;
            public float TravelDistance;

            public void Reset(Sprite sprite)
            {
                Current.DOKill();
                Next.DOKill();

                Current.sprite = sprite;
                Next.sprite = sprite;
                Current.rectTransform.anchoredPosition = Vector2.zero;
                Next.rectTransform.anchoredPosition = new Vector2(0f, TravelDistance);
            }

            public void SwapImages()
            {
                Image oldCurrent = Current;
                Current = Next;
                Next = oldCurrent;

                Current.rectTransform.anchoredPosition = Vector2.zero;
                Next.rectTransform.anchoredPosition = new Vector2(0f, TravelDistance);
            }

            public void KillTweens()
            {
                Current.DOKill();
                Next.DOKill();
            }
        }

        private void Awake()
        {
            LoadAssets();
            BuildVisuals();
            PrepareAudio();
            SetVisibleImmediately(false);
        }

        private void OnDisable()
        {
            if (queueCoroutine != null)
            {
                StopCoroutine(queueCoroutine);
                queueCoroutine = null;
            }

            KillReelTweens();
            spinQueue.Clear();
            SetVisibleImmediately(false);
        }

        private void OnDestroy()
        {
            KillReelTweens();

            foreach (Sprite sprite in runtimeSprites)
            {
                if (sprite != null)
                {
                    Destroy(sprite);
                }
            }

            if (generatedReelStopSound != null)
            {
                Destroy(generatedReelStopSound);
            }
        }

        public void Play(JackpotResultType result, Action onComplete)
        {
            if (!isActiveAndEnabled || symbols == null || symbols.Length < 6 || reels == null)
            {
                onComplete?.Invoke();
                return;
            }

            spinQueue.Enqueue(new SpinRequest
            {
                Result = result,
                OnComplete = onComplete
            });

            if (queueCoroutine == null)
            {
                queueCoroutine = StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            while (spinQueue.Count > 0)
            {
                SpinRequest request = spinQueue.Dequeue();
                yield return PlaySpin(
                    request.Result,
                    request.OnComplete);
            }

            queueCoroutine = null;
        }

        private IEnumerator PlaySpin(
            JackpotResultType result,
            Action onResultLocked)
        {
            Sprite[] finalSymbols = GetFinalSymbols(result);

            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);

            int stoppedCount = 0;

            StartCoroutine(SpinReel(reels[0], 0, leftStopTime, finalSymbols[0], () => stoppedCount++));
            StartCoroutine(SpinReel(reels[1], 1, middleStopTime, finalSymbols[1], () => stoppedCount++));
            StartCoroutine(SpinReel(reels[2], 2, rightStopTime, finalSymbols[2], () => stoppedCount++));

            while (stoppedCount < reels.Length)
            {
                yield return null;
            }

            SpinCompleted?.Invoke(result);
            onResultLocked?.Invoke();

            if (resultHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(resultHoldDuration);
            }

            Tween fadeTween = canvasGroup
                .DOFade(0f, fadeDuration)
                .SetUpdate(true);

            yield return fadeTween.WaitForCompletion();
        }

        private IEnumerator SpinReel(
            ReelView reel,
            int reelIndex,
            float stopTime,
            Sprite finalSprite,
            Action onStopped)
        {
            reel.Reset(GetRandomSymbol());

            float elapsed = 0f;

            while (elapsed + symbolStepDuration < stopTime)
            {
                reel.Next.sprite = GetRandomSymbol();

                yield return MoveToNextSymbol(reel, symbolStepDuration);

                reel.SwapImages();
                elapsed += symbolStepDuration;
            }

            float finalStepDuration = Mathf.Max(0.02f, stopTime - elapsed);
            reel.Next.sprite = finalSprite;

            yield return MoveToNextSymbol(reel, finalStepDuration);

            reel.SwapImages();
            reel.Current.sprite = finalSprite;

            reel.Current.rectTransform
                .DOPunchAnchorPos(new Vector2(0f, -12f), 0.16f, 5, 0.35f)
                .SetUpdate(true);

            if (audioSource != null && reelStopSound != null)
            {
                audioSource.PlayOneShot(reelStopSound, reelStopVolume);
            }

            ReelStopped?.Invoke(reelIndex);
            onStopped?.Invoke();
        }

        private YieldInstruction MoveToNextSymbol(ReelView reel, float duration)
        {
            reel.Current.rectTransform.anchoredPosition = Vector2.zero;
            reel.Next.rectTransform.anchoredPosition = new Vector2(0f, reel.TravelDistance);

            Sequence sequence = DOTween.Sequence();
            sequence.Join(
                reel.Current.rectTransform.DOAnchorPosY(-reel.TravelDistance, duration)
                    .SetEase(Ease.Linear));
            sequence.Join(
                reel.Next.rectTransform.DOAnchorPosY(0f, duration)
                    .SetEase(Ease.Linear));
            sequence.SetUpdate(true);
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            return sequence.WaitForCompletion();
        }

        private Sprite[] GetFinalSymbols(JackpotResultType result)
        {
            int symbolIndex = ResultToSymbolIndex(result);

            if (symbolIndex >= 0)
            {
                Sprite symbol = symbols[symbolIndex];
                return new[] { symbol, symbol, symbol };
            }

            int[][] missCombinations =
            {
                new[] { 0, 1, 2 },
                new[] { 3, 0, 1 },
                new[] { 4, 2, 5 },
                new[] { 1, 5, 0 },
                new[] { 2, 3, 4 }
            };

            int[] selected = missCombinations[Random.Range(0, missCombinations.Length)];

            return new[]
            {
                symbols[selected[0]],
                symbols[selected[1]],
                symbols[selected[2]]
            };
        }

        private int ResultToSymbolIndex(JackpotResultType result)
        {
            switch (result)
            {
                case JackpotResultType.Heal: return 0;
                case JackpotResultType.DamageUp: return 1;
                case JackpotResultType.Invincible: return 2;
                case JackpotResultType.SpeedUp: return 3;
                case JackpotResultType.InstantKill: return 4;
                case JackpotResultType.Jackpot777: return 5;
                default: return -1;
            }
        }

        private Sprite GetRandomSymbol()
        {
            return symbols[Random.Range(0, symbols.Length)];
        }

        private void LoadAssets()
        {
            Texture2D symbolSheet = Resources.Load<Texture2D>(SymbolSheetPath);
            Texture2D frameTexture = Resources.Load<Texture2D>(FramePath);
            Texture2D payLineTexture = Resources.Load<Texture2D>(PayLinePath);

            if (symbolSheet == null || frameTexture == null || payLineTexture == null)
            {
                return;
            }

            PrepareTexture(symbolSheet);
            PrepareTexture(frameTexture);
            PrepareTexture(payLineTexture);

            symbols = SliceSymbolSheet(symbolSheet, 6);

            Sprite frameSprite = CreateSprite(frameTexture, new Rect(0f, 0f, frameTexture.width, frameTexture.height));
            Sprite payLineSprite = CreateSprite(payLineTexture, new Rect(0f, 0f, payLineTexture.width, payLineTexture.height));

            loadedFrameSprite = frameSprite;
            loadedPayLineSprite = payLineSprite;
        }

        private void PrepareAudio()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;

            if (reelStopSound == null)
            {
                generatedReelStopSound = CreateReelStopSound();
                reelStopSound = generatedReelStopSound;
            }
        }

        private AudioClip CreateReelStopSound()
        {
            const int sampleRate = 22050;
            const float duration = 0.13f;

            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            System.Random noise = new System.Random(777);

            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float envelope = Mathf.Exp(-26f * time);
                float lowTone = Mathf.Sin(2f * Mathf.PI * 105f * time) * 0.72f;
                float knockTone = Mathf.Sin(2f * Mathf.PI * 210f * time) * 0.22f;
                float noiseSample = ((float)noise.NextDouble() * 2f - 1f) * 0.12f;

                samples[i] = (lowTone + knockTone + noiseSample) * envelope;
            }

            AudioClip clip = AudioClip.Create(
                "GeneratedSlotReelStop",
                sampleCount,
                1,
                sampleRate,
                false);

            clip.SetData(samples, 0);
            return clip;
        }

        private Sprite loadedFrameSprite;
        private Sprite loadedPayLineSprite;

        private void PrepareTexture(Texture2D texture)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        private Sprite[] SliceSymbolSheet(Texture2D texture, int count)
        {
            Sprite[] result = new Sprite[count];
            float cellWidth = texture.width / (float)count;

            for (int i = 0; i < count; i++)
            {
                Rect rect = new Rect(i * cellWidth, 0f, cellWidth, texture.height);
                result[i] = CreateSprite(texture, rect);
            }

            return result;
        }

        private Sprite CreateSprite(Texture2D texture, Rect rect)
        {
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            runtimeSprites.Add(sprite);
            return sprite;
        }

        private void BuildVisuals()
        {
            if (symbols == null || loadedFrameSprite == null || loadedPayLineSprite == null)
            {
                return;
            }

            Transform oldVisual = transform.Find("SlotCanvas");
            if (oldVisual != null)
            {
                Destroy(oldVisual.gameObject);
            }

            GameObject canvasObject = new GameObject(
                "SlotCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup));

            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.localPosition = visualLocalPosition;
            canvasObject.transform.localScale = Vector3.one * visualScale;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(720f, 250f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = sortingOrder;

            canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Image background = CreateImage(canvasRect, "Background", null, new Vector2(665f, 205f));
            background.color = new Color(0.015f, 0.035f, 0.025f, 0.96f);

            reels = new ReelView[3];
            float[] reelXPositions = { -196f, 0f, 196f };

            for (int i = 0; i < reels.Length; i++)
            {
                reels[i] = CreateReel(canvasRect, i, reelXPositions[i]);
            }

            Image frame = CreateImage(canvasRect, "Frame", loadedFrameSprite, new Vector2(700f, 245f));
            frame.preserveAspect = true;

            Image payLine = CreateImage(canvasRect, "PayLine", loadedPayLineSprite, new Vector2(650f, 42f));
            payLine.preserveAspect = false;
        }

        private ReelView CreateReel(RectTransform parent, int index, float xPosition)
        {
            GameObject viewportObject = new GameObject(
                $"Reel{index + 1}",
                typeof(RectTransform),
                typeof(RectMask2D));

            viewportObject.transform.SetParent(parent, false);

            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.sizeDelta = new Vector2(170f, 170f);
            viewportRect.anchoredPosition = new Vector2(xPosition, 0f);

            Image current = CreateImage(viewportRect, "Current", symbols[0], new Vector2(145f, 145f));
            Image next = CreateImage(viewportRect, "Next", symbols[1], new Vector2(145f, 145f));

            ReelView reel = new ReelView
            {
                Current = current,
                Next = next,
                TravelDistance = 170f
            };

            reel.Reset(symbols[0]);
            return reel;
        }

        private Image CreateImage(
            RectTransform parent,
            string objectName,
            Sprite sprite,
            Vector2 size)
        {
            GameObject imageObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            imageObject.transform.SetParent(parent, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            return image;
        }

        private void KillReelTweens()
        {
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
            }

            if (reels == null)
            {
                return;
            }

            foreach (ReelView reel in reels)
            {
                reel?.KillTweens();
            }
        }

        private void SetVisibleImmediately(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.DOKill();
            canvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}

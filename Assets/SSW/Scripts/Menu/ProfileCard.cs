using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class ProfileCard : MonoBehaviour
    {
        [SerializeField] TMP_Text _text;
        [SerializeField] JobCatalog _catalog;
        [SerializeField] JobSettingsPanel _jobs;
        [SerializeField] MenuDinosaurPreview _preview;
        [SerializeField] Image _accent;
        [SerializeField] Image _badge;
        [SerializeField] RectTransform _portrait;
        [SerializeField] CanvasGroup _group;

        void OnEnable()
        {
            _jobs.Equipped += Show;
            Apply(PlayerJobStorage.Load());
            _group.alpha = 0f;
            _group.DOFade(1f, 0.28f).SetUpdate(true);
        }

        void OnDisable()
        {
            _jobs.Equipped -= Show;
            _group.DOKill();
            _portrait.DOKill();
            _group.alpha = 1f;
            _portrait.localScale = Vector3.one;
        }

        void Show(PlayerJob job)
        {
            Apply(job);
            _portrait.DOKill();
            _portrait.localScale = Vector3.one * 0.94f;
            _portrait.DOScale(1f, 0.28f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        void Apply(PlayerJob job)
        {
            if (!_catalog.TryGet(job, out JobDefinition definition)) return;
            _text.SetText(definition.KoreanName);
            _preview.Show(definition);
            Color color = Color.Lerp(definition.Accent, Color.white, 0.45f);
            _accent.color = color;
            _text.color = color;
            _badge.color = Color.Lerp(new Color(0.12f, 0.12f, 0.18f), definition.Accent, 0.22f);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SSW
{
    public class JobRewardPanelUI : MonoBehaviour
    {
        [SerializeField] Image _icon;
        [SerializeField] Text _nameText;
        [SerializeField] float _slideDistance = 420f;
        [SerializeField] float _showDelay = 1.1f;
        [SerializeField] float _duration = 0.4f;

        public void Show(Augment augment)
        {
            gameObject.SetActive(true);

            if (augment != null)
            {
                _nameText.text = augment.displayName;
                _icon.enabled = augment.icon != null;
                _icon.sprite = augment.icon;
            }
            else
            {
                _nameText.text = "???";
                _icon.enabled = false;
            }

            RectTransform rt = (RectTransform)transform;
            Vector2 target = rt.anchoredPosition;
            rt.anchoredPosition = target + new Vector2(_slideDistance, 0f);
            rt.DOAnchorPos(target, _duration).SetEase(Ease.OutBack).SetDelay(_showDelay).SetUpdate(true);
        }
    }
}

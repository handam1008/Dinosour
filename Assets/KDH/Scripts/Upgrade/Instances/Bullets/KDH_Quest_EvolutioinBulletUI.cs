using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KDH.Scripts.Upgrade.Instances.Bullets
{
    public class KDH_Quest_EvolutioinBulletUI : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Canvas myCanvas;
        private Text myText;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            myCanvas =  GetComponentInParent<Canvas>();
            myText = GetComponentInChildren<Text>();
        }

        public void OnChangeText(string text)
        {
            myText.text = text;
        }

        public IEnumerator RemoveUI()
        {
            yield return new WaitForSeconds(5f);
            rectTransform.DOAnchorPos(new Vector2(-500, 0), 1f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(2f);
            Destroy(myCanvas.gameObject);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using RYU._01.Script.Customize;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class SkinPreview : MonoBehaviour
    {
        [Serializable]
        sealed class Choice
        {
            public Button Button;
            public string Id;
            [NonSerialized] public UnityAction Selected;
        }

        [SerializeField] MenuDinosaurPreview _preview;
        [SerializeField] DinoSkinCatalog _catalog;
        [SerializeField] Choice[] _choices = Array.Empty<Choice>();
        readonly Dictionary<string, Sprite[]> _frames = new Dictionary<string, Sprite[]>();

        void OnEnable()
        {
            foreach (Choice choice in _choices)
            {
                choice.Selected ??= () => Show(choice.Id);
                choice.Button.onClick.AddListener(choice.Selected);
            }
            Show(DinoSkinStorage.LoadId());
        }

        void OnDisable()
        {
            foreach (Choice choice in _choices)
                choice.Button.onClick.RemoveListener(choice.Selected);
        }

        void Show(string id)
        {
            DinoSkin skin = _catalog.Find(id) ?? _catalog.Default();
            if (skin == null || skin.library == null) return;
            if (!_frames.TryGetValue(skin.id, out Sprite[] frames))
            {
                frames = skin.library.GetCategoryLabelNames("Idle")
                    .Select(label => skin.library.GetSprite("Idle", label))
                    .Where(sprite => sprite != null).ToArray();
                if (frames.Length == 0 && skin.preview != null) frames = new[] { skin.preview };
                _frames.Add(skin.id, frames);
            }
            _preview.SetFrames(frames);
        }
    }
}

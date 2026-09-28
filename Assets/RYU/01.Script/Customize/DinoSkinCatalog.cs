using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace RYU._01.Script.Customize
{
    public enum DinoGender
    {
        Male = 0,
        Female = 1,
    }

    [Serializable]
    public class DinoSkin
    {
        public string id;
        public string displayName;
        public DinoGender gender;
        public SpriteLibraryAsset library;
        public Sprite preview;
    }

    [CreateAssetMenu(fileName = "DinoSkinCatalog", menuName = "RYU/Dino Skin Catalog")]
    public class DinoSkinCatalog : ScriptableObject
    {
        public const string ResourceName = "DinoSkinCatalog";

        [SerializeField] private List<DinoSkin> skins = new List<DinoSkin>();

        private static DinoSkinCatalog _cached;

        public IReadOnlyList<DinoSkin> Skins => skins;

        public static DinoSkinCatalog Load()
        {
            if (_cached == null) _cached = Resources.Load<DinoSkinCatalog>(ResourceName);
            return _cached;
        }

        public DinoSkin Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            for (int i = 0; i < skins.Count; i++)
                if (skins[i] != null && skins[i].id == id) return skins[i];

            return null;
        }

        public DinoSkin Default()
        {
            for (int i = 0; i < skins.Count; i++)
                if (skins[i] != null && skins[i].library != null) return skins[i];

            return null;
        }

        public void GetByGender(DinoGender gender, List<DinoSkin> results)
        {
            results.Clear();

            for (int i = 0; i < skins.Count; i++)
                if (skins[i] != null && skins[i].gender == gender) results.Add(skins[i]);
        }
    }
}

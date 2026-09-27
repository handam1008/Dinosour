using UnityEngine;

namespace RYU._01.Script.Customize
{
    public static class DinoSkinStorage
    {
        private const string Key = "RYU.DinoSkin.v1";

        public static string LoadId()
        {
            string id = PlayerPrefs.GetString(Key, string.Empty);

            DinoSkinCatalog catalog = DinoSkinCatalog.Load();
            if (catalog == null) return id;

            if (catalog.Find(id) != null) return id;

            DinoSkin fallback = catalog.Default();
            return fallback != null ? fallback.id : string.Empty;
        }

        public static DinoSkin Load()
        {
            DinoSkinCatalog catalog = DinoSkinCatalog.Load();
            if (catalog == null) return null;

            return catalog.Find(LoadId()) ?? catalog.Default();
        }

        public static void Save(string id)
        {
            PlayerPrefs.SetString(Key, id);
            PlayerPrefs.Save();
        }
    }
}

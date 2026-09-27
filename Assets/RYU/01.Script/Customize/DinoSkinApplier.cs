using SSW;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace RYU._01.Script.Customize
{

    public class DinoSkinApplier : MonoBehaviour
    {
        [SerializeField] private bool applyOnStart = true;

        private void Start()
        {
            if (applyOnStart) Apply(DinoSkinStorage.LoadId());
        }

        public void Apply(string skinId)
        {
            DinoSkinCatalog catalog = DinoSkinCatalog.Load();
            if (catalog == null) return;

            DinoSkin skin = catalog.Find(skinId) ?? catalog.Default();
            if (skin == null || skin.library == null) return;

            Apply(gameObject, skin.library);
        }

        public static void Apply(GameObject root, SpriteLibraryAsset library)
        {
            if (root == null || library == null) return;

            ISpriteLibraryReceiver receiver = root.GetComponentInChildren<ISpriteLibraryReceiver>(true);
            receiver?.SetSpriteLibrary(library);
        }
    }
}

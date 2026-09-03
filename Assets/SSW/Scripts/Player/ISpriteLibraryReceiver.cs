using UnityEngine.U2D.Animation;

namespace SSW
{
    public interface ISpriteLibraryReceiver
    {
        SpriteLibraryAsset CurrentSpriteLibrary { get; }
        void SetSpriteLibrary(SpriteLibraryAsset library);
    }
}

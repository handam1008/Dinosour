using UnityEngine;

namespace SSW
{
    public interface IForceReceiver
    {
        void ApplyForce(Vector2 force, ForceMode2D mode);
    }
}

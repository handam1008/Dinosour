using UnityEngine;

namespace SSW
{
    /// <summary>Receives pushes and pulls without exposing a concrete movement component.</summary>
    public interface IForceReceiver
    {
        void ApplyForce(Vector2 force, ForceMode2D mode);
    }
}

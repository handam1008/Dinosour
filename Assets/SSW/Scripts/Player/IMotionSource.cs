using UnityEngine;

namespace SSW
{
    public interface IMotionSource
    {
        Vector2 Velocity { get; }
        bool Grounded { get; }
    }
}
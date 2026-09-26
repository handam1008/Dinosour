using UnityEngine;

namespace SSW
{
    public interface IMapRider
    {
        void Ride(Vector2 point, Vector2 velocity, float mass, float gravity, float delta, bool landing);
    }
}

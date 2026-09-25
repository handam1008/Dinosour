using UnityEngine;
using UnityEngine.Events;

namespace SSW
{
    public sealed class MapGust : MonoBehaviour
    {
        [SerializeField] Vector2 _direction;
        [SerializeField] float _force;
        [SerializeField, Min(0.05f)] float _interval = 2f;
        [SerializeField] UnityEvent _onForce;
        float _elapsed;

        void Update()
        {
            NetGame game = NetGame.Current;
            if (!game.CanFight) return;
            _elapsed += Time.deltaTime;
            if (_elapsed < _interval) return;
            _elapsed %= _interval;
            if (game.Manager.IsServer)
                foreach (NetPlayer player in game.Players)
                    player.Drive.ApplyForce(_direction.normalized * _force, ForceMode2D.Impulse);
            _onForce.Invoke();
        }
    }
}

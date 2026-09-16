using UnityEngine;

namespace SSW
{
    public sealed class NetLabel : MonoBehaviour
    {
        Transform _view;

        void Start()
        {
            NetGame game = NetGame.Current;
            if (game == null || game.Arena == null)
            {
                enabled = false;
                return;
            }
            _view = game.Arena.View.transform;
            LateUpdate();
        }

        void LateUpdate()
        {
            transform.rotation = _view.rotation;
        }
    }
}

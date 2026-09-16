using UnityEngine;

namespace SSW
{
    [ExecuteAlways]
    public sealed class Rope : MonoBehaviour
    {
        [SerializeField] LineRenderer _line;
        [SerializeField] DistanceJoint2D _joint;

        void LateUpdate()
        {
            Vector3 start = _joint.connectedBody.transform.TransformPoint(_joint.connectedAnchor);
            Vector3 end = _joint.transform.TransformPoint(_joint.anchor);
            if (_line.GetPosition(0) != start) _line.SetPosition(0, start);
            if (_line.GetPosition(1) != end) _line.SetPosition(1, end);
        }
    }
}

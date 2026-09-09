using UnityEngine;

namespace NKY.Lib.EventChannel.EventChannelAsset
{
    [CreateAssetMenu(fileName = "DoubleFloatEvent", menuName = "Lib/EventChannel/DoubleFloat", order = 0)]
    public class DoubleFloatEventChannelSO : EventChannelSO<(float, float)>
    {
        public void ChangeTupleRaise(float a, float b)
        {
            Raise((a, b));
        }
    }
}
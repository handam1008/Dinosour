using UnityEngine;

namespace NKY.Lib.EventChannel
{
    public abstract class EventRaiserBase : MonoBehaviour
    {
        public abstract void Raise();
    }
}
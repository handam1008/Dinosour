using UnityEngine;

namespace KDH.Scripts.System.FeedbackSystem
{
    public abstract class KDH_AbstractFeedback : MonoBehaviour
    {
        public abstract void CreateFeedBack();
        public abstract void StopFeedBack();
    }
}
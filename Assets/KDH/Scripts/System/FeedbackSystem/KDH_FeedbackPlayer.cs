using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KDH.Scripts.System.FeedbackSystem
{
    public class KDH_FeedbackPlayer : MonoBehaviour
    {
        private List<KDH_AbstractFeedback> _feedbackToPlay =  new();

        private void OnEnable()
        {
            _feedbackToPlay = GetComponentsInChildren<KDH_AbstractFeedback>().ToList();
        }

        public void PlayAllFeedback()
        {
            _feedbackToPlay.ForEach(feedback => feedback.CreateFeedBack());
        }

        public void StopAllFeedback()
        {
            _feedbackToPlay.ForEach(feedback => feedback.StopFeedBack());
        }
    }
}
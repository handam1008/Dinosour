using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KDH.Scripts.System.FeedbackSystem
{
    public class KDH_FeedbackPlayer : MonoBehaviour
    {
        private List<KDH_AbstractFeedback> _feedbackToPlay =  new List<KDH_AbstractFeedback>();

        private void OnEnable()
        {
            _feedbackToPlay = GetComponentsInChildren<KDH_AbstractFeedback>().ToList();
            Debug.Log(_feedbackToPlay.Count);
        }

        public void PlayAllFeedback()
        {
            _feedbackToPlay.ForEach(feedback => feedback.CreateFeedBack());
            Debug.Log($"피드백 개수: {_feedbackToPlay.Count}");
        }

        public void StopAllFeedback()
        {
            _feedbackToPlay.ForEach(feedback => feedback.StopFeedBack());
        }
    }
}
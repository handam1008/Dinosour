using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RYU._01.Script.FeedBack
{
    public class FeedBackPlayer : MonoBehaviour
    {
        private List<AbstractFeedBack> _feedBacks;

        private void Awake()
        {
            _feedBacks = GetComponents<AbstractFeedBack>().ToList();
        }

        public void PlayAllFeedBacks(float scale = 1f)
        {
            _feedBacks.ForEach(x => x.CreateFeedBack(transform.position, scale));
        }

        public void StopAllFeedBacks()
        {
            _feedBacks.ForEach(x => x.StopFeedBack());
        }
    }
}

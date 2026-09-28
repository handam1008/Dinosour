#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    [Serializable]
    public sealed class HudProbe
    {
        public Vector3 anchor;
        public Vector3 bar;
        public float error;
        public int bars;
        public int icons;
        public bool visible;
        public Vector2 top;
        public Vector2 size;
        public string[] augments;

        public static HudProbe Read(NetPlayer player)
        {
            HealthBarDisplay[] bars = player.GetComponentsInChildren<HealthBarDisplay>(true);
            JobAugmentHUD hud = player.GetComponent<JobAugmentHUD>();
            ScrollRect scroll = hud.GetComponentInChildren<ScrollRect>(true);
            Vector3 anchor = player.Health.LabelPosition;
            return new HudProbe
            {
                anchor = anchor,
                bar = bars.Length > 0 ? bars[0].transform.position : Vector3.zero,
                error = bars.Length > 0 ? Vector3.Distance(anchor, bars[0].transform.position) : -1f,
                bars = bars.Length,
                icons = hud.IconCount,
                visible = scroll != null && scroll.gameObject.activeInHierarchy,
                top = scroll != null ? scroll.viewport.anchoredPosition : Vector2.zero,
                size = scroll != null ? scroll.viewport.sizeDelta : Vector2.zero,
                augments = hud.GetComponentsInChildren<JobAugmentIconUI>(true).Select(icon => icon.name).ToArray()
            };
        }
    }
}
#endif

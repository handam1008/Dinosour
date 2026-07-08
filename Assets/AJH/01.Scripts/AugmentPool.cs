using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Augments/AugmentPool")]
public class AugmentPool : ScriptableObject
{
    public List<Augment> allAugments = new List<Augment>();
}

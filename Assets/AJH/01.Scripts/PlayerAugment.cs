using System.Collections.Generic;
using UnityEngine;

public class PlayerA : MonoBehaviour
{
    private List<Augment> _augments = new List<Augment>();

    public void AddAugment(Augment augment)  // 어떤 증강이 와도 OK
    {
        _augments.Add(augment);
        augment.Apply(gameObject);
    }
}

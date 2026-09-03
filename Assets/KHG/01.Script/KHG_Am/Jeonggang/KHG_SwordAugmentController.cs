using SSW;
using System.Collections.Generic;
using UnityEngine;

public class KHG_SwordAugmentController : AugmentReceiverBehaviour
{
    public override PlayerJob Job => PlayerJob.Swordsman;

    public override bool TryReceive(Augment augment)
    {
        throw new System.NotImplementedException();
    }
}

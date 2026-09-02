using System;
using System.Collections.Generic;

namespace SSW
{
    public interface IAugmentSource
    {
        event Action<Augment> AugmentGranted;
        IReadOnlyList<Augment> Owned { get; }
        bool TryGrant(Augment augment);
    }

    public interface IAugmentReceiver
    {
        bool TryReceive(Augment augment);
    }

    public interface IJobRestrictedAugment
    {
        PlayerJob RequiredJob { get; }
    }

    public interface IAugmentCooldownProvider
    {
        bool TryGetCooldown(Augment augment, out float remaining, out float duration);
    }
}

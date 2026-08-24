using System;
using System.Collections.Generic;

namespace SSW
{
    /// <summary>Publishes augments without knowing any concrete job controller.</summary>
    public interface IAugmentSource
    {
        event Action<Augment> AugmentGranted;
        IReadOnlyList<Augment> Owned { get; }
        bool TryGrant(Augment augment);
    }

    /// <summary>A capability implemented by any module that can consume an augment.</summary>
    public interface IAugmentReceiver
    {
        bool TryReceive(Augment augment);
    }

    /// <summary>Optional restriction for job-specific augment assets.</summary>
    public interface IJobRestrictedAugment
    {
        PlayerJob RequiredJob { get; }
    }
}

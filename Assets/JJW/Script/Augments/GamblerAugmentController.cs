using System;
using System.Collections.Generic;
using SSW;
using UnityEngine;

namespace JJW.Script.Augments
{
    [DisallowMultipleComponent]
    public class GamblerAugmentController
        : AugmentReceiverBehaviour, IAugmentCooldownProvider
    {
        private readonly HashSet<GamblerAugmentType> acquired
            = new HashSet<GamblerAugmentType>();

        private JobAugmentHUD augmentHud;

        public event Action<GamblerAugmentType> AugmentAcquired;

        public override PlayerJob Job => PlayerJob.Gambler;

        public override bool TryReceive(Augment augment)
        {
            if (augment is not GamblerAugment gamblerAugment)
            {
                return false;
            }

            

            bool wasAdded = acquired.Add(gamblerAugment.type);

          

            if (wasAdded)
            {
                EnsureAugmentHud();
                augmentHud.AddAugment(gamblerAugment);
                AugmentAcquired?.Invoke(gamblerAugment.type);
            }

            return true;
        }

        public bool Has(GamblerAugmentType type)
        {
            return IsJobActive && acquired.Contains(type);
        }

        public bool TryGetCooldown(
            Augment augment,
            out float remaining,
            out float duration)
        {
            remaining = 0f;
            duration = 0f;
            return false;
        }

        protected override void OnJobActivated()
        {
            if (augmentHud != null)
            {
                augmentHud.SetJobVisible(true);
            }
        }

        protected override void OnJobDeactivated()
        {
            if (augmentHud != null)
            {
                augmentHud.SetJobVisible(false);
            }
        }

        private void EnsureAugmentHud()
        {
            if (augmentHud == null)
            {
                PlayerIdentity owner
                    = GetComponentInParent<PlayerIdentity>();

                GameObject hudOwner
                    = owner != null ? owner.gameObject : gameObject;

                augmentHud = hudOwner.GetComponent<JobAugmentHUD>();

                if (augmentHud == null)
                {
                    augmentHud =
                        hudOwner.AddComponent<JobAugmentHUD>();
                }
            }

            augmentHud.Configure(this);
            augmentHud.SetJobVisible(IsJobActive);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    /// <summary>
    /// Handles augment-source discovery, event lifetime, and replay for job modules.
    /// Receivers may live on the player root or any child object.
    /// </summary>
    public abstract class AugmentReceiverBehaviour : JobModuleBehaviour, IAugmentReceiver
    {
        IAugmentSource _source;
        readonly HashSet<Augment> _delivered = new HashSet<Augment>();

        protected override void Awake()
        {
            base.Awake();
            _source = GetComponentInParent<IAugmentSource>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_source == null)
                _source = GetComponentInParent<IAugmentSource>();

            if (_source == null)
                return;

            _source.AugmentGranted += HandleAugmentGranted;
            foreach (Augment augment in _source.Owned)
                DeliverOnce(augment);
        }

        protected override void OnDisable()
        {
            if (_source != null)
                _source.AugmentGranted -= HandleAugmentGranted;

            base.OnDisable();
        }

        public abstract bool TryReceive(Augment augment);

        void HandleAugmentGranted(Augment augment)
        {
            DeliverOnce(augment);
        }

        void DeliverOnce(Augment augment)
        {
            if (augment == null || _delivered.Contains(augment))
                return;

            if (TryReceive(augment))
                _delivered.Add(augment);
        }
    }
}

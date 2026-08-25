using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    public class Blink : MonoBehaviour
    {
        [SerializeField]private float _blinkDistance;
        [SerializeField]private float _blinkTime;
        
        AugmentDrafter _augmentDrafter;
        Defance _defance;

        private bool _lock;
        private bool _blinking;

        private void Awake()
        {
            _augmentDrafter = GetComponent<AugmentDrafter>();
            _defance = GetComponent<Defance>();

            _augmentDrafter.OnAugmentSelected += HandleSelected;
            _defance.OnBarrierUsed += DoBlink;
        }

        private void OnDisable()
        {
            if(_augmentDrafter != null)  _augmentDrafter.OnAugmentSelected -= HandleSelected;
            if (_defance != null) _defance.OnBarrierUsed -= DoBlink;
        }


        private void HandleSelected(Augment augment)
        {
            if(augment is not CommonAugment common) return;
            if(common.type != CommonAugmentType.Blink)return;
            
            _lock = true;
        }
        
        private void DoBlink()
        {
            if(!_lock ||  _blinking) return;
        }
    }
}
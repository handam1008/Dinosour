using System;
using System.Collections.Generic;
using System.Linq;
using NKY.Lib.EventChannel;
using NKY.Lib.EventChannel.EventChannelAsset;
using SSW;
using UnityEngine;

namespace NKY.Scripts.Job
{
    public class AssassinAugmentController : AugmentReceiverBehaviour
    {
        private readonly HashSet<AssassinAugmentType> _has = new();
        
        [Header("SO Database (모든 암살자 증강 SO 등록)")]
        [SerializeField] private List<AbstractAssassinAugmentSO> _augmentDatabase;

        [Header("Event Channels")]
        [SerializeField] private VoidEventChannelSO onSkillUsedChannel;
        [SerializeField] private VoidEventChannelSO onHitChannel;
        [SerializeField] private GameObjectEventChannelSO onBasicAttackChannel;
        [SerializeField] private DamageInfoEventChannelSO onDamageCalculateChannel;
        [SerializeField] private DoubleFloatEventChannelSO onHealthChangedChannel;
        [SerializeField] private VoidEventChannelSO onRoundStart;
        
        private VoidEventChannelSO _onSkillUsedChannel;
        private VoidEventChannelSO _onHitChannel;
        private GameObjectEventChannelSO _onBasicAttackChannel;
        private DamageInfoEventChannelSO _onDamageCalculateChannel;
        private DoubleFloatEventChannelSO _onHealthChangedChannel;
        private VoidEventChannelSO _onRoundStart;


        // 현재 '활성화(보유)'된 증강만 담는 딕셔너리
        private readonly Dictionary<AssassinAugmentType, AbstractAssassinAugmentSO> _activeAugments = new();
        
        // 쿨타임 및 플레이어 상태 전달용 컨텍스트
        private AugmentContext _context;

        public override PlayerJob Job => PlayerJob.Assassin;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            // 이벤트 채널 구독
            if (_onSkillUsedChannel != null) _onSkillUsedChannel.OnEventRaised += HandleSkillUsed;
            if (_onHitChannel != null) _onHitChannel.OnEventRaised += HandleHit;
            if (_onBasicAttackChannel != null) _onBasicAttackChannel.OnEventRaised += HandleBasicAttack;
            if (_onDamageCalculateChannel != null) _onDamageCalculateChannel.OnEventRaised += HandleDamageCalculate;
            if (_onHealthChangedChannel != null) _onHealthChangedChannel.OnEventRaised += HealthChanged;
            if (_onRoundStart != null) _onRoundStart.OnEventRaised += HandleRoundStart;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // 구독 해제 (메모리 누수 방지)
            if (_onSkillUsedChannel != null) _onSkillUsedChannel.OnEventRaised -= HandleSkillUsed;
            if (_onHitChannel != null) _onHitChannel.OnEventRaised -= HandleHit;
            if (_onBasicAttackChannel != null) _onBasicAttackChannel.OnEventRaised -= HandleBasicAttack;
            if (_onDamageCalculateChannel != null) _onDamageCalculateChannel.OnEventRaised -= HandleDamageCalculate;
            if (_onHealthChangedChannel != null) _onHealthChangedChannel.OnEventRaised -= HealthChanged;
            if (_onRoundStart != null) _onRoundStart.OnEventRaised -= HandleRoundStart;
        }
        
        protected override void Awake()
        {
            base.Awake();
            _context = new AugmentContext(transform.parent.gameObject, this);
        }

        private void Start()
        {
            _onSkillUsedChannel = Instantiate(onSkillUsedChannel); 
            _onHitChannel = Instantiate(onHitChannel); 
            _onBasicAttackChannel = Instantiate(onBasicAttackChannel); 
            _onDamageCalculateChannel = Instantiate(onDamageCalculateChannel);
            _onHealthChangedChannel = GetComponent<Health>().HealthChangeEvent;
            _onRoundStart = Instantiate(onRoundStart);
        }

        public override bool TryReceive(Augment augment)
        {
            if (augment is not AssassinAugment w) return false;

            _has.Add(w.type);

            // 데이터베이스에서 해당 타입 SO를 찾아 활성화 목록에 추가
            var targetSO = _augmentDatabase.Find(so => so.type == w.type);
            if (targetSO != null && !_activeAugments.ContainsKey(w.type))
            {
                _activeAugments.Add(w.type, targetSO);
            }

            return true;
        }

        public void HealthChanged((float currentHealth, float maxHealth) info)
        {
            foreach (var aug in _activeAugments.Values)
            {
                aug.OnHealthChanged(_context, info.currentHealth, info.maxHealth);
            }
        }

        #region EventHandlers

        public void HandleRoundStart()
        {
            foreach (var aug in _activeAugments.Values)
            {
                aug.OnRoundStart(_context);
            }
        }

        public void HandleHit()
        {
            foreach (var aug in _activeAugments.Values)
            {
                aug.OnHit(_context);
            }
        }

        public void NotifySpawnProjectile(GameObject projectile)
        {
            foreach (var aug in _activeAugments.Values)
            {
                aug.OnSpawnProjectile(_context, projectile);
            }
        }
        
        private void HandleSkillUsed()
        {
            // '보유 중인' 증강들에만 이벤트 전파
            foreach (var aug in _activeAugments.Values)
            {
                aug.OnSkillUsed(_context);
            }
        }

        private void HandleBasicAttack(GameObject target)
        {
            if(!target.TryGetComponent(out PlayerController player))
                return;
            
            foreach (var aug in _activeAugments.Values)
            {
                aug.OnBasicAttackHit(_context, player);
            }
        }

        private void HandleDamageCalculate(DamageInfo info)
        {
            foreach (var aug in _activeAugments.Values)
            {
                info.Damage = aug.ModifyDamage(_context, info);
            }
        }

        #endregion
    }
}
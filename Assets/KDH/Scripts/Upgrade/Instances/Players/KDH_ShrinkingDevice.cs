using System.Collections;
using DevLib.ServiceLocator;
using KDH.Scripts.Gun;
using KDH.Scripts.Sounds;
using KDH.Scripts.Upgrade;
using SSW;
using UnityEngine;
using UnityEngine.Events;

public class KDH_ShrinkingDevice : KDH_AbstractPlayerAbility
{
    [field: SerializeField] public KDH_PlayerAbilitySO ShrinkingDeviceAbility { get; private set; }
    [SerializeField] private float cooldown = 5.0f;
    [field: SerializeField] public float Duration { get; private set; } = 2f;
    [SerializeField] private UnityEvent onHitPlayer;
    
    private float _timer;
    private bool _canUseSkill;
    
    private KDH_Gun _gun;
    private SoundCue shrinkingSound;
    
    private void Awake()
    {
        _gun = transform.root.gameObject.GetComponent<KDH_Gun>();
        // shrinkingSound = _gun.SoundCues.list[3];
    }
        
    private void Update()
    {
        if (_canUseSkill) return;
            
        _timer +=  Time.deltaTime;

        if (_timer >= cooldown)
        {
            _canUseSkill = true;
            _timer = 0;
        }
    }
    
    public override void PlayerAbility()
    {
        if (!_canUseSkill) return;
        
        StartCoroutine(ChangeLocalScale());
        
        if (shrinkingSound != null)
            ServiceLocator.Get<IAudioService>().PlaySfx(shrinkingSound);
        
        _canUseSkill = false;
        onHitPlayer?.Invoke();
    }

    private IEnumerator ChangeLocalScale()
    {
        _gun.gameObject.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        yield return new WaitForSeconds(Duration);
        _gun.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
    }
}

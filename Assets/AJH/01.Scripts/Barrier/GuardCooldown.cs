using SSW;
using UnityEngine;

public class GuardCooldown : MonoBehaviour, ICooldownSource
{
    [SerializeField] BuffGuard _guard;

    public bool TryGetCooldown(out float remaining, out float duration)
    {
        double now = NetGame.Current.ServerTime;
        remaining = (float)(_guard.ReadyAt - now);
        duration = (float)(_guard.ReadyAt - _guard.GuardUntil);
        return _guard.IsOwner && now >= _guard.GuardUntil && remaining > 0f && duration > 0f;
    }
}

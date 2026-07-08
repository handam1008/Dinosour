using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class SuitSelector : MonoBehaviour
{
    [SerializeField] Sprite[] _suitSprites;
    [SerializeField] Transform[] _slotAnchors;
    [SerializeField] SpriteRenderer[] _slotRenderers;
    [SerializeField] float _slideDuration = 0.25f;
    [SerializeField] float _fadeDuration = 0.15f;
    [SerializeField] float _sideVisibleDuration = 0.9f;
    [SerializeField] float _centerVisibleDuration = 1.4f;
    [SerializeField] float _sideAlpha = 0.35f;
    [SerializeField] float _sideScale = 0.6f;

    static readonly float[] RolePositions = { -0.6f, 0f, 0.6f, 1.2f };

    int _index;
    int _baseIndex;
    readonly int[] _slotGen = new int[4];

    public Suit CurrentSuit => (Suit)_index;

    void OnEnable()
    {
        _index = 0;
        _baseIndex = 0;
        for (int s = 0; s < 4; s++) _slotGen[s] = 0;

        int count = _suitSprites.Length;
        for (int i = 0; i < 4; i++)
        {
            _slotAnchors[i].DOKill();
            _slotRenderers[i].DOKill();
            _slotRenderers[i].sprite = _suitSprites[((i - 1) % count + count) % count];
            _slotAnchors[i].localPosition = new Vector3(RolePositions[i], 0f, 0f);
            _slotAnchors[i].localScale = Vector3.one * RoleScale(i);
            SetAlphaInstant(_slotRenderers[i], 0f);
        }
    }

    void OnCycleSuit(InputValue value)
    {
        if (!value.isPressed) return;

        int count = _suitSprites.Length;
        _index = (_index + 1) % count;
        _baseIndex = (_baseIndex + 1) % 4;

        for (int slot = 0; slot < 4; slot++)
        {
            int role = ((slot - _baseIndex) % 4 + 4) % 4;
            Transform anchor = _slotAnchors[slot];
            anchor.DOKill();
            anchor.DOLocalMoveX(RolePositions[role], _slideDuration).SetEase(Ease.OutCubic);
            anchor.DOScale(RoleScale(role), _slideDuration).SetEase(Ease.OutCubic);

            if (role == 3) ForceHide(slot);
        }

        RevealRole(0, _sideAlpha, _sideVisibleDuration);
        RevealRole(1, 1f, _centerVisibleDuration);
        RevealRole(2, _sideAlpha, _sideVisibleDuration);
    }

    public void FlashCurrent()
    {
        RevealRole(1, 1f, _centerVisibleDuration);
    }

    void RevealRole(int role, float target, float holdDuration)
    {
        int slot = (_baseIndex + role) % 4;
        _slotGen[slot]++;
        int gen = _slotGen[slot];

        SpriteRenderer renderer = _slotRenderers[slot];
        renderer.DOKill();
        renderer.DOFade(target, _fadeDuration);

        DOVirtual.DelayedCall(holdDuration, () =>
        {
            if (gen != _slotGen[slot]) return;
            SpriteRenderer current = _slotRenderers[slot];
            current.DOKill();
            current.DOFade(0f, _fadeDuration);
        });
    }

    float RoleScale(int role) => role == 1 ? 1f : _sideScale;

    void ForceHide(int slot)
    {
        _slotGen[slot]++;
        SpriteRenderer renderer = _slotRenderers[slot];
        renderer.DOKill();
        SetAlphaInstant(renderer, 0f);
    }

    static void SetAlphaInstant(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}

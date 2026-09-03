using RYU._01.Script.Potions;
using UnityEngine;

public abstract class AbstractPotion : ScriptableObject
{
    public string Potionname;
    public GameObject PotionPrefab;
    public Sprite sprite;
    public float amount;

    // 장판·이펙트 색깔. 포션마다 다르게 지정한다.
    public Color potionColor = Color.white;

    // target: 맞은 대상 / source: 던진 마녀 / mods: 증강 배율 묶음
    public abstract void Use(GameObject target, Component source, PotionModifiers mods);
}

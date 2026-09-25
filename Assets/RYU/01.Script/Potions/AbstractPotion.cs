using RYU._01.Script.Potions;
using UnityEngine;

public abstract class AbstractPotion : ScriptableObject
{
    public string Potionname;
    public GameObject PotionPrefab;
    public Sprite sprite;
    public float amount;

    public Color potionColor = Color.white;

    
    [Min(0f)] public float weight = 1f;

    public abstract void Use(GameObject target, Component source, PotionModifiers mods);
}

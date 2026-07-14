using UnityEngine;

public abstract class AbstractPotion : ScriptableObject
{   
    
    public string Potionname;
    public GameObject PotionPrefab;
    public float Heal;
    public Sprite sprite;
    public float Damage;

    protected virtual void OnEnable()
    {
        
    }
    public abstract void Use();

}

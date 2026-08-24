using UnityEngine;

public abstract class AbstractPotion : ScriptableObject
{   
    
    public string Potionname;
    public GameObject PotionPrefab;
    public Sprite sprite;

    public abstract void Use(GameObject target);

}

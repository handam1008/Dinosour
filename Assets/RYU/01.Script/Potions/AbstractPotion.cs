using UnityEngine;

public abstract class AbstractPotion : ScriptableObject
{   
    
    public string Potionname;
    public GameObject PotionPrefab;
    public Sprite sprite; 
    public float amount;

    public abstract void Use(GameObject target);

}

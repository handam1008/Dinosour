using UnityEngine;

public abstract class Augment : ScriptableObject
{
    public string _augmentName; 
    public string description;

    public abstract void Apply(GameObject player);
    
    //public abstract void Remove(GameObject player)
    //{

    //}
}

using System.Collections.Generic;
using UnityEngine;

public class KDH_SakuraEffectPooling : MonoBehaviour
{
    public static KDH_SakuraEffectPooling Instance;
    
    public Stack<GameObject> effects = new();
    
    [SerializeField] private GameObject effectPrefab;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
        }
        
        CreateEffect();
    }

    void CreateEffect()
    {
        for (int i = 0; i < 10; i++)
        {
            GameObject entity = Instantiate(effectPrefab);
            entity.SetActive(false);
            effects.Push(entity);
        }
        
        Debug.Log(effects.Count);
    }
}

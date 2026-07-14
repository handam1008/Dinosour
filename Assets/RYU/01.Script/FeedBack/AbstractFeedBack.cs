using UnityEngine;

public abstract class AbstractFeedBack : MonoBehaviour
{
    public abstract void CreateFeedBack(Vector3 pos);
    public abstract void StopFeedBack();
    
}

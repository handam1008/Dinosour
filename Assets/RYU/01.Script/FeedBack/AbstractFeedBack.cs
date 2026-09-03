using UnityEngine;

public abstract class AbstractFeedBack : MonoBehaviour
{
    // scale: 넓은 살포처럼 연출 크기가 커져야 할 때 쓴다. 필요 없는 피드백은 무시하면 된다.
    public abstract void CreateFeedBack(Vector3 pos, float scale);
    public abstract void StopFeedBack();
}

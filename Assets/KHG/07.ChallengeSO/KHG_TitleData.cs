using UnityEngine;

[CreateAssetMenu(menuName = "KHG_SO/Title Data", fileName = "Title_")]
public class KHG_TitleData : ScriptableObject
{
    public string id;           // 저장/참조용
    public string titleName;    // 표시 이름
    [TextArea] public string description;
    public Sprite icon;
}

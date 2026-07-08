using UnityEngine;

[CreateAssetMenu(menuName = "Augments/Giant")]
public class Giant : Augment
{
    [SerializeField] private float _sizeUp = 1.5f;

    public override void Apply(GameObject player)
    {
        player.transform.localScale *= _sizeUp;
    }
}

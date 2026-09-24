var root = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/SSW/Resources/Network/Player.prefab");
var p = root.GetComponent<SSW.NetPlayer>();
var c = (CapsuleCollider2D)p.Collider;
return new { root=root.name, rootScale=root.transform.localScale, collider=c.name, size=c.size, offset=c.offset, scale=c.transform.localScale, lossy=c.transform.lossyScale, direction=c.direction.ToString(), ground=p.GroundMask.value };

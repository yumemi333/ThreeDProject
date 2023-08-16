using UnityEngine;

public class GroundChek : MonoBehaviour
{
    [SerializeField]
    CapsuleCollider capsule;
    [SerializeField]
    LayerMask groundLayers = 0;

    public bool CheckGroundStatus()
    {
        var corride = Physics.Raycast(transform.position, Vector2.down, capsule.height / 2 + 0.1f, groundLayers);
        return corride;
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, new Vector3(0, -1f, 0));
    }
}

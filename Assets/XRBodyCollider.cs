using UnityEngine;

/// Attach to the PlayerBodyCollider GameObject under your XR Rig.
/// Keeps a capsule collider lined up with the user's body under the HMD (camera).
[DefaultExecutionOrder(50)]
public class XRBodyCollider : MonoBehaviour
{
    [Header("References")]
    public Transform head;              // Assign Main Camera (HMD)
    public Transform rigRoot;           // Assign XR Origin (XR Rig). If null, uses this.transform.parent

    [Header("Shape")]
    public float minHeight = 1.2f;      // clamp for crouching
    public float maxHeight = 2.2f;      // clamp for tall users
    public float radius = 0.25f;        // body radius
    public float forwardOffset = 0.10f; // push collider slightly in front of HMD

    CapsuleCollider capsule;

    void Awake()
    {
        if (!capsule) capsule = GetComponent<CapsuleCollider>();
        if (!capsule) capsule = gameObject.AddComponent<CapsuleCollider>();
        capsule.radius = radius;
        capsule.direction = 1; // Y axis
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        if (!rigRoot) rigRoot = transform.parent != null ? transform.parent : null;
    }

    void LateUpdate()
    {
        if (head == null) return;

        // Choose a ground/base Y (rig origin Y works well)
        float baseY = rigRoot ? rigRoot.position.y : 0f;

        // Head world position
        Vector3 headPos = head.position;

        // Compute body height from base to head (clamped)
        float height = Mathf.Clamp(headPos.y - baseY, minHeight, maxHeight);
        if (height < minHeight) height = minHeight;

        // Keep the body centered under the head in XZ, but at half height in Y.
        // Add a tiny forward offset so the body sits just in front of the HMD.
        Vector3 yawForward = new Vector3(head.forward.x, 0f, head.forward.z).normalized;
        Vector3 bodyBase = new Vector3(headPos.x, baseY, headPos.z);
        Vector3 bodyCenter = bodyBase + yawForward * forwardOffset + Vector3.up * (height * 0.5f);

        transform.position = bodyCenter;

        // Face the same yaw as the head (ignore pitch/roll)
        Vector3 yawOnly = new Vector3(head.eulerAngles.x, head.eulerAngles.y, head.eulerAngles.z);
        transform.rotation = Quaternion.Euler(0f, yawOnly.y, 0f);

        // Update capsule dims
        capsule.height = height;
        capsule.radius = radius;
        capsule.center = Vector3.zero; // since we position the GO to the center already
    }
}

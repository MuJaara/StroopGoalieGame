using UnityEngine;
using System.Collections;

public class SelectiveProjectile : MonoBehaviour
{
    public Renderer body;
    public bool isDanger;

    // Changed callback: include a flag to indicate body vs paddle
    public System.Action<SelectiveProjectile, bool> onBlocked; // bool = blockedByBody
    public System.Action<SelectiveProjectile> onGoalReached;

    Rigidbody rb;

    private float despawnTime = 6f;
    private float spawnTimestamp;
    public bool reachedGoal = false;

    // Layer/tag names
    private const string LAYER_PADDLE = "Paddle_Solid";
    private const string LAYER_BODY = "PlayerBody_Solid";
    private const string LAYER_GOAL = "Goal";
    private const string TAG_BODY = "PlayerBody";  // fallback tag

    public void Fire(Vector3 pos, Vector3 velocity, bool danger, Color color)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        transform.position = pos;
        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.zero;

        isDanger = danger;

        if (body != null)
        {
            var mat = body.material;
            if (mat != null)
            {
                if (mat.HasProperty("_Color")) mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            }
        }

        gameObject.SetActive(true);
        spawnTimestamp = Time.time;
    }

    void OnCollisionEnter(Collision c)
    {
        if (IsPaddle(c.collider))
        {
            onBlocked?.Invoke(this, false);  // paddle
            Destroy(gameObject);
            return;
        }
        if (IsBody(c.collider))
        {
            onBlocked?.Invoke(this, true);   // body
            Destroy(gameObject);
            return;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPaddle(other))
        {
            onBlocked?.Invoke(this, false);  // paddle
            Destroy(gameObject);
            return;
        }
        if (IsBody(other))
        {
            onBlocked?.Invoke(this, true);   // body
            Destroy(gameObject);
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer(LAYER_GOAL))
        {
            reachedGoal = true;
            onGoalReached?.Invoke(this);
            Destroy(gameObject);
        }
    }

    bool IsPaddle(Collider col) =>
        col.gameObject.layer == LayerMask.NameToLayer(LAYER_PADDLE);

    bool IsBody(Collider col) =>
        col.gameObject.layer == LayerMask.NameToLayer(LAYER_BODY) || col.CompareTag(TAG_BODY);

    void Update()
    {
        if (Time.time - spawnTimestamp > despawnTime)
            Destroy(gameObject);
    }
}

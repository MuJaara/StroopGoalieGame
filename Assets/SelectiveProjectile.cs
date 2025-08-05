using UnityEngine;
using System.Collections;

public class SelectiveProjectile : MonoBehaviour
{
    public Renderer body; 
    public bool isDanger;
    public System.Action<SelectiveProjectile> onBlocked;
    public System.Action<SelectiveProjectile> onGoalReached;
    Rigidbody rb;
    private float despawnTime = 6f; 
    private float spawnTimestamp;
    public bool reachedGoal = false;

    public void Fire(Vector3 pos, Vector3 velocity, bool danger, Color color)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        transform.position = pos;
        rb.linearVelocity = velocity;
        isDanger = danger;
        if (body != null) body.material.color = color;
        gameObject.SetActive(true);
        spawnTimestamp = Time.time;
        StartCoroutine(CheckIfIgnoreBallBlocked());
    }

    IEnumerator CheckIfIgnoreBallBlocked()
    {

        if (isDanger) yield break;

        yield return new WaitForSeconds(6f);


        if (!reachedGoal)
        {
            if (onBlocked != null)
                onBlocked(this);
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.collider.gameObject.layer == LayerMask.NameToLayer("Paddle_Solid"))
        {
            onBlocked?.Invoke(this);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Goal"))
        {
            reachedGoal = true;
            onGoalReached?.Invoke(this);
            Destroy(gameObject);
        }
    }

    void Update()
    {

        if (Time.time - spawnTimestamp > despawnTime)
        {
            Destroy(gameObject);
        }
    }
}

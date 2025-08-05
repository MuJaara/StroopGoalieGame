using UnityEngine;

public class GoaliePaddle : MonoBehaviour
{
    public float handSpeedThreshold = 0.35f;
    public System.Action<float> onHandOnset;
    Vector3 lastPos;
    bool capturing = false;
    float cueT0 = 0f;
    bool onsetCaptured = false;

    void OnEnable() { lastPos = transform.position; }

    void Update()
    {
        if (!capturing) { lastPos = transform.position; return; }
        float dt = Time.unscaledDeltaTime;
        float v = (transform.position - lastPos).magnitude / Mathf.Max(dt, 1e-4f);
        if (!onsetCaptured && v >= handSpeedThreshold)
        {
            onsetCaptured = true;
            onHandOnset?.Invoke(Time.unscaledTime - cueT0);
        }
        lastPos = transform.position;
    }

    public void ArmForCue(float cueTimestamp)
    {
        cueT0 = cueTimestamp;
        capturing = true;
        onsetCaptured = false;
    }
    public void Disarm() { capturing = false; }
}

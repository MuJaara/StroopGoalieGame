using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SelectiveGoalieManager : MonoBehaviour
{
    [Header("HUD Canvas UI (Corner)")]
    public Canvas hudCanvas;
    public TextMeshProUGUI blockText;
    public TextMeshProUGUI ignoreText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public GameObject resultsPanel;
    public TextMeshProUGUI resultsText;

    [Header("World Space Start UI")]
    public Canvas startMenuCanvas;
    public Button startButton3D;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI blockText3D;
    public TextMeshProUGUI ignoreText3D;
    public TMP_InputField playerNameInput;
    public TMP_Dropdown timeDropdown;
    public TMP_Dropdown speedDropdown;


    private readonly int[] timeOptions = {120, 180, 300};
    private readonly string[] timeLabels = { "2:00", "3:00","5:00" };

    private readonly float[] speedOptions = { 6f, 8f, 10f, 12f, 14f };
    private readonly string[] speedLabels = { "Slow", "Medium-", "Medium", "Medium+", "Fast" };


    [Header("Game Objects")]
    public Transform[] spawners;
    public Transform goalCenter;
    public float goalRadius = 0.5f;
    public SelectiveProjectile projectilePrefab;

    [Header("Sounds")]
    public AudioSource buzzerAudioSource;
    public AudioSource blockAudioSource;

    [Header("Paddles")]
    public GameObject leftPaddleObject;
    public GameObject rightPaddleObject;

    [Header("Game Settings")]
    public float minInterval = 0.5f;
    public float maxInterval = 1.1f;
    public float speed = 10f;

    private readonly Color[] masterColors = {
    Color.red,                  // RED
    Color.green,                // GREEN
    new Color(0.5f, 0f, 0.5f),  // PURPLE
    new Color(1f, 0.55f, 0f),   // ORANGE
    Color.blue,                 // BLUE
    Color.black                 // BLACK
};
    private readonly string[] masterNames = { "RED", "GREEN", "PURPLE", "ORANGE", "BLUE", "BLACK" };
    private Color[] colorValues = new Color[2];
    private string[] colorNames = new string[2];
    private int activeA = -1, activeB = -1;

    private int blockColorIndex, ignoreColorIndex;
    private int score = 0;
    private bool running = false;
    private bool gameEnded = false;

    public float gameDuration = 120f; 
    private float timer = 0f;


    private int goodBlocks = 0;
    private int badBlocks = 0;
    private int missedBlocks = 0;
    private int ignoreMissed = 0;

    private string playerName = "Player";
    public Material spawnerCubeMaterial;

    private bool rulesChanged = false;
    private bool rulesChangePause = false;
    private float rulesChangePauseTimer = 0f;
    public GameObject rulesChangePanel;
    public TextMeshProUGUI blockChangeText;
    public TextMeshProUGUI ignoreChangeText;
    public Button resetButton;

    void Start()
    {
        foreach (Transform spawner in spawners)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = spawner.position;
            cube.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            var renderer = cube.GetComponent<Renderer>();
            if (spawnerCubeMaterial != null)
            {
                renderer.material = spawnerCubeMaterial;
            }
            else
            {
                // fallback: neutral, semi-transparent gray
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);  // RGBA
                renderer.material = mat;
            }

            Destroy(cube.GetComponent<BoxCollider>());
        }

        if (hudCanvas != null) hudCanvas.gameObject.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (leftPaddleObject != null) leftPaddleObject.SetActive(false);
        if (rightPaddleObject != null) rightPaddleObject.SetActive(false);
        if (rulesChangePanel != null) rulesChangePanel.SetActive(false);
        SetupDropdowns();

        startMenuCanvas.gameObject.SetActive(true);
        countdownText.gameObject.SetActive(false);
        blockText3D.gameObject.SetActive(false);
        ignoreText3D.gameObject.SetActive(false);

        startButton3D.onClick.AddListener(BeginCountdown);
        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();   // avoid duplicate bindings
            resetButton.onClick.AddListener(ResetGame);
        }
        else
        {
            // (Optional) Try to find it under resultsPanel by name
            if (resultsPanel != null)
            {
                var btn = resultsPanel.GetComponentInChildren<Button>(true);
                if (btn != null)
                {
                    resetButton = btn;
                    resetButton.onClick.RemoveAllListeners();
                    resetButton.onClick.AddListener(ResetGame);
                }
            }
        }

        SetupStroopRules();
    }

    void SetupStroopRules()
    {
        PickNewRulePair(false);                 // pick any 2 to start the game
        blockColorIndex = Random.Range(0, 2);  // which of the 2 is "block"
        ignoreColorIndex = 1 - blockColorIndex;

        int blockTextColorIndex = 1 - blockColorIndex; // ink color ≠ word
        int ignoreTextColorIndex = blockColorIndex;

        blockText3D.text = $"Block: <b><color=#{ColorUtility.ToHtmlStringRGB(colorValues[blockTextColorIndex])}>{colorNames[blockColorIndex]}</color></b>";
        ignoreText3D.text = $"Ignore: <b><color=#{ColorUtility.ToHtmlStringRGB(colorValues[ignoreTextColorIndex])}>{colorNames[ignoreColorIndex]}</color></b>";
        blockText.text = blockText3D.text;
        ignoreText.text = ignoreText3D.text;
    }

    void BeginCountdown()
    {
        if (timeDropdown) timeDropdown.gameObject.SetActive(false);
        if (speedDropdown) speedDropdown.gameObject.SetActive(false);
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
            playerName = playerNameInput.text;
        else
            playerName = "Player";

        startButton3D.gameObject.SetActive(false);
        countdownText.gameObject.SetActive(true);
        blockText3D.gameObject.SetActive(true);
        ignoreText3D.gameObject.SetActive(true);

        if (leftPaddleObject != null) leftPaddleObject.SetActive(true);
        if (rightPaddleObject != null) rightPaddleObject.SetActive(true);

        StartCoroutine(StartCountdownRoutine());
    }

    IEnumerator StartCountdownRoutine()
    {
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        countdownText.text = "GO!";
        yield return new WaitForSeconds(1f);

        startMenuCanvas.gameObject.SetActive(false);
        if (hudCanvas != null) hudCanvas.gameObject.SetActive(true);

        score = 0;
        timer = gameDuration;
        gameEnded = false;
        goodBlocks = 0;
        badBlocks = 0;
        missedBlocks = 0;
        ignoreMissed = 0;
        rulesChanged = false;
        rulesChangePause = false;

        UpdateScoreUI();

        if (resultsPanel != null) resultsPanel.SetActive(false);

        running = true;
        StartCoroutine(SpawnLoop());
    }

    void PickNewRulePair(bool avoidSameAsCurrent)
    {
        int a, b;
        do
        {
            a = Random.Range(0, masterColors.Length);
            do { b = Random.Range(0, masterColors.Length); } while (b == a);
            // if avoiding the same pair, check both (a,b) and (b,a)
        } while (avoidSameAsCurrent &&
                ((a == activeA && b == activeB) || (a == activeB && b == activeA)));

        activeA = a; activeB = b;

        colorValues[0] = masterColors[a];
        colorValues[1] = masterColors[b];
        colorNames[0] = masterNames[a];
        colorNames[1] = masterNames[b];
    }

    void Update()
    {
        if (running && !gameEnded && !rulesChangePause)
        {
            timer -= Time.deltaTime;
            if (timerText != null)
                timerText.text = $"Time: {Mathf.CeilToInt(timer)}";

            if (!rulesChanged && timer <= gameDuration / 2f)
            {
                StartCoroutine(RulesChangePauseSequence());
                rulesChanged = true;
            }

            if (timer <= 0)
            {
                timer = 0;
                EndGame();
            }
        }
        else if (gameEnded)
        {
            if (timerText != null)
                timerText.text = "Time: 0";
        }

        if (rulesChangePause)
        {
            rulesChangePauseTimer -= Time.deltaTime;
            if (rulesChangePauseTimer <= 0f)
            {
                ResumeGameAfterRulesChange();
            }
        }
    }

    IEnumerator SpawnLoop()
    {
        while (running)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            SpawnOneBall();
        }
    }

    void SpawnOneBall()
    {
        Transform spawner = spawners[Random.Range(0, spawners.Length)];
        int ballColorIndex = Random.Range(0, 2);
        Color ballColor = colorValues[ballColorIndex];

        Vector2 r = Random.insideUnitCircle * goalRadius;
        Vector3 goalPoint = goalCenter.position + goalCenter.right * r.x + goalCenter.up * r.y;
        Vector3 dir = (goalPoint - spawner.position).normalized;
        Vector3 vel = dir * speed;

        bool isDanger = (ballColorIndex == blockColorIndex);

        SelectiveProjectile ball = Instantiate(projectilePrefab);
        ball.onBlocked = OnBlocked;
        ball.onGoalReached = OnGoalReached;
        ball.Fire(spawner.position, vel, isDanger, ballColor);
    }

    void OnBlocked(SelectiveProjectile ball, bool blockedByBody)
    {
        if (blockedByBody)
        {
            // Body blocks never give +1, only penalize ignores
            if (!ball.isDanger)
            {
                badBlocks++;
                score -= 1;
                if (buzzerAudioSource != null)
                {
                    buzzerAudioSource.Stop();
                    buzzerAudioSource.Play();
                }
            }
            // If it's a danger ball blocked by the body: no score change
        }
        else
        {
            // Paddle block logic stays the same
            if (ball.isDanger)
            {
                goodBlocks++;
                score += 1;
                if (blockAudioSource != null) blockAudioSource.Play();
            }
            else
            {
                badBlocks++;
                score -= 1;
                if (buzzerAudioSource != null)
                {
                    buzzerAudioSource.Stop();
                    buzzerAudioSource.Play();
                }
            }
        }

        UpdateScoreUI();
    }

    void OnGoalReached(SelectiveProjectile ball)
    {
        if (ball.isDanger)
        {
            missedBlocks++;
            score -= 1;
            if (buzzerAudioSource != null)
            {
                buzzerAudioSource.Stop();
                buzzerAudioSource.Play();
            }
        }
        else
        {
            ignoreMissed++;
        }
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    void EndGame()
    {
        running = false;
        gameEnded = true;

        if (leftPaddleObject != null) leftPaddleObject.SetActive(false);
        if (rightPaddleObject != null) rightPaddleObject.SetActive(false);

        if (resultsPanel != null) resultsPanel.SetActive(true);
        var btn = resultsPanel ? resultsPanel.GetComponentInChildren<Button>(true) : null;
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(ResetGame);
            Debug.Log("[SelectiveGoalieManager] Reset re-wired at EndGame.");
        }

        // Hide HUD texts
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (blockText != null) blockText.gameObject.SetActive(false);
        if (ignoreText != null) ignoreText.gameObject.SetActive(false);

        int totalBlocks = goodBlocks + badBlocks;
        float errorRate = totalBlocks > 0 ? (float)badBlocks / totalBlocks : 0f;

        if (resultsText != null)
        {
            resultsText.text =
                $"<size=120%><b>Game Over!</b></size>\n\n" +
                $"<size=115%>Score: <b>{score}</b></size>\n\n" +            // ← added line
                $"Danger Balls Blocked: <b>{goodBlocks}</b>\n" +
                $"Ignore Balls Blocked: <b>{badBlocks}</b>\n" +
                $"Missed Danger Balls: <b>{missedBlocks}</b>\n" +
                $"Missed Ignore Balls: <b>{ignoreMissed}</b>\n" +
                $"<size=115%>Error %: <b>{(errorRate * 100f):F1}%</b></size>";
        }

        if (timerText != null)
            timerText.text = "Time: 0";
    }


    IEnumerator RulesChangePauseSequence()
    {
        rulesChangePause = true;
        rulesChangePauseTimer = 3f;
        running = false;

        PickNewRulePair(true);                  // avoid repeating the same pair
        blockColorIndex = Random.Range(0, 2);  // re-roll which of the 2 is "block"
        ignoreColorIndex = 1 - blockColorIndex;

        int blockTextColorIndex = 1 - blockColorIndex;
        int ignoreTextColorIndex = blockColorIndex;

        if (blockChangeText != null)
            blockChangeText.text = $"Block: <b><color=#{ColorUtility.ToHtmlStringRGB(colorValues[blockTextColorIndex])}>{colorNames[blockColorIndex]}</color></b>";
        if (ignoreChangeText != null)
            ignoreChangeText.text = $"Ignore: <b><color=#{ColorUtility.ToHtmlStringRGB(colorValues[ignoreTextColorIndex])}>{colorNames[ignoreColorIndex]}</color></b>";

        if (rulesChangePanel != null)
            rulesChangePanel.SetActive(true);

        yield return new WaitForSeconds(3f);
    }

    void ResumeGameAfterRulesChange()
    {
        rulesChangePause = false;
        running = true;
        if (rulesChangePanel != null)
            rulesChangePanel.SetActive(false);

        int blockTextColorIndex = 1 - blockColorIndex;
        int ignoreTextColorIndex = blockColorIndex;
        if (blockText != null)
            blockText.text = $"Block: <b><color=#{ColorUtility.ToHtmlStringRGB(colorValues[blockTextColorIndex])}>{colorNames[blockColorIndex]}</color></b>";
        if (ignoreText != null)
            ignoreText.text = $"Ignore: <b><color=#{ColorUtility.ToHtmlStringRGB(colorValues[ignoreTextColorIndex])}>{colorNames[ignoreColorIndex]}</color></b>";

        StartCoroutine(SpawnLoop());
    }

    void SetupDropdowns()
    {
        if (timeDropdown != null)
        {
            timeDropdown.ClearOptions();
            timeDropdown.AddOptions(new System.Collections.Generic.List<string>(timeLabels));

            // Preselect the entry that matches current gameDuration, else default to 120s
            int idx = System.Array.IndexOf(timeOptions, Mathf.RoundToInt(gameDuration));
            timeDropdown.value = (idx >= 0) ? idx : 3; // default to "2:00"
            timeDropdown.RefreshShownValue();
            timeDropdown.onValueChanged.RemoveAllListeners();
            timeDropdown.onValueChanged.AddListener(OnTimeDropdownChanged);
        }

        if (speedDropdown != null)
        {
            speedDropdown.ClearOptions();
            speedDropdown.AddOptions(new System.Collections.Generic.List<string>(speedLabels));

            // Preselect the entry that matches current speed, else default to 10
            int idx = System.Array.IndexOf(speedOptions, speed);
            speedDropdown.value = (idx >= 0) ? idx : 2; // default to "Medium" (10)
            speedDropdown.RefreshShownValue();
            speedDropdown.onValueChanged.RemoveAllListeners();
            speedDropdown.onValueChanged.AddListener(OnSpeedDropdownChanged);
        }
    }

    void OnTimeDropdownChanged(int index)
    {
        if (index >= 0 && index < timeOptions.Length)
            gameDuration = timeOptions[index];
    }

    void OnSpeedDropdownChanged(int index)
    {
        if (index >= 0 && index < speedOptions.Length)
            speed = speedOptions[index];
    }
    public void ResetGame()
    {
        Debug.Log("Reset Button Clicked!");
        if (resultsPanel != null) resultsPanel.SetActive(false);


        score = 0;
        goodBlocks = 0;
        badBlocks = 0;
        missedBlocks = 0;
        ignoreMissed = 0;
        rulesChanged = false;
        rulesChangePause = false;
        gameEnded = false;
        running = false;

        if (rulesChangePanel) rulesChangePanel.SetActive(false);
        if (blockText3D) blockText3D.gameObject.SetActive(false);   // ← add
        if (ignoreText3D) ignoreText3D.gameObject.SetActive(false);  // ← add
        if (countdownText) countdownText.gameObject.SetActive(false); // ← add
        if (leftPaddleObject != null) leftPaddleObject.SetActive(false);
        if (rightPaddleObject != null) rightPaddleObject.SetActive(false);


        SetupStroopRules();


        if (startMenuCanvas != null) startMenuCanvas.gameObject.SetActive(true);
        if (timeDropdown) timeDropdown.gameObject.SetActive(true);
        if (speedDropdown) speedDropdown.gameObject.SetActive(true);
        SetupDropdowns();


        if (hudCanvas != null) hudCanvas.gameObject.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(true);
        if (blockText != null) blockText.gameObject.SetActive(true);
        if (ignoreText != null) ignoreText.gameObject.SetActive(true);


        if (playerNameInput != null) playerNameInput.text = "";


        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (startButton3D != null) startButton3D.gameObject.SetActive(true);


        foreach (var proj in Object.FindObjectsByType<SelectiveProjectile>(FindObjectsSortMode.None))
        {
            Destroy(proj.gameObject);
        }
    }
}

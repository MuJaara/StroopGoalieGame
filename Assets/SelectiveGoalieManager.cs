using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class StroopGoalieManager : MonoBehaviour
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

    private Color[] colorValues = { Color.green, Color.red };
    private string[] colorNames = { "GREEN", "RED" };
    private Color[] newColorValues = { new Color(1f, 0.55f, 0f), Color.blue };
    private string[] newColorNames = { "ORANGE", "BLUE" };

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
            cube.GetComponent<Renderer>().material.color = Color.yellow;
            Destroy(cube.GetComponent<BoxCollider>());
        }

        if (hudCanvas != null) hudCanvas.gameObject.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (leftPaddleObject != null) leftPaddleObject.SetActive(false);
        if (rightPaddleObject != null) rightPaddleObject.SetActive(false);
        if (rulesChangePanel != null) rulesChangePanel.SetActive(false);

        startMenuCanvas.gameObject.SetActive(true);
        countdownText.gameObject.SetActive(false);
        blockText3D.gameObject.SetActive(false);
        ignoreText3D.gameObject.SetActive(false);

        startButton3D.onClick.AddListener(BeginCountdown);

        SetupStroopRules();
    }

    void SetupStroopRules()
    {
        blockColorIndex = Random.Range(0, 2);
        ignoreColorIndex = 1 - blockColorIndex;
        int blockTextColorIndex = 1 - blockColorIndex;
        int ignoreTextColorIndex = blockColorIndex;

        blockText3D.text = $"Block: <b><color=#{ColorUtility.ToHtmlStringRGB(colorValues[blockTextColorIndex])}>{colorNames[blockColorIndex]}</color></b>";
        ignoreText3D.text = $"Ignore: <b><color=#{ColorUtility.ToHtmlStringRGB(colorValues[ignoreTextColorIndex])}>{colorNames[ignoreColorIndex]}</color></b>";
        blockText.text = blockText3D.text;
        ignoreText.text = ignoreText3D.text;
    }

    void BeginCountdown()
    {
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

    void OnBlocked(SelectiveProjectile ball)
    {
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

        colorValues = newColorValues;
        colorNames = newColorNames;

        blockColorIndex = Random.Range(0, 2);
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


        if (leftPaddleObject != null) leftPaddleObject.SetActive(false);
        if (rightPaddleObject != null) rightPaddleObject.SetActive(false);


        colorValues = new Color[] { Color.green, Color.red };
        colorNames = new string[] { "GREEN", "RED" };
        SetupStroopRules();


        if (startMenuCanvas != null) startMenuCanvas.gameObject.SetActive(true);


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

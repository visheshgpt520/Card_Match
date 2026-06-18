using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class GameSaveData
{
    public int rows;
    public int columns;
    public int seed;
    public int score;
    public int comboMultiplier;
    public int moves;
    public float elapsedTime;
    public int[] spriteIDs;
    public string[] cardStates;
    public bool hasSavedGame;
}

public class _CardGameManager : MonoBehaviour
{
    public static _CardGameManager Instance;

    [Header("References")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private GameObject cardList;
    [SerializeField] private Sprite cardBack;
    [SerializeField] private Sprite[] sprites;

    [Header("Panels")]
    [SerializeField] private GameObject panel;      // GamePanel
    [SerializeField] private GameObject info;       // Menu
    [SerializeField] private GameObject winPanel;   // Win

    [Header("UI")]
    [SerializeField] private TMP_Text sizeLabel;
    [SerializeField] private TMP_Text timeLabel;
    [SerializeField] private TMP_Text winTimeText;
    [SerializeField] private TMP_Text winMovesText;
    [SerializeField] private Slider sizeSlider;

    [Header("Layout Settings")]
    [SerializeField] private float cardGap = 20f;

    private _Card[] cards;
    private MatchGameModel model;
    private bool gameStart;
    
    // Layout configurations: 2x2, 2x3, 3x3, 4x3, 4x4, 5x3, 5x4, 5x5
    private readonly (int rows, int cols)[] layouts = new[]
    {
        (2, 2),
        (2, 3),
        (3, 3),
        (4, 3),
        (4, 4),
        (5, 3),
        (5, 4),
        (5, 5)
    };

    private string[] savedCardStates;
    private List<int> faceUpCardIndices = new List<int>();

    private void Awake()
    {
        Instance = this;
        model = new MatchGameModel();
    }

    private void Start()
    {
        gameStart = false;

        panel.SetActive(false);
        info.SetActive(true);

        if (winPanel != null)
            winPanel.SetActive(false);

        // Retrofit size slider to select from the 8 layout options
        if (sizeSlider != null)
        {
            sizeSlider.minValue = 0;
            sizeSlider.maxValue = layouts.Length - 1;
            sizeSlider.wholeNumbers = true;
            sizeSlider.onValueChanged.RemoveAllListeners();
            sizeSlider.onValueChanged.AddListener(delegate { SetGameSize(); });
        }

        SetGameSize();
        ConfigureStartButton();
    }    private void ConfigureStartButton()
    {
        if (info == null) return;

        // Find Start button in Menu panel
        Button startButton = null;
        Button[] buttons = info.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.name.Contains("Start") || btn.name.Contains("Button") || btn.GetComponentInChildren<TMP_Text>()?.text.Contains("START") == true || btn.GetComponentInChildren<Text>()?.text.Contains("START") == true)
            {
                startButton = btn;
                break;
            }
        }

        if (startButton != null)
        {
            // Bind click to conditionally Resume or Start fresh
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() =>
            {
                bool hasSave = PlayerPrefs.HasKey("SaveGame") && 
                               JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString("SaveGame")).hasSavedGame;
                if (hasSave)
                {
                    ResumeCardGame();
                }
                else
                {
                    StartCardGame();
                }
            });

            // Update its text label
            bool saveExists = PlayerPrefs.HasKey("SaveGame") && 
                              JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString("SaveGame")).hasSavedGame;
            TMP_Text tmpText = startButton.GetComponentInChildren<TMP_Text>();
            Text legacyText = startButton.GetComponentInChildren<Text>();

            if (saveExists)
            {
                if (tmpText != null) tmpText.text = "RESUME GAME";
                else if (legacyText != null) legacyText.text = "RESUME GAME";
            }
            else
            {
                if (tmpText != null) tmpText.text = "START GAME";
                else if (legacyText != null) legacyText.text = "START GAME";
            }
        }
    }

    public void StartCardGame()
    {
        gameStart = true;
        info.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        panel.SetActive(true);

        // Generate a random seed
        int seed = UnityEngine.Random.Range(10000, 99999);

        // Selected layout configuration
        int layoutIndex = Mathf.Clamp((int)sizeSlider.value, 0, layouts.Length - 1);
        var layout = layouts[layoutIndex];

        model.Initialize(layout.rows, layout.cols, seed, sprites.Length);
        faceUpCardIndices.Clear();

        SetGamePanel(false);

        StartCoroutine(PreGameRevealCoroutine());
    }

    private IEnumerator PreGameRevealCoroutine()
    {
        // Reveal all card faces initially
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].FlipUp();
        }

        yield return new WaitForSeconds(1.25f);

        // Flip them all back down
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].AnimateMismatch();
        }

        yield return new WaitForSeconds(0.25f); // wait for flip down to complete
        
        UpdateUI();
        SaveGame();
    }    private void SetGamePanel(bool isResume = false)
    {
        // 1. Clear old card GameObjects
        foreach (Transform child in cardList.transform)
        {
            Destroy(child.gameObject);
        }

        int rows = model.Rows;
        int columns = model.Columns;
        int totalCells = rows * columns;
        int centerIndex = totalCells % 2 != 0 ? totalCells / 2 : -1;

        int cardsToInstantiate = totalCells;
        if (centerIndex != -1) cardsToInstantiate--;

        cards = new _Card[cardsToInstantiate];

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        float width = panelRect.rect.width;
        float height = panelRect.rect.height;

        // Get original prefab card dimensions
        RectTransform prefabRect = prefab.GetComponent<RectTransform>();
        float cardW = prefabRect != null ? prefabRect.rect.width : 400f;
        float cardH = prefabRect != null ? prefabRect.rect.height : 430f;
        if (cardW <= 0f) cardW = 400f;
        if (cardH <= 0f) cardH = 430f;

        // Scaling calculations
        float scaleX = (width - (columns + 1) * cardGap) / (columns * cardW);
        float scaleY = (height - (rows + 1) * cardGap) / (rows * cardH);
        float scale = Mathf.Min(scaleX, scaleY);
        if (scale <= 0f) scale = 0.1f;

        float gridWidth = columns * (cardW * scale) + (columns - 1) * cardGap;
        float gridHeight = rows * (cardH * scale) + (rows - 1) * cardGap;

        float startX = -gridWidth / 2f + (cardW * scale) / 2f;
        float startY = gridHeight / 2f - (cardH * scale) / 2f;

        int instantiatedIndex = 0;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int cellIndex = r * columns + c;
                
                // Skip center cell if odd grid
                if (cellIndex == centerIndex)
                    continue;

                GameObject cardObj = Instantiate(prefab, cardList.transform);
                cardObj.transform.localScale = new Vector3(scale, scale, 1f);

                float x = startX + c * (cardW * scale + cardGap);
                float y = startY - r * (cardH * scale + cardGap);
                cardObj.transform.localPosition = new Vector3(x, y, 0);

                _Card cardScript = cardObj.GetComponent<_Card>();
                cards[instantiatedIndex] = cardScript;

                int spriteID = model.CardSpriteIDs[cellIndex];

                if (isResume)
                {
                    CardState savedState = System.Enum.Parse<CardState>(savedCardStates[cellIndex]);
                    Sprite sprite = sprites[spriteID];
                    cardScript.RestoreState(savedState, spriteID, sprite);
                }
                else
                {
                    cardScript.Initialize(instantiatedIndex, spriteID, cardBack);
                }

                // Explicitly bind click event
                Button btn = cardObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(cardScript.CardBtn);
                }

                instantiatedIndex++;
            }
        }
    }

    public void CardClicked(int cardIndex)
    {
        if (!gameStart) return;

        faceUpCardIndices.Add(cardIndex);

        if (faceUpCardIndices.Count == 2)
        {
            int idxA = faceUpCardIndices[0];
            int idxB = faceUpCardIndices[1];
            faceUpCardIndices.Clear();

            StartCoroutine(ResolvePairCoroutine(idxA, idxB));
        }
    }

    private IEnumerator ResolvePairCoroutine(int idxA, int idxB)
    {
        // Wait for flip animation (0.25s)
        yield return new WaitForSeconds(0.25f);

        if (idxA >= cards.Length || idxB >= cards.Length || cards[idxA] == null || cards[idxB] == null)
            yield break;

        bool isMatch;
        model.CheckMatch(idxA, idxB, out isMatch);

        if (isMatch)
        {
            // Visual match pop and fade out
            cards[idxA].AnimateMatch();
            cards[idxB].AnimateMatch();

            if (model.CardsRemaining <= 0)
            {
                yield return new WaitForSeconds(0.5f); // Wait for animations to finish
                CheckGameWin();
            }
        }
        else
        {
            // Visual mismatch pause then flip down
            yield return new WaitForSeconds(0.8f);

            if (idxA < cards.Length && idxB < cards.Length && cards[idxA] != null && cards[idxB] != null)
            {
                cards[idxA].AnimateMismatch();
                cards[idxB].AnimateMismatch();
            }
        }

        UpdateUI();
        SaveGame();
    }

    private void CheckGameWin()
    {
        gameStart = false;
        panel.SetActive(false);

        if (winPanel != null)
        {
            winPanel.SetActive(true);

            if (winTimeText != null)
                winTimeText.text = $"TIME : {model.ElapsedTime:F1}s (Seed: {model.Seed})";

            if (winMovesText != null)
                winMovesText.text = $"MOVES : {model.Moves}   SCORE : {model.Score}";
        }

        if (AudioPlayer.Instance != null)
            AudioPlayer.Instance.PlayAudio(1); // Victory music

        ClearSaveGame();
    }

    public void BackToMenu()
    {
        if (winPanel != null)
            winPanel.SetActive(false);

        panel.SetActive(false);
        info.SetActive(true);
        ConfigureStartButton();
    }

    public void RestartGame()
    {
        if (winPanel != null)
            winPanel.SetActive(false);

        StartCardGame();
    }

    public void GiveUp()
    {
        // Save current game before leaving to main menu
        SaveGame();

        gameStart = false;
        panel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        info.SetActive(true);
        ConfigureStartButton();
    }

    public void SetGameSize()
    {
        if (sizeSlider == null) return;

        int index = Mathf.Clamp((int)sizeSlider.value, 0, layouts.Length - 1);
        var layout = layouts[index];

        if (sizeLabel != null)
            sizeLabel.text = $"{layout.rows} X {layout.cols}";
    }

    public Sprite GetSprite(int spriteId)
    {
        return sprites[spriteId];
    }

    public Sprite CardBack()
    {
        return cardBack;
    }

    public bool canClick()
    {
        return gameStart;
    }

    private void Update()
    {
        // Back button / escape key quit or back handler
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameStart)
            {
                GiveUp();
            }
            else
            {
                Application.Quit();
            }
            return;
        }

        if (!gameStart)
            return;

        model.ElapsedTime += Time.deltaTime;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timeLabel != null)
        {
            string comboStr = model.ComboMultiplier > 1 ? $" (x{model.ComboMultiplier} Combo!)" : "";
            timeLabel.text = $"TIME: {model.ElapsedTime:F1}s   MOVES: {model.Moves}   SCORE: {model.Score}{comboStr}";
        }
    }

    public void SaveGame()
    {
        if (!gameStart) return;

        GameSaveData data = new GameSaveData
        {
            rows = model.Rows,
            columns = model.Columns,
            seed = model.Seed,
            score = model.Score,
            comboMultiplier = model.ComboMultiplier,
            moves = model.Moves,
            elapsedTime = model.ElapsedTime,
            spriteIDs = (int[])model.CardSpriteIDs.Clone(),
            cardStates = new string[model.CardSpriteIDs.Length],
            hasSavedGame = true
        };

        int totalCells = model.Rows * model.Columns;
        int centerIndex = totalCells % 2 != 0 ? totalCells / 2 : -1;
        int cardIdx = 0;

        for (int i = 0; i < totalCells; i++)
        {
            if (i == centerIndex)
            {
                data.cardStates[i] = CardState.Matched.ToString();
                continue;
            }

            if (cardIdx < cards.Length && cards[cardIdx] != null)
            {
                data.cardStates[i] = cards[cardIdx].State.ToString();
                cardIdx++;
            }
            else
            {
                data.cardStates[i] = CardState.Matched.ToString();
            }
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("SaveGame", json);
        PlayerPrefs.Save();

        ConfigureStartButton();
    }

    public void ResumeCardGame()
    {
        if (!PlayerPrefs.HasKey("SaveGame")) return;

        string json = PlayerPrefs.GetString("SaveGame");
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        if (!data.hasSavedGame) return;

        gameStart = true;
        info.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        panel.SetActive(true);

        model.RestoreState(
            data.rows,
            data.columns,
            data.seed,
            data.score,
            data.comboMultiplier,
            data.moves,
            data.elapsedTime,
            data.spriteIDs,
            System.Array.ConvertAll(data.cardStates, s => s == CardState.Matched.ToString())
        );

        savedCardStates = data.cardStates;

        SetGamePanel(true);
        UpdateUI();
    }

    private void ClearSaveGame()
    {
        if (PlayerPrefs.HasKey("SaveGame"))
        {
            string json = PlayerPrefs.GetString("SaveGame");
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            data.hasSavedGame = false;
            PlayerPrefs.SetString("SaveGame", JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
        ConfigureStartButton();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}
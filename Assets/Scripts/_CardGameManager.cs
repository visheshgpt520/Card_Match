using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class _CardGameManager : MonoBehaviour
{
    public static _CardGameManager Instance;
    public static int gameSize = 2;

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

    private float time;
    private int moves;

    private int spriteSelected = -1;
    private int cardSelected = -1;
    private int cardLeft;

    private bool gameStart;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        gameStart = false;

        panel.SetActive(false);
        info.SetActive(true);

        if (winPanel != null)
            winPanel.SetActive(false);

        SetGameSize();
    }

    public void StartCardGame()
    {
        if (gameStart) return;

        gameStart = true;

        time = 0;
        moves = 0;

        spriteSelected = -1;
        cardSelected = -1;

        info.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        panel.SetActive(true);

        SetGamePanel();

        cardLeft = cards.Length;

        SpriteCardAllocation();

        StartCoroutine(HideFace());
    }

    private void SetGamePanel()
    {
        foreach (Transform child in cardList.transform)
        {
            Destroy(child.gameObject);
        }

        int totalCards = gameSize * gameSize;

        if (totalCards % 2 != 0)
            totalCards--;

        cards = new _Card[totalCards];

        RectTransform panelRect = panel.GetComponent<RectTransform>();

        float width = panelRect.rect.width;
        float height = panelRect.rect.height;

        int columns = gameSize;
        int rows = Mathf.CeilToInt((float)totalCards / columns);

        // Get original prefab card dimensions from its RectTransform
        RectTransform prefabRect = prefab.GetComponent<RectTransform>();
        float cardW = prefabRect != null ? prefabRect.rect.width : 400f;
        float cardH = prefabRect != null ? prefabRect.rect.height : 430f;

        // Fallback in case prefab RectTransform is missing or has zero dimensions
        if (cardW <= 0f) cardW = 400f;
        if (cardH <= 0f) cardH = 430f;

        // Calculate scale that fits both horizontally and vertically
        float scaleX = (width - (columns + 1) * cardGap) / (columns * cardW);
        float scaleY = (height - (rows + 1) * cardGap) / (rows * cardH);

        // Maintain aspect ratio
        float scale = Mathf.Min(scaleX, scaleY);
        if (scale <= 0f) scale = 0.1f;

        // Calculate total grid dimensions including gaps
        float gridWidth = columns * (cardW * scale) + (columns - 1) * cardGap;
        float gridHeight = rows * (cardH * scale) + (rows - 1) * cardGap;

        // Starting positions to center the grid inside the GamePanel
        float startX = -gridWidth / 2f + (cardW * scale) / 2f;
        float startY = gridHeight / 2f - (cardH * scale) / 2f;

        int index = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (index >= totalCards)
                    break;

                GameObject cardObj = Instantiate(prefab, cardList.transform);

                cardObj.transform.localScale = new Vector3(scale, scale, 1f);

                float x = startX + col * (cardW * scale + cardGap);
                float y = startY - row * (cardH * scale + cardGap);

                cardObj.transform.localPosition = new Vector3(x, y, 0);

                cards[index] = cardObj.GetComponent<_Card>();
                cards[index].ID = index;

                index++;
            }
        }
    }

    private IEnumerator HideFace()
    {
        yield return new WaitForSeconds(1f);

        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].Flip();
        }
    }

    private void SpriteCardAllocation()
    {
        int pairCount = cards.Length / 2;

        int[] selectedID = new int[pairCount];

        for (int i = 0; i < pairCount; i++)
        {
            selectedID[i] = Random.Range(0, sprites.Length);
        }

        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].Active();
            cards[i].SpriteID = -1;
            cards[i].ResetRotation();
        }

        for (int i = 0; i < pairCount; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                int randomIndex = Random.Range(0, cards.Length);

                while (cards[randomIndex].SpriteID != -1)
                {
                    randomIndex = (randomIndex + 1) % cards.Length;
                }

                cards[randomIndex].SpriteID = selectedID[i];
            }
        }
    }

    public void cardClicked(int spriteId, int cardId)
    {
        if (!gameStart)
            return;

        if (spriteSelected == -1)
        {
            spriteSelected = spriteId;
            cardSelected = cardId;
            return;
        }

        if (cardSelected == cardId)
            return;

        moves++;

        if (spriteSelected == spriteId)
        {
            cards[cardSelected].Inactive();
            cards[cardId].Inactive();

            cardLeft -= 2;

            CheckGameWin();
        }
        else
        {
            cards[cardSelected].Flip();
            cards[cardId].Flip();
        }

        spriteSelected = -1;
        cardSelected = -1;
    }

    private void CheckGameWin()
    {
        if (cardLeft > 0)
            return;

        gameStart = false;

        panel.SetActive(false);

        if (winPanel != null)
        {
            winPanel.SetActive(true);

            if (winTimeText != null)
                winTimeText.text = $"TIME : {time:F1}s";

            if (winMovesText != null)
                winMovesText.text = $"MOVES : {moves}";
        }

        if (AudioPlayer.Instance != null)
            AudioPlayer.Instance.PlayAudio(1);
    }

    public void BackToMenu()
    {
        if (winPanel != null)
            winPanel.SetActive(false);

        panel.SetActive(false);
        info.SetActive(true);
    }

    public void RestartGame()
    {
        if (winPanel != null)
            winPanel.SetActive(false);

        StartCardGame();
    }

    public void GiveUp()
    {
        gameStart = false;

        panel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        info.SetActive(true);
    }

    public void SetGameSize()
    {
        gameSize = (int)sizeSlider.value;

        if (sizeLabel != null)
            sizeLabel.text = $"{gameSize} X {gameSize}";
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
        if (!gameStart)
            return;

        time += Time.deltaTime;

        if (timeLabel != null)
            timeLabel.text = $"TIME : {time:F1}s";
    }
}
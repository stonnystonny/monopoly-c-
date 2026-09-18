using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MonopolyGame : MonoBehaviour
{
    private const int BoardSize = 24;
    private readonly string[] squareNames = {
        "СТАРТ", "Парк", "Проспект", "Шанс", "Вокзал", "Набережная",
        "НАЛОГ", "Площадь", "Сквер", "Тюрьма", "Бульвар", "Шанс",
        "ФЕСТИВАЛЬ", "Аллея", "Сад", "Вокзал", "НАЛОГ", "Парк",
        "Бульвар", "БЕСПЛАТНАЯ СТОЯНКА", "Улица", "Шанс", "Вокзал", "Набережная"
    };
    private readonly int[] prices = { 0, 140, 180, 0, 220, 200, 0, 240, 260, 0, 280, 0, 0, 300, 320, 350, 0, 360, 400, 0, 420, 0, 450, 480 };
    private readonly int[] rents = { 0, 18, 22, 0, 28, 25, 0, 32, 36, 0, 40, 0, 0, 44, 48, 52, 0, 56, 64, 0, 68, 0, 72, 80 };
    private readonly int[] owners = new int[BoardSize];
    private readonly int[] positions = { 0, 0 };
    private readonly int[] money = { 1500, 1500 };
    private readonly Color[] playerColors = { new Color(0.94f, 0.28f, 0.25f), new Color(0.16f, 0.53f, 0.86f) };
    private readonly List<Text> tileLabels = new List<Text>();
    private readonly List<Image> tileImages = new List<Image>();

    private Text statusText;
    private Text diceText;
    private Text playerOneText;
    private Text playerTwoText;
    private Button buyButton;
    private Button rollButton;
    private Button endTurnButton;
    private int currentPlayer;
    private int lastRolled;
    private bool gameOver;

    private void Start()
    {
        for (int i = 0; i < owners.Length; i++) owners[i] = -1;
        BuildInterface();
        RefreshInterface("Бросайте кубики. Ход игрока 1.");
    }

    private void BuildInterface()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
        var canvasObject = new GameObject("Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 800);
        canvasObject.AddComponent<GraphicRaycaster>();
        var background = CreatePanel(canvas.transform, "Background", new Color(0.055f, 0.075f, 0.11f), Vector2.zero, new Vector2(1, 1));
        CreateText(background.transform, "MONOPOLY", 34, Color.white, new Vector2(0.5f, 0.94f), new Vector2(0.5f, 0.94f), new Vector2(0, 0), FontStyle.Bold);
        CreateText(background.transform, "Городская версия", 16, new Color(0.55f, 0.65f, 0.76f), new Vector2(0.5f, 0.885f), new Vector2(0.5f, 0.885f), new Vector2(0, 0), FontStyle.Normal);

        var board = CreatePanel(background.transform, "Board", new Color(0.93f, 0.91f, 0.84f), new Vector2(0.08f, 0.08f), new Vector2(0.63f, 0.83f));
        BuildBoard(board.transform);
        var side = CreatePanel(background.transform, "SidePanel", new Color(0.10f, 0.13f, 0.19f), new Vector2(0.70f, 0.11f), new Vector2(0.94f, 0.83f));
        playerOneText = CreateText(side.transform, "", 18, playerColors[0], new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.95f), new Vector2(0, 0), FontStyle.Bold);
        playerTwoText = CreateText(side.transform, "", 18, playerColors[1], new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.85f), new Vector2(0, 0), FontStyle.Bold);
        diceText = CreateText(side.transform, "КУБИКИ\n-  -", 30, Color.white, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.72f), new Vector2(0, 0), FontStyle.Bold);
        statusText = CreateText(side.transform, "", 16, new Color(0.82f, 0.86f, 0.91f), new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.52f), new Vector2(0, 0), FontStyle.Normal);
        endTurnButton = CreateButton(side.transform, "ПЕРЕДАТЬ ХОД", new Color(0.28f, 0.38f, 0.49f), new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.38f), EndTurn);
        endTurnButton.gameObject.SetActive(false);
        rollButton = CreateButton(side.transform, "БРОСИТЬ КУБИКИ", new Color(0.91f, 0.56f, 0.18f), new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.27f), RollDice);
        buyButton = CreateButton(side.transform, "КУПИТЬ", new Color(0.24f, 0.68f, 0.45f), new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.14f), BuyProperty);
        buyButton.gameObject.SetActive(false);
    }

    private void BuildBoard(Transform parent)
    {
        var positionsOnBoard = new List<Vector2>();
        for (int x = 0; x < 7; x++) positionsOnBoard.Add(new Vector2(x, 6));
        for (int y = 5; y >= 0; y--) positionsOnBoard.Add(new Vector2(6, y));
        for (int x = 5; x >= 0; x--) positionsOnBoard.Add(new Vector2(x, 0));
        for (int y = 1; y <= 5; y++) positionsOnBoard.Add(new Vector2(0, y));
        var inner = CreatePanel(parent, "Center", new Color(0.17f, 0.25f, 0.25f), new Vector2(0.22f, 0.22f), new Vector2(0.78f, 0.78f));
        CreateText(inner.transform, "ГОРОД\nМЕЧТЫ", 25, new Color(0.97f, 0.83f, 0.36f), new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.65f), new Vector2(0, 0), FontStyle.Bold);
        for (int i = 0; i < BoardSize; i++)
        {
            float x = 0.045f + positionsOnBoard[i].x * 0.151f;
            float y = 0.045f + positionsOnBoard[i].y * 0.151f;
            var tile = CreatePanel(parent, "Tile" + i, TileColor(i), new Vector2(x, y), new Vector2(x + 0.135f, y + 0.135f));
            tileImages.Add(tile.GetComponent<Image>());
            var label = CreateText(tile.transform, squareNames[i], 10, Color.white, new Vector2(0.04f, 0.34f), new Vector2(0.96f, 0.96f), Vector2.zero, FontStyle.Bold);
            label.alignment = TextAnchor.UpperCenter;
            tileLabels.Add(label);
        }
    }

    private Color TileColor(int index)
    {
        if (index == 0) return new Color(0.15f, 0.62f, 0.46f);
        if (index == 3 || index == 11 || index == 21) return new Color(0.44f, 0.32f, 0.56f);
        if (index == 6 || index == 16) return new Color(0.76f, 0.32f, 0.28f);
        return new Color(0.20f, 0.30f, 0.36f);
    }

    private void RollDice()
    {
        if (gameOver) return;
        lastRolled = Random.Range(1, 7) + Random.Range(1, 7);
        int oldPosition = positions[currentPlayer];
        positions[currentPlayer] = (positions[currentPlayer] + lastRolled) % BoardSize;
        if (positions[currentPlayer] < oldPosition) money[currentPlayer] += 200;
        ResolveSquare();
        rollButton.interactable = false;
        endTurnButton.gameObject.SetActive(true);
        RefreshInterface("Игрок " + (currentPlayer + 1) + " выбросил " + lastRolled + ".");
    }

    private void ResolveSquare()
    {
        int square = positions[currentPlayer];
        if (prices[square] > 0)
        {
            if (owners[square] == -1 && money[currentPlayer] >= prices[square]) buyButton.gameObject.SetActive(true);
            else if (owners[square] >= 0 && owners[square] != currentPlayer)
            {
                int rent = rents[square];
                money[currentPlayer] -= rent;
                money[owners[square]] += rent;
                if (money[currentPlayer] <= 0) EndGame(owners[square]);
            }
        }
        else if (square == 6 || square == 16)
        {
            money[currentPlayer] -= 100;
            if (money[currentPlayer] <= 0) EndGame(1 - currentPlayer);
        }
        else if (square == 3 || square == 11 || square == 21)
        {
            int change = Random.Range(-120, 181);
            money[currentPlayer] += change;
        }
    }

    private void BuyProperty()
    {
        int square = positions[currentPlayer];
        if (owners[square] != -1 || money[currentPlayer] < prices[square]) return;
        owners[square] = currentPlayer;
        money[currentPlayer] -= prices[square];
        buyButton.gameObject.SetActive(false);
        RefreshInterface("Участок куплен. Передайте ход следующему игроку.");
    }

    private void EndTurn()
    {
        if (gameOver) return;
        buyButton.gameObject.SetActive(false);
        endTurnButton.gameObject.SetActive(false);
        currentPlayer = 1 - currentPlayer;
        rollButton.interactable = true;
        RefreshInterface("Ход игрока " + (currentPlayer + 1) + ".");
    }

    private void EndGame(int winner)
    {
        gameOver = true;
        rollButton.interactable = false;
        buyButton.gameObject.SetActive(false);
        statusText.text = "Победил игрок " + (winner + 1) + "!\nНажмите Play ещё раз для новой партии.";
    }

    private void RefreshInterface(string message)
    {
        playerOneText.text = "ИГРОК 1   " + money[0] + " ₽";
        playerTwoText.text = "ИГРОК 2   " + money[1] + " ₽";
        diceText.text = lastRolled == 0 ? "КУБИКИ\n-  -" : "КУБИКИ\n" + (lastRolled / 2) + "  " + (lastRolled - lastRolled / 2);
        statusText.text = message + "\n\nСейчас: игрок " + (currentPlayer + 1) + "\nКлетка: " + squareNames[positions[currentPlayer]];
        for (int i = 0; i < BoardSize; i++)
        {
            tileLabels[i].text = squareNames[i];
            if (owners[i] >= 0) tileImages[i].color = playerColors[owners[i]];
        }
        MarkPlayer(0);
        MarkPlayer(1);
    }

    private void MarkPlayer(int player)
    {
        int square = positions[player];
        tileLabels[square].text = squareNames[square] + "\n\n" + (player == 0 ? "●" : "◆");
    }

    private Image CreatePanel(Transform parent, string objectName, Color color, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var image = obj.AddComponent<Image>(); image.color = color;
        return image;
    }

    private Text CreateText(Transform parent, string value, int size, Color color, Vector2 min, Vector2 max, Vector2 pivot, FontStyle style)
    {
        var obj = new GameObject("Text"); obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; rect.pivot = pivot;
        var text = obj.AddComponent<Text>(); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.color = color; text.fontStyle = style; text.alignment = TextAnchor.MiddleCenter; text.resizeTextForBestFit = true; text.resizeTextMinSize = 8; text.resizeTextMaxSize = size;
        return text;
    }

    private Button CreateButton(Transform parent, string label, Color color, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
    {
        var image = CreatePanel(parent, label, color, min, max);
        var button = image.gameObject.AddComponent<Button>(); button.onClick.AddListener(action);
        var text = CreateText(image.transform, label, 17, Color.white, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f), Vector2.zero, FontStyle.Bold);
        text.raycastTarget = false;
        return button;
    }

    private void Update()
    {
        if (rollButton != null && !gameOver && !rollButton.interactable && Input.GetKeyDown(KeyCode.Space)) EndTurn();
    }
}

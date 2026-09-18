using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MonopolyGame : MonoBehaviour
{
    private const int BoardSize = 24;
    private const int StartSquare = 0;
    private const int JailSquare = 6;
    private const int CasinoSquare = 12;
    private const int ParkingSquare = 18;
    private const int StartMoney = 10000;
    private const int Salary = 2000;
    private const int JailFine = 500;
    private const int ParkingFine = 500;
    private const int CasinoPrize = 1000;
    private const int HotelLevel = 5;
    private const int MaxLogLines = 8;

    private readonly string[] squareNames = {
        "СТАРТ", "Парк", "Проспект", "ШАНС", "Вокзал", "Набережная",
        "ТЮРЬМА", "Площадь", "Сквер", "Бульвар", "Аллея", "ШАНС",
        "КАЗИНО", "Сад", "Улица", "Вокзал", "НАЛОГ", "Мост",
        "СТОЯНКА", "Театр", "ШАНС", "Вокзал", "НАЛОГ", "Пристань"
    };
    private readonly int[] prices = { 0, 500, 600, 0, 700, 800, 0, 900, 1000, 1100, 1300, 0, 0, 1400, 1500, 1700, 0, 1900, 0, 2100, 0, 2300, 0, 2500 };
    private readonly int[] rents = { 0, 100, 120, 0, 140, 160, 0, 180, 200, 220, 260, 0, 0, 280, 300, 340, 0, 380, 0, 420, 0, 460, 0, 500 };
    private readonly int[] taxes = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1000, 0, 0, 0, 0, 0, 750, 0 };
    private readonly int[] groups = { -1, 0, 0, -1, 0, 1, -1, 1, 1, 2, 2, -1, -1, 2, 3, 3, -1, 3, -1, 4, -1, 4, -1, 4 };
    private readonly Color[] groupColors = {
        new Color(0.45f, 0.26f, 0.22f),
        new Color(0.20f, 0.38f, 0.50f),
        new Color(0.44f, 0.24f, 0.40f),
        new Color(0.52f, 0.34f, 0.16f),
        new Color(0.20f, 0.42f, 0.30f)
    };

    private readonly ChanceCard[] chanceCards = {
        new ChanceCard("Банк выплачивает дивиденды: +500 ₽", 500),
        new ChanceCard("Вы выиграли в лотерею: +1000 ₽", 1000),
        new ChanceCard("Пособие по безработице: +1000 ₽", 1000),
        new ChanceCard("Возврат переплаты налога: +900 ₽", 900),
        new ChanceCard("Выигрыш на скачках: +600 ₽", 600),
        new ChanceCard("Ремонт дороги: −700 ₽", -700),
        new ChanceCard("Штраф за превышение скорости: −300 ₽", -300),
        new ChanceCard("Счёт за электричество: −400 ₽", -400),
        new ChanceCard("Оплата страховки: −250 ₽", -250),
        new ChanceCard("День рождения: соперник платит 500 ₽", 500, -1, true),
        new ChanceCard("Отправляйтесь на СТАРТ", 0, StartSquare),
        new ChanceCard("Отправляйтесь в ТЮРЬМУ", 0, JailSquare)
    };

    private readonly int[] owners = new int[BoardSize];
    private readonly int[] houses = new int[BoardSize];
    private readonly int[] positions = { 0, 0 };
    private readonly int[] money = { StartMoney, StartMoney };
    private readonly bool[] inJail = { false, false };
    private readonly bool[] skipNextTurn = { false, false };
    private readonly Color[] playerColors = { new Color(0.94f, 0.28f, 0.25f), new Color(0.16f, 0.53f, 0.86f) };
    private readonly Color gainColor = new Color(0.40f, 0.85f, 0.55f);
    private readonly Color lossColor = new Color(0.94f, 0.45f, 0.40f);
    private readonly Color neutralColor = new Color(0.97f, 0.83f, 0.36f);
    private readonly Color houseColor = new Color(1f, 0.85f, 0.25f);
    private readonly Color hotelColor = new Color(1f, 0.45f, 0.35f);
    private readonly List<Text> tileLabels = new List<Text>();
    private readonly List<Text> tilePriceLabels = new List<Text>();
    private readonly List<Text> tileStarLabels = new List<Text>();
    private readonly List<Image> tileOwnerStrips = new List<Image>();
    private readonly List<Image> tileImages = new List<Image>();
    private readonly List<RectTransform> tileRects = new List<RectTransform>();
    private readonly Image[] tokenImages = new Image[2];
    private readonly GameObject[] playerTokens = new GameObject[2];
    private readonly List<Vector2> boardCells = new List<Vector2>();
    private readonly List<string> logLines = new List<string>();
    private readonly string[] playerHex = new string[2];
    private RectTransform boardRect;

    private Text turnText;
    private Text actionText;
    private Text logText;
    private Text playerOneText;
    private Text playerTwoText;
    private Text buildButtonText;
    private Button buyButton;
    private Button buildButton;
    private Button rollButton;
    private Button endTurnButton;
    private Button payFineButton;
    private int currentPlayer;
    private int dieOne;
    private int dieTwo;
    private int lastRolled;
    private int pendingJump = -1;
    private string actionMessage = "";
    private Color actionColor = Color.white;
    private bool gameOver;
    private bool isMoving;
    private bool turnResolved;

    private struct ChanceCard
    {
        public readonly string Text;
        public readonly int Amount;
        public readonly int MoveTo;
        public readonly bool FromOpponent;

        public ChanceCard(string text, int amount, int moveTo = -1, bool fromOpponent = false)
        {
            Text = text;
            Amount = amount;
            MoveTo = moveTo;
            FromOpponent = fromOpponent;
        }
    }

    private void Start()
    {
        for (int i = 0; i < owners.Length; i++) owners[i] = -1;
        playerHex[0] = ColorUtility.ToHtmlStringRGB(playerColors[0]);
        playerHex[1] = ColorUtility.ToHtmlStringRGB(playerColors[1]);
        BuildInterface();
        SetAction("Игра началась: у каждого по " + StartMoney + " ₽", neutralColor, false);
        LogSystem("Новая партия. Ход игрока 1.");
        RefreshInterface();
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

        var board = CreatePanel(background.transform, "Board", new Color(0.93f, 0.91f, 0.84f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        boardRect = board.rectTransform;
        boardRect.sizeDelta = new Vector2(650, 650);
        boardRect.anchoredPosition = new Vector2(-205, -5);
        BuildBoard(board.transform);
        BuildCenterMenu(board.transform);
        BuildTokenVisuals(board.transform);
    }

    private void BuildCenterMenu(Transform board)
    {
        var menu = CreatePanel(board, "CenterMenu", new Color(0.07f, 0.10f, 0.16f), new Vector2(0.20f, 0.20f), new Vector2(0.80f, 0.80f));
        CreateText(menu.transform, "ЦЕНТР ГОРОДА", 14, neutralColor, new Vector2(0.04f, 0.945f), new Vector2(0.96f, 1f), Vector2.zero, FontStyle.Bold);
        playerOneText = CreateText(menu.transform, "", 15, playerColors[0], new Vector2(0.03f, 0.875f), new Vector2(0.49f, 0.94f), Vector2.zero, FontStyle.Bold);
        playerTwoText = CreateText(menu.transform, "", 15, playerColors[1], new Vector2(0.51f, 0.875f), new Vector2(0.97f, 0.94f), Vector2.zero, FontStyle.Bold);
        turnText = CreateText(menu.transform, "", 14, Color.white, new Vector2(0.03f, 0.80f), new Vector2(0.97f, 0.87f), Vector2.zero, FontStyle.Bold);

        var actionPanel = CreatePanel(menu.transform, "ActionPanel", new Color(0.13f, 0.17f, 0.25f), new Vector2(0.03f, 0.715f), new Vector2(0.97f, 0.79f));
        actionText = CreateText(actionPanel.transform, "", 13, neutralColor, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), Vector2.zero, FontStyle.Bold);

        var logPanel = CreatePanel(menu.transform, "LogPanel", new Color(0.04f, 0.06f, 0.10f), new Vector2(0.03f, 0.285f), new Vector2(0.97f, 0.705f));
        CreateText(logPanel.transform, "ЖУРНАЛ ХОДОВ", 11, new Color(0.45f, 0.55f, 0.66f), new Vector2(0.03f, 0.87f), new Vector2(0.97f, 0.99f), Vector2.zero, FontStyle.Bold);
        logText = CreateText(logPanel.transform, "", 10, new Color(0.82f, 0.86f, 0.91f), new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.86f), Vector2.zero, FontStyle.Normal);
        logText.resizeTextForBestFit = false;
        logText.alignment = TextAnchor.UpperLeft;
        logText.verticalOverflow = VerticalWrapMode.Truncate;
        logText.lineSpacing = 1.1f;

        rollButton = CreateButton(menu.transform, "БРОСИТЬ КУБИКИ", new Color(0.91f, 0.56f, 0.18f), new Vector2(0.03f, 0.155f), new Vector2(0.97f, 0.275f), RollDice);
        buyButton = CreateButton(menu.transform, "КУПИТЬ", new Color(0.24f, 0.68f, 0.45f), new Vector2(0.03f, 0.02f), new Vector2(0.35f, 0.14f), BuyProperty);
        payFineButton = CreateButton(menu.transform, "ШТРАФ " + JailFine, new Color(0.75f, 0.28f, 0.25f), new Vector2(0.03f, 0.02f), new Vector2(0.35f, 0.14f), PayJailFine);
        buildButton = CreateButton(menu.transform, "ДОМ", new Color(0.47f, 0.40f, 0.72f), new Vector2(0.365f, 0.02f), new Vector2(0.665f, 0.14f), BuildHouse);
        buildButtonText = buildButton.GetComponentInChildren<Text>();
        endTurnButton = CreateButton(menu.transform, "ПЕРЕДАТЬ ХОД", new Color(0.28f, 0.38f, 0.49f), new Vector2(0.68f, 0.02f), new Vector2(0.97f, 0.14f), EndTurn);
        buyButton.gameObject.SetActive(false);
        payFineButton.gameObject.SetActive(false);
        buildButton.gameObject.SetActive(false);
        endTurnButton.gameObject.SetActive(false);
    }

    private void BuildBoard(Transform parent)
    {
        boardCells.Clear();
        for (int x = 0; x < 7; x++) boardCells.Add(new Vector2(x, 6));
        for (int y = 5; y >= 0; y--) boardCells.Add(new Vector2(6, y));
        for (int x = 5; x >= 0; x--) boardCells.Add(new Vector2(x, 0));
        for (int y = 1; y <= 5; y++) boardCells.Add(new Vector2(0, y));
        CreatePanel(parent, "Center", new Color(0.17f, 0.25f, 0.25f), new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f));
        for (int i = 0; i < BoardSize; i++)
        {
            float x = 0.02f + boardCells[i].x * 0.14f;
            float y = 0.02f + boardCells[i].y * 0.14f;
            var tile = CreatePanel(parent, "Tile" + i, TileColor(i), new Vector2(x, y), new Vector2(x + 0.125f, y + 0.125f));
            tileImages.Add(tile.GetComponent<Image>());
            tileRects.Add(tile.rectTransform);

            var strip = CreatePanel(tile.transform, "Owner", new Color(1f, 1f, 1f, 0f), new Vector2(0f, 0.90f), new Vector2(1f, 1f));
            strip.raycastTarget = false;
            tileOwnerStrips.Add(strip);

            var label = CreateText(tile.transform, squareNames[i], 10, Color.white, new Vector2(0.04f, 0.64f), new Vector2(0.96f, 0.885f), Vector2.zero, FontStyle.Bold);
            tileLabels.Add(label);

            var stars = CreateText(tile.transform, "", 11, houseColor, new Vector2(0.02f, 0.46f), new Vector2(0.98f, 0.63f), Vector2.zero, FontStyle.Bold);
            tileStarLabels.Add(stars);

            var priceLabel = CreateText(tile.transform, TilePriceCaption(i), 10, neutralColor, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.26f), Vector2.zero, FontStyle.Bold);
            priceLabel.alignment = TextAnchor.LowerCenter;
            tilePriceLabels.Add(priceLabel);
        }
    }

    private string TilePriceCaption(int index)
    {
        if (owners[index] >= 0) return "аренда " + RentFor(index);
        if (prices[index] > 0) return prices[index] + " ₽";
        if (taxes[index] > 0) return "−" + taxes[index] + " ₽";
        if (index == StartSquare) return "+" + Salary + " ₽";
        if (index == JailSquare) return "выход " + JailFine;
        if (index == CasinoSquare) return "дубль +" + CasinoPrize;
        if (index == ParkingSquare) return "−" + ParkingFine + " ₽";
        return "?";
    }

    private bool IsChanceSquare(int index)
    {
        return index == 3 || index == 11 || index == 20;
    }

    private Color TileColor(int index)
    {
        if (index == StartSquare) return new Color(0.15f, 0.62f, 0.46f);
        if (index == JailSquare) return new Color(0.72f, 0.27f, 0.24f);
        if (index == CasinoSquare) return new Color(0.72f, 0.37f, 0.15f);
        if (index == ParkingSquare) return new Color(0.54f, 0.54f, 0.58f);
        if (IsChanceSquare(index)) return new Color(0.44f, 0.32f, 0.56f);
        if (taxes[index] > 0) return new Color(0.76f, 0.32f, 0.28f);
        if (groups[index] >= 0) return groupColors[groups[index]];
        return new Color(0.20f, 0.30f, 0.36f);
    }

    private int RentFor(int square)
    {
        float multiplier;
        switch (houses[square])
        {
            case 1: multiplier = 1.1f; break;
            case 2: multiplier = 1.2f; break;
            case 3: multiplier = 1.4f; break;
            case 4: multiplier = 1.8f; break;
            case HotelLevel: multiplier = 2f; break;
            default: multiplier = 1f; break;
        }
        return Mathf.RoundToInt(rents[square] * multiplier);
    }

    private int HouseCost(int square)
    {
        return Mathf.RoundToInt(prices[square] * 0.25f / 10f) * 10;
    }

    private bool OwnsWholeGroup(int player, int square)
    {
        int group = groups[square];
        if (group < 0) return false;
        for (int i = 0; i < BoardSize; i++)
            if (groups[i] == group && owners[i] != player) return false;
        return true;
    }

    private bool CanBuildHere(int player)
    {
        int square = positions[player];
        return !gameOver && !isMoving && turnResolved
            && owners[square] == player
            && houses[square] < HotelLevel
            && OwnsWholeGroup(player, square)
            && money[player] >= HouseCost(square);
    }

    private bool CanBuyHere(int player)
    {
        int square = positions[player];
        return !gameOver && !isMoving && turnResolved
            && prices[square] > 0
            && owners[square] == -1
            && money[player] >= prices[square];
    }

    private void RollDice()
    {
        if (gameOver || isMoving) return;
        turnResolved = false;
        UpdateActionButtons();
        dieOne = Random.Range(1, 7);
        dieTwo = Random.Range(1, 7);
        lastRolled = dieOne + dieTwo;
        Log("бросает кубики: " + dieOne + " + " + dieTwo + " = " + lastRolled + (dieOne == dieTwo ? " (дубль)" : ""));
        if (inJail[currentPlayer] && dieOne != dieTwo)
        {
            payFineButton.gameObject.SetActive(money[currentPlayer] >= JailFine);
            endTurnButton.gameObject.SetActive(true);
            rollButton.interactable = false;
            SetAction("ТЮРЬМА: дубля нет — заплатите " + JailFine + " ₽ или пропустите ход", lossColor);
            RefreshInterface();
            return;
        }
        if (inJail[currentPlayer]) inJail[currentPlayer] = false;
        rollButton.interactable = false;
        endTurnButton.gameObject.SetActive(false);
        MoveAfterRoll();
    }

    private void MoveAfterRoll()
    {
        int oldPosition = positions[currentPlayer];
        positions[currentPlayer] = (positions[currentPlayer] + lastRolled) % BoardSize;
        if (positions[currentPlayer] < oldPosition)
        {
            money[currentPlayer] += Salary;
            Log("проходит СТАРТ: +" + Salary + " ₽");
        }
        StartCoroutine(MoveTokenAlongPath(currentPlayer, oldPosition, lastRolled));
    }

    private void ResolveSquare()
    {
        int square = positions[currentPlayer];
        if (square == JailSquare)
        {
            inJail[currentPlayer] = true;
            SetAction("ТЮРЬМА: заплатите " + JailFine + " ₽ или выбросьте дубль", lossColor);
        }
        else if (square == CasinoSquare)
        {
            if (dieOne == dieTwo)
            {
                money[currentPlayer] += CasinoPrize;
                SetAction("КАЗИНО: дубль — получите " + CasinoPrize + " ₽", gainColor);
            }
            else SetAction("КАЗИНО: дубля нет — выигрыша нет", neutralColor);
        }
        else if (square == ParkingSquare)
        {
            skipNextTurn[currentPlayer] = true;
            money[currentPlayer] -= ParkingFine;
            SetAction("СТОЯНКА: заплатите " + ParkingFine + " ₽ и пропустите ход", lossColor);
            CheckBankruptcy();
        }
        else if (IsChanceSquare(square))
        {
            DrawChanceCard();
        }
        else if (taxes[square] > 0)
        {
            money[currentPlayer] -= taxes[square];
            SetAction("НАЛОГ: заплатите " + taxes[square] + " ₽ в казну", lossColor);
            CheckBankruptcy();
        }
        else if (prices[square] > 0)
        {
            if (owners[square] == -1)
            {
                bool affordable = money[currentPlayer] >= prices[square];
                SetAction(affordable
                    ? squareNames[square] + ": можно купить за " + prices[square] + " ₽"
                    : squareNames[square] + ": не хватает денег (" + prices[square] + " ₽)", neutralColor, false);
            }
            else if (owners[square] != currentPlayer)
            {
                int rent = RentFor(square);
                money[currentPlayer] -= rent;
                money[owners[square]] += rent;
                string extra = houses[square] == HotelLevel ? " (отель)"
                    : houses[square] > 0 ? " (" + houses[square] + " дом.)" : "";
                SetAction("АРЕНДА" + extra + ": платите " + rent + " ₽ игроку " + (owners[square] + 1), lossColor);
                CheckBankruptcy();
            }
            else SetAction(squareNames[square] + ": ваш участок", gainColor, false);
        }
        else if (square == StartSquare)
        {
            SetAction("СТАРТ: получите " + Salary + " ₽", gainColor, false);
        }
    }

    private void DrawChanceCard()
    {
        var card = chanceCards[Random.Range(0, chanceCards.Length)];
        if (card.MoveTo >= 0)
        {
            pendingJump = card.MoveTo;
            SetAction("ШАНС: " + card.Text, neutralColor);
            return;
        }
        money[currentPlayer] += card.Amount;
        if (card.FromOpponent) money[1 - currentPlayer] -= card.Amount;
        SetAction("ШАНС: " + card.Text, card.Amount >= 0 ? gainColor : lossColor);
        CheckBankruptcy();
    }

    private void ApplyJumpEffect(int player, int square)
    {
        if (square == StartSquare)
        {
            money[player] += Salary;
            SetAction("СТАРТ: получите " + Salary + " ₽", gainColor);
        }
        else if (square == JailSquare)
        {
            inJail[player] = true;
            SetAction("ТЮРЬМА: заплатите " + JailFine + " ₽ или выбросьте дубль", lossColor);
        }
    }

    private void CheckBankruptcy()
    {
        if (money[currentPlayer] <= 0) EndGame(1 - currentPlayer);
        else if (money[1 - currentPlayer] <= 0) EndGame(currentPlayer);
    }

    private void PayJailFine()
    {
        if (!inJail[currentPlayer] || money[currentPlayer] < JailFine) return;
        money[currentPlayer] -= JailFine;
        inJail[currentPlayer] = false;
        payFineButton.gameObject.SetActive(false);
        endTurnButton.gameObject.SetActive(false);
        SetAction("ТЮРЬМА: штраф " + JailFine + " ₽ оплачен", lossColor);
        MoveAfterRoll();
        RefreshInterface();
    }

    private void BuyProperty()
    {
        int square = positions[currentPlayer];
        if (!CanBuyHere(currentPlayer)) return;
        owners[square] = currentPlayer;
        money[currentPlayer] -= prices[square];
        SetAction("КУПЛЕНО: " + squareNames[square] + " за " + prices[square] + " ₽", gainColor);
        if (OwnsWholeGroup(currentPlayer, square)) LogSystem("Группа из 3 клеток собрана — можно строить дома.");
        UpdateActionButtons();
        RefreshInterface();
    }

    private void BuildHouse()
    {
        int square = positions[currentPlayer];
        if (!CanBuildHere(currentPlayer)) return;
        int cost = HouseCost(square);
        money[currentPlayer] -= cost;
        houses[square]++;
        if (houses[square] == HotelLevel)
            SetAction("ОТЕЛЬ на " + squareNames[square] + " за " + cost + " ₽: аренда +100%", gainColor);
        else
            SetAction("ДОМ " + houses[square] + "/4 на " + squareNames[square] + " за " + cost + " ₽: аренда " + RentFor(square) + " ₽", gainColor);
        UpdateActionButtons();
        RefreshInterface();
    }

    private void EndTurn()
    {
        if (gameOver || isMoving) return;
        turnResolved = false;
        buyButton.gameObject.SetActive(false);
        buildButton.gameObject.SetActive(false);
        endTurnButton.gameObject.SetActive(false);
        payFineButton.gameObject.SetActive(false);
        currentPlayer = 1 - currentPlayer;
        if (skipNextTurn[currentPlayer])
        {
            skipNextTurn[currentPlayer] = false;
            SetAction("Игрок " + (currentPlayer + 1) + " пропускает ход (штрафстоянка)", lossColor);
            currentPlayer = 1 - currentPlayer;
        }
        rollButton.interactable = true;
        LogSystem("Ход переходит к игроку " + (currentPlayer + 1) + ".");
        RefreshInterface();
    }

    private void EndGame(int winner)
    {
        gameOver = true;
        rollButton.interactable = false;
        buyButton.gameObject.SetActive(false);
        buildButton.gameObject.SetActive(false);
        endTurnButton.gameObject.SetActive(false);
        payFineButton.gameObject.SetActive(false);
        SetAction("ПОБЕДА: игрок " + (winner + 1) + " разорил соперника", gainColor, false);
        LogSystem("Победил игрок " + (winner + 1) + "! Нажмите Play для новой партии.");
        RefreshInterface();
    }

    private void SetAction(string message, Color color, bool addToLog = true)
    {
        actionMessage = message;
        actionColor = color;
        if (addToLog) Log(message);
    }

    private void Log(string message)
    {
        AppendLog("<color=#" + playerHex[currentPlayer] + "><b>И" + (currentPlayer + 1) + ":</b></color> " + message);
    }

    private void LogSystem(string message)
    {
        AppendLog("<color=#6E7D8C>" + message + "</color>");
    }

    private void AppendLog(string line)
    {
        logLines.Insert(0, line);
        while (logLines.Count > MaxLogLines) logLines.RemoveAt(logLines.Count - 1);
        if (logText != null) logText.text = string.Join("\n", logLines.ToArray());
    }

    private void UpdateActionButtons()
    {
        buyButton.gameObject.SetActive(CanBuyHere(currentPlayer));
        bool canBuild = CanBuildHere(currentPlayer);
        buildButton.gameObject.SetActive(canBuild);
        if (canBuild)
        {
            int square = positions[currentPlayer];
            buildButtonText.text = (houses[square] == 4 ? "ОТЕЛЬ " : "ДОМ ") + HouseCost(square);
        }
    }

    private void RefreshInterface()
    {
        playerOneText.text = "ИГРОК 1  " + money[0] + " ₽";
        playerTwoText.text = "ИГРОК 2  " + money[1] + " ₽";
        turnText.text = "ХОД ИГРОКА " + (currentPlayer + 1) + "   ·   " +
            (lastRolled == 0 ? "КУБИКИ  -  -" : "КУБИКИ  " + dieOne + " + " + dieTwo + " = " + lastRolled);
        actionText.text = actionMessage;
        actionText.color = actionColor;
        for (int i = 0; i < BoardSize; i++)
        {
            tilePriceLabels[i].text = TilePriceCaption(i);
            if (owners[i] >= 0)
            {
                var strip = tileOwnerStrips[i];
                strip.color = playerColors[owners[i]];
                tilePriceLabels[i].color = Color.white;
            }
            var stars = tileStarLabels[i];
            if (houses[i] == HotelLevel)
            {
                stars.text = "★";
                stars.fontSize = 20;
                stars.resizeTextMaxSize = 20;
                stars.color = hotelColor;
            }
            else
            {
                stars.text = new string('★', houses[i]);
                stars.fontSize = 11;
                stars.resizeTextMaxSize = 11;
                stars.color = houseColor;
            }
        }
    }

    private GameObject CreateToken(string tokenName, Color color)
    {
        var token = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        token.name = tokenName;
        token.transform.localScale = new Vector3(0.32f, 0.55f, 0.32f);
        var material = new Material(Shader.Find("Unlit/Color"));
        material.color = color;
        token.GetComponent<Renderer>().material = material;
        return token;
    }

    private void BuildTokenVisuals(Transform board)
    {
        tokenImages[0] = CreateTokenImage(board, "Красная фишка", new Color(0.95f, 0.12f, 0.10f));
        tokenImages[1] = CreateTokenImage(board, "Синяя фишка", new Color(0.10f, 0.36f, 0.95f));
        playerTokens[0] = CreateToken("Красная фишка", new Color(0.95f, 0.12f, 0.10f));
        playerTokens[1] = CreateToken("Синяя фишка", new Color(0.10f, 0.36f, 0.95f));
        PlaceTokenOnStart(0);
        PlaceTokenOnStart(1);
    }

    private void PlaceTokenOnStart(int player)
    {
        positions[player] = StartSquare;
        tokenImages[player].rectTransform.anchoredPosition = TokenUiPositionForSquare(StartSquare, player);
        playerTokens[player].transform.position = TokenWorldPositionForSquare(StartSquare, player);
    }

    private Image CreateTokenImage(Transform parent, string tokenName, Color color)
    {
        var image = CreatePanel(parent, tokenName, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        image.rectTransform.sizeDelta = new Vector2(24, 24);
        image.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
        var highlight = CreatePanel(image.transform, "Highlight", new Color(1f, 1f, 1f, 0.55f), new Vector2(0.20f, 0.62f), new Vector2(0.40f, 0.84f));
        highlight.raycastTarget = false;
        image.raycastTarget = false;
        return image;
    }

    private Vector2 TokenUiPositionForSquare(int square, int player)
    {
        var rect = tileRects[square];
        Vector2 normalizedCenter = (rect.anchorMin + rect.anchorMax) * 0.5f;
        Vector2 boardSize = boardRect != null ? boardRect.rect.size : new Vector2(650f, 650f);
        return (normalizedCenter - new Vector2(0.5f, 0.5f)) * boardSize + new Vector2(player == 0 ? -14f : 14f, -16f);
    }

    private Vector3 TokenWorldPositionForSquare(int square, int player)
    {
        Vector2 cell = square >= 0 && square < boardCells.Count ? boardCells[square] : Vector2.zero;
        float x = cell.x - 3f;
        float z = cell.y - 3f;
        return new Vector3(x * 1.25f + (player == 0 ? -0.18f : 0.18f), 0.65f, z * 1.25f);
    }

    private IEnumerator MoveTokenAlongPath(int player, int oldPosition, int steps)
    {
        isMoving = true;
        for (int step = 1; step <= steps; step++)
        {
            int nextSquare = (oldPosition + step) % BoardSize;
            yield return StartCoroutine(MoveTokenToCell(player, nextSquare));
        }
        Log("попадает на клетку «" + squareNames[positions[player]] + "»");
        ResolveSquare();
        RefreshInterface();
        if (pendingJump >= 0)
        {
            int target = pendingJump;
            pendingJump = -1;
            yield return StartCoroutine(MoveTokenToCell(player, target));
            positions[player] = target;
            ApplyJumpEffect(player, target);
        }
        isMoving = false;
        turnResolved = true;
        if (!gameOver)
        {
            endTurnButton.gameObject.SetActive(true);
            UpdateActionButtons();
        }
        RefreshInterface();
    }

    private IEnumerator MoveTokenToCell(int player, int square)
    {
        Vector2 startUiPosition = tokenImages[player].rectTransform.anchoredPosition;
        Vector2 targetUiPosition = TokenUiPositionForSquare(square, player);
        Vector3 startWorldPosition = playerTokens[player].transform.position;
        Vector3 targetWorldPosition = TokenWorldPositionForSquare(square, player);
        float elapsed = 0f;
        const float cellMoveDuration = 0.24f;

        while (elapsed < cellMoveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / cellMoveDuration);
            progress = progress * progress * (3f - 2f * progress);
            tokenImages[player].rectTransform.anchoredPosition = Vector2.Lerp(startUiPosition, targetUiPosition, progress);
            playerTokens[player].transform.position = Vector3.Lerp(startWorldPosition, targetWorldPosition, progress);
            playerTokens[player].transform.Rotate(0f, 240f * Time.deltaTime, 0f);
            yield return null;
        }

        tokenImages[player].rectTransform.anchoredPosition = targetUiPosition;
        playerTokens[player].transform.position = targetWorldPosition;
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
        var text = CreateText(image.transform, label, 16, Color.white, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), Vector2.zero, FontStyle.Bold);
        text.raycastTarget = false;
        return button;
    }

    private void Update()
    {
        if (rollButton != null && !gameOver && !isMoving && !rollButton.interactable && Input.GetKeyDown(KeyCode.Space)) EndTurn();
    }
}

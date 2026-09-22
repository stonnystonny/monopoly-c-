using System.Collections;
using Monopoly.Board;
using Monopoly.Core;
using Monopoly.UI;
using UnityEngine;

/// <summary>
/// Точка входа и управление ходом партии: связывает правила (Monopoly.Core),
/// 3D-доску (Monopoly.Board) и интерфейс (Monopoly.UI). Логика отрисовки здесь не живёт.
/// </summary>
public class MonopolyGame : MonoBehaviour
{
    private readonly GameState state = new GameState();
    private readonly GameLog log = new GameLog();

    private BoardView board;
    private GameHud hud;

    private string actionMessage = "";
    private Color actionColor = Color.white;
    private int pendingJump = -1;
    private bool gameOver;
    private bool isMoving;
    private bool turnResolved;

    // ---------------------------------------------------------------- запуск

    private void Awake()
    {
        LockLandscapeOrientation();
    }

    private void Start()
    {
        state.Reset();
        BuildScene();

        SetAction("Игра началась: у каждого по " + GameRules.StartMoney + " ₽", Palette.Neutral, false);
        log.System("Новая партия. Ход игрока 1.");
        RefreshAll();
    }

    /// <summary>
    /// Игра рассчитана на телефон в горизонтальном положении. Ориентацию трогаем только на самом
    /// устройстве: в редакторе эти вызовы ломают viewport камеры, и 3D-сцена перестаёт рисоваться.
    /// </summary>
    private static void LockLandscapeOrientation()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.orientation = ScreenOrientation.AutoRotation;
#endif
    }

    private void BuildScene()
    {
        Camera boardCamera = Camera.main;
        if (boardCamera == null)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            boardCamera = cameraObject.AddComponent<Camera>();
        }
        if (boardCamera.GetComponent<BoardCamera>() == null) boardCamera.gameObject.AddComponent<BoardCamera>();

        board = new GameObject("Board").AddComponent<BoardView>();
        board.Build(boardCamera);
        board.PlaceTokens(state);

        hud = new GameObject("Hud").AddComponent<GameHud>();
        hud.Build(board.MenuAnchor, new GameHud.Actions
        {
            Roll = RollDice,
            Buy = BuyProperty,
            Build = BuildHouse,
            EndTurn = EndTurn,
            PayFine = PayJailFine
        });
    }

    // ---------------------------------------------------------------- ход игрока

    private void RollDice()
    {
        if (gameOver || isMoving) return;

        turnResolved = false;
        state.RollDice();
        log.Player(state.CurrentPlayer, "бросает кубики: " + state.DieOne + " + " + state.DieTwo +
                                        " = " + state.LastRolled + (state.IsDouble ? " (дубль)" : ""));

        if (state.InJail[state.CurrentPlayer] && !state.IsDouble)
        {
            SetAction("ТЮРЬМА: дубля нет — заплатите " + GameRules.JailFine + " ₽ или пропустите ход", Palette.Loss);
            hud.SetButtons(false, false, false, string.Empty, true,
                state.Money[state.CurrentPlayer] >= GameRules.JailFine);
            RefreshAll();
            return;
        }

        state.InJail[state.CurrentPlayer] = false;
        hud.SetButtons(false, false, false, string.Empty, false, false);
        MoveAfterRoll();
    }

    private void MoveAfterRoll()
    {
        int fromSquare = state.Positions[state.CurrentPlayer];
        if (state.AdvanceCurrentPlayer(state.LastRolled))
            log.Player(state.CurrentPlayer, "проходит СТАРТ: +" + GameRules.Salary + " ₽");
        StartCoroutine(MoveSequence(state.CurrentPlayer, fromSquare, state.LastRolled));
    }

    private IEnumerator MoveSequence(int player, int fromSquare, int steps)
    {
        isMoving = true;
        yield return board.MoveAlongPath(player, fromSquare, steps);

        log.Player(player, "попадает на клетку «" + BoardData.Names[state.Positions[player]] + "»");
        ResolveSquare();
        RefreshAll();

        if (pendingJump >= 0)
        {
            int target = pendingJump;
            pendingJump = -1;
            yield return board.HopTo(player, target);
            state.Positions[player] = target;
            ApplyJumpEffect(player, target);
        }

        isMoving = false;
        turnResolved = true;
        if (!gameOver) UpdateActionButtons();
        RefreshAll();
    }

    private void ResolveSquare()
    {
        int player = state.CurrentPlayer;
        int square = state.Positions[player];

        if (square == GameRules.JailSquare)
        {
            state.InJail[player] = true;
            SetAction("ТЮРЬМА: заплатите " + GameRules.JailFine + " ₽ или выбросьте дубль", Palette.Loss);
        }
        else if (square == GameRules.CasinoSquare)
        {
            if (state.IsDouble)
            {
                state.Money[player] += GameRules.CasinoPrize;
                SetAction("КАЗИНО: дубль — получите " + GameRules.CasinoPrize + " ₽", Palette.Gain);
            }
            else SetAction("КАЗИНО: дубля нет — выигрыша нет", Palette.Neutral);
        }
        else if (square == GameRules.ParkingSquare)
        {
            state.SkipNextTurn[player] = true;
            state.Money[player] -= GameRules.ParkingFine;
            SetAction("СТОЯНКА: заплатите " + GameRules.ParkingFine + " ₽ и пропустите ход", Palette.Loss);
            CheckBankruptcy();
        }
        else if (BoardData.IsChance(square))
        {
            DrawChanceCard();
        }
        else if (BoardData.Taxes[square] > 0)
        {
            state.Money[player] -= BoardData.Taxes[square];
            SetAction("НАЛОГ: заплатите " + BoardData.Taxes[square] + " ₽ в казну", Palette.Loss);
            CheckBankruptcy();
        }
        else if (BoardData.IsProperty(square))
        {
            ResolveProperty(player, square);
        }
        else if (square == GameRules.StartSquare)
        {
            SetAction("СТАРТ: получите " + GameRules.Salary + " ₽", Palette.Gain, false);
        }
    }

    private void ResolveProperty(int player, int square)
    {
        if (state.Owners[square] == -1)
        {
            bool affordable = state.Money[player] >= BoardData.Prices[square];
            SetAction(affordable
                ? BoardData.Names[square] + ": можно купить за " + BoardData.Prices[square] + " ₽"
                : BoardData.Names[square] + ": не хватает денег (" + BoardData.Prices[square] + " ₽)",
                Palette.Neutral, false);
        }
        else if (state.Owners[square] != player)
        {
            int rent = state.RentFor(square);
            state.Transfer(player, state.Owners[square], rent);
            string extra = state.Houses[square] == GameRules.HotelLevel ? " (отель)"
                : state.Houses[square] > 0 ? " (" + state.Houses[square] + " дом.)" : "";
            SetAction("АРЕНДА" + extra + ": платите " + rent + " ₽ игроку " + (state.Owners[square] + 1), Palette.Loss);
            CheckBankruptcy();
        }
        else SetAction(BoardData.Names[square] + ": ваш участок", Palette.Gain, false);
    }

    private void DrawChanceCard()
    {
        ChanceCard card = ChanceDeck.Draw();
        if (card.IsMove)
        {
            pendingJump = card.MoveTo;
            SetAction("ШАНС: " + card.Text, Palette.Neutral);
            return;
        }

        state.Money[state.CurrentPlayer] += card.Amount;
        if (card.FromOpponent) state.Money[state.Opponent] -= card.Amount;
        SetAction("ШАНС: " + card.Text, card.Amount >= 0 ? Palette.Gain : Palette.Loss);
        CheckBankruptcy();
    }

    private void ApplyJumpEffect(int player, int square)
    {
        if (square == GameRules.StartSquare)
        {
            state.Money[player] += GameRules.Salary;
            SetAction("СТАРТ: получите " + GameRules.Salary + " ₽", Palette.Gain);
        }
        else if (square == GameRules.JailSquare)
        {
            state.InJail[player] = true;
            SetAction("ТЮРЬМА: заплатите " + GameRules.JailFine + " ₽ или выбросьте дубль", Palette.Loss);
        }
    }

    // ---------------------------------------------------------------- действия игрока

    private void PayJailFine()
    {
        int player = state.CurrentPlayer;
        if (!state.InJail[player] || state.Money[player] < GameRules.JailFine) return;

        state.Money[player] -= GameRules.JailFine;
        state.InJail[player] = false;
        SetAction("ТЮРЬМА: штраф " + GameRules.JailFine + " ₽ оплачен", Palette.Loss);
        hud.SetButtons(false, false, false, string.Empty, false, false);
        MoveAfterRoll();
        RefreshAll();
    }

    private void BuyProperty()
    {
        if (!CanBuyNow()) return;

        int square = state.CurrentSquare;
        state.Buy(state.CurrentPlayer);
        SetAction("КУПЛЕНО: " + BoardData.Names[square] + " за " + BoardData.Prices[square] + " ₽", Palette.Gain);
        if (state.OwnsWholeGroup(state.CurrentPlayer, square))
            log.System("Группа из " + BoardData.GroupSize(BoardData.Groups[square]) +
                       " улиц собрана — можно строить дома.");

        UpdateActionButtons();
        RefreshAll();
    }

    private void BuildHouse()
    {
        if (!CanBuildNow()) return;

        int square = state.CurrentSquare;
        int cost = state.Build(state.CurrentPlayer);
        if (state.Houses[square] == GameRules.HotelLevel)
            SetAction("ОТЕЛЬ на " + BoardData.Names[square] + " за " + cost + " ₽: аренда +100%", Palette.Gain);
        else
            SetAction("ДОМ " + state.Houses[square] + "/4 на " + BoardData.Names[square] + " за " + cost +
                      " ₽: аренда " + state.RentFor(square) + " ₽", Palette.Gain);

        UpdateActionButtons();
        RefreshAll();
    }

    private void EndTurn()
    {
        if (gameOver || isMoving) return;

        turnResolved = false;
        state.CurrentPlayer = state.Opponent;
        if (state.SkipNextTurn[state.CurrentPlayer])
        {
            state.SkipNextTurn[state.CurrentPlayer] = false;
            SetAction("Игрок " + (state.CurrentPlayer + 1) + " пропускает ход (штрафстоянка)", Palette.Loss);
            state.CurrentPlayer = state.Opponent;
        }

        hud.SetButtons(true, false, false, string.Empty, false, false);
        log.System("Ход переходит к игроку " + (state.CurrentPlayer + 1) + ".");
        RefreshAll();
    }

    private void CheckBankruptcy()
    {
        int winner = state.FindWinner();
        if (winner >= 0) EndGame(winner);
    }

    private void EndGame(int winner)
    {
        gameOver = true;
        hud.SetButtons(false, false, false, string.Empty, false, false);
        SetAction("ПОБЕДА: игрок " + (winner + 1) + " разорил соперника", Palette.Gain, false);
        log.System("Победил игрок " + (winner + 1) + "! Нажмите Play для новой партии.");
        RefreshAll();
    }

    // ---------------------------------------------------------------- состояние интерфейса

    private bool CanBuyNow()
    {
        return !gameOver && !isMoving && turnResolved && state.CanBuy(state.CurrentPlayer);
    }

    private bool CanBuildNow()
    {
        return !gameOver && !isMoving && turnResolved && state.CanBuild(state.CurrentPlayer);
    }

    private void UpdateActionButtons()
    {
        bool canBuild = CanBuildNow();
        string buildCaption = canBuild
            ? (state.Houses[state.CurrentSquare] == 4 ? "ОТЕЛЬ " : "ДОМ ") + state.HouseCost(state.CurrentSquare)
            : string.Empty;

        hud.SetButtons(false, CanBuyNow(), canBuild, buildCaption, turnResolved && !gameOver, false);
    }

    private void SetAction(string message, Color color, bool addToLog = true)
    {
        actionMessage = message;
        actionColor = color;
        if (addToLog) log.Player(state.CurrentPlayer, message);
    }

    private void RefreshAll()
    {
        hud.Refresh(state, actionMessage, actionColor, log.Text);
        board.Refresh(state);
    }

    private void Update()
    {
        if (gameOver || isMoving || hud == null) return;
        if (hud.EndTurnAvailable && Input.GetKeyDown(KeyCode.Space)) EndTurn();
    }
}

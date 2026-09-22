using UnityEngine;

namespace Monopoly.Core
{
    /// <summary>Изменяемое состояние партии и производные расчёты (аренда, покупка, стройка).</summary>
    public class GameState
    {
        public readonly int[] Owners = new int[GameRules.BoardSize];
        public readonly int[] Houses = new int[GameRules.BoardSize];
        public readonly int[] Positions = new int[GameRules.PlayerCount];
        public readonly int[] Money = new int[GameRules.PlayerCount];
        public readonly bool[] InJail = new bool[GameRules.PlayerCount];
        public readonly bool[] SkipNextTurn = new bool[GameRules.PlayerCount];

        public int CurrentPlayer;
        public int DieOne;
        public int DieTwo;
        public int LastRolled;

        public GameState()
        {
            Reset();
        }

        public void Reset()
        {
            for (int i = 0; i < GameRules.BoardSize; i++)
            {
                Owners[i] = -1;
                Houses[i] = 0;
            }
            for (int p = 0; p < GameRules.PlayerCount; p++)
            {
                Positions[p] = GameRules.StartSquare;
                Money[p] = GameRules.StartMoney;
                InJail[p] = false;
                SkipNextTurn[p] = false;
            }
            CurrentPlayer = 0;
            DieOne = DieTwo = LastRolled = 0;
        }

        public int Opponent => 1 - CurrentPlayer;
        public int CurrentSquare => Positions[CurrentPlayer];
        public bool IsDouble => DieOne == DieTwo && LastRolled > 0;

        public void RollDice()
        {
            DieOne = Random.Range(1, 7);
            DieTwo = Random.Range(1, 7);
            LastRolled = DieOne + DieTwo;
        }

        /// <summary>Двигает игрока вперёд, возвращает true если пройден СТАРТ (с начислением зарплаты).</summary>
        public bool AdvanceCurrentPlayer(int steps)
        {
            int oldPosition = Positions[CurrentPlayer];
            Positions[CurrentPlayer] = (oldPosition + steps) % GameRules.BoardSize;
            bool passedStart = Positions[CurrentPlayer] < oldPosition;
            if (passedStart) Money[CurrentPlayer] += GameRules.Salary;
            return passedStart;
        }

        public int RentFor(int square)
        {
            return Mathf.RoundToInt(BoardData.Rents[square] * GameRules.RentMultiplier(Houses[square]));
        }

        public int HouseCost(int square)
        {
            return Mathf.RoundToInt(BoardData.Prices[square] * 0.25f / 10f) * 10;
        }

        public bool OwnsWholeGroup(int player, int square)
        {
            int group = BoardData.Groups[square];
            if (group < 0) return false;
            for (int i = 0; i < GameRules.BoardSize; i++)
                if (BoardData.Groups[i] == group && Owners[i] != player) return false;
            return true;
        }

        public bool CanBuy(int player)
        {
            int square = Positions[player];
            return BoardData.IsProperty(square)
                && Owners[square] == -1
                && Money[player] >= BoardData.Prices[square];
        }

        public bool CanBuild(int player)
        {
            int square = Positions[player];
            return Owners[square] == player
                && Houses[square] < GameRules.HotelLevel
                && OwnsWholeGroup(player, square)
                && Money[player] >= HouseCost(square);
        }

        public void Buy(int player)
        {
            int square = Positions[player];
            Owners[square] = player;
            Money[player] -= BoardData.Prices[square];
        }

        public int Build(int player)
        {
            int square = Positions[player];
            int cost = HouseCost(square);
            Money[player] -= cost;
            Houses[square]++;
            return cost;
        }

        public void Transfer(int from, int to, int amount)
        {
            Money[from] -= amount;
            Money[to] += amount;
        }

        /// <summary>Индекс победителя, если соперник разорён, иначе -1.</summary>
        public int FindWinner()
        {
            if (Money[CurrentPlayer] <= 0) return Opponent;
            if (Money[Opponent] <= 0) return CurrentPlayer;
            return -1;
        }
    }
}

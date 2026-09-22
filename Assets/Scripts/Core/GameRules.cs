namespace Monopoly.Core
{
    /// <summary>Числовые правила партии. Один источник правды для всех модулей.</summary>
    public static class GameRules
    {
        public const int BoardSize = 40;
        public const int PlayerCount = 2;

        /// <summary>Углы доски: между ними по девять клеток.</summary>
        public const int StartSquare = 0;
        public const int JailSquare = 10;
        public const int CasinoSquare = 20;
        public const int ParkingSquare = 30;

        public const int StartMoney = 10000;
        public const int Salary = 2000;
        public const int JailFine = 500;
        public const int ParkingFine = 500;
        public const int CasinoPrize = 1000;

        public const int HotelLevel = 5;
        public const int MaxLogLines = 8;

        /// <summary>Множитель аренды по количеству построек на клетке.</summary>
        public static float RentMultiplier(int houses)
        {
            switch (houses)
            {
                case 1: return 1.1f;
                case 2: return 1.2f;
                case 3: return 1.4f;
                case 4: return 1.8f;
                case HotelLevel: return 2f;
                default: return 1f;
            }
        }
    }
}

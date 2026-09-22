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

        public const int StartMoney = 12000;
        public const int Salary = 1500;
        public const int JailFine = 1000;
        public const int ParkingFine = 1000;
        public const int CasinoPrize = 2000;

        public const int HotelLevel = 5;
        public const int MaxLogLines = 8;

        /// <summary>Дом стоит половину цены участка.</summary>
        public const float HouseCostRate = 0.5f;

        /// <summary>Аренда за полную группу без построек удваивается.</summary>
        public const float MonopolyBonus = 2f;

        /// <summary>Базовая аренда вокзала; за каждый следующий вокзал владельца она удваивается.</summary>
        public const int StationBaseRent = 250;

        /// <summary>
        /// Множитель аренды по числу построек. Рост резкий: именно застройка
        /// делает партию конечной, иначе зарплата всегда перекрывает расходы.
        /// </summary>
        public static float RentMultiplier(int houses)
        {
            switch (houses)
            {
                case 1: return 4f;
                case 2: return 8f;
                case 3: return 16f;
                case 4: return 28f;
                case HotelLevel: return 40f;
                default: return 1f;
            }
        }
    }
}

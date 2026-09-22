using UnityEngine;

namespace Monopoly.Core
{
    /// <summary>Тип клетки — по нему строится и логика хода, и внешний вид.</summary>
    public enum TileKind
    {
        Start,
        Property,
        Station,
        Chance,
        Tax,
        Jail,
        Casino,
        Parking
    }

    /// <summary>
    /// Неизменяемое описание доски на 40 клеток: четыре угла, по девять клеток между ними,
    /// на каждой стороне по вокзалу. Крайние группы улиц — по две, остальные — по три.
    /// </summary>
    public static class BoardData
    {
        public static readonly TileKind[] Kinds =
        {
            TileKind.Start, TileKind.Property, TileKind.Chance, TileKind.Property, TileKind.Tax,
            TileKind.Station, TileKind.Property, TileKind.Chance, TileKind.Property, TileKind.Property,
            TileKind.Jail, TileKind.Property, TileKind.Chance, TileKind.Property, TileKind.Property,
            TileKind.Station, TileKind.Property, TileKind.Chance, TileKind.Property, TileKind.Property,
            TileKind.Casino, TileKind.Property, TileKind.Chance, TileKind.Property, TileKind.Property,
            TileKind.Station, TileKind.Property, TileKind.Property, TileKind.Chance, TileKind.Property,
            TileKind.Parking, TileKind.Property, TileKind.Property, TileKind.Chance, TileKind.Property,
            TileKind.Station, TileKind.Chance, TileKind.Property, TileKind.Tax, TileKind.Property
        };

        public static readonly string[] Names =
        {
            "СТАРТ", "Полянка", "ШАНС", "Ямская", "НАЛОГ",
            "Вокзал", "Садовая", "ШАНС", "Ольховая", "Лесная",
            "ТЮРЬМА", "Парковая", "ШАНС", "Сиреневая", "Луговая",
            "Вокзал", "Морская", "ШАНС", "Речная", "Портовая",
            "КАЗИНО", "Зелёная", "ШАНС", "Вишнёвая", "Янтарная",
            "Вокзал", "Соборная", "Успенская", "ШАНС", "Гранитная",
            "СТОЯНКА", "Дворцовая", "Княжеская", "ШАНС", "Царская",
            "Вокзал", "ШАНС", "Набережная", "НАЛОГ", "Приморская"
        };

        public static readonly int[] Prices =
        {
            0, 600, 0, 700, 0, 2000, 1000, 0, 1000, 1200,
            0, 1400, 0, 1400, 1600, 2000, 1800, 0, 1800, 2000,
            0, 2200, 0, 2200, 2400, 2000, 2600, 2600, 0, 2800,
            0, 3000, 3000, 0, 3200, 2000, 0, 3500, 0, 4000
        };

        /// <summary>Базовая аренда — 12 процентов цены участка;
        /// у вокзалов своя ставка, растущая от их числа у владельца.</summary>
        public static readonly int[] Rents =
        {
            0, 70, 0, 80, 0, 250, 120, 0, 120, 140,
            0, 170, 0, 170, 190, 250, 220, 0, 220, 240,
            0, 260, 0, 260, 290, 250, 310, 310, 0, 340,
            0, 360, 360, 0, 380, 250, 0, 420, 0, 480
        };

        public static readonly int[] Taxes =
        {
            0, 0, 0, 0, 1000, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 1500, 0
        };

        public static readonly int[] Groups =
        {
            -1, 0, -1, 0, -1, -1, 1, -1, 1, 1,
            -1, 2, -1, 2, 2, -1, 3, -1, 3, 3,
            -1, 4, -1, 4, 4, -1, 5, 5, -1, 5,
            -1, 6, 6, -1, 6, -1, -1, 7, -1, 7
        };

        public static readonly Color[] GroupColors =
        {
            new Color(0.45f, 0.26f, 0.22f),
            new Color(0.36f, 0.62f, 0.76f),
            new Color(0.68f, 0.34f, 0.56f),
            new Color(0.85f, 0.50f, 0.18f),
            new Color(0.76f, 0.24f, 0.22f),
            new Color(0.85f, 0.72f, 0.20f),
            new Color(0.22f, 0.55f, 0.33f),
            new Color(0.20f, 0.32f, 0.62f)
        };

        private static readonly Color StationBand = new Color(0.24f, 0.28f, 0.34f);

        public static bool IsChance(int square)
        {
            return Kinds[square] == TileKind.Chance;
        }

        public static bool IsStation(int square)
        {
            return Kinds[square] == TileKind.Station;
        }

        /// <summary>Клетка, которую можно купить: улица или вокзал.</summary>
        public static bool IsProperty(int square)
        {
            return Prices[square] > 0;
        }

        /// <summary>Сколько улиц в группе — крайние группы по две, остальные по три.</summary>
        public static int GroupSize(int group)
        {
            if (group < 0) return 0;
            int count = 0;
            for (int i = 0; i < Groups.Length; i++)
                if (Groups[i] == group) count++;
            return count;
        }

        /// <summary>Цвет поля клетки: участки — светлые, служебные клетки — цветные.</summary>
        public static Color FaceColor(int square)
        {
            return IsProperty(square) ? Palette.TileFace : TileColor(square);
        }

        /// <summary>Цветная полоса группы у внутреннего края клетки, как на настоящей доске.</summary>
        public static Color BandColor(int square)
        {
            if (IsStation(square)) return StationBand;
            int group = Groups[square];
            return group >= 0 ? GroupColors[group] : new Color(0f, 0f, 0f, 0f);
        }

        /// <summary>Цвет подписей: тёмный на светлом поле участка, белый на цветной служебной клетке.</summary>
        public static Color TextColor(int square)
        {
            return IsProperty(square) ? Palette.TileText : Color.white;
        }

        public static Color TileColor(int square)
        {
            switch (Kinds[square])
            {
                case TileKind.Start: return new Color(0.15f, 0.62f, 0.46f);
                case TileKind.Jail: return new Color(0.72f, 0.27f, 0.24f);
                case TileKind.Casino: return new Color(0.72f, 0.37f, 0.15f);
                case TileKind.Parking: return new Color(0.54f, 0.54f, 0.58f);
                case TileKind.Chance: return new Color(0.44f, 0.32f, 0.56f);
                case TileKind.Tax: return new Color(0.76f, 0.32f, 0.28f);
                default: return Palette.TileFace;
            }
        }
    }
}

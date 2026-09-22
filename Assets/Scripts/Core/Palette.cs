using UnityEngine;

namespace Monopoly.Core
{
    /// <summary>Единая палитра: интерфейс, доска и 3D-сцена берут цвета отсюда.</summary>
    public static class Palette
    {
        public static readonly Color[] Players =
        {
            new Color(0.94f, 0.28f, 0.25f),
            new Color(0.16f, 0.53f, 0.86f)
        };

        public static readonly Color[] PlayerTokens =
        {
            new Color(0.95f, 0.16f, 0.13f),
            new Color(0.13f, 0.40f, 0.95f)
        };

        public static readonly Color Gain = new Color(0.40f, 0.85f, 0.55f);
        public static readonly Color Loss = new Color(0.94f, 0.45f, 0.40f);
        public static readonly Color Neutral = new Color(0.97f, 0.83f, 0.36f);
        public static readonly Color House = new Color(0.72f, 0.48f, 0.05f);
        public static readonly Color Hotel = new Color(0.80f, 0.20f, 0.12f);

        public static readonly Color Sky = new Color(0.045f, 0.06f, 0.10f);
        public static readonly Color FloorBase = new Color(0.07f, 0.10f, 0.15f);
        public static readonly Color FloorLine = new Color(0.16f, 0.24f, 0.34f);
        public static readonly Color BoardBase = new Color(0.80f, 0.87f, 0.78f);
        public static readonly Color BoardCenter = new Color(0.78f, 0.86f, 0.76f);
        public static readonly Color TileFace = new Color(0.91f, 0.94f, 0.88f);
        public static readonly Color TileEdge = new Color(0.20f, 0.24f, 0.22f);
        public static readonly Color TileText = new Color(0.11f, 0.14f, 0.16f);

        public static readonly Color HudPanel = new Color(0.07f, 0.10f, 0.16f, 0.94f);
        public static readonly Color HudInset = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        public static readonly Color HudAction = new Color(0.13f, 0.17f, 0.25f, 0.96f);
        public static readonly Color HudCaption = new Color(0.45f, 0.55f, 0.66f);
        public static readonly Color HudText = new Color(0.82f, 0.86f, 0.91f);

        public static readonly Color ButtonRoll = new Color(0.91f, 0.56f, 0.18f);
        public static readonly Color ButtonBuy = new Color(0.24f, 0.68f, 0.45f);
        public static readonly Color ButtonBuild = new Color(0.47f, 0.40f, 0.72f);
        public static readonly Color ButtonEndTurn = new Color(0.28f, 0.38f, 0.49f);
        public static readonly Color ButtonFine = new Color(0.75f, 0.28f, 0.25f);

        public static string Hex(Color color)
        {
            return ColorUtility.ToHtmlStringRGB(color);
        }
    }
}

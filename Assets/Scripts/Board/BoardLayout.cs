using Monopoly.Core;
using UnityEngine;

namespace Monopoly.Board
{
    /// <summary>
    /// Геометрия доски в стиле «Монополии»: по углам квадратные клетки, между ними по девять
    /// прямоугольных, вплотную друг к другу. СТАРТ — левый верхний угол, ход по часовой стрелке.
    /// Доска лежит в плоскости XZ, центр — в начале координат.
    /// </summary>
    public static class BoardLayout
    {
        /// <summary>Сторона доски в мировых единицах.</summary>
        public const float Side = 8.75f;

        /// <summary>Высота верхней грани плиты доски над полом.</summary>
        public const float SurfaceHeight = 0.16f;

        /// <summary>Плоскость с клетками: чуть выше плиты, на ней же стоят фишки.</summary>
        public const float TileSurface = SurfaceHeight + 0.02f;

        /// <summary>Сколько рядовых клеток между двумя углами.</summary>
        public const int TilesPerSide = 9;

        /// <summary>Длина стороны в клетках вместе с угловой.</summary>
        public const int SideStride = TilesPerSide + 1;

        /// <summary>Доля стороны, занятая угловой клеткой.</summary>
        public const float CornerSize = 0.131f;

        /// <summary>Ширина рядовой клетки.</summary>
        public const float TileWidth = (1f - 2f * CornerSize) / TilesPerSide;

        /// <summary>Шаг сетки пола — по ширине рядовой клетки.</summary>
        public const float CellSize = Side * TileWidth;

        /// <summary>Сторона доски, на которой стоит клетка. Углы относятся к следующей стороне.</summary>
        public enum Edge
        {
            Top,
            Right,
            Bottom,
            Left
        }

        public static bool IsCorner(int square)
        {
            return square % SideStride == 0;
        }

        public static Edge EdgeOf(int square)
        {
            return (Edge)(square / SideStride);
        }

        /// <summary>Поворот содержимого клетки: подписи всегда читаются снаружи доски.</summary>
        public static float ContentAngle(int square)
        {
            if (IsCorner(square)) return 0f;
            switch (EdgeOf(square))
            {
                case Edge.Top: return 180f;
                case Edge.Right: return 90f;
                case Edge.Left: return -90f;
                default: return 0f;
            }
        }

        /// <summary>Границы клетки в координатах канваса доски (0..1), без зазоров между соседями.</summary>
        public static void Anchors(int square, out Vector2 min, out Vector2 max)
        {
            const float c = CornerSize;
            const float w = TileWidth;

            Edge edge = EdgeOf(square);
            int step = square % SideStride;

            if (step == 0)
            {
                // Угол: СТАРТ слева сверху, дальше по часовой стрелке.
                switch (edge)
                {
                    case Edge.Top: min = new Vector2(0f, 1f - c); break;
                    case Edge.Right: min = new Vector2(1f - c, 1f - c); break;
                    case Edge.Bottom: min = new Vector2(1f - c, 0f); break;
                    default: min = Vector2.zero; break;
                }
                max = min + new Vector2(c, c);
                return;
            }

            float offset = (step - 1) * w;
            switch (edge)
            {
                case Edge.Top:
                    min = new Vector2(c + offset, 1f - c);
                    max = new Vector2(c + offset + w, 1f);
                    break;
                case Edge.Right:
                    min = new Vector2(1f - c, 1f - c - offset - w);
                    max = new Vector2(1f, 1f - c - offset);
                    break;
                case Edge.Bottom:
                    min = new Vector2(1f - c - offset - w, 0f);
                    max = new Vector2(1f - c - offset, c);
                    break;
                default:
                    min = new Vector2(0f, c + offset);
                    max = new Vector2(c, c + offset + w);
                    break;
            }
        }

        /// <summary>Свободная середина доски — там лежит меню партии.</summary>
        public static Vector2 CenterMin => new Vector2(CornerSize, CornerSize);
        public static Vector2 CenterMax => new Vector2(1f - CornerSize, 1f - CornerSize);

        /// <summary>Центр клетки на поверхности доски в мировых координатах.</summary>
        public static Vector3 SurfacePosition(int square)
        {
            Anchors(square, out Vector2 min, out Vector2 max);
            Vector2 center = (min + max) * 0.5f;
            return new Vector3((center.x - 0.5f) * Side, TileSurface, (center.y - 0.5f) * Side);
        }

        /// <summary>Точка, где стоит фишка игрока: центр клетки со смещением, чтобы фишки не слипались.</summary>
        public static Vector3 TokenPosition(int square, int player)
        {
            Vector3 position = SurfacePosition(square);
            float offset = CellSize * 0.22f;
            position.x += player == 0 ? -offset : offset;
            position.z -= offset * 0.4f;
            return position;
        }

        /// <summary>Размер клетки в пикселях канваса доски.</summary>
        public static Vector2 TileSize(int square, float canvasPixels)
        {
            Anchors(square, out Vector2 min, out Vector2 max);
            return (max - min) * canvasPixels;
        }

        static BoardLayout()
        {
            Debug.Assert(GameRules.BoardSize == 4 * SideStride,
                "Раскладка рассчитана на " + 4 * SideStride + " клеток: 4 угла и по " + TilesPerSide + " клеток между ними.");
        }
    }
}

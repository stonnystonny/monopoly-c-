using System.Collections;
using System.Collections.Generic;
using Monopoly.Core;
using Monopoly.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Monopoly.Board
{
    /// <summary>
    /// 3D-представление доски: плита лежит на сетке пола, клетки нарисованы на горизонтальном
    /// world-space канвасе в стиле «Монополии», в середине доски — место под меню партии.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        private const float CanvasPixels = 1600f;
        private const float TileBorder = 3f;
        private const float HopDuration = 0.22f;
        private const float HopHeight = 0.45f;

        private readonly List<Image> tileOwnerBars = new List<Image>();
        private readonly List<Text> tileStarLabels = new List<Text>();
        private readonly List<Text> tilePriceLabels = new List<Text>();
        private readonly Transform[] tokens = new Transform[GameRules.PlayerCount];

        /// <summary>Середина доски: сюда встраивается меню партии.</summary>
        public RectTransform MenuAnchor { get; private set; }

        public void Build(Camera boardCamera)
        {
            SceneEnvironment.Build(transform);
            BuildBase();
            BuildTiles(BuildBoardCanvas(boardCamera));
            BuildTokens();
        }

        // ---------------------------------------------------------------- плита доски

        private void BuildBase()
        {
            var rim = Primitives.Create(PrimitiveType.Cube, "BoardRim", transform,
                SceneEnvironment.LitMaterial(new Color(0.22f, 0.20f, 0.18f)));
            rim.transform.localScale = new Vector3(BoardLayout.Side * 1.03f, BoardLayout.SurfaceHeight, BoardLayout.Side * 1.03f);
            rim.transform.localPosition = new Vector3(0f, BoardLayout.SurfaceHeight * 0.5f - 0.015f, 0f);

            var surface = Primitives.Create(PrimitiveType.Cube, "BoardSurface", transform,
                SceneEnvironment.LitMaterial(Palette.BoardBase));
            surface.transform.localScale = new Vector3(BoardLayout.Side, BoardLayout.SurfaceHeight, BoardLayout.Side);
            surface.transform.localPosition = new Vector3(0f, BoardLayout.SurfaceHeight * 0.5f, 0f);
        }

        /// <summary>Канвас лежит горизонтально: локальный X → мировой X, локальный Y → мировой Z.</summary>
        private Transform BuildBoardCanvas(Camera boardCamera)
        {
            var canvasObject = new GameObject("BoardCanvas");
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = boardCamera;
            // Меню лежит на этом же канвасе, поэтому кнопкам нужен райкастер.
            canvasObject.AddComponent<GraphicRaycaster>().blockingObjects = GraphicRaycaster.BlockingObjects.None;

            // Позицию и поворот задаём только после AddComponent<Canvas>():
            // он подменяет Transform на RectTransform и сбрасывает заданные до этого значения.
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(CanvasPixels, CanvasPixels);
            rect.localScale = Vector3.one * (BoardLayout.Side / CanvasPixels);
            rect.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rect.localPosition = new Vector3(0f, BoardLayout.TileSurface, 0f);
            return canvasObject.transform;
        }

        // ---------------------------------------------------------------- клетки

        private void BuildTiles(Transform canvas)
        {
            var center = UiFactory.Panel(canvas, "Center", Palette.BoardCenter,
                BoardLayout.CenterMin, BoardLayout.CenterMax);
            center.raycastTarget = false;
            MenuAnchor = center.rectTransform;

            for (int square = 0; square < GameRules.BoardSize; square++) BuildTile(canvas, square);
        }

        private void BuildTile(Transform canvas, int square)
        {
            BoardLayout.Anchors(square, out Vector2 min, out Vector2 max);
            bool corner = BoardLayout.IsCorner(square);
            Color textColor = BoardData.TextColor(square);

            // Тёмная подложка даёт тонкую разделительную рамку: клетки стоят вплотную.
            var tile = UiFactory.Panel(canvas, "Tile" + square, Palette.TileEdge, min, max);
            tile.raycastTarget = false;

            var face = UiFactory.Panel(tile.transform, "Face", BoardData.FaceColor(square), Vector2.zero, Vector2.one);
            face.raycastTarget = false;
            face.rectTransform.offsetMin = new Vector2(TileBorder, TileBorder);
            face.rectTransform.offsetMax = new Vector2(-TileBorder, -TileBorder);

            RectTransform content = CreateContent(face.transform, square);

            // Полоса группы у внутреннего края клетки.
            var band = UiFactory.Panel(content, "Band", BoardData.BandColor(square),
                new Vector2(0f, corner ? 1f : 0.78f), Vector2.one);
            band.raycastTarget = false;

            tileStarLabels.Add(UiFactory.Label(content, "", 24, Palette.House,
                new Vector2(0.02f, 0.60f), new Vector2(0.98f, 0.77f), FontStyle.Bold));

            UiFactory.Label(content, BoardData.Names[square], corner ? 34 : 26, textColor,
                new Vector2(0.06f, corner ? 0.50f : 0.28f), new Vector2(0.94f, corner ? 0.86f : 0.59f),
                FontStyle.Bold);

            // Полоса владельца у внешнего края: на ней же печатается цена или аренда.
            var ownerBar = UiFactory.Panel(content, "Owner", new Color(0f, 0f, 0f, 0f),
                Vector2.zero, new Vector2(1f, corner ? 0.46f : 0.26f));
            ownerBar.raycastTarget = false;
            tileOwnerBars.Add(ownerBar);

            tilePriceLabels.Add(UiFactory.Label(ownerBar.transform, "", corner ? 26 : 22, textColor,
                new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), FontStyle.Bold));
        }

        /// <summary>
        /// Содержимое клетки разворачивается так, чтобы подпись читалась снаружи доски.
        /// У боковых сторон стороны прямоугольника меняются местами — после поворота он снова по клетке.
        /// </summary>
        private static RectTransform CreateContent(Transform tile, int square)
        {
            var obj = new GameObject("Content");
            obj.transform.SetParent(tile, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);

            Vector2 size = BoardLayout.TileSize(square, CanvasPixels) - Vector2.one * (TileBorder * 2f);
            float angle = BoardLayout.ContentAngle(square);
            bool quarterTurn = Mathf.Approximately(Mathf.Abs(angle), 90f);

            rect.sizeDelta = quarterTurn ? new Vector2(size.y, size.x) : size;
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            return rect;
        }

        // ---------------------------------------------------------------- фишки

        private void BuildTokens()
        {
            for (int player = 0; player < GameRules.PlayerCount; player++)
            {
                tokens[player] = CreateToken(player).transform;
                tokens[player].position = BoardLayout.TokenPosition(GameRules.StartSquare, player);
            }
        }

        private GameObject CreateToken(int player)
        {
            var root = new GameObject("Token" + (player + 1));
            root.transform.SetParent(transform, false);
            var material = SceneEnvironment.LitMaterial(Palette.PlayerTokens[player]);

            var foot = Primitives.Create(PrimitiveType.Cylinder, "Foot", root.transform, material);
            foot.transform.localScale = new Vector3(0.40f, 0.06f, 0.40f);
            foot.transform.localPosition = new Vector3(0f, 0.06f, 0f);

            var body = Primitives.Create(PrimitiveType.Capsule, "Body", root.transform, material);
            body.transform.localScale = new Vector3(0.24f, 0.20f, 0.24f);
            body.transform.localPosition = new Vector3(0f, 0.26f, 0f);

            var head = Primitives.Create(PrimitiveType.Sphere, "Head", root.transform, material);
            head.transform.localScale = Vector3.one * 0.26f;
            head.transform.localPosition = new Vector3(0f, 0.50f, 0f);

            return root;
        }

        public void PlaceTokens(GameState state)
        {
            for (int player = 0; player < GameRules.PlayerCount; player++)
                tokens[player].position = BoardLayout.TokenPosition(state.Positions[player], player);
        }

        /// <summary>Пошаговый проход фишки по клеткам с прыжком между ними.</summary>
        public IEnumerator MoveAlongPath(int player, int fromSquare, int steps)
        {
            for (int step = 1; step <= steps; step++)
            {
                int nextSquare = (fromSquare + step) % GameRules.BoardSize;
                yield return HopTo(player, nextSquare);
            }
        }

        public IEnumerator HopTo(int player, int square)
        {
            Transform token = tokens[player];
            Vector3 start = token.position;
            Vector3 target = BoardLayout.TokenPosition(square, player);
            float elapsed = 0f;

            while (elapsed < HopDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / HopDuration);
                float smooth = progress * progress * (3f - 2f * progress);
                Vector3 position = Vector3.Lerp(start, target, smooth);
                position.y += Mathf.Sin(progress * Mathf.PI) * HopHeight;
                token.position = position;
                token.Rotate(0f, 260f * Time.deltaTime, 0f, Space.World);
                yield return null;
            }

            token.position = target;
        }

        // ---------------------------------------------------------------- обновление

        public void Refresh(GameState state)
        {
            for (int square = 0; square < GameRules.BoardSize; square++)
            {
                tilePriceLabels[square].text = TileCaption(state, square);

                bool owned = state.Owners[square] >= 0;
                tileOwnerBars[square].color = owned
                    ? Palette.Players[state.Owners[square]]
                    : new Color(0f, 0f, 0f, 0f);
                tilePriceLabels[square].color = owned ? Color.white : BoardData.TextColor(square);

                var stars = tileStarLabels[square];
                bool hotel = state.Houses[square] == GameRules.HotelLevel;
                stars.text = hotel ? "★" : new string('★', state.Houses[square]);
                stars.color = hotel ? Palette.Hotel : Palette.House;
            }
        }

        private static string TileCaption(GameState state, int square)
        {
            if (state.Owners[square] >= 0) return "аренда " + state.RentFor(square);
            if (BoardData.Prices[square] > 0) return BoardData.Prices[square] + " ₽";
            if (BoardData.Taxes[square] > 0) return "−" + BoardData.Taxes[square] + " ₽";
            if (square == GameRules.StartSquare) return "+" + GameRules.Salary + " ₽";
            if (square == GameRules.JailSquare) return "выход " + GameRules.JailFine;
            if (square == GameRules.CasinoSquare) return "дубль +" + GameRules.CasinoPrize;
            if (square == GameRules.ParkingSquare) return "−" + GameRules.ParkingFine + " ₽";
            return "";
        }
    }
}

using Monopoly.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Monopoly.UI
{
    /// <summary>
    /// Меню партии: ходы, кубики, журнал и кнопки. Живёт не на экране, а на самой доске —
    /// встраивается в её середину, поэтому лежит вместе с доской и не висит в воздухе.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        public struct Actions
        {
            public UnityEngine.Events.UnityAction Roll;
            public UnityEngine.Events.UnityAction Buy;
            public UnityEngine.Events.UnityAction Build;
            public UnityEngine.Events.UnityAction EndTurn;
            public UnityEngine.Events.UnityAction PayFine;
        }

        private Text[] playerLabels;
        private Text turnLabel;
        private Text actionLabel;
        private Text logLabel;

        private Button rollButton;
        private Button buyButton;
        private Button buildButton;
        private Button endTurnButton;
        private Button payFineButton;

        /// <param name="boardCenter">Середина доски, в которую встраивается меню.</param>
        public void Build(RectTransform boardCenter, Actions actions)
        {
            EnsureEventSystem();
            BuildMenu(boardCenter, actions);
            SetButtons(true, false, false, string.Empty, false, false);
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void BuildMenu(RectTransform boardCenter, Actions actions)
        {
            // Нижняя полоса середины доски отдана под лоток с кубиками.
            var menu = UiFactory.Panel(boardCenter, "CenterMenu", Palette.HudPanel,
                new Vector2(0.04f, Monopoly.Board.BoardLayout.DiceTrayHeight + 0.02f),
                new Vector2(0.96f, 0.97f));

            UiFactory.Label(menu.transform, "ЦЕНТР ГОРОДА", 36, Palette.Neutral,
                new Vector2(0.04f, 0.945f), new Vector2(0.96f, 1f), FontStyle.Bold);

            playerLabels = new Text[GameRules.PlayerCount];
            playerLabels[0] = UiFactory.Label(menu.transform, "", 42, Palette.Players[0],
                new Vector2(0.03f, 0.875f), new Vector2(0.49f, 0.94f), FontStyle.Bold);
            playerLabels[1] = UiFactory.Label(menu.transform, "", 42, Palette.Players[1],
                new Vector2(0.51f, 0.875f), new Vector2(0.97f, 0.94f), FontStyle.Bold);

            turnLabel = UiFactory.Label(menu.transform, "", 38, Color.white,
                new Vector2(0.03f, 0.80f), new Vector2(0.97f, 0.87f), FontStyle.Bold);

            var actionPanel = UiFactory.Panel(menu.transform, "ActionPanel", Palette.HudAction,
                new Vector2(0.03f, 0.715f), new Vector2(0.97f, 0.79f));
            actionPanel.raycastTarget = false;
            actionLabel = UiFactory.Label(actionPanel.transform, "", 34, Palette.Neutral,
                new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), FontStyle.Bold);

            var logPanel = UiFactory.Panel(menu.transform, "LogPanel", Palette.HudInset,
                new Vector2(0.03f, 0.285f), new Vector2(0.97f, 0.705f));
            logPanel.raycastTarget = false;
            UiFactory.Label(logPanel.transform, "ЖУРНАЛ ХОДОВ", 28, Palette.HudCaption,
                new Vector2(0.03f, 0.88f), new Vector2(0.97f, 0.99f), FontStyle.Bold);
            logLabel = UiFactory.Label(logPanel.transform, "", 26, Palette.HudText,
                new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.87f), FontStyle.Normal, TextAnchor.UpperLeft, false);
            logLabel.verticalOverflow = VerticalWrapMode.Truncate;
            logLabel.lineSpacing = 1.1f;

            rollButton = UiFactory.Button(menu.transform, "БРОСИТЬ КУБИКИ", Palette.ButtonRoll,
                new Vector2(0.03f, 0.155f), new Vector2(0.97f, 0.275f), actions.Roll, 46);
            buyButton = UiFactory.Button(menu.transform, "КУПИТЬ", Palette.ButtonBuy,
                new Vector2(0.03f, 0.02f), new Vector2(0.35f, 0.14f), actions.Buy, 38);
            payFineButton = UiFactory.Button(menu.transform, "ШТРАФ " + GameRules.JailFine, Palette.ButtonFine,
                new Vector2(0.03f, 0.02f), new Vector2(0.35f, 0.14f), actions.PayFine, 38);
            buildButton = UiFactory.Button(menu.transform, "ДОМ", Palette.ButtonBuild,
                new Vector2(0.365f, 0.02f), new Vector2(0.665f, 0.14f), actions.Build, 38);
            endTurnButton = UiFactory.Button(menu.transform, "ПЕРЕДАТЬ ХОД", Palette.ButtonEndTurn,
                new Vector2(0.68f, 0.02f), new Vector2(0.97f, 0.14f), actions.EndTurn, 38);
        }

        // ---------------------------------------------------------------- обновление

        public void Refresh(GameState state, string actionMessage, Color actionColor, string logText)
        {
            for (int player = 0; player < GameRules.PlayerCount; player++)
                playerLabels[player].text = "ИГРОК " + (player + 1) + "  " + state.Money[player] + " ₽";

            turnLabel.text = "ХОД ИГРОКА " + (state.CurrentPlayer + 1) + "   ·   " +
                (state.LastRolled == 0
                    ? "КУБИКИ  -  -"
                    : "КУБИКИ  " + state.DieOne + " + " + state.DieTwo + " = " + state.LastRolled);

            actionLabel.text = actionMessage;
            actionLabel.color = actionColor;
            logLabel.text = logText;
        }

        /// <summary>Доступна ли сейчас передача хода — по ней работает и клавиша Space.</summary>
        public bool EndTurnAvailable => endTurnButton != null && endTurnButton.gameObject.activeSelf;

        public void SetButtons(bool roll, bool buy, bool build, string buildCaption, bool endTurn, bool payFine)
        {
            rollButton.interactable = roll;
            buyButton.gameObject.SetActive(buy);
            buildButton.gameObject.SetActive(build);
            if (build) UiFactory.SetButtonCaption(buildButton, buildCaption);
            endTurnButton.gameObject.SetActive(endTurn);
            payFineButton.gameObject.SetActive(payFine);
        }
    }
}

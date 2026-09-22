using System.Collections.Generic;

namespace Monopoly.Core
{
    /// <summary>Журнал ходов: хранит последние строки и отдаёт готовый текст.</summary>
    public class GameLog
    {
        private readonly List<string> lines = new List<string>();

        public string Text => string.Join("\n", lines.ToArray());

        public void Clear()
        {
            lines.Clear();
        }

        public void Player(int player, string message)
        {
            string hex = Palette.Hex(Palette.Players[player]);
            Append("<color=#" + hex + "><b>И" + (player + 1) + ":</b></color> " + message);
        }

        public void System(string message)
        {
            Append("<color=#6E7D8C>" + message + "</color>");
        }

        private void Append(string line)
        {
            lines.Insert(0, line);
            while (lines.Count > GameRules.MaxLogLines) lines.RemoveAt(lines.Count - 1);
        }
    }
}

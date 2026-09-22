using UnityEngine;

namespace Monopoly.Core
{
    /// <summary>Карточка «ШАНС»: деньги, перемещение или оплата соперником.</summary>
    public readonly struct ChanceCard
    {
        public readonly string Text;
        public readonly int Amount;
        public readonly int MoveTo;
        public readonly bool FromOpponent;

        public ChanceCard(string text, int amount, int moveTo = -1, bool fromOpponent = false)
        {
            Text = text;
            Amount = amount;
            MoveTo = moveTo;
            FromOpponent = fromOpponent;
        }

        public bool IsMove => MoveTo >= 0;
    }

    public static class ChanceDeck
    {
        private static readonly ChanceCard[] Cards =
        {
            new ChanceCard("Банк выплачивает дивиденды: +1000 ₽", 1000),
            new ChanceCard("Вы выиграли в лотерею: +2000 ₽", 2000),
            new ChanceCard("Пособие по безработице: +2000 ₽", 2000),
            new ChanceCard("Возврат переплаты налога: +1800 ₽", 1800),
            new ChanceCard("Выигрыш на скачках: +1200 ₽", 1200),
            new ChanceCard("Ремонт дороги: −1400 ₽", -1400),
            new ChanceCard("Штраф за превышение скорости: −600 ₽", -600),
            new ChanceCard("Счёт за электричество: −800 ₽", -800),
            new ChanceCard("Оплата страховки: −500 ₽", -500),
            new ChanceCard("День рождения: соперник платит 1000 ₽", 1000, -1, true),
            new ChanceCard("Отправляйтесь на СТАРТ", 0, GameRules.StartSquare),
            new ChanceCard("Отправляйтесь в ТЮРЬМУ", 0, GameRules.JailSquare)
        };

        public static ChanceCard Draw()
        {
            return Cards[Random.Range(0, Cards.Length)];
        }
    }
}

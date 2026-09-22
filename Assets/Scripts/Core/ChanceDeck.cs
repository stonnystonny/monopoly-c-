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
            new ChanceCard("Банк выплачивает дивиденды: +500 ₽", 500),
            new ChanceCard("Вы выиграли в лотерею: +1000 ₽", 1000),
            new ChanceCard("Пособие по безработице: +1000 ₽", 1000),
            new ChanceCard("Возврат переплаты налога: +900 ₽", 900),
            new ChanceCard("Выигрыш на скачках: +600 ₽", 600),
            new ChanceCard("Ремонт дороги: −700 ₽", -700),
            new ChanceCard("Штраф за превышение скорости: −300 ₽", -300),
            new ChanceCard("Счёт за электричество: −400 ₽", -400),
            new ChanceCard("Оплата страховки: −250 ₽", -250),
            new ChanceCard("День рождения: соперник платит 500 ₽", 500, -1, true),
            new ChanceCard("Отправляйтесь на СТАРТ", 0, GameRules.StartSquare),
            new ChanceCard("Отправляйтесь в ТЮРЬМУ", 0, GameRules.JailSquare)
        };

        public static ChanceCard Draw()
        {
            return Cards[Random.Range(0, Cards.Length)];
        }
    }
}

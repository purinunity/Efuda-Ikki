using System;

namespace EfudaIkki.Core
{
    /// <summary>
    /// Unity assets and scene objectsから独立した、役判定用のカード値。
    /// Number and Suit intentionally use integers so the Unity compatibility layer can
    /// preserve the serialized enum values without making Core reference Unity types.
    /// </summary>
    public readonly struct CardValue : IEquatable<CardValue>
    {
        public int Number { get; }
        public int Suit { get; }
        public bool HasValue { get; }

        public CardValue(int number, int suit)
        {
            Number = number;
            Suit = suit;
            HasValue = true;
        }

        private CardValue(int number, int suit, bool hasValue)
        {
            Number = number;
            Suit = suit;
            HasValue = hasValue;
        }

        public static CardValue Invalid => new CardValue(0, 0, false);

        public bool Equals(CardValue other)
        {
            return Number == other.Number && Suit == other.Suit && HasValue == other.HasValue;
        }

        public override bool Equals(object obj)
        {
            return obj is CardValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Number;
                hash = (hash * 397) ^ Suit;
                return (hash * 397) ^ HasValue.GetHashCode();
            }
        }
    }
}

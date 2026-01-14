using HOSUnlock.Configuration;

namespace HOSUnlock.Models.Common
{
    public sealed record TokenShiftDefinition(int TokenIndex, string Token, int ShiftIndex, int ShiftMilliseconds) : IComparable<TokenShiftDefinition>, IEquatable<TokenShiftDefinition>
    {
        public int CompareTo(TokenShiftDefinition? other)
        {
            if (other == null)
                return -1;

            return Equals(other) ? 0 : -1;
        }
        public override int GetHashCode()
        {
            return Token.Length + TokenIndex + ShiftMilliseconds + ShiftIndex;
        }

        public bool Equals(TokenShiftDefinition? other)
        {
            if (other == null)
                return false;

            return string.Equals(Token, other.Token, StringComparison.OrdinalIgnoreCase)
                && TokenIndex == other.TokenIndex
                && ShiftMilliseconds == other.ShiftMilliseconds
                && ShiftIndex == other.ShiftIndex;
        }

        public TokenInfo GetTokenInfo() => new() { Index = TokenIndex, Token = Token };
    }
}

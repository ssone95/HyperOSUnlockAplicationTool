using HOSUnlock.Configuration;

namespace HOSUnlock.Models.Common;

/// <summary>
/// Represents a token shift definition for threshold timing.
/// </summary>
public sealed record TokenShiftDefinition(
    int TokenIndex,
    string Token,
    int ShiftIndex,
    int ShiftMilliseconds) : IComparable<TokenShiftDefinition>
{
    public int CompareTo(TokenShiftDefinition? other)
    {
        if (other is null)
            return 1;

        var tokenIndexComparison = TokenIndex.CompareTo(other.TokenIndex);
        if (tokenIndexComparison != 0)
            return tokenIndexComparison;

        var shiftIndexComparison = ShiftIndex.CompareTo(other.ShiftIndex);
        if (shiftIndexComparison != 0)
            return shiftIndexComparison;

        var shiftMsComparison = ShiftMilliseconds.CompareTo(other.ShiftMilliseconds);
        if (shiftMsComparison != 0)
            return shiftMsComparison;

        return string.Compare(Token, other.Token, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(TokenShiftDefinition? other)
    {
        if (other is null)
            return false;

        return TokenIndex == other.TokenIndex
            && ShiftIndex == other.ShiftIndex
            && ShiftMilliseconds == other.ShiftMilliseconds
            && string.Equals(Token, other.Token, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
        => HashCode.Combine(
            TokenIndex,
            ShiftIndex,
            ShiftMilliseconds,
            StringComparer.OrdinalIgnoreCase.GetHashCode(Token));

    public TokenInfo ToTokenInfo() => new(Token, TokenIndex);
}

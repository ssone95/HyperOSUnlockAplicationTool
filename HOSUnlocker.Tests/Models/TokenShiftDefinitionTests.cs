using HOSUnlock.Configuration;
using HOSUnlock.Models.Common;

namespace HOSUnlock.Tests.Models;

[TestClass]
public sealed class TokenShiftDefinitionTests
{
    private const int TestTimeoutMs = 360000; // 6 minutes

    #region Construction Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var definition = new TokenShiftDefinition(1, "TestToken", 1, 100);

        // Assert
        Assert.AreEqual(1, definition.TokenIndex);
        Assert.AreEqual("TestToken", definition.Token);
        Assert.AreEqual(1, definition.ShiftIndex);
        Assert.AreEqual(100, definition.ShiftMilliseconds);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constructor_NegativeShift_Allowed()
    {
        // Arrange & Act
        var definition = new TokenShiftDefinition(1, "TestToken", 1, -500);

        // Assert
        Assert.AreEqual(-500, definition.ShiftMilliseconds);
    }

    #endregion

    #region ToTokenInfo Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ToTokenInfo_ReturnsCorrectTokenInfo()
    {
        // Arrange
        var definition = new TokenShiftDefinition(5, "MyToken%2B", 3, 200);

        // Act
        var tokenInfo = definition.ToTokenInfo();

        // Assert
        Assert.IsNotNull(tokenInfo);
        Assert.AreEqual("MyToken%2B", tokenInfo.Token);
        Assert.AreEqual(5, tokenInfo.Index);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void ToTokenInfo_PreservesTokenValue()
    {
        // Arrange
        var token = "ComplexToken%2BEncoded%3D";
        var definition = new TokenShiftDefinition(1, token, 1, 100);

        // Act
        var tokenInfo = definition.ToTokenInfo();

        // Assert
        Assert.AreEqual(token, tokenInfo.Token);
    }

    #endregion

    #region Equality Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "Token", 2, 100);
        var def2 = new TokenShiftDefinition(1, "Token", 2, 100);

        // Act & Assert
        Assert.IsTrue(def1.Equals(def2));
        Assert.AreEqual(def1, def2);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Equals_DifferentTokenIndex_ReturnsFalse()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "Token", 2, 100);
        var def2 = new TokenShiftDefinition(2, "Token", 2, 100);

        // Act & Assert
        Assert.IsFalse(def1.Equals(def2));
        Assert.AreNotEqual(def1, def2);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Equals_DifferentShiftIndex_ReturnsFalse()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "Token", 1, 100);
        var def2 = new TokenShiftDefinition(1, "Token", 2, 100);

        // Act & Assert
        Assert.IsFalse(def1.Equals(def2));
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Equals_DifferentShiftMilliseconds_ReturnsFalse()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "Token", 1, 100);
        var def2 = new TokenShiftDefinition(1, "Token", 1, 200);

        // Act & Assert
        Assert.IsFalse(def1.Equals(def2));
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Equals_DifferentToken_ReturnsFalse()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "Token1", 1, 100);
        var def2 = new TokenShiftDefinition(1, "Token2", 1, 100);

        // Act & Assert
        Assert.IsFalse(def1.Equals(def2));
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Equals_TokenCaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "TOKEN", 1, 100);
        var def2 = new TokenShiftDefinition(1, "token", 1, 100);

        // Act & Assert
        Assert.IsTrue(def1.Equals(def2));
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "Token", 1, 100);

        // Act & Assert
        Assert.IsFalse(def1.Equals(null));
    }

    #endregion

    #region GetHashCode Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetHashCode_SameValues_SameHash()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "Token", 2, 100);
        var def2 = new TokenShiftDefinition(1, "Token", 2, 100);

        // Act & Assert
        Assert.AreEqual(def1.GetHashCode(), def2.GetHashCode());
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetHashCode_TokenCaseInsensitive_SameHash()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "TOKEN", 1, 100);
        var def2 = new TokenShiftDefinition(1, "token", 1, 100);

        // Act & Assert
        Assert.AreEqual(def1.GetHashCode(), def2.GetHashCode());
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetHashCode_DifferentValues_DifferentHash()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "Token1", 1, 100);
        var def2 = new TokenShiftDefinition(2, "Token2", 2, 200);

        // Act & Assert
        Assert.AreNotEqual(def1.GetHashCode(), def2.GetHashCode());
    }

    #endregion

    #region CompareTo Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void CompareTo_SameValues_ReturnsZero()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "Token", 1, 100);
        var def2 = new TokenShiftDefinition(1, "Token", 1, 100);

        // Act
        var result = def1.CompareTo(def2);

        // Assert
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void CompareTo_Null_ReturnsPositive()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "Token", 1, 100);

        // Act
        var result = def1.CompareTo(null);

        // Assert
        Assert.IsTrue(result > 0);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void CompareTo_SmallerTokenIndex_ReturnsNegative()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "Token", 1, 100);
        var def2 = new TokenShiftDefinition(2, "Token", 1, 100);

        // Act
        var result = def1.CompareTo(def2);

        // Assert
        Assert.IsTrue(result < 0);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void CompareTo_LargerTokenIndex_ReturnsPositive()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(2, "Token", 1, 100);
        var def2 = new TokenShiftDefinition(1, "Token", 1, 100);

        // Act
        var result = def1.CompareTo(def2);

        // Assert
        Assert.IsTrue(result > 0);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void CompareTo_SameTokenIndex_ComparesShiftIndex()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "Token", 1, 100);
        var def2 = new TokenShiftDefinition(1, "Token", 2, 100);

        // Act
        var result = def1.CompareTo(def2);

        // Assert
        Assert.IsTrue(result < 0);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void CompareTo_SameIndices_ComparesShiftMilliseconds()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "Token", 1, 100);
        var def2 = new TokenShiftDefinition(1, "Token", 1, 200);

        // Act
        var result = def1.CompareTo(def2);

        // Assert
        Assert.IsTrue(result < 0);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void CompareTo_SameEverythingElse_ComparesToken()
    {
        // Arrange
        var def1 = new TokenShiftDefinition(1, "AAA", 1, 100);
        var def2 = new TokenShiftDefinition(1, "ZZZ", 1, 100);

        // Act
        var result = def1.CompareTo(def2);

        // Assert
        Assert.IsTrue(result < 0);
    }

    #endregion

    #region Sorting Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Sort_MultipleDefinitions_SortsCorrectly()
    {
        // Arrange
        var definitions = new List<TokenShiftDefinition>
        {
            new(2, "Token", 1, 100),
            new(1, "Token", 2, 100),
            new(1, "Token", 1, 200),
            new(1, "Token", 1, 100)
        };

        // Act
        definitions.Sort();

        // Assert
        Assert.AreEqual(1, definitions[0].TokenIndex);
        Assert.AreEqual(1, definitions[0].ShiftIndex);
        Assert.AreEqual(100, definitions[0].ShiftMilliseconds);

        Assert.AreEqual(1, definitions[1].TokenIndex);
        Assert.AreEqual(1, definitions[1].ShiftIndex);
        Assert.AreEqual(200, definitions[1].ShiftMilliseconds);

        Assert.AreEqual(1, definitions[2].TokenIndex);
        Assert.AreEqual(2, definitions[2].ShiftIndex);

        Assert.AreEqual(2, definitions[3].TokenIndex);
    }

    #endregion
}

[TestClass]
public sealed class TokenInfoTests
{
    private const int TestTimeoutMs = 360000; // 6 minutes

    #region Construction Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var tokenInfo = new TokenInfo("TestToken%2B", 1);

        // Assert
        Assert.AreEqual("TestToken%2B", tokenInfo.Token);
        Assert.AreEqual(1, tokenInfo.Index);
    }

    #endregion

    #region Equality Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var info1 = new TokenInfo("Token", 1);
        var info2 = new TokenInfo("Token", 1);

        // Act & Assert
        Assert.IsTrue(info1.Equals(info2));
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Equals_TokenCaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var info1 = new TokenInfo("TOKEN", 1);
        var info2 = new TokenInfo("token", 1);

        // Act & Assert
        Assert.IsTrue(info1.Equals(info2));
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Equals_DifferentIndex_ReturnsFalse()
    {
        // Arrange
        var info1 = new TokenInfo("Token", 1);
        var info2 = new TokenInfo("Token", 2);

        // Act & Assert
        Assert.IsFalse(info1.Equals(info2));
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Equals_DifferentToken_ReturnsFalse()
    {
        // Arrange
        var info1 = new TokenInfo("Token1", 1);
        var info2 = new TokenInfo("Token2", 1);

        // Act & Assert
        Assert.IsFalse(info1.Equals(info2));
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        var info1 = new TokenInfo("Token", 1);

        // Act & Assert
        Assert.IsFalse(info1.Equals(null));
    }

    #endregion

    #region GetHashCode Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetHashCode_SameValues_SameHash()
    {
        // Arrange
        var info1 = new TokenInfo("Token", 1);
        var info2 = new TokenInfo("Token", 1);

        // Act & Assert
        Assert.AreEqual(info1.GetHashCode(), info2.GetHashCode());
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void GetHashCode_TokenCaseInsensitive_SameHash()
    {
        // Arrange
        var info1 = new TokenInfo("TOKEN", 1);
        var info2 = new TokenInfo("token", 1);

        // Act & Assert
        Assert.AreEqual(info1.GetHashCode(), info2.GetHashCode());
    }

    #endregion

    #region CompareTo Tests

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void CompareTo_SameValues_ReturnsZero()
    {
        // Arrange
        var info1 = new TokenInfo("Token", 1);
        var info2 = new TokenInfo("Token", 1);

        // Act
        var result = info1.CompareTo(info2);

        // Assert
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void CompareTo_Null_ReturnsPositive()
    {
        // Arrange
        var info1 = new TokenInfo("Token", 1);

        // Act
        var result = info1.CompareTo(null);

        // Assert
        Assert.IsTrue(result > 0);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void CompareTo_SmallerIndex_ReturnsNegative()
    {
        // Arrange
        var info1 = new TokenInfo("Token", 1);
        var info2 = new TokenInfo("Token", 2);

        // Act
        var result = info1.CompareTo(info2);

        // Assert
        Assert.IsTrue(result < 0);
    }

    [TestMethod]
    [Timeout(TestTimeoutMs, CooperativeCancellation = true)]
    public void CompareTo_SameIndex_ComparesToken()
    {
        // Arrange
        var info1 = new TokenInfo("AAA", 1);
        var info2 = new TokenInfo("ZZZ", 1);

        // Act
        var result = info1.CompareTo(info2);

        // Assert
        Assert.IsTrue(result < 0);
    }

    #endregion
}

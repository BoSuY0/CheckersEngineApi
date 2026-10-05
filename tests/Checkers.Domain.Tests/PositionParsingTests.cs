namespace Checkers.Domain.Tests;

public sealed class PositionParsingTests
{
    private const string SpecSample = "B:W18,19,22,25,27,28,30,32:B1,5,6,7,10,12,14,16";

    [Fact]
    public void Parse_SpecSample_ReadsSideAndPiecesAndFormatsCanonically()
    {
        var position = Position.Parse(SpecSample);

        Assert.Equal(PieceColor.Black, position.SideToMove);
        Assert.Equal(16, position.PieceCount);
        Assert.Equal(new Piece(PieceColor.White, PieceKind.Man), position[new Square(18)]);
        Assert.Equal(new Piece(PieceColor.Black, PieceKind.Man), position[new Square(1)]);
        Assert.Null(position[new Square(2)]);
        Assert.Equal(SpecSample, position.ToString());
    }

    [Fact]
    public void Parse_Kings_RoundTripsWithKingPrefix()
    {
        var position = Position.Parse("W:WK3,18:BK30,1");

        Assert.Equal(PieceColor.White, position.SideToMove);
        Assert.Equal(new Piece(PieceColor.White, PieceKind.King), position[new Square(3)]);
        Assert.Equal(new Piece(PieceColor.Black, PieceKind.King), position[new Square(30)]);
        Assert.Equal("W:WK3,18:B1,K30", position.ToString());
    }

    [Theory]
    [InlineData(" B : B1-3 , K5 :W30,K31 ", "B:W30,K31:B1,2,3,K5")]
    [InlineData("B:W30,31:B5,1,2,3", "B:W30,31:B1,2,3,5")]
    [InlineData("B:WK29-31:B1", "B:WK29,K30,K31:B1")]
    [InlineData("W:W:BK1", "W:W:BK1")]
    [InlineData("B:B:W", "B:W:B")]
    public void Parse_EquivalentNotations_GiveCanonicalForm(string pdn, string canonical)
    {
        var position = Position.Parse(pdn);

        Assert.Equal(canonical, position.ToString());
        Assert.Equal(Position.Parse(canonical), position);
    }

    [Fact]
    public void Initial_IsTheStartingPosition()
    {
        Assert.Equal("B:W21,22,23,24,25,26,27,28,29,30,31,32:B1,2,3,4,5,6,7,8,9,10,11,12", Position.Initial.ToString());
        Assert.Equal(24, Position.Initial.PieceCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("B")]
    [InlineData("B:W18")]
    [InlineData("B:W18:B1:")]
    [InlineData("X:W18:B1")]
    [InlineData("b:W18:B1")]
    [InlineData("B:w18:B1")]
    [InlineData("B:W18;B1")]
    [InlineData("B:W18,:B1")]
    [InlineData("B:W 18:B1")]
    [InlineData("B:WK 18:B1")]
    [InlineData("B:W18-:B1")]
    [InlineData("B:W18:W1")]
    [InlineData("B:B18:B1")]
    public void Parse_Malformed_ThrowsInvalidPdn(string pdn) =>
        Assert.Throws<InvalidPdnException>(() => Position.Parse(pdn));

    [Theory]
    [InlineData("B:W0:B1")]
    [InlineData("B:W33:B1")]
    [InlineData("B:W99999999999:B1")]
    [InlineData("B:W18-33:B1")]
    public void Parse_SquareOutsideBoard_ThrowsInvalidPdn(string pdn) =>
        Assert.Throws<InvalidPdnException>(() => Position.Parse(pdn));

    [Fact]
    public void Parse_DescendingRange_ThrowsInvalidPdn() =>
        Assert.Throws<InvalidPdnException>(() => Position.Parse("B:W24-21:B1"));

    [Theory]
    [InlineData("B:W18,18:B1")]
    [InlineData("B:W18:B18")]
    [InlineData("B:W21-24,K23:B1")]
    public void Parse_DuplicateSquare_ThrowsInvalidPdn(string pdn) =>
        Assert.Throws<InvalidPdnException>(() => Position.Parse(pdn));

    [Theory]
    [InlineData("B:W5-17:B20")]
    [InlineData("B:W30:B1-13")]
    public void Parse_MoreThanTwelvePiecesPerSide_ThrowsInvalidPdn(string pdn) =>
        Assert.Throws<InvalidPdnException>(() => Position.Parse(pdn));

    [Theory]
    [InlineData("B:W4:B10")]
    [InlineData("B:W18:B29")]
    public void Parse_ManOnItsCrowningRow_ThrowsInvalidPdn(string pdn) =>
        Assert.Throws<InvalidPdnException>(() => Position.Parse(pdn));

    [Fact]
    public void Parse_KingsOnCrowningRows_AreValid()
    {
        var position = Position.Parse("B:WK4:BK29");

        Assert.Equal(PieceKind.King, position[new Square(4)]?.Kind);
        Assert.Equal(PieceKind.King, position[new Square(29)]?.Kind);
    }

    [Fact]
    public void Equality_DependsOnPiecesAndSideToMove()
    {
        var position = Position.Parse("B:W18:B1");

        Assert.Equal(Position.Parse("B:B1:W18"), position);
        Assert.Equal(Position.Parse("B:B1:W18").GetHashCode(), position.GetHashCode());
        Assert.NotEqual(Position.Parse("W:W18:B1"), position);
        Assert.NotEqual(Position.Parse("B:WK18:B1"), position);
        Assert.NotEqual(Position.Parse("B:W19:B1"), position);
    }
}

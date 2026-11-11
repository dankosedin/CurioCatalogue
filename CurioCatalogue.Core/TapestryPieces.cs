using System;
using System.Collections.Generic;
using System.Linq;

namespace CurioCatalogue.Core;

/// <summary>A tapestry piece (the stack's "type"), the picture it belongs to and its place in it (1-based; the digits of a
/// two-dimensional name are the row, then the column). <paramref name="Section"/> names the place for words, the same for every
/// picture of a size: the size, then the piece's number in it (<c>3x1-2</c>, the middle third; <c>2x2-21</c>, the bottom left
/// quarter); <c>1x1</c> for a picture of one piece.</summary>
public sealed record TapestryPiece(string Piece, string Picture, int X = 1, int Y = 1, string Section = TapestryPiece.Whole)
{
    public const string Whole = "1x1";
}

/// <summary>
/// Lists the collectable tapestry pieces from the tapestry block's "sizes" attribute (picture name → "WxH").
/// A large tapestry is made of pieces (modding.md, Vanilla content we rely on): "3x1" ambush → ambush1..3, "2x2" salvation →
/// salvation11, 12, 21, 22, "1x1" stays a single piece without a number.
/// </summary>
public static class TapestryPieces
{
    public static List<TapestryPiece> WithPictures(IEnumerable<KeyValuePair<string, string>> sizes)
    {
        var pieces = new List<TapestryPiece>();
        foreach (var (name, size) in sizes)
        {
            var parts = size.Split('x');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int w) || !int.TryParse(parts[1], out int h) || w < 1 || h < 1)
                throw new FormatException($"Tapestry '{name}' has a bad size '{size}'");

            if (w == 1 && h == 1) pieces.Add(new TapestryPiece(name, name));
            else if (w == 1 || h == 1)
                for (int n = 1; n <= Math.Max(w, h); n++)
                    pieces.Add(new TapestryPiece(name + n, name, w == 1 ? 1 : n, w == 1 ? n : 1, $"{size}-{n}"));
            else
                // The digits are row then column ("yx"): 21 is the bottom left quarter (vssurvivalmod BlockTapestry.cs:148#neighbours2x2 and
                // GetWordedSection, "Section: Bottom Left Quarter").
                for (int y = 1; y <= h; y++)
                for (int x = 1; x <= w; x++)
                    pieces.Add(new TapestryPiece($"{name}{y}{x}", name, x, y, $"{size}-{y}{x}"));
        }
        return pieces;
    }
}

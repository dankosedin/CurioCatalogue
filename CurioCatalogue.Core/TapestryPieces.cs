using System;
using System.Collections.Generic;
using System.Linq;

namespace CurioCatalogue.Core;

/// <summary>A tapestry piece (the stack's "type") and the picture it belongs to.</summary>
public sealed record TapestryPiece(string Piece, string Picture);

/// <summary>
/// Lists the collectable tapestry pieces from the tapestry block's "sizes" attribute (picture name → "WxH").
/// A large tapestry is made of pieces (modding.md, Vanilla content we rely on): "3x1" ambush → ambush1..3, "2x2" salvation →
/// salvation11, 12, 21, 22, "1x1" stays a single piece without a number.
/// </summary>
public static class TapestryPieces
{
    public static List<string> FromSizes(IEnumerable<KeyValuePair<string, string>> sizes) =>
        WithPictures(sizes).Select(p => p.Piece).ToList();

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
                for (int n = 1; n <= Math.Max(w, h); n++) pieces.Add(new TapestryPiece(name + n, name));
            else
                for (int x = 1; x <= w; x++)
                for (int y = 1; y <= h; y++)
                    pieces.Add(new TapestryPiece($"{name}{x}{y}", name));
        }
        return pieces;
    }
}

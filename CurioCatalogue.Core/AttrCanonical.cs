using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ExhaustiveMatching;

namespace CurioCatalogue.Core;

/// <summary>
/// Deterministic text form of an attribute tree: equal trees always give equal strings, so the string
/// can serve as a map key and be stored in the ledger.
///
/// Format: <c>{"key":value,...}</c> with keys sorted ordinally. Strings are quoted, numbers carry a type
/// suffix where the type is ambiguous (<c>5</c> int, <c>5L</c> long, <c>1.5F</c> float, <c>1.5D</c> double),
/// bytes are hex (<c>0x0aff</c>), arrays are <c>[a,b]</c>. An item without attributes, or whose only
/// attributes are ignored, gets the empty string.
/// </summary>
public static class AttrCanonical
{
    public static string Write(AttrTree? tree, IReadOnlySet<string> ignoredPaths)
    {
        if (tree == null) return "";
        var sb = new StringBuilder();
        WriteTree(sb, tree, "", ignoredPaths);
        return sb.Length == 2 ? "" : sb.ToString(); // "{}" at the top level means "no attributes"
    }

    private static void WriteTree(StringBuilder sb, AttrTree tree, string path, IReadOnlySet<string> ignoredPaths)
    {
        sb.Append('{');
        bool first = true;
        foreach (var (key, value) in tree.Children.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            string childPath = path.Length == 0 ? key : path + "/" + key;
            if (ignoredPaths.Contains(childPath)) continue;
            if (!first) sb.Append(',');
            first = false;
            WriteString(sb, key);
            sb.Append(':');
            WriteValue(sb, value, childPath, ignoredPaths);
        }
        sb.Append('}');
    }

    private static void WriteValue(StringBuilder sb, AttrValue value, string path, IReadOnlySet<string> ignoredPaths)
    {
        var inv = CultureInfo.InvariantCulture;
        switch (value)
        {
            case AttrString s: WriteString(sb, s.Value); break;
            case AttrInt i: sb.Append(i.Value.ToString(inv)); break;
            case AttrLong l: sb.Append(l.Value.ToString(inv)).Append('L'); break;
            case AttrFloat f: sb.Append(f.Value.ToString("R", inv)).Append('F'); break;
            case AttrDouble d: sb.Append(d.Value.ToString("R", inv)).Append('D'); break;
            case AttrBool b: sb.Append(b.Value ? "true" : "false"); break;
            case AttrBytes bytes: sb.Append("0x").Append(Convert.ToHexStringLower(bytes.Value)); break;
            case AttrArray array:
                sb.Append('[');
                for (int n = 0; n < array.Items.Count; n++)
                {
                    if (n > 0) sb.Append(',');
                    // Array elements have no names, so ignored paths do not reach inside them.
                    WriteValue(sb, array.Items[n], path, EmptySet);
                }
                sb.Append(']');
                break;
            case AttrTree tree: WriteTree(sb, tree, path, ignoredPaths); break;
            default: throw ExhaustiveMatch.Failed(value);
        }
    }

    private static readonly IReadOnlySet<string> EmptySet = new HashSet<string>();

    private static void WriteString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                default:
                    if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }
}

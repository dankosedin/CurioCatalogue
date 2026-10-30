using System.Runtime.CompilerServices;
using Vintagestory.API.Common;

namespace CurioCatalogue;

internal static class ModLog
{
    /// <summary>
    /// Diagnostics on or off: "Diagnostics" in the config. When off, <see cref="Debug"/> costs nothing:
    /// the expressions inside its message are not even evaluated. Wrap larger data gathering
    /// (loops, lookups made only for the log) in <c>if (ModLog.DebugEnabled)</c>.
    /// </summary>
    public static bool DebugEnabled { get; set; }

    /// <summary>A diagnostics line, written only when <see cref="DebugEnabled"/>. Use an interpolated string.</summary>
    public static void Debug(ICoreAPI api, [InterpolatedStringHandlerArgument] ref DebugMessage message)
    {
        if (!DebugEnabled) return;
        api.Logger.Notification("[CurioCatalogue] {0} debug: {1}", api.Side, message.ToStringAndClear());
    }
}

/// <summary>
/// Builds the text of <see cref="ModLog.Debug"/>. When diagnostics are off it tells the compiler so in its
/// constructor, and the compiler then skips every part of the interpolated string, values included.
/// </summary>
[InterpolatedStringHandler]
internal ref struct DebugMessage
{
    private DefaultInterpolatedStringHandler text;

    public DebugMessage(int literalLength, int formattedCount, out bool enabled)
    {
        enabled = ModLog.DebugEnabled;
        text = enabled ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
    }

    public void AppendLiteral(string value) => text.AppendLiteral(value);
    public void AppendFormatted<T>(T value) => text.AppendFormatted(value);
    public void AppendFormatted<T>(T value, string? format) => text.AppendFormatted(value, format);
    public string ToStringAndClear() => text.ToStringAndClear();
}

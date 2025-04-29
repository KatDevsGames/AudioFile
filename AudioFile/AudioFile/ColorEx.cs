using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using EnterTheCastle.Extensions;

namespace AudioFile;

public static class ColorEx
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color WithRed(this Color color, float r) => new(r.Clamp(0, 1), color.G / 255f, color.B / 255f, color.A / 255f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color WithRed(this Color color, byte r) => new(r.Clamp(0, 255), color.G, color.B, color.A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color WithGreen(this Color color, float g) => new(color.R / 255f, g.Clamp(0, 1), color.B / 255f, color.A / 255f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color WithGreen(this Color color, byte g) => new(color.R, g.Clamp(0, 255), color.B, color.A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color WithBlue(this Color color, float b) => new(color.R / 255f, color.G / 255f, b.Clamp(0, 1), color.A / 255f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color WithBlue(this Color color, byte b) => new(color.R, color.G, b.Clamp(0, 255), color.A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color WithAlpha(this Color color, float a) => new(color, a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color WithAlpha(this Color color, byte a) => new(color.R, color.G, color.B, a.Clamp(0, 255));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToHexCode(this Color color) => $"{color.R:X2}{color.G:X2}{color.B:X2}";
}
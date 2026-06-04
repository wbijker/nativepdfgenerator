namespace PdfSpec.Geometry;

/// <summary>
/// Common colours, copied from the Tailwind CSS default palette. Each family
/// (Red, Orange, …, Slate, Stone, …) exposes a method taking a
/// <paramref name="shade"/>.
///
/// <para>
/// <paramref name="shade"/> accepts either the literal Tailwind shade —
/// <c>50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950</c> — or the
/// short form <c>1..9</c> which maps to <c>100..900</c>. Anything else is
/// snapped to the nearest valid shade. Default is <c>500</c>.
/// </para>
///
/// <para>
/// Example: <c>PdfColors.Red(200)</c> → red-200. For transparency apply
/// <c>cs.SetFillOpacity(...)</c> / <c>cs.SetStrokeOpacity(...)</c> alongside.
/// </para>
/// </summary>
public static class PdfColors
{
    /// <summary>Pure white — 0xFFFFFF.</summary>
    public static PdfColor White() => PdfColor.Rgb(1, 1, 1);

    /// <summary>Pure black — 0x000000.</summary>
    public static PdfColor Black() => PdfColor.Rgb(0, 0, 0);

    // ===== Neutrals ===========================================================

    private static readonly int[] _slate = { 0xf8fafc, 0xf1f5f9, 0xe2e8f0, 0xcbd5e1, 0x94a3b8, 0x64748b, 0x475569, 0x334155, 0x1e293b, 0x0f172a, 0x020617 };
    public static PdfColor Slate(int shade = 500) => Pick(_slate, shade);

    private static readonly int[] _gray = { 0xf9fafb, 0xf3f4f6, 0xe5e7eb, 0xd1d5db, 0x9ca3af, 0x6b7280, 0x4b5563, 0x374151, 0x1f2937, 0x111827, 0x030712 };
    public static PdfColor Gray(int shade = 500) => Pick(_gray, shade);

    private static readonly int[] _zinc = { 0xfafafa, 0xf4f4f5, 0xe4e4e7, 0xd4d4d8, 0xa1a1aa, 0x71717a, 0x52525b, 0x3f3f46, 0x27272a, 0x18181b, 0x09090b };
    public static PdfColor Zinc(int shade = 500) => Pick(_zinc, shade);

    private static readonly int[] _neutral = { 0xfafafa, 0xf5f5f5, 0xe5e5e5, 0xd4d4d4, 0xa3a3a3, 0x737373, 0x525252, 0x404040, 0x262626, 0x171717, 0x0a0a0a };
    public static PdfColor Neutral(int shade = 500) => Pick(_neutral, shade);

    private static readonly int[] _stone = { 0xfafaf9, 0xf5f5f4, 0xe7e5e4, 0xd6d3d1, 0xa8a29e, 0x78716c, 0x57534e, 0x44403c, 0x292524, 0x1c1917, 0x0c0a09 };
    public static PdfColor Stone(int shade = 500) => Pick(_stone, shade);

    // ===== Warm ===============================================================

    private static readonly int[] _red = { 0xfef2f2, 0xfee2e2, 0xfecaca, 0xfca5a5, 0xf87171, 0xef4444, 0xdc2626, 0xb91c1c, 0x991b1b, 0x7f1d1d, 0x450a0a };
    public static PdfColor Red(int shade = 500) => Pick(_red, shade);

    private static readonly int[] _orange = { 0xfff7ed, 0xffedd5, 0xfed7aa, 0xfdba74, 0xfb923c, 0xf97316, 0xea580c, 0xc2410c, 0x9a3412, 0x7c2d12, 0x431407 };
    public static PdfColor Orange(int shade = 500) => Pick(_orange, shade);

    private static readonly int[] _amber = { 0xfffbeb, 0xfef3c7, 0xfde68a, 0xfcd34d, 0xfbbf24, 0xf59e0b, 0xd97706, 0xb45309, 0x92400e, 0x78350f, 0x451a03 };
    public static PdfColor Amber(int shade = 500) => Pick(_amber, shade);

    private static readonly int[] _yellow = { 0xfefce8, 0xfef9c3, 0xfef08a, 0xfde047, 0xfacc15, 0xeab308, 0xca8a04, 0xa16207, 0x854d0e, 0x713f12, 0x422006 };
    public static PdfColor Yellow(int shade = 500) => Pick(_yellow, shade);

    // ===== Cool ===============================================================

    private static readonly int[] _lime = { 0xf7fee7, 0xecfccb, 0xd9f99d, 0xbef264, 0xa3e635, 0x84cc16, 0x65a30d, 0x4d7c0f, 0x3f6212, 0x365314, 0x1a2e05 };
    public static PdfColor Lime(int shade = 500) => Pick(_lime, shade);

    private static readonly int[] _green = { 0xf0fdf4, 0xdcfce7, 0xbbf7d0, 0x86efac, 0x4ade80, 0x22c55e, 0x16a34a, 0x15803d, 0x166534, 0x14532d, 0x052e16 };
    public static PdfColor Green(int shade = 500) => Pick(_green, shade);

    private static readonly int[] _emerald = { 0xecfdf5, 0xd1fae5, 0xa7f3d0, 0x6ee7b7, 0x34d399, 0x10b981, 0x059669, 0x047857, 0x065f46, 0x064e3b, 0x022c22 };
    public static PdfColor Emerald(int shade = 500) => Pick(_emerald, shade);

    private static readonly int[] _teal = { 0xf0fdfa, 0xccfbf1, 0x99f6e4, 0x5eead4, 0x2dd4bf, 0x14b8a6, 0x0d9488, 0x0f766e, 0x115e59, 0x134e4a, 0x042f2e };
    public static PdfColor Teal(int shade = 500) => Pick(_teal, shade);

    private static readonly int[] _cyan = { 0xecfeff, 0xcffafe, 0xa5f3fc, 0x67e8f9, 0x22d3ee, 0x06b6d4, 0x0891b2, 0x0e7490, 0x155e75, 0x164e63, 0x083344 };
    public static PdfColor Cyan(int shade = 500) => Pick(_cyan, shade);

    private static readonly int[] _sky = { 0xf0f9ff, 0xe0f2fe, 0xbae6fd, 0x7dd3fc, 0x38bdf8, 0x0ea5e9, 0x0284c7, 0x0369a1, 0x075985, 0x0c4a6e, 0x082f49 };
    public static PdfColor Sky(int shade = 500) => Pick(_sky, shade);

    private static readonly int[] _blue = { 0xeff6ff, 0xdbeafe, 0xbfdbfe, 0x93c5fd, 0x60a5fa, 0x3b82f6, 0x2563eb, 0x1d4ed8, 0x1e40af, 0x1e3a8a, 0x172554 };
    public static PdfColor Blue(int shade = 500) => Pick(_blue, shade);

    private static readonly int[] _indigo = { 0xeef2ff, 0xe0e7ff, 0xc7d2fe, 0xa5b4fc, 0x818cf8, 0x6366f1, 0x4f46e5, 0x4338ca, 0x3730a3, 0x312e81, 0x1e1b4b };
    public static PdfColor Indigo(int shade = 500) => Pick(_indigo, shade);

    // ===== Purples / pinks ====================================================

    private static readonly int[] _violet = { 0xf5f3ff, 0xede9fe, 0xddd6fe, 0xc4b5fd, 0xa78bfa, 0x8b5cf6, 0x7c3aed, 0x6d28d9, 0x5b21b6, 0x4c1d95, 0x2e1065 };
    public static PdfColor Violet(int shade = 500) => Pick(_violet, shade);

    private static readonly int[] _purple = { 0xfaf5ff, 0xf3e8ff, 0xe9d5ff, 0xd8b4fe, 0xc084fc, 0xa855f7, 0x9333ea, 0x7e22ce, 0x6b21a8, 0x581c87, 0x3b0764 };
    public static PdfColor Purple(int shade = 500) => Pick(_purple, shade);

    private static readonly int[] _fuchsia = { 0xfdf4ff, 0xfae8ff, 0xf5d0fe, 0xf0abfc, 0xe879f9, 0xd946ef, 0xc026d3, 0xa21caf, 0x86198f, 0x701a75, 0x4a044e };
    public static PdfColor Fuchsia(int shade = 500) => Pick(_fuchsia, shade);

    private static readonly int[] _pink = { 0xfdf2f8, 0xfce7f3, 0xfbcfe8, 0xf9a8d4, 0xf472b6, 0xec4899, 0xdb2777, 0xbe185d, 0x9d174d, 0x831843, 0x500724 };
    public static PdfColor Pink(int shade = 500) => Pick(_pink, shade);

    private static readonly int[] _rose = { 0xfff1f2, 0xffe4e6, 0xfecdd3, 0xfda4af, 0xfb7185, 0xf43f5e, 0xe11d48, 0xbe123c, 0x9f1239, 0x881337, 0x4c0519 };
    public static PdfColor Rose(int shade = 500) => Pick(_rose, shade);

    // ===== Helpers ============================================================

    private static PdfColor Pick(int[] palette, int shade) =>
        PdfColor.FromHex(palette[ShadeIndex(shade)]);

    /// <summary>
    /// Map a <paramref name="shade"/> argument to an index into an
    /// 11-entry palette [50, 100, 200, …, 900, 950].
    ///
    /// <list type="bullet">
    ///   <item><description>1..9 → 100..900 (short form).</description></item>
    ///   <item><description>50 → 50; 950 → 950.</description></item>
    ///   <item><description>100..900 in steps of 100 → literal.</description></item>
    ///   <item><description>Anything else snaps to the nearest valid shade.</description></item>
    /// </list>
    /// </summary>
    private static int ShadeIndex(int shade)
    {
        int s = shade >= 1 && shade <= 9 ? shade * 100 : shade;
        return s switch
        {
            < 75 => 0,   //  50
            < 150 => 1,  // 100
            < 250 => 2,  // 200
            < 350 => 3,  // 300
            < 450 => 4,  // 400
            < 550 => 5,  // 500
            < 650 => 6,  // 600
            < 750 => 7,  // 700
            < 850 => 8,  // 800
            < 925 => 9,  // 900
            _ => 10,     // 950
        };
    }
}

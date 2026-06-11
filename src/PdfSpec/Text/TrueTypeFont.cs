using PdfSpec.Objects;

namespace PdfSpec.Fonts;

/// <summary>
/// A TrueType font (.ttf) loaded from a font program and embedded as a PDF simple
/// font (Subtype /TrueType + FontFile2). Single-byte (WinAnsi/Latin-1) only.
/// </summary>
public sealed class TrueTypeFont : EmbeddedFont
{
    private const int First = 32, Last = 255;

    private readonly byte[] _program;
    private readonly int _unitsPerEm;
    private readonly ushort[] _advances;
    private readonly int[] _gidByCode = new int[256];
    private readonly int[] _charWidths;
    private readonly string _psName;
    private readonly int _ascent, _descent, _capHeight, _xHeight;
    private readonly int _xMin, _yMin, _xMax, _yMax;
    private readonly int _winAscent, _winDescent;
    private readonly double _italicAngle;
    private readonly bool _fixedPitch;
    private readonly int _weightClass;

    public static TrueTypeFont FromFile(string path) => new(File.ReadAllBytes(path));
    public static TrueTypeFont FromBytes(byte[] data) => new(data);

    private TrueTypeFont(byte[] program)
    {
        _program = program;
        uint version = U32(program, 0);
        if (version == 0x74746366)
        {
            throw new NotSupportedException("TrueType Collections (.ttc) are not supported; use a single-face .ttf.");
        }
        if (version == 0x4F54544F)
        {
            throw new NotSupportedException("OpenType/CFF (.otf) is not supported yet; use a TrueType (.ttf).");
        }

        int numTables = U16(program, 4);
        var offsets = new Dictionary<string, int>();
        var lengths = new Dictionary<string, int>();
        for (int i = 0; i < numTables; i++)
        {
            int rec = 12 + i * 16;
            string tag = System.Text.Encoding.ASCII.GetString(program, rec, 4);
            offsets[tag] = (int)U32(program, rec + 8);
            lengths[tag] = (int)U32(program, rec + 12);
        }

        int head = Require(offsets, "head");
        _unitsPerEm = U16(program, head + 18);
        if (_unitsPerEm == 0)
        {
            _unitsPerEm = 1000;
        }
        int xMin = S16(program, head + 36), yMin = S16(program, head + 38);
        int xMax = S16(program, head + 40), yMax = S16(program, head + 42);

        int maxp = Require(offsets, "maxp");
        _ = U16(program, maxp + 4);

        int hhea = Require(offsets, "hhea");
        int ascent = S16(program, hhea + 4);
        int descent = S16(program, hhea + 6);
        int numHMetrics = U16(program, hhea + 34);

        int hmtx = Require(offsets, "hmtx");
        _advances = new ushort[Math.Max(1, numHMetrics)];
        for (int i = 0; i < numHMetrics; i++)
        {
            _advances[i] = U16(program, hmtx + i * 4);
        }

        int capHeight = 0, xHeight = 0, winAscent = 0, winDescent = 0;
        _weightClass = 400;
        if (offsets.TryGetValue("OS/2", out int os2))
        {
            int v = U16(program, os2);
            _weightClass = U16(program, os2 + 4);
            ascent = S16(program, os2 + 68);
            descent = S16(program, os2 + 70);
            // usWinAscent / usWinDescent — the Windows clipping bounds.
            // These hug the typical glyph extent (caps + diacritic margin
            // and descenders), giving a tighter line box than the head
            // table's overall yMax / yMin without undershooting cap height
            // on decorative faces the way OS/2 sTypoAscender does.
            winAscent = U16(program, os2 + 74);
            winDescent = U16(program, os2 + 76);
            if (v >= 2 && lengths["OS/2"] >= 90)
            {
                xHeight = S16(program, os2 + 86);
                capHeight = S16(program, os2 + 88);
            }
        }
        if (capHeight == 0) capHeight = (int)(0.7 * _unitsPerEm);
        if (xHeight == 0) xHeight = (int)(0.5 * _unitsPerEm);

        if (offsets.TryGetValue("post", out int post))
        {
            _italicAngle = S16(program, post + 4) + U16(program, post + 6) / 65536.0;
            _fixedPitch = U32(program, post + 16) != 0;
        }

        _psName = ReadPostScriptName(program, offsets, lengths) ?? "TrueTypeFont";

        _ascent = ToEm(ascent);
        _descent = ToEm(descent);
        _capHeight = ToEm(capHeight);
        _xHeight = ToEm(xHeight);
        _xMin = ToEm(xMin); _yMin = ToEm(yMin); _xMax = ToEm(xMax); _yMax = ToEm(yMax);
        // Fall back to head.yMax / -yMin when OS/2 doesn't supply usWin*
        // (very old fonts, or non-Microsoft-targeted faces). yMax/yMin are
        // the bbox of every glyph, so they'll never undershoot.
        _winAscent = winAscent != 0 ? ToEm(winAscent) : _yMax;
        _winDescent = winDescent != 0 ? ToEm(winDescent) : -_yMin;

        BuildGlyphMap(program, Require(offsets, "cmap"));

        _charWidths = new int[Last - First + 1];
        for (int code = First; code <= Last; code++)
        {
            _charWidths[code - First] = AdvanceFor(code);
        }
    }

    public override string Key => "TTF:" + _psName;
    public override string BaseFont => _psName;

    public override int GetGlyphWidth(char c) => AdvanceFor(c < 256 ? c : 0);

    public override FontVerticalMetrics GetVerticalMetrics(double fontSize)
    {
        // Typographic ascent / descent — what body-text leading should
        // use. Static font-level values; on decorative TTFs these can
        // be tighter than the actual glyph reach (sTypoAscender
        // undershoots), which is a known cost of staying static.
        double s = fontSize / 1000.0;
        return new FontVerticalMetrics(
            Ascent: _ascent * s,
            Descent: -_descent * s,
            LineGap: 0,
            CapHeight: _capHeight * s,
            XHeight: _xHeight * s);
    }

    protected override byte[] Program => _program;
    protected override string Subtype => "TrueType";
    protected override string FontFileKey => "FontFile2";
    protected override string Encoding => "WinAnsiEncoding";
    protected override int FirstCode => First;
    protected override int LastCode => Last;
    protected override int[] CharWidths => _charWidths;

    protected override FontDescriptor BuildDescriptor() => new()
    {
        FontName = _psName,
        Flags = Flags(),
        BBoxXMin = _xMin,
        BBoxYMin = _yMin,
        BBoxXMax = _xMax,
        BBoxYMax = _yMax,
        ItalicAngle = _italicAngle,
        Ascent = _ascent,
        Descent = _descent,
        CapHeight = _capHeight,
        StemV = _weightClass >= 600 ? 140 : 80,
    };

    private int Flags()
    {
        int flags = 32;
        if (_fixedPitch) flags |= 1;
        if (_italicAngle != 0) flags |= 64;
        return flags;
    }

    private int ToEm(int value) => (int)Math.Round(value * 1000.0 / _unitsPerEm);

    private int AdvanceFor(int code)
    {
        int gid = code is >= 0 and < 256 ? _gidByCode[code] : 0;
        int advance = gid < _advances.Length ? _advances[gid] : _advances[^1];
        return ToEm(advance);
    }

    private void BuildGlyphMap(byte[] d, int cmap)
    {
        int count = U16(d, cmap + 2);
        int best = -1, bestScore = -1, bestFormat = 0;
        bool bestSymbol = false;
        for (int i = 0; i < count; i++)
        {
            int rec = cmap + 4 + i * 8;
            int plat = U16(d, rec), enc = U16(d, rec + 2);
            int sub = cmap + (int)U32(d, rec + 4);
            int format = U16(d, sub);
            int score = (plat, enc) switch
            {
                (3, 1) => 4,
                (0, _) => 3,
                (3, 0) => 2,
                (1, 0) => 1,
                _ => 0,
            };
            if (score > bestScore)
            {
                bestScore = score; best = sub; bestFormat = format; bestSymbol = plat == 3 && enc == 0;
            }
        }
        if (best < 0)
        {
            return;
        }
        for (int code = 0; code < 256; code++)
        {
            int gid = LookupGlyph(d, best, bestFormat, code);
            if (gid == 0 && bestSymbol)
            {
                gid = LookupGlyph(d, best, bestFormat, 0xF000 + code);
            }
            _gidByCode[code] = gid;
        }
    }

    private static int LookupGlyph(byte[] d, int sub, int format, int u) => format switch
    {
        0 => u < 256 ? d[sub + 6 + u] : 0,
        4 => LookupFormat4(d, sub, u),
        6 => LookupFormat6(d, sub, u),
        12 => LookupFormat12(d, sub, u),
        _ => 0,
    };

    private static int LookupFormat4(byte[] d, int sub, int u)
    {
        if (u > 0xFFFF) return 0;
        int segX2 = U16(d, sub + 6);
        int endO = sub + 14;
        int startO = endO + segX2 + 2;
        int deltaO = startO + segX2;
        int rangeO = deltaO + segX2;
        for (int i = 0; i < segX2; i += 2)
        {
            if (u <= U16(d, endO + i))
            {
                int start = U16(d, startO + i);
                if (u < start) return 0;
                short delta = S16(d, deltaO + i);
                int ro = U16(d, rangeO + i);
                if (ro == 0) return (u + delta) & 0xFFFF;
                int gi = rangeO + i + ro + (u - start) * 2;
                int g = U16(d, gi);
                return g == 0 ? 0 : (g + delta) & 0xFFFF;
            }
        }
        return 0;
    }

    private static int LookupFormat6(byte[] d, int sub, int u)
    {
        int first = U16(d, sub + 6), entries = U16(d, sub + 8);
        return u >= first && u < first + entries ? U16(d, sub + 10 + (u - first) * 2) : 0;
    }

    private static int LookupFormat12(byte[] d, int sub, int u)
    {
        int groups = (int)U32(d, sub + 12);
        for (int g = 0; g < groups; g++)
        {
            int o = sub + 16 + g * 12;
            uint start = U32(d, o), end = U32(d, o + 4);
            if (u >= start && u <= end) return (int)(U32(d, o + 8) + (u - start));
        }
        return 0;
    }

    private static string? ReadPostScriptName(byte[] d, Dictionary<string, int> offsets, Dictionary<string, int> lengths)
    {
        if (!offsets.TryGetValue("name", out int name)) return null;
        int count = U16(d, name + 2), stringBase = name + U16(d, name + 4);
        for (int i = 0; i < count; i++)
        {
            int rec = name + 6 + i * 12;
            int plat = U16(d, rec), nameId = U16(d, rec + 6);
            int len = U16(d, rec + 8), off = U16(d, rec + 10);
            if (nameId != 6) continue;
            var enc = plat == 3 ? System.Text.Encoding.BigEndianUnicode : System.Text.Encoding.ASCII;
            string ps = enc.GetString(d, stringBase + off, len);
            ps = ps.Trim().Replace(" ", string.Empty);
            if (ps.Length > 0) return ps;
        }
        return null;
    }

    private static int Require(Dictionary<string, int> offsets, string tag) =>
        offsets.TryGetValue(tag, out int o) ? o : throw new InvalidDataException($"TrueType font missing required '{tag}' table.");

    private static ushort U16(byte[] d, int o) => (ushort)((d[o] << 8) | d[o + 1]);
    private static short S16(byte[] d, int o) => (short)U16(d, o);
    private static uint U32(byte[] d, int o) => ((uint)d[o] << 24) | ((uint)d[o + 1] << 16) | ((uint)d[o + 2] << 8) | d[o + 3];
}

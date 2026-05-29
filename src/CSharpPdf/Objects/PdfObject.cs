using System.Globalization;
using System.Text;

namespace CSharpPdf.Objects;

/// <summary>
/// Base type for every value that can appear in a PDF file. Each object knows
/// how to serialize itself to its PDF byte representation.
/// </summary>
public abstract class PdfObject
{
    public abstract void Write(Stream stream);

    /// <summary>Write text as raw Latin-1 bytes (1 char == 1 byte).</summary>
    protected static void Emit(Stream stream, string text)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }
}

/// <summary>The PDF <c>null</c> object.</summary>
public sealed class PdfNull : PdfObject
{
    public static readonly PdfNull Instance = new();
    private PdfNull() { }
    public override void Write(Stream stream) => Emit(stream, "null");
}

/// <summary>A PDF boolean (<c>true</c>/<c>false</c>).</summary>
public sealed class PdfBoolean : PdfObject
{
    public bool Value { get; }
    public PdfBoolean(bool value) => Value = value;
    public override void Write(Stream stream) => Emit(stream, Value ? "true" : "false");
}

/// <summary>A PDF numeric object, either an integer or a real number.</summary>
public sealed class PdfNumber : PdfObject
{
    public double Value { get; }
    public bool IsInteger { get; }

    public PdfNumber(long value)
    {
        Value = value;
        IsInteger = true;
    }

    public PdfNumber(double value)
    {
        Value = value;
        IsInteger = false;
    }

    public override void Write(Stream stream)
    {
        if (IsInteger)
        {
            Emit(stream, ((long)Value).ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            // Fixed notation, trimmed trailing zeros; never scientific notation.
            string text = Value.ToString("0.######", CultureInfo.InvariantCulture);
            Emit(stream, text);
        }
    }
}

/// <summary>A PDF name object such as <c>/Type</c>.</summary>
public sealed class PdfName : PdfObject
{
    public string Value { get; }
    public PdfName(string value) => Value = value;

    public override void Write(Stream stream) => Emit(stream, "/" + Escape(Value));

    /// <summary>Escape a name per PDF rules, using #xx for non-regular characters.</summary>
    public static string Escape(string name)
    {
        var sb = new StringBuilder(name.Length + 4);
        foreach (char c in name)
        {
            if (c is > (char)0x20 and < (char)0x7F && !IsDelimiterOrSpecial(c))
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('#').Append(((int)c).ToString("X2", CultureInfo.InvariantCulture));
            }
        }
        return sb.ToString();
    }

    private static bool IsDelimiterOrSpecial(char c) =>
        c is '(' or ')' or '<' or '>' or '[' or ']' or '{' or '}' or '/' or '%' or '#';
}

/// <summary>An indirect reference such as <c>3 0 R</c>.</summary>
public sealed class PdfReference : PdfObject
{
    public int ObjectNumber { get; }
    public int Generation { get; }

    public PdfReference(int objectNumber, int generation = 0)
    {
        ObjectNumber = objectNumber;
        Generation = generation;
    }

    public override void Write(Stream stream) =>
        Emit(stream, $"{ObjectNumber} {Generation} R");
}

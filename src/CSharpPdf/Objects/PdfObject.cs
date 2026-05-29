using System.Globalization;
using System.Text;

namespace CSharpPdf.Objects;

/// <summary>
/// Base type for every value that can appear in a PDF file (ISO 32000-1 §7.3,
/// "Objects"). Each object knows how to serialize itself to its PDF byte
/// representation. PDF defines eight basic object types plus the stream object.
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

/// <summary>The PDF <c>null</c> object (ISO 32000-1 §7.3.9).</summary>
public sealed class PdfNull : PdfObject
{
    public static readonly PdfNull Instance = new();
    private PdfNull() { }
    public override void Write(Stream stream) => Emit(stream, "null");
}

/// <summary>A PDF boolean, true/false (ISO 32000-1 §7.3.2).</summary>
public sealed class PdfBoolean : PdfObject
{
    public bool Value { get; }
    public PdfBoolean(bool value) => Value = value;
    public override void Write(Stream stream) => Emit(stream, Value ? "true" : "false");
}

/// <summary>A PDF numeric object — integer or real (ISO 32000-1 §7.3.3).</summary>
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

/// <summary>A PDF name object such as <c>/Type</c> (ISO 32000-1 §7.3.5).</summary>
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

/// <summary>A PDF literal string such as <c>(Hello)</c> (ISO 32000-1 §7.3.4.2).</summary>
public sealed class PdfString : PdfObject
{
    public string Value { get; }
    public PdfString(string value) => Value = value;

    public override void Write(Stream stream)
    {
        var sb = new StringBuilder(Value.Length + 2);
        sb.Append('(');
        foreach (char c in Value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '(': sb.Append("\\("); break;
                case ')': sb.Append("\\)"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (c < 0x20 || c > 0x7E)
                    {
                        // Non-printable byte: emit as 3-digit octal escape.
                        sb.Append('\\').Append(Convert.ToString(c & 0xFF, 8).PadLeft(3, '0'));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        sb.Append(')');
        Emit(stream, sb.ToString());
    }
}

/// <summary>A PDF hexadecimal string such as <c>&lt;48656C&gt;</c>, for binary data (ISO 32000-1 §7.3.4.3).</summary>
public sealed class PdfHexString : PdfObject
{
    public byte[] Bytes { get; }
    public PdfHexString(byte[] bytes) => Bytes = bytes;

    public override void Write(Stream stream)
    {
        var sb = new StringBuilder(Bytes.Length * 2 + 2);
        sb.Append('<');
        foreach (byte b in Bytes)
        {
            sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        }
        sb.Append('>');
        Emit(stream, sb.ToString());
    }
}

/// <summary>An indirect reference such as <c>3 0 R</c> (ISO 32000-1 §7.3.10).</summary>
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

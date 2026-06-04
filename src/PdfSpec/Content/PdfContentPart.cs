using System.Globalization;
using System.Text;
using PdfSpec.Objects;

namespace PdfSpec.Content;

/// <summary>
/// A fragment of a PDF content stream (ISO 32000-1 §8.2). Buffers operators
/// in postfix form and knows how to flush itself onto a parent fragment's
/// buffer, framed by its own opening/closing operators (e.g. <c>q…Q</c> for
/// a graphics-state scope, <c>BT…ET</c> for a text object).
///
/// <para>
/// A part may hold at most one open child at a time — the most recent
/// <see cref="ContentStream.Push"/> or <see cref="ContentStream.AddText"/>.
/// Any operator on this part, or opening a sibling child, first flushes
/// the open one; the cascade serialises a nested tree depth-first.
/// </para>
/// </summary>
public abstract class PdfContentPart
{
    internal StringBuilder Buffer { get; } = new();
    private PdfContentPart? _openChild;
    private PdfContentPart? _parent;
    private bool _closed;

    /// <summary>Open <paramref name="child"/> as the current child, first flushing any prior open child.</summary>
    protected void OpenChild(PdfContentPart child)
    {
        FlushChild();
        child._parent = this;
        _openChild = child;
    }

    /// <summary>Flush the currently open child (if any) onto this part's buffer, sealing it.</summary>
    protected void FlushChild()
    {
        if (_openChild is null) return;
        var child = _openChild;
        _openChild = null;
        child.FlushOnto(Buffer);
        child._closed = true;
        child._parent = null;
    }

    /// <summary>
    /// Append this part's buffered body — with its framing operators — onto
    /// <paramref name="parentBuffer"/>. Implementations must first call
    /// <see cref="FlushChild"/> to drain any nested open child.
    /// </summary>
    internal abstract void FlushOnto(StringBuilder parentBuffer);

    /// <summary>
    /// Close this part: flush onto the parent (if attached) and seal.
    /// Further operators throw <see cref="InvalidOperationException"/>.
    /// Idempotent; safe to call on a detached root.
    /// </summary>
    public void Flush()
    {
        if (_closed) return;
        _parent?.FlushChild();
    }

    /// <summary>Throw if this part has already been flushed onto its parent.</summary>
    protected void EnsureOpen()
    {
        if (_closed) throw new InvalidOperationException(
            $"{GetType().Name} has been closed (flushed onto its parent) and can no longer accept operators. " +
            "Open a new part on the parent instead.");
    }

    /// <summary>Format <paramref name="value"/> as a PDF number — integer when exact, otherwise up to 6 fractional digits, invariant culture.</summary>
    internal static string N(double value) =>
        value == Math.Floor(value) && !double.IsInfinity(value)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.######", CultureInfo.InvariantCulture);

    /// <summary>Serialise <paramref name="obj"/> as inline PDF syntax (for embedding dicts/arrays/strings into operator operand lists).</summary>
    internal static string Inline(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.Write(ms);
        return Encoding.Latin1.GetString(ms.ToArray());
    }
}

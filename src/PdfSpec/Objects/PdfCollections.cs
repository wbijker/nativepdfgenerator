using System.Collections;

namespace PdfSpec.Objects;

/// <summary>A PDF array: an ordered, heterogeneous list (ISO 32000-1 §7.3.6).</summary>
public sealed class PdfArray : PdfObject
{
    public List<PdfObject> Items { get; } = new();

    public PdfArray() { }
    public PdfArray(params PdfObject[] items) => Items.AddRange(items);

    public void Add(PdfObject item) => Items.Add(item);

    public override void Write(Stream stream)
    {
        Emit(stream, "[");
        for (int i = 0; i < Items.Count; i++)
        {
            if (i > 0)
            {
                Emit(stream, " ");
            }
            Items[i].Write(stream);
        }
        Emit(stream, "]");
    }
}

/// <summary>
/// A PDF dictionary (ISO 32000-1 §7.3.7) — an ordered list of name → value
/// entries written out as <c>&lt;&lt; /name value … &gt;&gt;</c>. Append-only:
/// <see cref="Add"/> appends a pair, <see cref="Entries"/> exposes the list,
/// <see cref="Write"/> serializes. No lookup, no removal — typed wrappers
/// (Catalog, PdfPage, ExtGState, …) hold the actual document state and build
/// a fresh PdfDictionary at write time.
///
/// <para>
/// Object-initializer syntax uses the collection-initializer form:
/// <code>
/// new PdfDictionary {
///     { "Type", new PdfName("Catalog") },
///     { "Pages", pagesRef },
/// }
/// </code>
/// </para>
/// </summary>
public sealed class PdfDictionary : PdfObject, IEnumerable<KeyValuePair<string, PdfObject>>
{
    private readonly List<KeyValuePair<string, PdfObject>> _entries = new();

    /// <summary>Append a (name, value) entry. Order is preserved; duplicate names are not deduplicated.</summary>
    public void Add(string key, PdfObject value) =>
        _entries.Add(new KeyValuePair<string, PdfObject>(key, value));

    /// <summary>The dictionary's entries in insertion order.</summary>
    public IReadOnlyList<KeyValuePair<string, PdfObject>> Entries => _entries;

    // IEnumerable implementation only exists to enable C# collection-initializer
    // syntax: `new PdfDictionary { { "k", v } }`. Iterate via Entries.
    public IEnumerator<KeyValuePair<string, PdfObject>> GetEnumerator() => _entries.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();

    public override void Write(Stream stream)
    {
        Emit(stream, "<<\n");
        foreach (var (key, value) in _entries)
        {
            Emit(stream, "/" + PdfName.Escape(key) + " ");
            value.Write(stream);
            Emit(stream, "\n");
        }
        Emit(stream, ">>");
    }
}

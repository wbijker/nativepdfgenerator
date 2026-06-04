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
/// entries written out as <c>&lt;&lt; /name value … &gt;&gt;</c>. The public
/// surface is two operations: <see cref="Add"/> (append, or replace in place
/// when the key already exists) and <see cref="Remove"/> (delete by key).
/// No lookup is exposed; typed wrappers (<c>Catalog</c>, <c>PdfPage</c>,
/// <see cref="Content.ExtGState"/>, …) hold the actual document state and
/// mutate their owned dictionary in place via these two operations.
///
/// <para>
/// An internal <see cref="Dictionary{TKey, TValue}"/> indexes the entry list
/// for O(1) replace-on-add: setting the same property twice on a typed
/// wrapper updates the entry at its original position instead of duplicating
/// the key.
/// </para>
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
    // Insertion-ordered backing list — what gets written out.
    private readonly List<KeyValuePair<string, PdfObject>> _entries = new();
    // Index from key → position in _entries, for O(1) replace-on-add and remove.
    private readonly Dictionary<string, int> _index = new();

    /// <summary>
    /// Append <c>(key, value)</c>, or replace the value of an existing entry
    /// in place (preserving its position). Insertion order is preserved for
    /// freshly-added keys.
    /// </summary>
    public void Add(string key, PdfObject value)
    {
        if (_index.TryGetValue(key, out int idx))
        {
            _entries[idx] = new KeyValuePair<string, PdfObject>(key, value);
        }
        else
        {
            _index[key] = _entries.Count;
            _entries.Add(new KeyValuePair<string, PdfObject>(key, value));
        }
    }

    /// <summary>
    /// Set <c>key</c> to <c>value</c>, or remove the entry when <c>value</c>
    /// is null. The natural shape for an optional typed-wrapper property:
    /// <c>Dictionary.Set("ca", value is null ? null : new PdfNumber(value.Value))</c>.
    /// </summary>
    public void Set(string key, PdfObject? value)
    {
        if (value is null) Remove(key);
        else Add(key, value);
    }

    /// <summary>
    /// Remove the entry with the given key, if present. Returns true when an
    /// entry was removed. Trailing entries shift down by one position; the
    /// index is rebuilt from scratch (cheap for the dictionary sizes typical
    /// of PDF objects).
    /// </summary>
    public bool Remove(string key)
    {
        if (!_index.TryGetValue(key, out int idx)) return false;
        _entries.RemoveAt(idx);
        _index.Clear();
        for (int i = 0; i < _entries.Count; i++)
        {
            _index[_entries[i].Key] = i;
        }
        return true;
    }

    /// <summary>The dictionary's entries in insertion order.</summary>
    public IReadOnlyList<KeyValuePair<string, PdfObject>> Entries => _entries;

    // IEnumerable implementation exists to enable C# collection-initializer
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

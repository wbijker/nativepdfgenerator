namespace CSharpPdf.Objects;

/// <summary>A PDF array: an ordered, heterogeneous list of objects.</summary>
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

/// <summary>A PDF dictionary: an ordered set of name/value pairs.</summary>
public sealed class PdfDictionary : PdfObject
{
    private readonly List<KeyValuePair<string, PdfObject>> _entries = new();

    public IReadOnlyList<KeyValuePair<string, PdfObject>> Entries => _entries;

    /// <summary>Add or replace an entry, preserving insertion order.</summary>
    public void Set(string key, PdfObject value)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Key == key)
            {
                _entries[i] = new KeyValuePair<string, PdfObject>(key, value);
                return;
            }
        }
        _entries.Add(new KeyValuePair<string, PdfObject>(key, value));
    }

    /// <summary>Return the value for <paramref name="key"/>, or null if absent.</summary>
    public PdfObject? Get(string key)
    {
        foreach (var entry in _entries)
        {
            if (entry.Key == key)
            {
                return entry.Value;
            }
        }
        return null;
    }

    public PdfObject? this[string key]
    {
        get => Get(key);
        set => Set(key, value!);
    }

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

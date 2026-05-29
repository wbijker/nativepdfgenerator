using CSharpPdf.Objects;

namespace CSharpPdf;

/// <summary>
/// Builds a PDF name tree (Chapter 1, "The Name Dictionary"): a structure that
/// maps string names to objects, used for named destinations, embedded files,
/// JavaScript, and so on. This produces a single-node tree, whose <c>/Names</c>
/// array must list the entries sorted by name.
/// </summary>
public sealed class PdfNameTree
{
    private readonly List<KeyValuePair<string, PdfObject>> _entries = new();

    public void Add(string name, PdfObject value) =>
        _entries.Add(new KeyValuePair<string, PdfObject>(name, value));

    public PdfDictionary Build()
    {
        var names = new PdfArray();
        foreach (var entry in _entries.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            names.Add(new PdfString(entry.Key));
            names.Add(entry.Value);
        }

        var root = new PdfDictionary();
        root["Names"] = names;
        return root;
    }
}

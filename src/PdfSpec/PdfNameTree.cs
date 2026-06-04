using PdfSpec.Objects;

namespace PdfSpec;

/// <summary>
/// Builds a PDF name tree (ISO 32000-1 §7.9.6): a balanced tree that maps
/// string keys to objects, used for named destinations, embedded files, etc.
/// </summary>
public sealed class PdfNameTree
{
    private readonly List<KeyValuePair<string, PdfObject>> _entries = new();

    public static int MaxLeafSize = 32;

    public void Add(string name, PdfObject value) =>
        _entries.Add(new KeyValuePair<string, PdfObject>(name, value));

    public PdfDictionary Build(PdfObjectStore? store = null)
    {
        var sorted = _entries.OrderBy(e => e.Key, StringComparer.Ordinal).ToList();

        if (sorted.Count == 0)
        {
            return new PdfDictionary { { "Names", new PdfArray() } };
        }

        if (sorted.Count <= MaxLeafSize)
        {
            // Single-leaf root. Spec says root must NOT carry /Limits.
            return Leaf(sorted, 0, sorted.Count, includeLimits: false);
        }

        // Multi-leaf builds. We pair every node with its (first, last) keys in
        // a parallel C# tuple instead of looking them back up via the dict.
        var leaves = new List<(PdfDictionary node, string first, string last)>();
        for (int start = 0; start < sorted.Count; start += MaxLeafSize)
        {
            int len = System.Math.Min(MaxLeafSize, sorted.Count - start);
            var leaf = Leaf(sorted, start, len, includeLimits: true);
            leaves.Add((leaf, sorted[start].Key, sorted[start + len - 1].Key));
        }

        if (store is null)
        {
            // Legacy inline builder (kept as a fallback when no store is available).
            // The Kids array references the dictionaries directly — tolerated by
            // lenient readers but a spec violation for trees with /Kids.
            var level = leaves;
            while (level.Count > MaxLeafSize)
            {
                var next = new List<(PdfDictionary, string, string)>();
                for (int start = 0; start < level.Count; start += MaxLeafSize)
                {
                    int len = System.Math.Min(MaxLeafSize, level.Count - start);
                    var kids = new PdfArray();
                    for (int i = 0; i < len; i++) kids.Add(level[start + i].node);
                    string first = level[start].first;
                    string last = level[start + len - 1].last;
                    var node = new PdfDictionary
                    {
                        { "Kids", kids },
                        { "Limits", new PdfArray(new PdfString(first), new PdfString(last)) },
                    };
                    next.Add((node, first, last));
                }
                level = next;
            }
            var root = new PdfDictionary();
            var rootKids = new PdfArray();
            foreach (var (node, _, _) in level) rootKids.Add(node);
            root.Add("Kids", rootKids);
            return root;
        }

        // Indirect build: register each leaf (and every intermediate node) as
        // an indirect object so /Kids arrays reference rather than inline.
        var leafRefs = new List<(PdfReference reference, string first, string last)>();
        foreach (var (leaf, first, last) in leaves)
        {
            leafRefs.Add((store.Add(leaf), first, last));
        }

        var levelR = leafRefs;
        while (levelR.Count > MaxLeafSize)
        {
            var next = new List<(PdfReference, string, string)>();
            for (int start = 0; start < levelR.Count; start += MaxLeafSize)
            {
                int len = System.Math.Min(MaxLeafSize, levelR.Count - start);
                var kids = new PdfArray();
                for (int i = 0; i < len; i++) kids.Add(levelR[start + i].Item1);
                string first = levelR[start].Item2;
                string last = levelR[start + len - 1].Item3;
                var node = new PdfDictionary
                {
                    { "Kids", kids },
                    { "Limits", new PdfArray(new PdfString(first), new PdfString(last)) },
                };
                next.Add((store.Add(node), first, last));
            }
            levelR = next;
        }

        var rootIndirect = new PdfDictionary();
        var rootKidsArr = new PdfArray();
        foreach (var (refr, _, _) in levelR) rootKidsArr.Add(refr);
        rootIndirect.Add("Kids", rootKidsArr);
        return rootIndirect;
    }

    private static PdfDictionary Leaf(List<KeyValuePair<string, PdfObject>> sorted, int start, int len, bool includeLimits)
    {
        var names = new PdfArray();
        for (int i = 0; i < len; i++)
        {
            names.Add(new PdfString(sorted[start + i].Key));
            names.Add(sorted[start + i].Value);
        }
        var node = new PdfDictionary { { "Names", names } };
        if (includeLimits)
        {
            node.Add("Limits", new PdfArray(
                new PdfString(sorted[start].Key),
                new PdfString(sorted[start + len - 1].Key)));
        }
        return node;
    }
}

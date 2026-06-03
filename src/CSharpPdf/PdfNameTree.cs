using CSharpPdf.Objects;

namespace CSharpPdf;

/// <summary>
/// Builds a PDF name tree (ISO 32000-1 §7.9.6): a balanced tree that maps
/// string keys to objects, used for named destinations, embedded files,
/// JavaScript, and so on.
///
/// The tree is a root dictionary whose <c>/Kids</c> point to intermediate
/// nodes, which in turn point to leaf nodes that hold the actual
/// <c>/Names</c> arrays. Every non-root node carries a <c>/Limits</c> entry
/// (first/last key in its subtree) and keys must be sorted byte-wise. Some
/// readers (and qpdf's validator) reject single-leaf trees lacking
/// <c>/Limits</c>, or trees whose leaves grow unbounded — so we always emit
/// a hierarchy when the entry count exceeds <see cref="MaxLeafSize"/>.
/// </summary>
public sealed class PdfNameTree
{
    private readonly List<KeyValuePair<string, PdfObject>> _entries = new();

    /// <summary>Max entries packed into a single leaf node. Common practice is 32-64.</summary>
    public static int MaxLeafSize = 32;

    public void Add(string name, PdfObject value) =>
        _entries.Add(new KeyValuePair<string, PdfObject>(name, value));

    /// <summary>
    /// Build the name-tree root dictionary. If <paramref name="store"/> is
    /// provided, intermediate and leaf nodes are registered as indirect
    /// objects (required by the spec for trees with /Kids, and by stricter
    /// validators like qpdf). If null (legacy small-tree builder), kids are
    /// inlined — fine only for trees that fit in a single leaf.
    /// </summary>
    public PdfDictionary Build(PdfObjectStore? store = null)
    {
        // Sort byte-wise (PDF spec compares the raw bytes of each key).
        var sorted = _entries.OrderBy(e => e.Key, StringComparer.Ordinal).ToList();

        if (sorted.Count == 0)
        {
            return new PdfDictionary { ["Names"] = new PdfArray() };
        }

        if (sorted.Count <= MaxLeafSize)
        {
            // Single-leaf root. The root must NOT carry /Limits.
            return Leaf(sorted, 0, sorted.Count, includeLimits: false);
        }

        if (store is null)
        {
            // Legacy inline builder. Each Kids entry is the dictionary itself
            // (not a reference). Tolerated by lenient readers but a spec
            // violation; only used when no store is available.
            var level = new List<PdfDictionary>();
            for (int start = 0; start < sorted.Count; start += MaxLeafSize)
            {
                int len = System.Math.Min(MaxLeafSize, sorted.Count - start);
                level.Add(Leaf(sorted, start, len, includeLimits: true));
            }
            while (level.Count > 1)
            {
                var next = new List<PdfDictionary>();
                for (int start = 0; start < level.Count; start += MaxLeafSize)
                {
                    int len = System.Math.Min(MaxLeafSize, level.Count - start);
                    var kids = new PdfArray();
                    for (int i = 0; i < len; i++) kids.Add(level[start + i]);
                    var node = new PdfDictionary { ["Kids"] = kids };
                    var fl = (PdfArray)level[start].Get("Limits")!;
                    var ll = (PdfArray)level[start + len - 1].Get("Limits")!;
                    node["Limits"] = new PdfArray(fl.Items[0], ll.Items[1]);
                    next.Add(node);
                }
                level = next;
            }
            level[0].Remove("Limits");
            return level[0];
        }

        // Build leaf nodes and register each as an indirect object so the
        // Kids array can reference them rather than inline them.
        var leafRefs = new List<(PdfReference Ref, string First, string Last)>();
        for (int start = 0; start < sorted.Count; start += MaxLeafSize)
        {
            int len = System.Math.Min(MaxLeafSize, sorted.Count - start);
            var leaf = Leaf(sorted, start, len, includeLimits: true);
            var leafRef = store.Add(leaf);
            leafRefs.Add((leafRef, sorted[start].Key, sorted[start + len - 1].Key));
        }

        // Roll up into intermediate levels until only as many groups remain
        // as fit directly under the root.
        var levelR = leafRefs;
        while (levelR.Count > MaxLeafSize)
        {
            var next = new List<(PdfReference, string, string)>();
            for (int start = 0; start < levelR.Count; start += MaxLeafSize)
            {
                int len = System.Math.Min(MaxLeafSize, levelR.Count - start);
                var kids = new PdfArray();
                for (int i = 0; i < len; i++) kids.Add(levelR[start + i].Item1);
                var node = new PdfDictionary { ["Kids"] = kids };
                string firstKey = levelR[start].Item2;
                string lastKey = levelR[start + len - 1].Item3;
                node["Limits"] = new PdfArray(new PdfString(firstKey), new PdfString(lastKey));
                var nodeRef = store.Add(node);
                next.Add((nodeRef, firstKey, lastKey));
            }
            levelR = next;
        }

        // Build the root directly referencing the top-level groups (no extra
        // wrapping layer). Root must NOT have /Limits.
        var root = new PdfDictionary();
        var rootKids = new PdfArray();
        foreach (var (refr, _, _) in levelR) rootKids.Add(refr);
        root["Kids"] = rootKids;
        return root;
    }

    private static PdfDictionary Leaf(List<KeyValuePair<string, PdfObject>> sorted, int start, int len, bool includeLimits)
    {
        var names = new PdfArray();
        for (int i = 0; i < len; i++)
        {
            names.Add(new PdfString(sorted[start + i].Key));
            names.Add(sorted[start + i].Value);
        }
        var node = new PdfDictionary { ["Names"] = names };
        if (includeLimits)
        {
            node["Limits"] = new PdfArray(
                new PdfString(sorted[start].Key),
                new PdfString(sorted[start + len - 1].Key));
        }
        return node;
    }
}

using CSharpPdf.Content;
using CSharpPdf.Objects;

namespace CSharpPdf.Tagging;

/// <summary>
/// Builds a logical structure tree (Chapter 11): a StructTreeRoot whose single
/// Document element collects per-page structure elements, each tied to page
/// content through marked-content ids (MCIDs) and the ParentTree. Use
/// <see cref="TagPage"/> to obtain a per-page tagger.
/// </summary>
public sealed class StructureTreeBuilder
{
    private readonly PdfDoc _doc;
    private readonly PdfDictionary _root = new();
    private readonly PdfReference _rootRef;
    private readonly PdfReference _documentRef;
    private readonly PdfArray _documentKids = new();
    private readonly PdfArray _parentTreeNums = new();
    private readonly PdfDictionary _roleMap = new();
    private int _nextStructParents;

    public StructureTreeBuilder(PdfDoc doc)
    {
        _doc = doc;
        _root["Type"] = new PdfName("StructTreeRoot");
        _rootRef = doc.AddObject(_root);

        var document = new PdfDictionary
        {
            ["Type"] = new PdfName("StructElem"),
            ["S"] = new PdfName("Document"),
            ["P"] = _rootRef,
            ["K"] = _documentKids,
        };
        _documentRef = doc.AddObject(document);
        _root["K"] = _documentRef;
        _root["ParentTree"] = doc.AddObject(new PdfDictionary { ["Nums"] = _parentTreeNums });
        doc.SetStructTreeRoot(_rootRef);
    }

    /// <summary>Map a custom structure type to a standard one via the RoleMap.</summary>
    public void MapRole(string customType, string standardType)
    {
        _roleMap[customType] = new PdfName(standardType);
        _root["RoleMap"] = _roleMap;
    }

    /// <summary>Begin tagging a page; the returned tagger assigns MCIDs in order.</summary>
    public PageTagger TagPage(PdfPage page)
    {
        int structParents = _nextStructParents++;
        page.Dictionary["StructParents"] = new PdfNumber(structParents);
        return new PageTagger(this, page, structParents);
    }

    /// <summary>Per-page tagger: brackets content with structure elements and MCIDs.</summary>
    public sealed class PageTagger
    {
        private readonly StructureTreeBuilder _builder;
        private readonly PdfPage _page;
        private readonly int _structParents;
        private readonly List<PdfReference> _elementsByMcid = new();

        internal PageTagger(StructureTreeBuilder builder, PdfPage page, int structParents)
        {
            _builder = builder;
            _page = page;
            _structParents = structParents;
        }

        public ContentStream Content => _page.Content;

        /// <summary>
        /// Open a structure element of <paramref name="structureType"/>, creating
        /// its StructElem (child of Document) and beginning its marked content.
        /// </summary>
        public PageTagger Begin(string structureType)
        {
            int mcid = _elementsByMcid.Count;
            var element = new PdfDictionary
            {
                ["Type"] = new PdfName("StructElem"),
                ["S"] = new PdfName(structureType),
                ["P"] = _builder._documentRef,
                ["Pg"] = _page.Reference,
                ["K"] = new PdfNumber(mcid),
            };
            _elementsByMcid.Add(_builder._doc.AddObject(element));
            _builder._documentKids.Add(_elementsByMcid[^1]);
            _page.Content.BeginStructureContent(structureType, mcid);
            return this;
        }

        public PageTagger End()
        {
            _page.Content.EndMarkedContent();
            return this;
        }

        /// <summary>Register this page's elements in the ParentTree (call once, when done).</summary>
        public void Finish()
        {
            var elements = new PdfArray();
            foreach (var e in _elementsByMcid)
            {
                elements.Add(e);
            }
            _builder._parentTreeNums.Add(new PdfNumber(_structParents));
            _builder._parentTreeNums.Add(elements);
        }
    }
}

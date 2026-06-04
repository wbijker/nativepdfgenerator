using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace PdfSpec.Structure;

/// <summary>
/// XMP metadata for the document (ISO 32000-1 §14.3.2) — a richer parallel
/// to the <c>/Info</c> dictionary. Set the optional fields and pass to
/// <see cref="PdfDoc.SetXmpMetadata(XmpMetadata)"/>. The XMP packet is
/// rendered by <see cref="XmlSerializer"/> over the
/// <see cref="XmpDocument"/> wire-type tree underneath.
/// <para>
/// The XMP packet is what flags a document as PDF/A — set
/// <see cref="PdfA"/> to declare the conformance level.
/// </para>
/// </summary>
public sealed class XmpMetadata
{
    /// <summary><c>dc:title</c>.</summary>
    public string? Title { get; set; }

    /// <summary><c>dc:creator</c> — the author (person).</summary>
    public string? Author { get; set; }

    /// <summary><c>dc:description</c> — usually maps to the Info dictionary's <c>Subject</c>.</summary>
    public string? Description { get; set; }

    /// <summary><c>pdf:Keywords</c>.</summary>
    public string? Keywords { get; set; }

    /// <summary><c>xmp:CreatorTool</c> — the application that produced the document.</summary>
    public string? CreatorTool { get; set; }

    /// <summary><c>pdf:Producer</c>.</summary>
    public string? Producer { get; set; }

    /// <summary><c>xmp:CreateDate</c>.</summary>
    public DateTimeOffset? CreateDate { get; set; }

    /// <summary><c>xmp:ModifyDate</c>.</summary>
    public DateTimeOffset? ModifyDate { get; set; }

    /// <summary>If set, declares the document as PDF/A and emits the <c>pdfaid:part</c> + <c>pdfaid:conformance</c> pair.</summary>
    public PdfAConformance? PdfA { get; set; }

    /// <summary>Render the XMP packet as the UTF-8 string that goes inside the <c>/Metadata</c> stream.</summary>
    public string Build()
    {
        var document = new XmpDocument();
        var d = document.Rdf.Description;
        if (Title is not null) d.Title = XmpAlt.Default(Title);
        if (Author is not null) d.Creator = XmpSeq.Default(Author);
        if (Description is not null) d.Description = XmpAlt.Default(Description);
        d.Keywords = Keywords;
        d.CreatorTool = CreatorTool;
        d.Producer = Producer;
        if (CreateDate is not null) d.CreateDate = FormatDate(CreateDate.Value);
        if (ModifyDate is not null) d.ModifyDate = FormatDate(ModifyDate.Value);
        if (PdfA is { } level)
        {
            d.PdfAPart = level.Part().ToString(CultureInfo.InvariantCulture);
            d.PdfAConformance = level.Conformance().ToString();
        }

        var serializer = new XmlSerializer(typeof(XmpDocument));
        var prefixes = new XmlSerializerNamespaces();
        prefixes.Add("x", XmpNs.X);
        prefixes.Add("rdf", XmpNs.Rdf);
        prefixes.Add("dc", XmpNs.Dc);
        prefixes.Add("xmp", XmpNs.Xmp);
        prefixes.Add("pdf", XmpNs.Pdf);
        if (PdfA is not null) prefixes.Add("pdfaid", XmpNs.PdfAId);

        var sb = new StringBuilder();
        sb.Append("<?xpacket begin=\"﻿\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n");
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
        };
        using (var writer = XmlWriter.Create(sb, settings))
        {
            serializer.Serialize(writer, document, prefixes);
        }
        sb.Append("\n<?xpacket end=\"w\"?>");
        return sb.ToString();
    }

    private static string FormatDate(DateTimeOffset when) =>
        when.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
}

/// <summary>
/// PDF/A conformance level — combines the part (1, 2, 3) and the
/// conformance letter (A, B, U). Used as <c>pdfaid:part</c> +
/// <c>pdfaid:conformance</c> in the XMP packet.
/// </summary>
public enum PdfAConformance
{
    A1A, A1B,
    A2A, A2B, A2U,
    A3A, A3B, A3U,
}

internal static class PdfAConformanceExtensions
{
    public static int Part(this PdfAConformance level) => level switch
    {
        PdfAConformance.A1A or PdfAConformance.A1B => 1,
        PdfAConformance.A2A or PdfAConformance.A2B or PdfAConformance.A2U => 2,
        PdfAConformance.A3A or PdfAConformance.A3B or PdfAConformance.A3U => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
    };

    public static char Conformance(this PdfAConformance level) => level switch
    {
        PdfAConformance.A1A or PdfAConformance.A2A or PdfAConformance.A3A => 'A',
        PdfAConformance.A1B or PdfAConformance.A2B or PdfAConformance.A3B => 'B',
        PdfAConformance.A2U or PdfAConformance.A3U => 'U',
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
    };
}

/// <summary>XMP namespace URIs — public consts so they can be used in <c>[XmlElement]</c> attributes on the wire types.</summary>
public static class XmpNs
{
    public const string X = "adobe:ns:meta/";
    public const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    public const string Dc = "http://purl.org/dc/elements/1.1/";
    public const string Xmp = "http://ns.adobe.com/xap/1.0/";
    public const string Pdf = "http://ns.adobe.com/pdf/1.3/";
    public const string PdfAId = "http://www.aiim.org/pdfa/ns/id/";
    public const string XmlLang = "http://www.w3.org/XML/1998/namespace";
}

// ===== Wire types — the XML structure that XmlSerializer renders. ============

/// <summary>Root <c>x:xmpmeta</c> element.</summary>
[XmlRoot("xmpmeta", Namespace = XmpNs.X)]
public sealed class XmpDocument
{
    [XmlElement("RDF", Namespace = XmpNs.Rdf)]
    public XmpRdf Rdf { get; set; } = new();
}

public sealed class XmpRdf
{
    [XmlElement("Description", Namespace = XmpNs.Rdf)]
    public XmpDescription Description { get; set; } = new();
}

public sealed class XmpDescription
{
    [XmlAttribute("about", Namespace = XmpNs.Rdf)]
    public string About { get; set; } = string.Empty;

    [XmlElement("title", Namespace = XmpNs.Dc)] public XmpAlt? Title { get; set; }
    [XmlElement("creator", Namespace = XmpNs.Dc)] public XmpSeq? Creator { get; set; }
    [XmlElement("description", Namespace = XmpNs.Dc)] public XmpAlt? Description { get; set; }
    [XmlElement("Keywords", Namespace = XmpNs.Pdf)] public string? Keywords { get; set; }
    [XmlElement("CreatorTool", Namespace = XmpNs.Xmp)] public string? CreatorTool { get; set; }
    [XmlElement("Producer", Namespace = XmpNs.Pdf)] public string? Producer { get; set; }
    [XmlElement("CreateDate", Namespace = XmpNs.Xmp)] public string? CreateDate { get; set; }
    [XmlElement("ModifyDate", Namespace = XmpNs.Xmp)] public string? ModifyDate { get; set; }
    [XmlElement("part", Namespace = XmpNs.PdfAId)] public string? PdfAPart { get; set; }
    [XmlElement("conformance", Namespace = XmpNs.PdfAId)] public string? PdfAConformance { get; set; }
}

/// <summary><c>&lt;…&gt;&lt;rdf:Alt&gt;&lt;rdf:li xml:lang="x-default"&gt;text&lt;/rdf:li&gt;&lt;/rdf:Alt&gt;&lt;/…&gt;</c></summary>
public sealed class XmpAlt
{
    [XmlElement("Alt", Namespace = XmpNs.Rdf)]
    public XmpAltContainer Container { get; set; } = new();

    public static XmpAlt Default(string text) =>
        new() { Container = new XmpAltContainer { Li = new XmpLangText { Value = text } } };
}

public sealed class XmpAltContainer
{
    [XmlElement("li", Namespace = XmpNs.Rdf)]
    public XmpLangText Li { get; set; } = new();
}

public sealed class XmpLangText
{
    [XmlAttribute("lang", Namespace = XmpNs.XmlLang)]
    public string Lang { get; set; } = "x-default";

    [XmlText]
    public string Value { get; set; } = string.Empty;
}

/// <summary><c>&lt;…&gt;&lt;rdf:Seq&gt;&lt;rdf:li&gt;text&lt;/rdf:li&gt;&lt;/rdf:Seq&gt;&lt;/…&gt;</c></summary>
public sealed class XmpSeq
{
    [XmlElement("Seq", Namespace = XmpNs.Rdf)]
    public XmpSeqContainer Container { get; set; } = new();

    public static XmpSeq Default(string text) =>
        new() { Container = new XmpSeqContainer { Li = text } };
}

public sealed class XmpSeqContainer
{
    [XmlElement("li", Namespace = XmpNs.Rdf)]
    public string Li { get; set; } = string.Empty;
}

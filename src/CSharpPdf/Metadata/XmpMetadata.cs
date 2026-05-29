using System.Globalization;
using System.Text;

namespace CSharpPdf.Metadata;

/// <summary>
/// Builds an XMP metadata packet (Chapter 12) — the canonical, XML/RDF-based
/// document metadata stored in the catalog's Metadata stream. Covers the common
/// Dublin Core (dc), XMP basic (xmp), and PDF (pdf) properties.
/// </summary>
public static class XmpMetadata
{
    public static string Build(
        string? title = null, string? author = null, string? subject = null,
        string? keywords = null, string? creator = null, string? producer = null,
        DateTimeOffset? created = null, DateTimeOffset? modified = null)
    {
        var sb = new StringBuilder();
        sb.Append("<?xpacket begin=\"﻿\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n");
        sb.Append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n");
        sb.Append("  <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n");
        sb.Append("    <rdf:Description rdf:about=\"\"\n");
        sb.Append("        xmlns:dc=\"http://purl.org/dc/elements/1.1/\"\n");
        sb.Append("        xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"\n");
        sb.Append("        xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\">\n");

        if (title is not null)
        {
            sb.Append($"      <dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">{Escape(title)}</rdf:li></rdf:Alt></dc:title>\n");
        }
        if (author is not null)
        {
            sb.Append($"      <dc:creator><rdf:Seq><rdf:li>{Escape(author)}</rdf:li></rdf:Seq></dc:creator>\n");
        }
        if (subject is not null)
        {
            sb.Append($"      <dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">{Escape(subject)}</rdf:li></rdf:Alt></dc:description>\n");
        }
        if (keywords is not null)
        {
            sb.Append($"      <pdf:Keywords>{Escape(keywords)}</pdf:Keywords>\n");
        }
        if (producer is not null)
        {
            sb.Append($"      <pdf:Producer>{Escape(producer)}</pdf:Producer>\n");
        }
        if (creator is not null)
        {
            sb.Append($"      <xmp:CreatorTool>{Escape(creator)}</xmp:CreatorTool>\n");
        }
        if (created is { } c)
        {
            sb.Append($"      <xmp:CreateDate>{Iso(c)}</xmp:CreateDate>\n");
        }
        if (modified is { } m)
        {
            sb.Append($"      <xmp:ModifyDate>{Iso(m)}</xmp:ModifyDate>\n");
        }

        sb.Append("    </rdf:Description>\n");
        sb.Append("  </rdf:RDF>\n");
        sb.Append("</x:xmpmeta>\n");
        sb.Append("<?xpacket end=\"w\"?>");
        return sb.ToString();
    }

    private static string Iso(DateTimeOffset when) =>
        when.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);

    private static string Escape(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");
}

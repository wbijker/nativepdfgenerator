using PdfSpec.Objects;

namespace CSharpPdf.Layers;

/// <summary>
/// Helpers for optional content membership (Chapter 10): an OCMD ties content
/// to several OCGs and resolves their combined state via a visibility policy
/// (AllOn/AnyOn/AnyOff/AllOff) or a visibility expression (And/Or/Not).
/// </summary>
public static class OptionalContent
{
    /// <summary>An OCMD using a visibility policy over a set of groups.</summary>
    public static PdfDictionary Membership(PdfReference[] groups, string policy = "AnyOn")
    {
        var ocgs = new PdfArray();
        foreach (var g in groups) ocgs.Add(g);
        var d = new PdfDictionary();
        d.SetName("Type", "OCMD");
        d.Add("OCGs", ocgs);
        d.SetName("P", policy);
        return d;
    }

    /// <summary>An OCMD whose visibility is a Boolean expression (the VE key).</summary>
    public static PdfDictionary MembershipExpression(PdfArray visibilityExpression)
    {
        var d = new PdfDictionary();
        d.SetName("Type", "OCMD");
        d.Add("VE", visibilityExpression);
        return d;
    }

    /// <summary>Build an <c>[/And ...]</c> visibility expression of OCGs or sub-expressions.</summary>
    public static PdfArray And(params PdfObject[] operands) => Expression("And", operands);

    /// <summary>Build an <c>[/Or ...]</c> visibility expression.</summary>
    public static PdfArray Or(params PdfObject[] operands) => Expression("Or", operands);

    /// <summary>Build a <c>[/Not operand]</c> visibility expression.</summary>
    public static PdfArray Not(PdfObject operand) => Expression("Not", new[] { operand });

    private static PdfArray Expression(string op, PdfObject[] operands)
    {
        var array = new PdfArray(new PdfName(op));
        foreach (var operand in operands) array.Add(operand);
        return array;
    }
}

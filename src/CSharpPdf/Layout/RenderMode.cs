namespace CSharpPdf.Layout;

/// <summary>Phase of a two-phase save. See <see cref="LayoutEngine.SaveTwoPhase"/>.</summary>
public enum RenderMode
{
    /// <summary>Drawing is suppressed; the engine paginates so document-level totals can be captured.</summary>
    Measure,
    /// <summary>Drawing is real; PDF objects, annotations, outline entries are all emitted.</summary>
    Render,
}

using CSharpPdf.Layout;

namespace CSharpPdf.Fluent;

/// <summary>Configures a <see cref="TransformElement"/> and supplies its child.</summary>
public sealed class TransformBuilder
{
    private readonly TransformElement _t;
    internal TransformBuilder(TransformElement t) { _t = t; }

    public TransformBuilder Rotate(double degrees) { _t.Rotate = degrees; return this; }
    public TransformBuilder Scale(double sx, double sy) { _t.ScaleX = sx; _t.ScaleY = sy; return this; }
    public TransformBuilder Scale(double s) { _t.ScaleX = s; _t.ScaleY = s; return this; }
    public TransformBuilder Pivot(double fractionX, double fractionY) { _t.PivotX = fractionX; _t.PivotY = fractionY; return this; }

    public void Content(System.Action<FluentContainer> build)
    {
        var inner = new FluentContainer();
        build(inner);
        _t.Content = inner.Slot.Content;
    }
}

using PdfSpec.Content;

namespace PdfSpec.Layout;

public abstract class Element
{
    public abstract PdfSizeHint SizeHint(PdfSize available);
    public abstract RenderResult Render(ContentStream cs, PdfSize available);
}
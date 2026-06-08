using PdfSpec.Content;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

public class Rectangle(int size, PdfColor color) : Element
{
    public override PdfSizeHint SizeHint(PdfSize available)
    {
        return PdfSizeHint.Fixed(size, size);
    }

    public override RenderResult Render(ContentStream cs, PdfSize available)
    {
        cs.SetFillColor(color);
        cs.Rectangle(0, 0, size, size);
        cs.Fill();

        // cs.AddText()
        //     .SetFillColor(PdfColors.Black())
        //     .SetStrokeColor(PdfColors.Black())
        //     .SetTextMatrix(PdfMatrix.Translate(0, 0))
        //     .ShowText("Die hond blaf")
        //     .Build();

        return RenderResult.Done(size);
    }
}
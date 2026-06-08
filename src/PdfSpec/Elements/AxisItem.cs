using PdfSpec.Layout;

namespace PdfSpec.Elements;

public class AxisItem(AxisSize size, Element content)
{
    public AxisSize Size { get; } = size;
    public Element Content { get; } = content;
}

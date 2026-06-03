using CSharpPdf.Layout;

namespace CSharpPdf.Fluent;

/// <summary>
/// A reusable <see cref="IComponent"/> that composes a small product card —
/// title, tagline, rating, price. The rating row is a custom Element
/// (<see cref="StarRatingElement"/>) plugged in via
/// <see cref="Column.Element"/>, demonstrating the "Component contains a
/// custom Element" direction.
/// </summary>
public sealed class ProductCard : IComponent
{
    public string Title { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string Price { get; set; } = "";
    public int Rating { get; set; } = 4;
    public Color Accent { get; set; } = Colors.DarkBlue;
    public Color Surface { get; set; } = Colors.PaleYellow;

    public void Compose(Container container)
    {
        container
            .Padding(10)
            .Background(Surface)
            .Border(Colors.LightGray, 0.5)
            .BorderRadius(4)
            .Column(col =>
            {
                col.Item().Text(Title).Bold().FontSize(13).FontColor(Accent);
                col.Item().Padding(1);
                col.Item().Text(Tagline).Italic().FontSize(10).FontColor(Colors.Gray);
                col.Item().Padding(4);

                // The rating row is a custom Element. Compose it into the
                // fluent tree via the Column.Element shortcut.
                col.Element(new StarRatingElement { Filled = Rating });

                col.Item().Padding(3);
                col.Item().Text(Price).Bold().FontSize(12).FontColor(Colors.Black);
            });
    }
}

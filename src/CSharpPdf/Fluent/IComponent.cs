namespace CSharpPdf.Fluent;

/// <summary>
/// A reusable fluent fragment. <see cref="Compose"/> is called once with a
/// <see cref="Container"/> and uses the normal fluent API to fill it. This
/// is the high-level counterpart to <see cref="Container.Element"/>: where
/// <c>.Element(uiElement)</c> drops in a raw <see cref="Layout.UIElement"/>
/// (requiring you to implement <c>SpaceHint</c>/<c>RenderCore</c> yourself),
/// <c>.Component(iComponent)</c> lets a component build itself by composing
/// the fluent surface — no measurement code needed.
/// <code>
/// public sealed class Callout : IComponent
/// {
///     public string Text { get; set; } = "";
///     public Color Color { get; set; } = Colors.PaleBlue;
///
///     public void Compose(Container container) =>
///         container
///             .Padding(8).Background(Color).BorderRadius(4)
///             .Text(Text).FontSize(11);
/// }
///
/// // …
/// col.Item().Component(new Callout { Text = "Heads up!" });
/// </code>
/// </summary>
public interface IComponent
{
    /// <summary>Fill <paramref name="container"/> with this component's content.</summary>
    void Compose(Container container);
}

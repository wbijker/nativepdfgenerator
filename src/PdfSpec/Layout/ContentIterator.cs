namespace PdfSpec.Layout;

/// <summary>
/// Stateful pointer over a read-only list — peek without consuming,
/// advance with <see cref="MoveNext"/>, push a continuation back into
/// the head with <see cref="Putback"/>. The same instance is threaded
/// through a render's continuation chain so the next slot resumes from
/// exactly where the previous one stopped (no list slicing per page).
///
/// <para>
/// Shared by <see cref="Elements.Paragraph"/> (words) and
/// <see cref="Elements.MultiColumn"/> (items). The Putback slot is the
/// "I rendered some of this and have a continuation" channel: stash the
/// new element returned by a child's Partial render and the very next
/// <see cref="TryPeek"/> hands it back instead of advancing the list.
/// A <see cref="MoveNext"/> clears the Putback slot before the list
/// index moves on.
/// </para>
/// </summary>
public sealed class ContentIterator<T>
{
    private readonly IReadOnlyList<T> _items;
    private int _i;
    private T _pending = default!;
    private bool _hasPending;

    public ContentIterator(IReadOnlyList<T> items)
    {
        _items = items;
    }

    public bool Done => !_hasPending && _i >= _items.Count;

    public bool TryPeek(out T item)
    {
        if (_hasPending) { item = _pending; return true; }
        if (_i >= _items.Count) { item = default!; return false; }
        item = _items[_i];
        return true;
    }

    public void MoveNext()
    {
        if (_hasPending) { _hasPending = false; _pending = default!; return; }
        _i++;
    }

    public void Putback(T continuation)
    {
        _pending = continuation;
        _hasPending = true;
    }
}

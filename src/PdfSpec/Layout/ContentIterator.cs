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

    // Stack of pending items pushed back via Putback — top of stack is the
    // very next item TryPeek/MoveNext sees. Multi-element putback (e.g. a
    // whole line of consumed-but-not-emitted words rolled back when it
    // turns out to overflow vertically) calls Putback in reverse order
    // so the items come back out in their original order.
    private readonly Stack<T> _pending = new();

    public ContentIterator(IReadOnlyList<T> items)
    {
        _items = items;
    }

    public bool Done => _pending.Count == 0 && _i >= _items.Count;

    public bool TryPeek(out T item)
    {
        if (_pending.Count > 0) { item = _pending.Peek(); return true; }
        if (_i >= _items.Count) { item = default!; return false; }
        item = _items[_i];
        return true;
    }

    public void MoveNext()
    {
        if (_pending.Count > 0) { _pending.Pop(); return; }
        _i++;
    }

    public void Putback(T continuation)
    {
        _pending.Push(continuation);
    }
}

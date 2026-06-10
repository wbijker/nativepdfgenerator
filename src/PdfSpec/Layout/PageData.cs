namespace PdfSpec.Layout;

/// <summary>
/// Page-level state handed to a deferred component's render callback
/// once the document's page count is final. Both fields are 1-based
/// — <see cref="PageNumber"/> is the page the deferred content will
/// render on, <see cref="TotalPages"/> is the count of all pages in
/// the document at save time. The canonical use is page-number /
/// header / footer content that can't be known until the document
/// is fully laid out.
/// </summary>
public sealed record PageData(int PageNumber, int TotalPages);

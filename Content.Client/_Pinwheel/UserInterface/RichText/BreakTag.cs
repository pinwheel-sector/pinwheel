using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

public sealed class BreakTag : IMarkupTagHandler
{
    public string Name => "break";

    /// <inheritdoc/>
    public string TextBefore(MarkupNode _) => "\n";
}

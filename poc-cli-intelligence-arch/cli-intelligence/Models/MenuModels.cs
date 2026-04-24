#region Using

namespace cli_intelligence.Models;

#endregion

/// <summary>
/// Represents a root menu category that groups related actions.
/// </summary>
/// <param name="Title">Category title shown in the root menu.</param>
/// <param name="ColorStyle">Spectre color style name for the category header.</param>
/// <param name="Description">Short category description.</param>
/// <param name="Items">Menu items belonging to this category.</param>
sealed record MenuCategory(
    string Title,
    string ColorStyle,
    string Description,
    IReadOnlyList<MenuItem> Items);

/// <summary>
/// Represents an actionable item in a menu category.
/// </summary>
/// <param name="Number">Visible shortcut number or key.</param>
/// <param name="Label">Short user-facing label.</param>
/// <param name="Description">Contextual description for the action.</param>
/// <param name="StatusBadge">Optional status badge rendered beside the label.</param>
/// <param name="Action">Action invoked when the item is selected.</param>
sealed record MenuItem(
    string Number,
    string Label,
    string Description,
    string? StatusBadge,
    Func<Screens.AppNavigator, Task> Action);

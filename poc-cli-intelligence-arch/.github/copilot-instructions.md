# Copilot Instructions

## General Guidelines
- Proactively perform cleanup and non-breaking improvements when opportunities are found.
- Add comments, clarify them or refactor single methods or classes is is worth and not out of scope.

## Project Guidelines
- Move configuration mapping classes out of Program.cs into a Models folder; keep Program.cs minimal and unpolluted.
- Implement CLI-Intelligence as an interactive-first Spectre.Console app with hierarchical menus/screens.
  - Clear the console fully before rendering each screen.
  - Render the standard banner, then a blank line, then the screen content.
  - Make ESC always navigate back to the previous menu/screen.
  - Treat switch-based flags as secondary; use them only for automation/non-interactive runs.

## Code Documentation
- Add XML comments to classes and public methods for clarity.
  - Include <summary> for all classes and public methods.
  - Add <param> for method parameters and <returns> for return values where applicable.
- Ensure comments are concise, informative, and follow standard C# documentation conventions.
- Wrap the using statement in a #region for better organization and readability.
    - Same for the fields and properties of the class.

# YAi.Client.CLI.Components Docs

| Field | Value |
| --- | --- |
| Purpose | Local documentation index for YAi CLI screen hosts, Razor screens, and shared CLI presentation state |
| Audience | Project maintainers and contributors |
| Status | Active |
| Last reviewed | 2026-04-27 |

## What belongs here

Use this folder for component-local notes about:

- Razor screen hosts under `Screens/`
- reusable presentation components under `Components/`
- reusable response view state and response screens under `ResponseViewState.cs` and `Screens/ResponseScreen*`
- reusable conversation transcript state and conversation prompt screens under `ConversationTranscriptEntryViewState.cs` and `Screens/ConversationPromptScreen*`
- reusable prompt editors and history-navigation helpers under `Input/`
- markup renderers under `Rendering/`
- `AppHeaderState.cs`
- `StatusBarState.cs`

## Shared docs this project depends on

- [Shared docs index](../../../docs/README.md)
- [Documentation governance](../../../docs/DOCUMENTATION-GOVERNANCE.md)
- [Filesystem skill pack](../../../docs/specs/active/packs/filesystem-skill/README.md)

## Local files

- [Local changelog](local-changelog.md)

## Current code anchors

- `Screens/`
- `Components/`
- `ActionButton.razor`
- `ConversationTranscriptEntryViewState.cs`
- `ResponseViewState.cs`
- `Input/`
- `Rendering/`
- `AppHeaderState.cs`
- `StatusBarState.cs`

The shared action-button wrapper now lives in `Components/ActionButton.razor` and centralizes the `TextButton` styling used by dialogs, cards, and screens with green primary, amber danger, and light-gray neutral variants.

Approval-card behavior, workflow UX, and screen-local implementation notes belong here when they only describe the CLI presentation layer.
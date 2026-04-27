# YAi.Client.CLI.Components Docs

| Field | Value |
| --- | --- |
| Purpose | Local documentation index for YAi CLI Terminal.Gui windows and shared CLI presentation state |
| Audience | Project maintainers and contributors |
| Status | Active |
| Last reviewed | 2026-04-27 |

## What belongs here

Use this folder for component-local notes about:

- screen hosts and Terminal.Gui windows under `Screens/`
- reusable presentation components under `Components/`
- reusable response view state and response screens under `ResponseViewState.cs` and `Screens/ResponseScreen*`
- reusable conversation transcript state and conversation prompt windows under `ConversationTranscriptEntryViewState.cs` and `Screens/ConversationPromptWindow.cs`
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
- `Screens/ConversationPromptWindow.cs`
- `Screens/Tools/Filesystem/ApprovalCardWindow.cs`
- `ConversationTranscriptEntryViewState.cs`
- `ResponseViewState.cs`
- `Input/`
- `Rendering/`
- `AppHeaderState.cs`
- `StatusBarState.cs`

Approval-card behavior, workflow UX, and screen-local implementation notes belong here when they only describe the CLI presentation layer.
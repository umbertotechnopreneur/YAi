# Contributing to YAi!

Thank you for taking the time to improve this project.

YAi is intended to stay local-first, explicit, safe by default, and multiplatform across Windows, macOS, and Linux. Contributions are welcome, but they should support those goals rather than weaken them. When you add, rename, or remove configuration files, memory files, skill files, workspace files, or the SQLite storage path, keep the path inventory used by `--show-paths` and `--gonuclear` in sync with the code and docs.

> YAi!
>
> Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
>
> Website: https://umbertogiacobbi.biz
> Email: hello@umbertogiacobbi.biz
>
> This file is part of YAi!.
>
> YAi! is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
>
> YAi! is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
>
> You should have received a copy of the GNU Affero General Public License along with YAi!. If not, see <https://www.gnu.org/licenses/>.

## Before You Start

Please read the repository documentation first, especially:

- [README.md](README.md)
- any project-specific docs under `docs/`
- any workspace or agent instructions under `workspace/`
- the existing code style and naming patterns in the repository

For large changes, open an issue or discussion first so the approach can be aligned before implementation starts.

## What We Welcome

Useful contributions usually include:

- bug fixes
- reliability improvements
- safer file or tool behavior
- documentation improvements
- test coverage improvements
- performance improvements with clear tradeoffs
- new skills, workflows, or integrations that fit the project direction
- code cleanup that improves clarity without changing behavior

## What We Usually Avoid

The following changes are less likely to be accepted unless strongly justified:

- broad refactors without a clear benefit
- rewriting stable code only for style reasons
- heavy dependencies without a strong need
- hidden destructive behavior
- features that increase risk without clear user value
- placeholder documentation that is not ready for public use
- changes that expose secrets, tokens, paths, or internal data

## Working Standards

1. Keep changes small and focused.
2. Prefer explicit behavior over hidden magic.
3. Prefer safe and reversible operations.
4. Do not modify unrelated files.
5. Do not rewrite an entire file when a targeted patch is enough.
6. Do not add hidden side effects.
7. Preserve user data unless a change explicitly requires otherwise.
8. Never hardcode credentials, tokens, API keys, secrets, or private endpoints.
9. Write code and docs that another engineer can understand quickly.
10. Assume the project will be maintained over time, not just demoed.

## Code and Docs Conventions

- Use the existing .NET and C# style already present in the repository.
- Keep one main type per file where practical.
- Match file names to the main type name.
- Prefer the narrowest reasonable visibility.
- Use `Async` suffixes for asynchronous methods.
- Avoid `.Result` and `.Wait()` unless there is a very strong reason.
- Catch exceptions only when you can add context, recover, or present a better failure mode.
- Never log secrets, tokens, passwords, private keys, or sensitive user content.
- Update README files or other docs when public behavior changes.

## Source File Header Standard

This repository uses a short, consistent source file header across supported languages.

Use it for new source files and when you materially edit an existing file that does not already follow the standard.

### Purpose

The header should:

- identify ownership clearly
- include the YAi! copyright and license notice
- keep contact information minimal
- leave two trailing comment lines for the file-specific name and short purpose
- remain short enough for everyday source files

### Standard Header Fields

- Project: `YAi!`
- Copyright: `Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.`
- Website: `https://umbertogiacobbi.biz`
- Email: `hello@umbertogiacobbi.biz`
- License notice: `This file is part of YAi!.` followed by the AGPLv3 notice and warranty disclaimer.
- Trailing context: `[Project or Module Name]` and `[Short file purpose or description]`

### C# Header

```csharp
/*
 * YAi!
 *
 * Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file is part of YAi!.
 *
 * YAi! is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Affero General Public License version 3 as published by the Free
 * Software Foundation.
 *
 * YAi! is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License along
 * with YAi!. If not, see <https://www.gnu.org/licenses/>.
 *
 * [Project or Module Name]
 * [Short file purpose or description]
 */
```

### JavaScript Header

```javascript
/*
 * YAi!
 *
 * Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file is part of YAi!.
 *
 * YAi! is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Affero General Public License version 3 as published by the Free
 * Software Foundation.
 *
 * YAi! is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License along
 * with YAi!. If not, see <https://www.gnu.org/licenses/>.
 *
 * [Project or Module Name]
 * [Short file purpose or description]
 */
```

### HTML / CSS Header

```html
<!--
	YAi!

	Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
	Website: https://umbertogiacobbi.biz
	Email: hello@umbertogiacobbi.biz

	This file is part of YAi!.

	YAi! is free software: you can redistribute it and/or modify it under the terms
	of the GNU Affero General Public License version 3 as published by the Free
	Software Foundation.

	YAi! is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
	without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
	PURPOSE. See the GNU Affero General Public License for more details.

	You should have received a copy of the GNU Affero General Public License along
	with YAi!. If not, see <https://www.gnu.org/licenses/>.

	[Project or Module Name]
	[Short file purpose or description]
-->
```

### Notes

- Keep the first line as the project, module, or product name.
- Keep the second line as a short description of the file.
- Do not expand the header with internal metadata unless explicitly required.
- Use the same wording across repositories unless a project needs a stricter legal notice.

## Safety Expectations

If your change touches tools, scripts, file operations, shell commands, or automation:

- prefer preview or dry-run behavior where appropriate
- avoid destructive defaults
- make side effects visible
- ensure failures are understandable
- preserve user trust

If a feature can delete, overwrite, move, reset, or mutate important data, it must be explicit and deliberate.

## Tests and Validation

Where practical, include tests for:

- parsing behavior
- extraction behavior
- tool-call behavior
- file mutation safety
- configuration loading
- regression cases for previous bugs

If you do not add a test, explain why in the pull request.

Validate the change before opening a pull request. At minimum, run the relevant build or smoke check for the area you changed.

## Pull Request Guidance

A good pull request should be easy to review.

- Keep the description concise.
- Explain the problem being solved.
- Explain the chosen approach.
- Call out any tradeoffs.
- Mention any user-visible behavior changes.
- Include screenshots or terminal output only if they help explain the change.
- Keep unrelated cleanup out of the same pull request.

## Security

If you discover a security issue, do not open a public issue with exploit details.
Report it privately through the maintainer contact path once that is defined for the repository.

## Code of Conduct

Be respectful, direct, and constructive.

This project values clarity, professionalism, and useful contributions over volume.

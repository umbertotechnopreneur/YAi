# Importing Skills from ZIP Files

This guide explains how to use the new skill import feature in CLI-Intelligence to import PowerShell skills packaged as ZIP files.

---

## Quick Start

### Interactive Method (Easiest)

1. Run the app: `dotnet run`
2. Go to **Settings / Tools ⚙️**
3. Select **Import skill from ZIP 📦**
4. Choose your ZIP file using the file browser
5. Select import location (workspace or bundled)
6. Restart the app to use the skill

### Command Line Method

```powershell
# Opens file browser
dotnet run -- --import-skill

# Direct path to ZIP file
dotnet run -- --import-skill "C:\skills\concat-files-skill.zip"

# Import to workspace (personal/local)
dotnet run -- --import-skill "skills\my-skill.zip" --workspace
```

---

## ZIP File Structure Requirements

Your skill ZIP must contain this structure:

```
concat-files-skill.zip
└── concat-files-skill/              ← Top-level folder (can be any name)
    ├── SKILL.md                     ← REQUIRED
    ├── scripts/                     ← REQUIRED
    │   ├── run.ps1                  ← PowerShell script(s)
    │   ├── action1.ps1
    │   └── action2.ps1
    ├── README.md                    ← Optional
    └── other-files                  ← Optional
```

### Required Files

#### 1. SKILL.md

The skill definition file with YAML frontmatter:

```yaml
---
name: concat-files
description: Concatenate matching source files into grouped UTF-8 text chunks
version: 1.0.0
metadata:
  openclaw:
    os: [win32, darwin, linux]
    requires:
      bins: [pwsh]
    emoji: 📝
---

# Concat Files Skill

Combines multiple files into organized text output.

## Actions

- **run**: Concatenate matching files with grouping

## Usage

```
[TOOL: concat-files action=run ...]
```

## Parameters

[Document your parameters here]
```

**Required fields:**
- `name` — skill identifier (lowercase, no spaces)
- `description` — one-line description
- `metadata.openclaw.os` — supported OS list

#### 2. scripts/ Directory

Must contain at least one `.ps1` PowerShell script. For each action in your SKILL.md:

- `action_name` → `scripts/action_name.ps1`
- Multiple actions = multiple scripts

Example:
```
scripts/
├── run.ps1          # For [TOOL: concat-files action=run]
├── validate.ps1     # For [TOOL: concat-files action=validate]
└── preview.ps1      # For [TOOL: concat-files action=preview]
```

---

## File Browser Navigation

The interactive file browser lets you navigate your system:

```
File Browser
Current: C:\Users\YourName\Documents

 ↓ Navigate folders
 📁 [Parent Directory] ..
 📁 Downloads
 📁 Desktop
 📁 skills
 📄 my-skill.zip (1.2 MB)
 📄 other-skill.zip (845 KB)

 → Select: my-skill.zip
```

**Controls:**
- Arrow keys to navigate
- Enter to select file or open folder
- Enter custom path by typing the full path
- ESC to cancel (Back option)

---

## Example: Importing concat-files-skill

Given this ZIP structure:

```
concat-files-skill.zip
└── concat-files-skill/
    ├── SKILL.md
    ├── scripts/
    │   └── run.ps1
    └── README.md
```

**Step 1: Start Import**
```powershell
dotnet run -- --import-skill
```

**Step 2: Select File**
- Browser opens to Documents/skills/
- Select `concat-files-skill.zip`

**Step 3: Choose Location**
```
Import to location

  Workspace (data/skills/) - personal/local
  Bundled (storage/skills/) - shared/committed
> Cancel
```

Select "Bundled" for a shared skill (commits to git).
Select "Workspace" for personal skills (local only).

**Step 4: Confirmation**
```
Skill Importer 📦
Importing Skill...

✓ Skill 'concat-files' (v1.0.0) imported successfully to bundled (storage/skills/)

The skill will be available the next time you start the application.

Press any key...
```

**Step 5: Restart**
```powershell
dotnet run
```

The skill is now loaded and available to use!

---

## Import Locations

### Workspace Skills (`data/skills/`)

✅ **Use for:**
- Personal/custom skills
- Local-only extensions
- Skills you don't want to commit

❌ **Not committed** to version control

**When to use:** Testing, personal experiments, machine-specific tools

### Bundled Skills (`storage/skills/`)

✅ **Use for:**
- Shared team skills
- Production-ready extensions
- Skills for the open-source community

✅ **Committed** to version control

**When to use:** Official skills, widely-used tools, public release

---

## Validation During Import

The importer automatically validates:

✅ ZIP file exists and is valid
✅ Contains a top-level directory
✅ `SKILL.md` file present
✅ YAML frontmatter valid (between `---` markers)
✅ Required YAML fields: `name`, `description`
✅ `scripts/` directory exists
✅ At least one `.ps1` file in scripts/
✅ Skill name doesn't already exist

**Error Examples:**

```
✗ ZIP file not found: C:\missing\file.zip
✗ ZIP file is empty or has no folders
✗ SKILL.md not found in skill directory
✗ scripts/ directory not found
✗ No PowerShell scripts (.ps1) found
✗ Skill 'concat-files' already exists at C:\...\concat-files
```

---

## PowerShell Script Requirements

Each script in `scripts/` must:

1. **Have param() declarations:**
```powershell
param(
    [Parameter(Mandatory = $true)]
    [string]$InputFolder,
    
    [Parameter(Mandatory = $false)]
    [int]$MaxSize = 1000
)
```

2. **Output results with Write-Output:**
```powershell
Write-Output "Processing complete"
Write-Output $result
```

3. **Signal errors with Write-Error and exit 1:**
```powershell
Write-Error "Something failed"
exit 1
```

---

## Testing Your Import

After importing a skill, verify it works:

### 1. Check Help

```powershell
dotnet run -- --help
```

Look for your skill listed under "Local Language Model" section.

### 2. Interactive Mode

```powershell
dotnet run
# Choose your skill from the Tools menu
```

### 3. Chat Test

```powershell
dotnet run -- --talk
# Ask: "Can you use the concat-files skill?"
```

### 4. Direct Invocation

```powershell
dotnet run -- --query "Use concat-files to combine these files..."
```

---

## Troubleshooting

### "ZIP file not found"

**Cause:** Path is incorrect or file doesn't exist

**Solution:**
- Verify the file exists: `Test-Path "C:\path\to\file.zip"`
- Use the file browser instead of typing the path
- Check file extension is exactly `.zip`

### "Skill already exists"

**Cause:** A skill with that name is already imported

**Solution:**
- Import to a different location (workspace vs bundled)
- Delete the existing skill first:
  ```powershell
  Remove-Item storage/skills/concat-files -Recurse
  ```
- Rename the folder inside the ZIP before importing

### "SKILL.md not found"

**Cause:** ZIP structure is wrong

**Solution:**
- ZIP must contain a top-level folder
- That folder must contain `SKILL.md` directly
- Check: `SKILL.md` not in a subfolder

**Correct:**
```
concat-files-skill.zip
└── concat-files-skill/
    └── SKILL.md        ✓ Correct
```

**Incorrect:**
```
concat-files-skill.zip
└── SKILL.md            ✗ Should be in a folder

concat-files-skill.zip
└── concat-files-skill/
    └── docs/
        └── SKILL.md    ✗ Should be at root
```

### "scripts/ directory not found"

**Cause:** Missing `scripts/` folder

**Solution:**
- ZIP must contain `scripts/` folder at the same level as `SKILL.md`
- Add `.ps1` files to the scripts folder

### "No PowerShell scripts found"

**Cause:** No `.ps1` files in `scripts/`

**Solution:**
- Ensure scripts have `.ps1` extension (not `.txt` renamed)
- Scripts must be directly in `scripts/`, not in subfolders
- At least one script required (typically named `run.ps1` or matching the action name)

### "Skill not appearing after import"

**Cause:** App needs to be restarted

**Solution:**
- Restart the application
- Skills are loaded at startup
- Check logs: `dotnet run 2>&1 | Select-String -Pattern "skill|import"`

---

## File Browser Tips

### Navigating to Downloads

1. Select "📁 [Parent Directory] .." to go up
2. Navigate to Downloads
3. Find your skill ZIP file

### Custom Path Entry

If you know the exact path:
1. Select "📍 Enter custom path"
2. Type the full path: `C:\Users\YourName\Downloads\my-skill.zip`
3. Press Enter

### Filtering

The file browser automatically:
- Shows only `.zip` files when importing skills
- Displays file sizes in human-readable format
- Handles large folders efficiently

---

## Creating Skills for Distribution

To make your skill easy for others to import:

1. **Create the structure:**
   ```
   my-skill/
   ├── SKILL.md
   ├── scripts/
   │   └── run.ps1
   └── README.md
   ```

2. **Zip it:**
   ```powershell
   Compress-Archive my-skill -DestinationPath my-skill.zip
   ```

3. **Share the ZIP file:**
   - GitHub releases
   - Email
   - Private repositories
   - Cloud storage

Others can import with:
```powershell
dotnet run -- --import-skill "path/to/my-skill.zip"
```

---

## Summary

| Task | Method |
|------|--------|
| Import with browser | Run app → Settings → Import skill |
| Import from command line | `dotnet run -- --import-skill` |
| Import direct path | `dotnet run -- --import-skill "file.zip"` |
| Import to workspace | `--import-skill "file.zip" --workspace` |
| Navigate browser | Arrows + Enter to navigate/select |
| Enter custom path | Select "Enter custom path" option |
| Verify import | Restart app and check Help |

The importer handles validation, extraction, and file placement automatically. Just provide a valid ZIP file and select where to import it!

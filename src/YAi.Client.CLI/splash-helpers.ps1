<#
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

YAi!
Shared splash console helpers for centered ANSI art
#>

Remove-Item -Path Alias:printf -ErrorAction SilentlyContinue

function Initialize-SplashConsole {
    [Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
    [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $OutputEncoding = [System.Text.UTF8Encoding]::new($false)

    [Console]::Clear()
    [Console]::Write("`e[3J")
}

function Write-CenteredConsoleBlock {
    param(
        [Parameter (ValueFromRemainingArguments = $true)]
        [string[]]$ArgumentList
    )

    $text = if ($null -ne $ArgumentList) { $ArgumentList -join ' ' } else { '' }
    $text = $text.Replace('\e', [string][char]27)
    $lines = $text -split "`r?`n"

    $visibleWidth = 0
    foreach ($line in $lines) {
        $plainLine = [regex]::Replace($line, "`e\[[0-9;]*m", '')
        if ($plainLine.Length -gt $visibleWidth) {
            $visibleWidth = $plainLine.Length
        }
    }

    $windowWidth = 0
    $bufferWidth = 0
    $windowHeight = 0
    $bufferHeight = 0

    try { $windowWidth = [Console]::WindowWidth } catch { }
    try { $bufferWidth = [Console]::BufferWidth } catch { }
    try { $windowHeight = [Console]::WindowHeight } catch { }
    try { $bufferHeight = [Console]::BufferHeight } catch { }

    $viewportWidth = $windowWidth
    if ($viewportWidth -le 0 -or ($bufferWidth -gt 0 -and $bufferWidth -lt $viewportWidth)) {
        $viewportWidth = $bufferWidth
    }
    if ($viewportWidth -le 0) {
        $viewportWidth = $visibleWidth
    }

    $viewportHeight = $windowHeight
    if ($viewportHeight -le 0 -or ($bufferHeight -gt 0 -and $bufferHeight -lt $viewportHeight)) {
        $viewportHeight = $bufferHeight
    }
    if ($viewportHeight -le 0) {
        $viewportHeight = $lines.Count
    }

    $leftPadding = [Math]::Max(0, [int][Math]::Floor(($viewportWidth - $visibleWidth) / 2))
    $topPadding = [Math]::Max(0, [int][Math]::Floor(($viewportHeight - $lines.Count) / 2))

    for ($index = 0; $index -lt $topPadding; $index++) {
        [Console]::WriteLine()
    }

    $padding = if ($leftPadding -gt 0) { ' ' * $leftPadding } else { '' }

    foreach ($line in $lines) {
        [Console]::WriteLine($padding + $line)
    }
}
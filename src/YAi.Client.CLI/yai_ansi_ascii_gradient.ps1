param(
    [int]$Width = 240,
    [int]$Height = 90,
    [switch]$NoClear
)

$esc = [char]27

function Color-Char([int]$r, [int]$g, [int]$b, [string]$ch) {
    return "$esc[38;2;$r;$g;$b" + "m$ch"
}

$glyphs = @{
    "Y" = @(
        "███   ███",
        " ███ ███ ",
        "  █████  ",
        "   ███   ",
        "   ███   ",
        "   ███   ",
        "   ███   "
    )
    "A" = @(
        "   ███   ",
        "  █████  ",
        " ███ ███ ",
        "███   ███",
        "█████████",
        "███   ███",
        "███   ███"
    )
    "i" = @(
        "█████",
        "     ",
        "█████",
        "  ██ ",
        "  ██ ",
        "  ██ ",
        "█████"
    )
    "!" = @(
        "███",
        "███",
        "███",
        "███",
        "███",
        "   ",
        "███"
    )
}

function New-Mask([string]$Text, [int]$ScaleX, [int]$ScaleY, [int]$Gap) {
    $rows = 7 * $ScaleY
    $width = 0
    foreach ($ch in $Text.ToCharArray()) {
        $width += $glyphs[[string]$ch][0].Length * $ScaleX
    }
    $width += $Gap * ($Text.Length - 1)

    $mask = New-Object 'bool[,]' $rows, $width
    $x = 0

    foreach ($ch in $Text.ToCharArray()) {
        $g = $glyphs[[string]$ch]
        for ($gy = 0; $gy -lt $g.Count; $gy++) {
            for ($gx = 0; $gx -lt $g[$gy].Length; $gx++) {
                if ($g[$gy][$gx] -ne ' ') {
                    for ($yy = $gy * $ScaleY; $yy -lt ($gy + 1) * $ScaleY; $yy++) {
                        for ($xx = $gx * $ScaleX; $xx -lt ($gx + 1) * $ScaleX; $xx++) {
                            $mask[$yy, $x + $xx] = $true
                        }
                    }
                }
            }
        }
        $x += $g[0].Length * $ScaleX + $Gap
    }

    return @{
        Mask = $mask
        Rows = $rows
        Width = $width
    }
}

$density = " .,:;irsXA253hMHGS#9B&@"
$scaleX = [Math]::Max(2, [int]($Width / 55))
$scaleY = [Math]::Max(2, [int]($Height / 22))
$gap = [Math]::Max(4, [int]($Width / 28))

$m = New-Mask "YAi!" $scaleX $scaleY $gap
$mask = $m.Mask
$mw = $m.Width
$mh = $m.Rows
$offx = [Math]::Max(0, [int](($Width - $mw) / 2))
$offy = [Math]::Max(2, [int]($Height * 0.22))

if (-not $NoClear) {
    Write-Host "$esc[?25l$esc[2J$esc[H" -NoNewline
}

for ($y = 0; $y -lt $Height; $y++) {
    $line = New-Object System.Text.StringBuilder

    for ($x = 0; $x -lt $Width; $x++) {
        $inLogo = $false
        if ($y -ge $offy -and $y -lt ($offy + $mh) -and $x -ge $offx -and $x -lt ($offx + $mw)) {
            $inLogo = $mask[$y - $offy, $x - $offx]
        }

        $slogan = "PLAN  >  VERIFY  >  APPROVE  >  EXECUTE  >  AUDIT"
        $sy = $offy + $mh + [Math]::Max(4, [int]($Height / 14))
        $sx = [Math]::Max(0, [int](($Width - $slogan.Length) / 2))
        $inSlogan = ($y -eq $sy -and $x -ge $sx -and $x -lt ($sx + $slogan.Length))

        if ($inLogo) {
            $t = ($x - $offx) / [Math]::Max(1, ($mw - 1))
            $r = [int](30 + 225 * [Math]::Max(0, [Math]::Min(1, $t * 1.6 - 0.15)))
            $g = [int](255 * [Math]::Pow([Math]::Sin([Math]::PI * [Math]::Min(1, $t)), 0.7))
            $b = [int](255 * [Math]::Max(0, 1 - $t * 1.15))
            if ($t -gt 0.70) {
                $b = [int](120 + 135 * ($t - 0.7) / 0.3)
                $r = 255
                $g = [int](130 * (1 - ($t - 0.7) / 0.3))
            }

            $v = ([Math]::Sin($x * 0.12) + [Math]::Cos($y * 0.18) + 2) / 4
            $idx = [Math]::Max(0, [Math]::Min($density.Length - 1, [int]($v * ($density.Length - 1))))
            [void]$line.Append((Color-Char $r $g $b ([string]$density[$idx])))
        }
        elseif ($inSlogan) {
            $ch = [string]$slogan[$x - $sx]
            $t = ($x - $sx) / [Math]::Max(1, ($slogan.Length - 1))
            $r = [int](40 + 215 * $t)
            $g = [int](230 * [Math]::Pow([Math]::Sin([Math]::PI * $t), 0.8) + 20)
            $b = [int](255 * (1 - $t) + 180 * $t)
            [void]$line.Append((Color-Char $r $g $b $ch))
        }
        else {
            $val = [Math]::Sin($x * 0.097 + $y * 0.041) + [Math]::Sin($x * 0.023 - $y * 0.17)
            if ($val -gt 1.90) {
                [void]$line.Append((Color-Char 28 80 130 "."))
            }
            else {
                [void]$line.Append(" ")
            }
        }
    }

    Write-Host ($line.ToString() + "$esc[0m")
}

Write-Host "$esc[0m$esc[?25h" -NoNewline

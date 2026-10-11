# SPDX-License-Identifier: MIT
# Generates simple first-party PNG tiles for the acceptance MSIX; never reuses Flint branding.
[CmdletBinding()]
param([string]$Output = (Join-Path $PSScriptRoot '..\src\ExamLex\Assets'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force -Path $Output | Out-Null
$tiles = [ordered]@{
    'StoreLogo.png' = @(50,50)
    'Square44x44Logo.png' = @(44,44)
    'Square71x71Logo.png' = @(71,71)
    'Square150x150Logo.png' = @(150,150)
    'Square310x310Logo.png' = @(310,310)
    'Wide310x150Logo.png' = @(310,150)
    'SplashScreen.png' = @(620,300)
}
foreach ($tile in $tiles.GetEnumerator()) {
    $w = [int]$tile.Value[0]; $h = [int]$tile.Value[1]
    $bmp = [System.Drawing.Bitmap]::new($w,$h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        $g.Clear([System.Drawing.Color]::FromArgb(25,34,34))
        $diameter = [Math]::Min($w,$h)
        $dotBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(79,189,156))
        $wordBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(244,249,246))
        $size = [Math]::Max(10, [Math]::Floor($diameter*0.35))
        $font = [System.Drawing.Font]::new('Segoe UI', [single]$size,[System.Drawing.FontStyle]::Bold,[System.Drawing.GraphicsUnit]::Pixel)
        try {
            $text='EL'
            $rect = [System.Drawing.RectangleF]::new(0,0,$w,$h)
            $fmt = [System.Drawing.StringFormat]::new()
            $fmt.Alignment = [System.Drawing.StringAlignment]::Center
            $fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
            $g.FillEllipse($dotBrush,[single]($w/2-$diameter*0.36),[single]($h/2+$diameter*0.30),[single]($diameter*0.72),[single]($diameter*0.07))
            $g.DrawString($text,$font,$wordBrush,$rect,$fmt)
            $fmt.Dispose()
        }
        finally {$font.Dispose();$dotBrush.Dispose();$wordBrush.Dispose()}
        $path = Join-Path $Output $tile.Key
        $bmp.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host "Generated original ExamLex asset: $($tile.Key) ($w x $h)"
    }
    finally {$g.Dispose();$bmp.Dispose()}
}

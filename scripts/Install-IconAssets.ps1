# SPDX-License-Identifier: MIT
# Install the icon master files provided by ExamLex's owner. No placeholder artwork.
[CmdletBinding()]
param(
    [string]$Archive = (Join-Path $PSScriptRoot '..\ExamLex_Complete_Icon_Assets_v3.zip'),
    [string]$Output = (Join-Path $PSScriptRoot '..\src\ExamLex\Assets')
)
$ErrorActionPreference='Stop'
$Archive=[IO.Path]::GetFullPath($Archive)
$Output=[IO.Path]::GetFullPath($Output)
if(-not (Test-Path -LiteralPath $Archive)) { throw "ExamLex icon archive not found: $Archive" }
$unpacked=Join-Path $env:RUNNER_TEMP 'ExamLex_Icon_Assets_v3'
if(Test-Path $unpacked) { Remove-Item -LiteralPath $unpacked -Force -Recurse }
Expand-Archive -LiteralPath $Archive -DestinationPath $unpacked -Force
$all=Get-ChildItem $unpacked -Recurse -File
$ico=@($all | Where-Object Extension -EQ '.ico')
$images=@($all | Where-Object Extension -In @('.png','.jpg','.jpeg'))
Write-Host "User icon archive: $($all.Count) files; ICO: $($ico.Count); raster: $($images.Count)."
$all | ForEach-Object { Write-Host "EXAMLEX_ASSET: $($_.FullName.Substring($unpacked.Length+1)) [$($_.Length) bytes]" }
if($ico.Count -lt 1) { throw 'The v3 archive contains no .ico icon. Refusing old placeholder icons.' }
if($images.Count -lt 1) { throw 'The v3 archive contains no raster image. Refusing old placeholder tiles.' }
New-Item -ItemType Directory -Force -Path $Output | Out-Null
$preferredIco=$ico | Sort-Object @{Expression={
    if($_.BaseName -match 'ExamLex') { 0 } elseif($_.BaseName -match 'Icon') { 1 } else { 2 }
}},@{Expression='Length';Descending=$true} | Select-Object -First 1
Copy-Item -LiteralPath $preferredIco.FullName -Destination (Join-Path $Output 'ExamLex.ico') -Force
Write-Host "APP ICON: $($preferredIco.FullName.Substring($unpacked.Length+1))"

Add-Type -AssemblyName System.Drawing
$meta=@()
foreach($p in $images) {
  try {
    $im=[Drawing.Image]::FromFile($p.FullName)
    try {
      $meta+= [pscustomobject]@{
        File=$p; Width=$im.Width; Height=$im.Height;
        Area=([double]$im.Width*$im.Height);
        IsSquare=([Math]::Abs($im.Width-$im.Height) -le 4)
      }
    } finally {$im.Dispose()}
  } catch { Write-Warning "Skipping unreadable image: $($p.Name)" }
}
if($meta.Count -eq 0) { throw 'No usable images found in icon asset archive.' }
$tiles=[ordered]@{
  'StoreLogo.png'=@(50,50)
  'Square44x44Logo.png'=@(44,44)
  'Square71x71Logo.png'=@(71,71)
  'Square150x150Logo.png'=@(150,150)
  'Square310x310Logo.png'=@(310,310)
  'Wide310x150Logo.png'=@(310,150)
  'SplashScreen.png'=@(620,300)
}
foreach($entry in $tiles.GetEnumerator()) {
  $name=[string]$entry.Key
  $w=[int]$entry.Value[0]; $h=[int]$entry.Value[1]
  $stem=[IO.Path]::GetFileNameWithoutExtension($name)
  # Prefer exact original MSIX asset names when included; otherwise use the
  # owner's largest square icon master, fitting proportionally without distortion.
  $exact=@($meta | Where-Object { $_.File.BaseName -ieq $stem })
  if($exact.Count) {
    $src=$exact | Sort-Object @{Expression={ [Math]::Abs($_.Width-$w)+[Math]::Abs($_.Height-$h) }} | Select-Object -First 1
  } else {
    $masters=@($meta | Where-Object IsSquare -EQ $true)
    if(-not $masters.Count) { throw "No square logo master for $name" }
    $src=$masters | Sort-Object @{Expression={
      if($_.File.Name -match '(?i)examlex|icon|master|logo') { 0 } else { 1 }
    }}, @{Expression='Area';Descending=$true} | Select-Object -First 1
  }
  $image=[Drawing.Image]::FromFile($src.File.FullName)
  $bmp=[Drawing.Bitmap]::new($w,$h,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g=[Drawing.Graphics]::FromImage($bmp)
  try {
    $g.Clear([Drawing.Color]::Transparent)
    $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality=[Drawing.Drawing2D.CompositingQuality]::HighQuality
    $scale=[Math]::Min([double]$w/$image.Width,[double]$h/$image.Height)
    if($w -gt $h) {
      # Retain a compact centered square logo for wide banners.
      $scale=[Math]::Min($scale,[double]$h/$image.Height)
    }
    $dw=[Math]::Max(1,[int][Math]::Round($image.Width*$scale))
    $dh=[Math]::Max(1,[int][Math]::Round($image.Height*$scale))
    $g.DrawImage($image,[Drawing.Rectangle]::new([int](($w-$dw)/2),[int](($h-$dh)/2),$dw,$dh))
    $bmp.Save((Join-Path $Output $name),[Drawing.Imaging.ImageFormat]::Png)
    Write-Host "INSTALLED $name ($w x $h) <= $($src.File.FullName.Substring($unpacked.Length+1)) [$($src.Width)x$($src.Height)]"
  } finally { $g.Dispose();$bmp.Dispose();$image.Dispose() }
}
$logo=Join-Path $Output 'Square150x150Logo.png'
if(-not (Test-Path $logo) -or -not (Test-Path (Join-Path $Output 'ExamLex.ico'))) { throw 'Incomplete v3 icon extraction.' }

# MSIX uses targetsize-N_altform-unplated assets for taskbar/start icons.
# Without these, Windows plates a transparent logo over a colored square.
# Always rasterize from the largest high-resolution owner-provided master;
# never upscale the 44px MSIX tile.
$masters=@($meta | Where-Object IsSquare -EQ $true)
$master=$masters | Sort-Object @{Expression='Area';Descending=$true} | Select-Object -First 1
if(-not $master) { throw 'Missing square transparent icon master.' }
Write-Host "HI-DPI MASTER: $($master.File.FullName.Substring($unpacked.Length+1))"
function Write-IconFromMaster([string]$name,[int]$width,[int]$height) {
    $img=[Drawing.Image]::FromFile($master.File.FullName)
    $bitmap=[Drawing.Bitmap]::new($width,$height,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $gr=[Drawing.Graphics]::FromImage($bitmap)
    try {
        $gr.Clear([Drawing.Color]::Transparent)
        $gr.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::HighQuality
        $gr.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $gr.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $scale=[Math]::Min([double]$width/$img.Width,[double]$height/$img.Height)
        $ww=[Math]::Max(1,[int][Math]::Round($img.Width*$scale))
        $hh=[Math]::Max(1,[int][Math]::Round($img.Height*$scale))
        $gr.DrawImage($img,[Drawing.Rectangle]::new(
           [int](($width-$ww)/2),[int](($height-$hh)/2),$ww,$hh))
        $bitmap.Save((Join-Path $Output $name),[Drawing.Imaging.ImageFormat]::Png)
    } finally { $gr.Dispose();$bitmap.Dispose();$img.Dispose() }
}
foreach($pixels in @(16,24,32,44,48,64,96,256)) {
    Write-IconFromMaster "Square44x44Logo.targetsize-$pixels.png" $pixels $pixels
    Write-IconFromMaster "Square44x44Logo.targetsize-${pixels}_altform-unplated.png" $pixels $pixels
}
foreach($scale in @(100,125,150,200,400)) {
    $px=[int][Math]::Ceiling(150*$scale/100)
    Write-IconFromMaster "Square150x150Logo.scale-$scale.png" $px $px
}

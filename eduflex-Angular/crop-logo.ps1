Add-Type -AssemblyName System.Drawing

$srcPath = Join-Path $PSScriptRoot "src\assets\logo\Eduflex_Solutions_Transparent.png"
$dstPath = Join-Path $PSScriptRoot "src\assets\logo\eduflex-logo-mark.png"

$bmp = New-Object System.Drawing.Bitmap -ArgumentList $srcPath

[int]$cx = 512
[int]$cy = 483
[int]$half = 350
[int]$side = $half * 2

$srcRect  = New-Object System.Drawing.Rectangle -ArgumentList ($cx - $half), ($cy - $half), $side, $side
$destRect = New-Object System.Drawing.Rectangle -ArgumentList 0, 0, $side, $side

$cropped = New-Object System.Drawing.Bitmap -ArgumentList $side, $side
$g = [System.Drawing.Graphics]::FromImage($cropped)
$g.DrawImage($bmp, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

$cropped.Save($dstPath, [System.Drawing.Imaging.ImageFormat]::Png)

$g.Dispose()
$bmp.Dispose()
$cropped.Dispose()

Write-Host "Saved: $dstPath"
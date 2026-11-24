param([switch]$AppOnly, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'Se necesita .NET Framework 4.8 o posterior.' }
$out = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $root 'dist' }
[IO.Directory]::CreateDirectory($out) | Out-Null
$icon = Join-Path $out 'Blockganizer.ico'
$art = Join-Path $root 'assets\WeekBlocks.png'
if (!(Test-Path -LiteralPath $art)) { throw 'Falta la ilustración assets\WeekBlocks.png.' }
Add-Type -AssemblyName System.Drawing
$source = [Drawing.Image]::FromFile($art)
$bitmap = [Drawing.Bitmap]::new(64,64,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.DrawImage($source,0,0,64,64)
$source.Dispose()
$png = [IO.MemoryStream]::new()
$bitmap.Save($png,[Drawing.Imaging.ImageFormat]::Png)
$bytes = $png.ToArray()
$iconStream = [IO.File]::Create($icon)
$writer = [IO.BinaryWriter]::new($iconStream)
try {
    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]1)
    $writer.Write([byte]64)
    $writer.Write([byte]64)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$bytes.Length)
    $writer.Write([UInt32]22)
    $writer.Write($bytes)
} finally {
    $writer.Dispose()
    $png.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}
$referenceNames = 'WindowsBase','PresentationCore','PresentationFramework','UIAutomationTypes','UIAutomationProvider','System.Xaml'
$references = foreach ($name in $referenceNames) {
    $path = Join-Path $framework "WPF\$name.dll"
    if (!(Test-Path -LiteralPath $path)) { $path = Join-Path $framework "$name.dll" }
    if (!(Test-Path -LiteralPath $path)) { $path = Join-Path $framework "WPF\$name.dll" }
    "/reference:$path"
}
$src = Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName
$exe = Join-Path $out 'Blockganizer.exe'
$wash = Join-Path $root 'assets\Watercolor.jpg'
if (!(Test-Path -LiteralPath $wash)) { throw 'Falta el fondo assets\Watercolor.jpg.' }
$cube = Join-Path $root 'assets\ActivityCube.png'
if (!(Test-Path -LiteralPath $cube)) { throw 'Falta la ilustración assets\ActivityCube.png.' }
$clockFabric = Join-Path $root 'assets\ClockFabric.png'
if (!(Test-Path -LiteralPath $clockFabric)) { throw 'Falta la ilustración assets\ClockFabric.png.' }
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/resource:$art,Blockganizer.WeekBlocks.png" "/resource:$wash,Blockganizer.Watercolor.jpg" "/resource:$cube,Blockganizer.ActivityCube.png" "/resource:$clockFabric,Blockganizer.ClockFabric.png" "/win32icon:$icon" "/win32manifest:$root\src\app.manifest" "/out:$exe" @references @src
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de Blockganizer.' }
Copy-Item -LiteralPath (Join-Path $root 'src\App.config') -Destination "$exe.config"
if (!$AppOnly) { & (Join-Path $root 'installer\make-msi.ps1') -OutputDirectory $out }

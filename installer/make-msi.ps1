param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$work = Join-Path $OutputDirectory 'msi-obj'
[IO.Directory]::CreateDirectory($work) | Out-Null
$target = Join-Path $OutputDirectory 'Blockganizer-1.10.0-x64.msi'
$tempMsi = Join-Path $work ('package-' + [Guid]::NewGuid().ToString('N') + '.msi')
$app = Join-Path $OutputDirectory 'Blockganizer.exe'
$config = "$app.config"
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class PackageApi
{
    private const string Msi = "msi.dll";
    [DllImport(Msi, CharSet=CharSet.Unicode)] public static extern uint MsiOpenDatabase(string path, IntPtr mode, out uint db);
    [DllImport(Msi, CharSet=CharSet.Unicode)] public static extern uint MsiDatabaseImport(uint db, string folder, string file);
    [DllImport(Msi, CharSet=CharSet.Unicode)] public static extern uint MsiDatabaseOpenView(uint db, string query, out uint view);
    [DllImport(Msi, CharSet=CharSet.Unicode)] public static extern uint MsiRecordSetString(uint record, uint field, string value);
    [DllImport(Msi, CharSet=CharSet.Unicode)] public static extern uint MsiRecordSetStream(uint record, uint field, string value);
    [DllImport(Msi, CharSet=CharSet.Unicode)] public static extern uint MsiGetSummaryInformation(uint db, string path, uint count, out uint summary);
    [DllImport(Msi, CharSet=CharSet.Unicode)] private static extern uint MsiSummaryInfoSetProperty(uint summary, uint property, uint type, int value, IntPtr time, string text);
    [DllImport(Msi)] public static extern uint MsiCreateRecord(uint fields);
    [DllImport(Msi)] public static extern uint MsiViewExecute(uint view, uint record);
    [DllImport(Msi)] public static extern uint MsiCloseHandle(uint handle);
    [DllImport(Msi)] public static extern uint MsiSummaryInfoPersist(uint summary);
    [DllImport(Msi)] public static extern uint MsiDatabaseCommit(uint db);
    public static uint SetText(uint summary, uint property, string text)
    { return MsiSummaryInfoSetProperty(summary, property, 30, 0, IntPtr.Zero, text); }
    public static uint SetInt(uint summary, uint property, uint type, int value)
    { return MsiSummaryInfoSetProperty(summary, property, type, value, IntPtr.Zero, null); }
}
"@
function Check([uint32]$result, [string]$operation) {
    if ($result -ne 0) { throw "Windows Installer ($operation): $result" }
}
[uint32]$db = 0
Check ([PackageApi]::MsiOpenDatabase($tempMsi,[IntPtr]3,[ref]$db)) 'open database'
function Import-Table($name, [string]$head, [string]$types, [string]$key, [string[]]$rows) {
    $lines = @($head, $types, "$name|$key") + $rows
    $table = ($lines | ForEach-Object { $_.Replace('|', [string][char]9).Replace('{pipe}', '|') }) -join "`r`n"
    $file = "$name.idt"
    [IO.File]::WriteAllText((Join-Path $work $file), ($table + "`r`n"), [Text.Encoding]::ASCII)
    Check ([PackageApi]::MsiDatabaseImport($db,$work,$file)) $name
}

Import-Table Property 'Property|Value' 's72|l0' 'Property' @(
    'ProductCode|{EE32DC44-4887-45BD-A8D8-5771E03EB7AC}'
    'UpgradeCode|{78D16463-5A6A-4812-A614-A7E89D03479A}'
    'ProductLanguage|1033'
    'ProductName|Blockganizer'
    'ProductVersion|1.10.0'
    'SecureCustomProperties|OLDERBLOCKGANIZER'
    'Manufacturer|Blockganizer'
    'LIMITUI|1'
)
Import-Table Upgrade 'UpgradeCode|VersionMin|VersionMax|Language|Attributes|Remove|ActionProperty' 's38|S20|S20|S255|i4|S255|s72' 'UpgradeCode|VersionMin|VersionMax|Language|Attributes' @(
    '{78D16463-5A6A-4812-A614-A7E89D03479A}||1.10.0||0||OLDERBLOCKGANIZER'
)
Import-Table Directory 'Directory|Directory_Parent|DefaultDir' 's72|S72|l255' 'Directory' @(
    'TARGETDIR||SourceDir'
    'LocalAppDataFolder|TARGETDIR|.'
    'BLOCKROOT|LocalAppDataFolder|Blockganizer'
    'INSTALLDIR|BLOCKROOT|app'
    'ProgramMenuFolder|TARGETDIR|.'
)
Import-Table Component 'Component|ComponentId|Directory_|Attributes|Condition|KeyPath' 's72|S38|s72|i2|S255|S72' 'Component' @(
    'AppBinary|{B2915AB7-E1CF-4F62-9C54-898A1A363162}|INSTALLDIR|256||MainExe'
    'MenuLink|{FB116E60-5A26-4B91-AD06-55FF25160DBB}|ProgramMenuFolder|260||MenuKey'
)
Import-Table Feature 'Feature|Feature_Parent|Title|Description|Display|Level|Directory_|Attributes' 's38|S38|L64|L255|I2|i2|S72|i2' 'Feature' @(
    'Desktop||Blockganizer|Blockganizer|1|1|INSTALLDIR|0'
)
Import-Table FeatureComponents 'Feature_|Component_' 's38|s72' 'Feature_|Component_' @('Desktop|AppBinary','Desktop|MenuLink')
Import-Table File 'File|Component_|FileName|FileSize|Version|Language|Attributes|Sequence' 's72|s72|l255|i4|S72|S20|I2|i2' 'File' @(
    ('MainExe|AppBinary|BLOCKG~1.EXE{pipe}Blockganizer.exe|{0}|1.10.0.0||16384|1'.Replace('{pipe}','!PIPE!') -f (Get-Item -LiteralPath $app).Length).Replace('!PIPE!','{pipe}')
    ('AppConfig|AppBinary|BLKCFG~1.CFG{pipe}Blockganizer.exe.config|{0}|||16384|2'.Replace('{pipe}','!PIPE!') -f (Get-Item -LiteralPath $config).Length).Replace('!PIPE!','{pipe}')
)
Import-Table Media 'DiskId|LastSequence|DiskPrompt|Cabinet|VolumeLabel|Source' 'i2|i2|L64|S255|S32|S72' 'DiskId' @('1|2||#app.cab||')
Import-Table Registry 'Registry|Root|Key|Name|Value|Component_' 's72|i2|l255|L255|L0|s72' 'Registry' @(
    'MenuKey|1|Software\Blockganizer|Path|[INSTALLDIR]|MenuLink'
)
Import-Table Shortcut 'Shortcut|Directory_|Name|Component_|Target|Arguments|Description|Hotkey|Icon_|IconIndex|ShowCmd|WkDir' 's72|s72|l128|s72|s72|S255|L255|I2|S72|I2|I2|S72' 'Shortcut' @(
    'StartShortcut|ProgramMenuFolder|BLOCKG~1{pipe}Blockganizer|MenuLink|[#MainExe]||Blockganizer|||0|1|INSTALLDIR'
)

Import-Table InstallExecuteSequence 'Action|Condition|Sequence' 's72|S255|I2' 'Action' @(
    'FindRelatedProducts||25'
    'CostInitialize||800'
    'FileCost||900'
    'CostFinalize||1000'
    'InstallValidate||1400'
    'InstallInitialize||1500'
    'RemoveExistingProducts|OLDERBLOCKGANIZER|1510'
    'ProcessComponents||1600'
    'UnpublishFeatures||1800'
    'RemoveRegistryValues||2600'
    'RemoveShortcuts||3200'
    'RemoveFiles||3500'
    'RemoveFolders||3600'
    'InstallFiles||4000'
    'CreateShortcuts||4500'
    'WriteRegistryValues||5000'
    'RegisterUser||6000'
    'RegisterProduct||6100'
    'PublishFeatures||6300'
    'PublishProduct||6400'
    'InstallFinalize||6600'
)
Import-Table AdminExecuteSequence 'Action|Condition|Sequence' 's72|S255|I2' 'Action' @(
    'CostInitialize||800'
    'FileCost||900'
    'CostFinalize||1000'
    'InstallValidate||1400'
    'InstallInitialize||1500'
    'InstallAdminPackage||3900'
    'InstallFiles||4000'
    'InstallFinalize||6600'
)

$ddf = Join-Path $work 'package.ddf'
$lines = @(
    '.OPTION EXPLICIT'
    '.Set Cabinet=ON'
    '.Set Compress=ON'
    '.Set CompressionType=MSZIP'
    '.Set CabinetNameTemplate=app.cab'
    ".Set DiskDirectoryTemplate=`"$work`""
    ".Set RptFileName=`"$work\cabinet.rpt`""
    ".Set InfFileName=`"$work\cabinet.inf`""
    "`"$app`" MainExe"
    "`"$config`" AppConfig"
)
[IO.File]::WriteAllLines($ddf, $lines, [Text.Encoding]::Default)
& makecab.exe /V0 /F $ddf | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear el gabinete del instalador.' }
[uint32]$view = 0
[uint32]$summary = 0
$record = [PackageApi]::MsiCreateRecord(2)
try {
    Check ([PackageApi]::MsiDatabaseOpenView($db,'INSERT INTO `_Streams` (`Name`, `Data`) VALUES (?, ?)',[ref]$view)) 'stream view'
    Check ([PackageApi]::MsiRecordSetString($record,1,'app.cab')) 'stream name'
    Check ([PackageApi]::MsiRecordSetStream($record,2,(Join-Path $work 'app.cab'))) 'cabinet stream'
    Check ([PackageApi]::MsiViewExecute($view,$record)) 'stream insert'
    Check ([PackageApi]::MsiGetSummaryInformation($db,$null,14,[ref]$summary)) 'summary'
    Check ([PackageApi]::SetText($summary,2,'Installation Database')) 'title'
    Check ([PackageApi]::SetText($summary,3,'Blockganizer')) 'subject'
    Check ([PackageApi]::SetText($summary,4,'Blockganizer')) 'author'
    Check ([PackageApi]::SetText($summary,7,'x64;1033')) 'architecture'
    Check ([PackageApi]::SetText($summary,9,[Guid]::NewGuid().ToString('B').ToUpperInvariant())) 'package code'
    Check ([PackageApi]::SetInt($summary,14,3,200)) 'installer version'
    Check ([PackageApi]::SetInt($summary,15,3,10)) 'compressed source'
    Check ([PackageApi]::SetInt($summary,19,3,2)) 'package security'
    Check ([PackageApi]::MsiSummaryInfoPersist($summary)) 'persist summary'
    Check ([PackageApi]::MsiDatabaseCommit($db)) 'commit'
} finally {
    foreach ($handle in @($summary,$record,$view,$db)) {
        if ($handle -ne 0) { [PackageApi]::MsiCloseHandle($handle) | Out-Null }
    }
}
Move-Item -LiteralPath $tempMsi -Destination $target -Force
Get-Item -LiteralPath $target | Select-Object FullName, Length

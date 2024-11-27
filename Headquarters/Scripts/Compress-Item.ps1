<#
.SYNOPSIS
ローカルPC上のファイルを圧縮します

.DESCRIPTION
ローカルPC上のファイルを圧縮します
SourcePathと同じディレクトリに圧縮ファイルを作成します
例: C:\Temp\sample.txt -> C:\Temp\sample.7z

.PARAMETER SourcePath
圧縮するファイルまたディレクトリのパス

.PARAMETER ArichiveFormat
圧縮フォーマット
#>

param
(
    [ValidateNotNullOrEmpty()]
    $SourcePath,
    [ValidateSet("7z", "zip")]
    $ArichiveFormat
)

. .\Scripts\IncludeScripts\Test-PathExistence.ps1
Test-PathExistence -Path $SourcePath

$sourceItem = Get-Item $SourcePath
if (!($sourceItem.Count -eq 1)) {
    throw "SourcePath[$SourcePath]: 複数のアイテムが見つかりました。単一アイテムのみ対応しています。 $sourceItem"
}

$sourceDir = ""

if ($sourceItem.PSIsContainer) {
    $sourceDir = $sourceItem.Parent.FullName
}
else {
    $sourceDir = $sourceItem.DirectoryName
}

. .\Scripts\IncludeScripts\Get-UniqueFileName.ps1
$archiveFileName = Get-UniqueFileName -Directory $sourceDir -FileName "$($sourceItem.BaseName).$ArichiveFormat"
$archiveFilePath = Join-Path $sourceDir $archiveFileName

tar.exe -C $sourceDir -a -cf $archiveFilePath $($sourceItem.Name)

return $archiveFilePath
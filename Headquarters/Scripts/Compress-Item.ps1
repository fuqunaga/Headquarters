<#
.SYNOPSIS
ローカルPC上のファイル、フォルダを圧縮します

.PARAMETER SourcePath
圧縮するファイルのパス
ファイル、フォルダのどちらも指定できます

.PARAMETER DestinationPath
圧縮ファイルの保存先パス
拡張子で自動的に圧縮フォーマットを選択します
例: myFile.zip, myFile.7z
#>

param
(
    [Parameter(Mandatory)]
    $SourcePath, 
    $DestinationPath
)

try {
    $sourceItem = Get-Item -Path $SourcePath -ErrorAction Stop
}
catch {
    throw $_.Exception.Message
}

$sourceDir = ""
$sourceFile = ""

if ($sourceItem.PSIsContainer) {
    $sourceDir = $sourceItem.Parent.FullName
    $sourceFile = $sourceItem.Name
}
else {
    $sourceDir = $sourceItem.DirectoryName
    $sourceFile = $sourceItem.Name
}

tar.exe -C $sourceDir -a -cf $DestinationPath $sourceFile
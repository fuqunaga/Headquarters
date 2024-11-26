<#
.SYNOPSIS
ローカルPC上の圧縮ファイルを解凍します

.PARAMETER ArchivePath
解凍するファイルのパス

.PARAMETER DestinationDirectory
解凍先のディレクトリ
存在しなければ自動的に作成します
#>

param
(
    $ArchivePath, 
    $DestinationDirectory
)

try {
    $archiveItem = Get-Item -Path $ArchivePath -ErrorAction Stop
}
catch {
    throw $_.Exception.Message
}


# 解凍先のディレクトリが存在しない場合、作成
if (-not (Test-Path -Path $DestinationDirectory)) {
    New-Item -ItemType Directory -Path $DestinationDirectory > $null
}

# tarコマンドを使用して解凍
# Write-Output "tar -xf $archiveItem -C $DestinationDirectory"
tar -xf $archiveItem -C $DestinationDirectory
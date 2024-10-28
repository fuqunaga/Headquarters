<#
.SYNOPSIS
PowerShellのCopy-Itemコマンドレットでファイルをコピーします

.DESCRIPTION
PowerShellのCopy-Itemコマンドレットでファイルをコピーします
remotePathに"$(IP)"という文字列が含まれている場合、対応するIPListのIPアドレスに置換してUNCパスに変換します

.PARAMETER localPath
コピー元のローカルパス

.PARAMETER remotePath
コピー先のリモートパス
"$(IP)"という文字列が含まれている場合、対応するIPListのIPアドレスに置換してUNCパスに変換します
#>

param($session, $ip, $localPath, $remotePath)

$remotePathExtracted = $remotePath.Replace("`$(IP)", $ip)
$remotePathInfo = [System.Uri]$remotePathExtracted

if ($remotePathInfo.IsUnc)
{
    Copy-Item -Recurse -Force -Path $localPath -Destination $remotePathExtracted
}
else
{
    Copy-Item -ToSession $session -Recurse -Force -Path $localPath -Destination $remotePath
}
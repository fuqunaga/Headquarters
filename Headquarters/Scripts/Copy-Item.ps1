<#
.SYNOPSIS
PowerShellのCopy-Itemコマンドレットでファイルをコピーします

.DESCRIPTION
PowerShellのCopy-Itemコマンドレットでファイルをコピーします
RemotePathに"$(IP)"という文字列が含まれている場合、対応するIPListのIPアドレスに置換します
これを利用してUNCパス（Windowsの共有フォルダ機能）でのコピーも可能です
例: \\$(IP)\share\path -> \\192.168.0.1\share\path

.PARAMETER LocalPath
コピー元のパス

.PARAMETER RemotePath
コピー先のパス
RemotePathに"$(IP)"という文字列が含まれている場合、対応するIPListのIPアドレスに置換します
これを利用してUNCパス（Windowsの共有フォルダ機能）でのコピーも可能です
例: \\$(IP)\share\path -> \\192.168.0.1\share\path
#>

param(
    [ValidateNotNullOrEmpty()]
    $LocalPath, 
    [ValidateNotNullOrEmpty()]
    $RemotePath,
    $Session, 
    $Ip, 
    [PSCredential]$Credential
)

. .\Scripts\IncludeScripts\Test-PathExistance.ps1
Test-PathExistance $LocalPath

$remotePathExtracted = $RemotePath.Replace("`$(IP)", $Ip)
$remotePathInfo = [System.Uri]$remotePathExtracted

if ($remotePathInfo.IsUnc)
{
    $destination = $remotePathExtracted
    if (!(Test-Path $remotePathExtracted))
    {
        # Write-Output "Create temporary drive for UNC path"
        $root = [System.IO.Path]::GetPathRoot($remotePathExtracted)
        New-PSDrive -Name "Copy-Item_TempDrive" -PSProvider FileSystem -Root $root -Credential $Credential > $null
        $destination = $remotePathExtracted.Replace($root, "Copy-Item_TempDrive:")
    }
    # Write-Output $destination
    Copy-Item -Recurse -Force -Path $LocalPath -Destination $destination
}
else
{
    Copy-Item -ToSession $session -Recurse -Force -Path $LocalPath -Destination $RemotePath
}
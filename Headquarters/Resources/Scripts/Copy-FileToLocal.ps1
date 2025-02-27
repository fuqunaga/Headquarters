<#
.SYNOPSIS
リモートPCのファイルをローカルPCにコピーします

.PARAMETER RemotePath
コピー元のパス（リモートPC）
"$(IP)"という文字列が含まれている場合、対応するIPListのIPアドレスに置換されます
これにより共有フォルダ（UNCパス）でのコピーも可能です
例: \\$(IP)\share\path -> \\192.168.0.1\share\path

.PARAMETER LocalPath
コピー先のパス（ローカルPC）
"$(IP)"という文字列が含まれている場合、対応するIPListのIPアドレスに置換されます
例: C:\Backup\$(IP)\MyFile -> C:\Backup\192.168.0.1\MyFile
"$(IP)"またはIPListを用いて各IPアドレスごとにユニークなパスになるよう指定してください
#>


param(
    [ValidateNotNullOrEmpty()]
    $RemotePath,
    [ValidateNotNullOrEmpty()]
    [Headquarters.Path()]
    $LocalPath,
    $TaskContext
)

$sourcePath = Convert-PathToUncAndAuth -Path $RemotePath -TaskContext $TaskContext

# フォルダかどうかを判定
# IPアドレスが展開されるとc:\192.168.0.1などが拡張子ありでファイルとして認識されるため、展開前に判定する
$isDestinaionPathFolder = $LocalPath.EndsWith("\") -or ([System.IO.Path]::GetExtension($LocalPath) -eq "")
$destinationPath = $LocalPath.Replace("`$(IP)", $TaskContext.IpAddress).TrimEnd("\")
$destinationFolderPath = $destinationPath

# Destinationがフォルダ
if ($isDestinaionPathFolder)
{
    if (Test-Path -PathType Leaf $destinationPath)
    {
        throw "コピー先がフォルダとして指定されていますが同名のファイルが存在します $destinationPath"
    }
}
else
{
    $destinationFolderPath = [System.IO.Path]::GetDirectoryName($destinationPath)
}


# フォルダが存在しない場合、作成
if (-not (Test-Path $destinationFolderPath))
{
    New-Item $destinationFolderPath -ItemType Directory
}

if (([System.Uri]$sourcePath).IsUnc)
{
    Copy-Item -Recurse -Force -Path $sourcePath -Destination $destinationPath
}
else
{
    $session = New-PSSessionFromTaskContext -TaskContext $TaskContext
    Copy-Item -Recurse -Force -Path $sourcePath -Destination $destinationPath -FromSession $session
}
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
"$(IP)"またはIPListで指定されたIPアドレスごとにパラメータを分けないと、最後にコピーされたファイルで上書きされてしまいます
各IPアドレスに対応するユニークなパスを指定してください
#>


param(
    [ValidateNotNullOrEmpty()]
    $RemotePath,
    [ValidateNotNullOrEmpty()]
    $LocalPath,
    $TaskContext
)



<#
入力されたパスを実際に使用可能なパスに変換します
UNCパスの場合、認証を通すために一時的なドライブを作成します
#>
function ConvertPathAndConnectUNC()
{
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        $TargetPath,
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        $TaskContext
    )

    $outputPath = $TargetPath.Replace("`$(IP)", $TaskContext.IpAddress)

    $isUnc = ([System.Uri]$outputPath).IsUnc
    if ($isUnc)
    {
        $root = [System.IO.Path]::GetPathRoot($outputPath)
        $cred = $TaskContext.Credential

        # UNC認証が必要な場合、一時的なドライブを作成して認証を通すハック
        # https://stackoverflow.com/questions/67469217/powershell-unc-path-with-credentials
        if (-not (Test-Path $root)) {
            $driveName = "Copy-File_TempDrive_$($root -replace '[\\.]', '')"
            New-PSDrive -Name $driveName -PSProvider FileSystem -Root $root -Credential $cred > $null
            if (-not $?) {
                throw "UNCパスの認証に失敗しました。共有フォルダの設定がされていないかもしれません $root"
            }
        }
    }

    return $outputPath
}


$sourcePath = ConvertPathAndConnectUNC -TargetPath $RemotePath -TaskContext $TaskContext

# フォルダかどうかを判定
# IPアドレスが展開されるとc:\192.168.0.1などが拡張子ありでファイルとして認識されるため、展開前に判定する
$isDestinaionPathFolder = $LocalPath.EndsWith("\") -or ([System.IO.Path]::GetExtension($LocalPath) -eq "")
$destinationPath = $LocalPath.Replace("`$(IP)", $TaskContext.IpAddress).TrimEnd("\")

# Check if destinationPath
if ($isDestinaionPathFolder)
{
    if (Test-Path -PathType Leaf $destinationPath)
    {
        throw "コピー先がフォルダとして指定されていますが同名のファイルが存在します $destinationPath"
    }

    if (-not (Test-Path $destinationPath))
    {
        New-Item $destinationPath -ItemType Directory
    }
}

$session = New-PSSession -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential
Copy-Item -FromSession $session -Recurse -Force -Path $sourcePath -Destination $destinationPath

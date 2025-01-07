<#
.SYNOPSIS
ファイルをコピーします

.DESCRIPTION
PowerShellのCopy-Itemコマンドレットでファイルをコピーします
RemoteFolderに"$(IP)"という文字列が含まれている場合、対応するIPListのIPアドレスに置換します
Windowsの共有フォルダ機能（UNCパス）でのコピーも可能です
例: \\$(IP)\share\path -> \\192.168.0.1\share\path

.PARAMETER LocalPath
コピー元のパス

.PARAMETER RemoteFolder
コピー先のフォルダ
"$(IP)"という文字列が含まれていると対応するIPListのIPアドレスに置換します
これにより共有フォルダ（UNCパス）でのコピーも可能です
例: \\$(IP)\share\path -> \\192.168.0.1\share\path

.PARAMETER EnableCompression
圧縮ファイルでコピー
コピー元と同じ場所に圧縮ファイルを作成し、コピー、解凍します
7z形式

.PARAMETER SkipCompressionIfNewer
不要な圧縮をスキップ
コピー元より新しい圧縮ファイルがある場合は圧縮をスキップします

.PARAMETER DeleteCompressedFile
圧縮ファイルを削除
正常終了時に圧縮ファイルを残すか削除するか指定できます
#>


function BeginTask()
{
    param(
        [ValidateScript({Test-PathExistence $_})]
        [Headquarters.Path()]
        $LocalPath,
        [bool]$EnableCompression=$true,
        [bool]$SkipCompressionIfNewer=$true
    )

    if (-not $EnableCompression)
    {
        return
    }
    
    $archivePath = Get-ArchivePath -FilePath $LocalPath
    $needArchive = $true
    
    # $archivePathが存在し、$LocalPathより新しい場合は圧縮をスキップ
    if ($SkipCompressionIfNewer -and (Test-Path $archivePath))
    {
        $archiveLastWriteTime = (Get-Item -Path $archivePath).LastWriteTime
        $lastWriteItem = Get-LastWriteItem (Get-Item -Path $LocalPath)
        $lastWriteTime = $lastWriteItem.LastWriteTime

        $needArchive = $lastWriteTime -gt $archiveLastWriteTime
        if (!$needArchive)
        {
            Write-Output "コピー元より新しい圧縮ファイルが見つかりました。圧縮をスキップします"
            Write-Output " [$archiveLastWriteTime] $archivePath  圧縮ファイル "
            Write-Output " [$lastWriteTime] $($lastWriteItem.FullName)  コピー元の最も新しいファイル "
        }
    }

    if($needArchive)
    {
        Compress-7ZipExt -ArchiveFileName $archivePath -Path $LocalPath
    }
}

function IpAddressTask()
{
    param(
        [ValidateNotNullOrEmpty()]
        $LocalPath,
        [ValidateNotNullOrEmpty()]
        $RemoteFolder,
        $EnableCompression,
        [ValidateSet("DontDelete", "DeleteRemoteOnly", "DeleteLocalOnly", "DeleteRemoteAndLocal")]
        $DeleteCompressedFile,
        $Session,
        $Ip,
        [PSCredential]$Credential
    )

    $archivePath = Get-ArchivePath -FilePath $LocalPath

    $remoteFolderExtracted = $RemoteFolder.Replace("`$(IP)", $Ip)
    $remoteFolderInfo = [System.Uri]$remoteFolderExtracted
    
    $sourcePath = $LocalPath
    if ($EnableCompression)
    {
        $sourcePath = $archivePath
    }

    $destinationFolder = $RemoteFolder
    
    # UNCパス形式
    if ($remoteFolderInfo.IsUnc)
    {
        $destinationFolder = $remoteFolderExtracted
        $root = [System.IO.Path]::GetPathRoot($remoteFolderExtracted)
        
        # UNC認証が必要な場合、一時的なドライブを作成して認証を通すハック
        # https://stackoverflow.com/questions/67469217/powershell-unc-path-with-credentials
        if (!(Test-Path $root))
        {
            New-PSDrive -Name "Copy-Item_TempDrive" -PSProvider FileSystem -Root $root -Credential $Credential > $null
        }
    }

    # リモートPCにディレクトリが存在しない場合、作成
    # Copy-Itemはディレクトリが存在するとその直下にファイルをコピーするが
    # ディレクトリが存在しない場合はファイル名として解釈してしまう
    Invoke-Command -Session $Session -ScriptBlock {
        if (-not (Test-Path -Path $using:destinationFolder))
        {
            New-Item -ItemType Directory -Path $using:destinationFolder > $null
        }
    }

    # Write-Output $destination
    if ($remoteFolderInfo.IsUnc)
    {
        Copy-Item -Recurse -Force -Path $sourcePath -Destination $destinationFolder
    }
    else
    {
        Copy-Item -Recurse -Force -Path $sourcePath -Destination $destinationFolder -ToSession $Session
    }

    Write-Output "コピー $sourcePath -> (Remote)$(Join-Path $destinationFolder (Split-Path $sourcePath -Leaf))"
    

    # 圧縮してたら解凍して削除
    if ($EnableCompression)
    {
        $remoteArchivePath = Join-Path $destinationFolder (Split-Path $archivePath -Leaf)
        $expandTargetPath = $destinationFolder
        
        # フォルダの場合はdestinationFolder下に元のフォルダ名を作成して解凍
        if ( Test-Path $LocalPath -PathType Container )
        {
            $expandTargetPath = Join-Path $destinationFolder (Split-Path $LocalPath -Leaf)
        }

        Expand-7ZipExt -ArchiveFileName $remoteArchivePath -TargetPath $expandTargetPath -Session $Session

        if (IsDeleteRemoteArchiveFile $DeleteCompressedFile)
        {
            Invoke-Command -Session $Session -ScriptBlock {
                Remove-Item -Path $using:remoteArchivePath
            }

            Write-Output "圧縮ファイル削除(Remote) $remoteArchivePath"
        }
    }
}

function EndTask()
{
    param(
        [ValidateNotNullOrEmpty()]
        $LocalPath,
        $EnableCompression,
        $DeleteCompressedFile
    )

    if ($ArchiveFormat -eq "none")
    {
        return
    }

    if (IsDeleteLocalArchiveFile $DeleteCompressedFile)
    {
        $archivePath = Get-ArchivePath -FilePath $LocalPath
        Remove-Item -Path $archivePath

        Write-Output "圧縮ファイル削除 $archivePath"
    }
}


function Get-ArchivePath()
{
    param(
        [ValidateNotNullOrEmpty()]
        $FilePath,
        [string]$ArchiveFormat="7z"
    )

    if ($ArchiveFormat -eq "none")
    {
        return $FilePath
    }

    $path = $FilePath.TrimEnd([System.IO.Path]::DirectorySeparatorChar)
    return "$path.$ArchiveFormat"
}

function Get-LastWriteItem($path)
{ 
    $item = Get-Item $path

    if ($item.Attributes -band [System.IO.FileAttributes]::Directory) {
        # The path is a directory, get the newest file's last write time recursively
        return Get-ChildItem $path -Recurse |
                      Sort-Object LastWriteTime -Descending |
                      Select-Object -First 1
    }
    else {
        return $item
    }
}

function IsDeleteRemoteArchiveFile()
{
    param(
        $DeleteCompressedFile
    )

    return $DeleteCompressedFile -eq "DeleteRemoteOnly" -or $DeleteCompressedFile -eq "DeleteRemoteAndLocal"
}

function IsDeleteLocalArchiveFile()
{
    param(
        $DeleteCompressedFile
    )

    return $DeleteCompressedFile -eq "DeleteLocalOnly" -or $DeleteCompressedFile -eq "DeleteRemoteAndLocal"
}
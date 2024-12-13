<#
.SYNOPSIS
PowerShellのCopy-Itemコマンドレットでファイルをコピーします

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
これによりWindowsの共有フォルダ機能（UNCパス）でのコピーも可能です
例: \\$(IP)\share\path -> \\192.168.0.1\share\path

.PARAMETER ArchiveFormat
圧縮フォーマット
指定された場合は圧縮ファイルを作成、コピー、解凍します

.PARAMETER DeleteArchiveFile
圧縮ファイルを削除
正常に処理が終わったら圧縮ファイルを削除するか

.PARAMETER SkipArchiveIfNewer
不要な圧縮をスキップ
コピー元より新しい圧縮ファイルがある場合は圧縮をスキップします
#>


function PreProcess()
{
    param(
        [ValidateScript({Test-PathExistence $_})]
        [Headquarters.Path()]
        $LocalPath,
        [ValidateSet("none", "7z", "zip")]
        [string]$ArchiveFormat,
        [bool]$SkipArchiveIfNewer=$true
    )

    if ($ArchiveFormat -eq "none")
    {
        return
    }
    
    $archivePath = Get-ArchivePath -FilePath $LocalPath -ArchiveFormat $ArchiveFormat
    $needArchive = $true
    
    # $archivePathが存在し、$LocalPathより新しい場合は圧縮をスキップ
    if ($SkipArchiveIfNewer -and (Test-Path $archivePath))
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
        Compress-File -FilePath $LocalPath -ArchiveFilePath $archivePath
        Write-Output "ファイル圧縮"
        Write-Output " $LocalPath"
        Write-Output "-> $archivePath"
    }
}

function IpAddressProcess()
{
    param(
        [ValidateNotNullOrEmpty()]
        $LocalPath,
        [ValidateNotNullOrEmpty()]
        $RemoteFolder,
        $ArchiveFormat,
        [ValidateSet("DontDelete", "DeleteRemoteOnly", "DeleteLocalOnly", "DeleteRemoteAndLocal")]
        $DeleteArchiveFile,
        $Session,
        $Ip,
        [PSCredential]$Credential
    )

    $archiveEnabled = !($ArchiveFormat -eq "none")
    $archivePath = Get-ArchivePath -FilePath $LocalPath -ArchiveFormat $ArchiveFormat

    $remoteFolderExtracted = $RemoteFolder.Replace("`$(IP)", $Ip)
    $remoteFolderInfo = [System.Uri]$remoteFolderExtracted
    
    $sourcePath = $LocalPath
    if ($archiveEnabled)
    {
        $sourcePath = $archivePath
    }

    $destinationFolder = $RemoteFolder
    
    # UNCパス形式
    if ($remoteFolderInfo.IsUnc)
    {
        $destinationFolder = $remoteFolderExtracted
        $root = [System.IO.Path]::GetPathRoot($remoteFolderExtracted)
        
        # UNC認証が必要な場合、一時的なドライブを作成
        if (!(Test-Path $root))
        {
            New-PSDrive -Name "Copy-Item_TempDrive" -PSProvider FileSystem -Root $root -Credential $Credential > $null
            $destinationFolder = $remoteFolderExtracted.Replace($root, "Copy-Item_TempDrive:")
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

    Write-Output "コピー $sourcePath -> (Remote)$destinationFolder"
    

    # 圧縮してたら解凍して削除
    if ($archiveEnabled)
    {
        $remoteArchivePath = Join-Path $destinationFolder (Split-Path $archivePath -Leaf)
        $deleteRemoteArchiveFile = IsDeleteRemoteArchiveFile $DeleteArchiveFile

        Import-CommandToSession -CommandName Expand-File -Session $Session
        Invoke-Command -Session $Session -ScriptBlock {
            Expand-File -ArchiveFilePath $using:remoteArchivePath -DestinationDirectory $using:destinationFolder
        }

        Write-Output "解凍(Remote) $remoteArchivePath -> $destinationFolder"

        if ($deleteRemoteArchiveFile)
        {
            Invoke-Command -Session $Session -ScriptBlock {
                Remove-Item -Path $using:remoteArchivePath
            }

            Write-Output "圧縮ファイル削除(Remote) $remoteArchivePath"
        }
    }
}

function PostProcess()
{
    param(
        [ValidateNotNullOrEmpty()]
        $LocalPath,
        [string]$ArchiveFormat,
        $DeleteArchiveFile
    )

    if ($ArchiveFormat -eq "none")
    {
        return
    }

    if (IsDeleteLocalArchiveFile $DeleteArchiveFile)
    {
        $archivePath = Get-ArchivePath -FilePath $LocalPath -ArchiveFormat $ArchiveFormat
        Remove-Item -Path $archivePath

        Write-Output "圧縮ファイル削除 $archivePath"
    }
}


function Get-ArchivePath()
{
    param(
        [ValidateNotNullOrEmpty()]
        $FilePath,
        [string]$ArchiveFormat
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
        $DeleteArchiveFile
    )

    return $DeleteArchiveFile -eq "DeleteRemoteOnly" -or $DeleteArchiveFile -eq "DeleteRemoteAndLocal"
}

function IsDeleteLocalArchiveFile()
{
    param(
        $DeleteArchiveFile
    )

    return $DeleteArchiveFile -eq "DeleteLocalOnly" -or $DeleteArchiveFile -eq "DeleteRemoteAndLocal"
}
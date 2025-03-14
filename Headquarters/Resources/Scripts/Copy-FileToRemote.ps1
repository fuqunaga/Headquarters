﻿<#
.SYNOPSIS
ローカルPCのファイルをリモートPCにコピーします

.PARAMETER LocalPath
コピー元のパス（ローカルPC）

.PARAMETER RemoteFolder
コピー先のフォルダ（リモートPC）
"$(IP)"という文字列が含まれている場合、対応するIPListのIPアドレスに置換されます
これにより共有フォルダ（UNCパス）でのコピーも可能です
例: \\$(IP)\share\path -> \\192.168.0.1\share\path

.PARAMETER EnableRelayCopy
リレーコピー
コピー済みのPCをコピー元として再利用しローカルPCのネットワーク帯域を節約します
大容量のファイルを多数のPCにコピーする場合におすすめです

.PARAMETER MaxTargetsPerSource
リレーコピー時に1PCが同時に行う最大コピー先数

.PARAMETER EnableCompression
圧縮ファイルでコピー
コピー元と同じ場所に7zの圧縮ファイルを作成し、コピー、解凍します
初回実行時はモジュールをインストールするためインターネット接続が必要です

.PARAMETER SkipCompressionIfNewer
不要な圧縮をスキップ
コピー元より新しい圧縮ファイルがある場合は圧縮をスキップします

.PARAMETER DeleteCompressedFileRemote
コピー完了後、リモートの圧縮ファイルを削除
リレーコピー時は無視されます

.PARAMETER DeleteCompressedFileLocal
コピー完了後、ローカルの圧縮ファイルを削除
#>


# プールID
$poolId = "CopyFileToRemotePool"

# リレーコピーでPoolに登録するローカルPCの値
# TaskContext型でなければ何でもいい
$localPcDummyValue = "LocalPC"

function BeginTask()
{
    param(
        [ValidateScript({Test-PathExistence $_})]
        [Headquarters.Path()]
        $LocalPath,
        [bool]$EnableRelayCopy=$false,
        [UInt32]$MaxTargetsPerSource=5,
        [bool]$EnableCompression=$true,
        [bool]$SkipCompressionIfNewer=$true,
        [Headquarters.TaskContext]$TaskContext
    )

    # リレーコピーならあらかじめPoolに$MaxTargetsPerSource分だけローカルPCが使えるように登録
    if ($EnableRelayCopy)
    {
        $pool = Get-PoolFromTaskContext -PoolId $poolId -TaskContext $TaskContext
        for($i = 0; $i -lt $MaxTargetsPerSource; $i++)
        {
            $pool.SetObject($localPcDummyValue)
        }
    }


    if (-not $EnableCompression)
    {
        return
    }

    $archivePath = Get-CompressedFilePath -FilePath $LocalPath
    $needCompression = $true

    # $archivePathが存在し、$LocalPathより新しい場合は圧縮をスキップ
    if ($SkipCompressionIfNewer -and (Test-Path $archivePath))
    {
        $archiveLastWriteTime = (Get-Item -Path $archivePath).LastWriteTime
        $lastWriteItem = Get-LastWriteItem (Get-Item -Path $LocalPath)
        $lastWriteTime = $lastWriteItem.LastWriteTime

        $needCompression = $lastWriteTime -gt $archiveLastWriteTime
        if (!$needCompression)
        {
            Write-Host "コピー元より新しい圧縮ファイルが見つかりました。圧縮をスキップします
- [$archiveLastWriteTime] $archivePath  圧縮ファイル
- [$lastWriteTime] $($lastWriteItem.FullName)  コピー元の最も新しいファイル"
        }
    }

    if($needCompression)
    {
        Compress-7ZipExt -OutputFilePath $archivePath -SourcePath $LocalPath
    }
}


function IpAddressTask()
{
    param(
        [ValidateNotNullOrEmpty()]
        $LocalPath,
        [ValidateNotNullOrEmpty()]
        $RemoteFolder,
        [bool]$EnableRelayCopy,
        [int]$MaxTargetsPerSource,
        [bool]$EnableCompression,
        [bool]$DeleteCompressedFileRemote,
        $TaskContext
    )

    # リレーコピー時は完了済みのPCがあればそのPCをコピー元として利用

    $sessionLocal = $null
    $sourceTaskContext = $null
    if ($EnableRelayCopy)
    {
        $pool = Get-PoolFromTaskContext -PoolId $poolId -TaskContext $TaskContext

        $sourceTaskContext = Get-SourceTaskContext -Pool $pool -TaskContext $TaskContext
        if ($sourceTaskContext -is [Headquarters.TaskContext])
        {
            # New-PSSessionではなくInvoke-PSSessionを使うことでダブルホップを回避
            # https://zenn.dev/fuqunaga/articles/1d52ec77c643c5
            Install-ModuleIfNotYet -Name "Invoke-PSSession"
            $sessionLocal = Invoke-PSSession -ComputerName $sourceTaskContext.IpAddress -Credential $sourceTaskContext.Credential
        }
    }

    try
    {
        CopyTask `
            -LocalPath $LocalPath `
            -RemoteFolder $RemoteFolder `
            -EnableRelayCopy $EnableRelayCopy `
            -EnableCompression $EnableCompression `
            -DeleteCompressedFileRemote $DeleteCompressedFileRemote `
            -TaskContext $TaskContext `
            -sessionLocal $sessionLocal `
            -SourceTaskContext $sourceTaskContext
    }
    finally
    {
        if ($EnableRelayCopy)
        {
            # 終了済みTaskContextを返却
            if ($sessionLocal)
            {
                $pool.SetObject($sourceTaskContext)
            }
            else
            {
                $pool.SetObject($localPcDummyValue)
            }
        }
    }

    if ($EnableRelayCopy)
    {
        # 今回コピーした先のTaskContextは$MaxTargetsPerSource分だけ登録
        for($i = 0; $i -lt $MaxTargetsPerSource; $i++)
        {
            $pool.SetObject($TaskContext)
        }
    }
}


function EndTask()
{
    param(
        [ValidateNotNullOrEmpty()]
        $LocalPath,
        [bool]$EnableCompression=$true,
        [bool]$DeleteCompressedFileLocal=$false
    )

    if ($EnableCompression -and $DeleteCompressedFileLocal)
    {
        $archivePath = Get-CompressedFilePath -FilePath $LocalPath
        Write-Host "圧縮ファイル削除 $archivePath"
        Remove-Item -Path $archivePath
    }
}


# ファイルのコピー元として扱うPCのTaskContextを取得
# ローカルで行う場合は$nullを返却
function Get-SourceTaskContext($pool, $atomicUseLocalKey, $TaskContext)
{
    $waitStartTime = Get-Date

    while($true)
    {
        $sourceTaskContext = $pool.TryGetObject()
        if ($sourceTaskContext) {
            # Completedを出力して表示を削除
            Write-Progress "Completed" -Completed
            return $sourceTaskContext
        }

        $elapsedTime = (Get-Date) - $waitStartTime
        Write-Progress "ローカルPCかリレーコピー元PCが使用可能になるのを待機しています 経過時間[$($elapsedTime.ToString("hh\:mm\:ss"))]"

        # 0.5~1秒待機
        # 複数のタスクが同時にチェックするのは避けるため待ち時間を散らす
        Start-Sleep -Seconds (Get-Random -Minimum 0.5 -Maximum 1)
    }
}

function CopyTask()
{
    param(
        [ValidateNotNullOrEmpty()]
        $LocalPath,
        [ValidateNotNullOrEmpty()]
        $RemoteFolder,
        [bool]$EnableRelayCopy=$true,
        [bool]$EnableCompression=$true,
        [bool]$DeleteCompressedFileRemote=$false,
        $TaskContext,
        $SessionLocal,
        $SourceTaskContext
    )


    $isRelayCopy = $EnableRelayCopy -and $SessionLocal
    if ($isRelayCopy)
    {
        Write-Host "リレーコピー: $($SourceTaskContext.IpAddress) をコピー元として使用します"
    }

    # $sourcePath 取得
    # 圧縮してたら圧縮ファイルをコピー元として利用
    # リレーコピー時はリモートPCにコピーしたファイルをコピー元として利用
    $sourcePath = $LocalPath
    if ($EnableCompression)
    {
        $sourcePath = Get-CompressedFilePath -FilePath $LocalPath
    }
    if ($isRelayCopy)
    {
        $sourceFolder = Convert-PathToUncAndAuthWithSession -TargetPath $RemoteFolder -SessionLocal $SessionLocal -TaskContext $SourceTaskContext
        $sourcePath = Join-Path $sourceFolder (Split-Path $sourcePath -Leaf)
    }

    $destinationFolder = Convert-PathToUncAndAuthWithSession -TargetPath $RemoteFolder -SessionLocal $SessionLocal -TaskContext $TaskContext
    $outputPath = Join-Path $destinationFolder (Split-Path $sourcePath -Leaf)

    $label = "コピー"
    if ($isRelayCopy)
    {
        $label = "リレーコピー"
    }
    Write-Host "$($label): $sourcePath -> (Remote)$outputPath"
    Copy-FileToRemote -sourcePath $sourcePath -destinationFolder $destinationFolder -SessionLocal $SessionLocal -TaskContext $TaskContext


    # 圧縮してたら解凍
    if ($EnableCompression)
    {
        $sessionRemote = New-PSSession -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential

        # フォルダの場合はdestinationFolder下に元のフォルダ名を作成して解凍
        $expandTargetPath = $destinationFolder
        if ( Test-Path $LocalPath -PathType Container )
        {
            $expandTargetPath = Join-Path $destinationFolder (Split-Path $LocalPath -Leaf)
        }

        Expand-7ZipExt -ArchiveFilePath $outputPath -OutputFolderPath $expandTargetPath -Session $SessionRemote

        # 圧縮ファイルを削除
        if ($DeleteCompressedFileRemote -and -not $EnableRelayCopy)
        {
            Write-Host "圧縮ファイル削除(Remote) $outputPath"
            Invoke-Command -Session $sessionRemote -ScriptBlock {
                Remove-Item -Path $using:outputPath
            }
        }
    }

}

function Get-CompressedFilePath()
{
    param(
        [ValidateNotNullOrEmpty()]
        $FilePath,
        [string]$ArchiveFormat="7z"
    )

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


function Convert-PathToUncAndAuthWithSession()
{
    param(
        [ValidateNotNullOrEmpty()]
        $TargetPath,
        $SessionLocal,
        [ValidateNotNull()]
        $TaskContext
    )
    
    if ($SessionLocal)
    {
        Import-CommandToSession -CommandName "Convert-PathToUncAndAuth" -Session $SessionLocal
        return Invoke-Command -Session $SessionLocal -ScriptBlock {
            Convert-PathToUncAndAuth -Path $using:TargetPath -TaskContext $using:TaskContext
        }
    }
    else
    {
        return Convert-PathToUncAndAuth -Path $TargetPath -TaskContext $TaskContext
    }
}


# SessionLocal上でリモートにファイルをコピーします
# リモートPCにディレクトリが存在しない場合、作成
# Copy-Itemはディレクトリが存在するとその直下にファイルをコピーするが
# ディレクトリが存在しない場合はファイル名として解釈してしまう
function Copy-FileToRemote()
{
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$sourcePath,
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$destinationFolder,
        $SessionLocal,
        $TaskContext
    )

    Invoke-Command -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential -ScriptBlock {
        if (-not (Test-Path -Path $using:destinationFolder))
        {
            New-Item -ItemType Directory -Path $using:destinationFolder > $null
        }
    }

    $isUnc = ([System.Uri]$destinationFolder).IsUnc

    # SessionLocal上で実行されるスクリプトブロック
    $copyScriptBlock = $null
    if ($isUnc)
    {
        $copyScriptBlock = {
            param($sourcePath, $destinationFolder)
            Copy-Item -Recurse -Force -Path $sourcePath -Destination $destinationFolder
        }
    }
    else
    {
        # PSSessionはリモート上に持っていけないので,
        # TaskContextのIpAddressとCredentialを引数に渡してリモートでNew-PSSessionを実行する
        $copyScriptBlock = {
            param($sourcePath, $destinationFolder, $ipAddress, [PSCredential]$cred)

            $sessionRemote = New-PSSession -ComputerName $ipAddress -Credential $cred
            Copy-Item -Recurse -Force -Path $sourcePath -Destination $destinationFolder -ToSession $sessionRemote
        }
    }

    Invoke-ScriptBlock -ScriptBlock $copyScriptBlock -Session $SessionLocal -Arguments $sourcePath, $destinationFolder, $TaskContext.IpAddress, $TaskContext.Credential
}


function Invoke-ScriptBlock {
    param (
        [parameter(Mandatory)]
        [ScriptBlock]$ScriptBlock,
        [parameter(Mandatory = $false)]
        $Session,
        [parameter(Mandatory)]
        [psobject[]]$Arguments
    )

    if ($null -eq $Session) {
        & $ScriptBlock @Arguments
    }
    else {
        Invoke-Command -Session $Session -ScriptBlock $ScriptBlock -ArgumentList $Arguments
    }
}
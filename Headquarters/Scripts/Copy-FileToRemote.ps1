<#
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
            Write-Information "コピー元より新しい圧縮ファイルが見つかりました。圧縮をスキップします"
            Write-Information "- [$archiveLastWriteTime] $archivePath  圧縮ファイル "
            Write-Information "- [$lastWriteTime] $($lastWriteItem.FullName)  コピー元の最も新しいファイル "
        }
    }

    if($needCompression)
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
        [bool]$EnableRelayCopy=$true,
        [bool]$EnableCompression=$true,
        [bool]$DeleteCompressedFileRemote=$false,
        $TaskContext
    )

    # リレーコピー時は完了済みのPCがあればそのPCをコピー元として利用
    $sessionLocal = $null
    if ($EnableRelayCopy)
    {
        $atomicUseLocalKey = "UseLocalPcAsSource"
        $pool = Get-Pool -PoolId "CopyFileTask" -TaskContext $TaskContext
        $waitStartTime = Get-Date

        while($true)
        {
            $sourceTaskContext = $pool.TryGetObject()
            if ($sourceTaskContext) {
                $sessionLocal = Invoke-PSSession -ComputerName $sourceTaskContext.IpAddress -Credential $sourceTaskContext.Credential
                break;
            }

            # ローカルPCをコピー元として利用
            # 1タスクだけ許可する
            if ($TaskContext.SharedDictionary.TryAdd($atomicUseLocalKey, $TaskContext.IpAddress)) {
                break;
            }

            $elapsedTime = (Get-Date) - $waitStartTime
            Write-Progress "ローカルPCかリレーコピー元PCが使用可能になるのを待機しています 経過時間[$($elapsedTime.ToString("hh\:mm\:ss"))]"

            # 1~5秒待機
            # 複数のタスクが同時にチェックするのは避けるため待ち時間を散らす
            Start-Sleep -Seconds (Get-Random -Minimum 1 -Maximum 5)
        }
    }

    try {
        CopyTask -LocalPath $LocalPath -RemoteFolder $RemoteFolder -EnableRelayCopy $EnableRelayCopy -EnableCompression $EnableCompression -DeleteCompressedFileRemote $DeleteCompressedFileRemote -TaskContext $TaskContext -sessionLocal $sessionLocal
    }
    finally {
        if ($EnableRelayCopy) {
            # ローカルPC使用中フラグを削除
            if(-not $sessionLocal)
            {
                $dummyFlag = ""
                $TaskContext.SharedDictionary.TryRemove($atomicUseLocalKey, [ref]$dummyFlag)
            }

            # 終了済みTaskContextを返却
            $pool.SetObject($TaskContext)
            if ($sessionLocal) {
                $pool.SetObject($sourceTaskContext)
            }
        }
    }
}

function EndTask()
{
    param(
        [ValidateNotNullOrEmpty()]
        $LocalPath,
        [bool]$DeleteCompressedFileLocal=$false
    )

    if ($DeleteCompressedFileLocal)
    {
        $archivePath = Get-CompressedFilePath -FilePath $LocalPath
        Write-Information "圧縮ファイル削除 $archivePath"
        Remove-Item -Path $archivePath
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
        $sessionLocal
    )


    $isRelayCopy = $EnableRelayCopy -and $sessionLocal
    if ($isRelayCopy)
    {
        Write-Information "リレーコピー: $($sourceTaskContext.IpAddress) をコピー元として使用します"
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
        $sourceFolder = ConvertPathAndConnectUNC -TargetPath $RemoteFolder -SessionLocal $sessionLocal -TaskContext $sourceTaskContext
        $sourcePath = Join-Path $sourceFolder (Split-Path $sourcePath -Leaf)
    }



    $destinationFolder = ConvertPathAndConnectUNC -TargetPath $RemoteFolder -SessionLocal $sessionLocal -TaskContext $TaskContext

    $sessionRemote = New-PSSession -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential
    
    $outputPath = Join-Path $destinationFolder (Split-Path $sourcePath -Leaf)
    $label = "コピー"
    if ($isRelayCopy)
    {
        $label = "リレーコピー($($sourceTaskContext.IpAddress))"
    }
    Write-Information "$($label): $sourcePath -> (Remote)$outputPath"
    Copy-FileToRemote -sourcePath $sourcePath -destinationFolder $destinationFolder -SessionLocal $sessionLocal -SessionRemote $sessionRemote


    # 圧縮してたら解凍
    if ($EnableCompression)
    {
        # フォルダの場合はdestinationFolder下に元のフォルダ名を作成して解凍
        $expandTargetPath = $destinationFolder
        if ( Test-Path $LocalPath -PathType Container )
        {
            $expandTargetPath = Join-Path $destinationFolder (Split-Path $LocalPath -Leaf)
        }

        Expand-7ZipExt -ArchiveFileName $outputPath -TargetPath $expandTargetPath -Session $SessionRemote

        # 圧縮ファイルを削除
        if ($DeleteCompressedFileRemote -and -not $EnableRelayCopy)
        {
            Write-Information "圧縮ファイル削除(Remote) $outputPath"
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
        $SessionLocal,
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
        $AuthenticateUncFunc = {
            param($root, [pscredential]$cred)

            if (-not (Test-Path $root)) {
                $driveName = "Copy-File_TempDrive_$($root -replace '[\\.]', '')"
                New-PSDrive -Name $driveName -PSProvider FileSystem -Root $root -Credential $cred > $null
                if (-not $?) {
                    throw "UNCパスの認証に失敗しました。共有フォルダの設定がされていないかもしれません $root"
                }
            }
        }

        Invoke-ScriptBlock -ScriptBlock $AuthenticateUncFunc -Session $sessionLocal -Arguments $root, $cred
    }

    return $outputPath
}

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
        $SessionRemote
    )

    # リモートPCにディレクトリが存在しない場合、作成
    # Copy-Itemはディレクトリが存在するとその直下にファイルをコピーするが
    # ディレクトリが存在しない場合はファイル名として解釈してしまう
    Invoke-Command -Session $SessionRemote -ScriptBlock {
        if (-not (Test-Path -Path $using:destinationFolder))
        {
            New-Item -ItemType Directory -Path $using:destinationFolder > $null
        }
    }

    $isUnc = ([System.Uri]$destinationFolder).IsUnc
    $copyScriptBlock = {
        param($sourcePath, $destinationFolder)
        Copy-Item -Recurse -Force -Path $sourcePath -Destination $destinationFolder
    }
    if (-not $isUnc)
    {
        $copyScriptBlock = {
            param($sourcePath, $destinationFolder, $sessionRemote)
            Copy-Item -Recurse -Force -Path $sourcePath -Destination $destinationFolder -ToSession $sessionRemote
        }
    }

    Invoke-ScriptBlock -ScriptBlock $copyScriptBlock -Session $SessionLocal -Arguments $sourcePath, $destinationFolder, $SessionRemote
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
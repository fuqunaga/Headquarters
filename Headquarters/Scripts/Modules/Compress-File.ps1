<#
.SYNOPSIS
ファイルをtar.exeで圧縮します

.PARAMETER SourcePath
圧縮するファイルまたディレクトリのパス

.PARAMETER DestinationPath
圧縮ファイルのパス
拡張によって圧縮ファイルの形式が変わります
例: .7z, .zip
#>
function Compress-File()
{
    param
    (
        [ValidateNotNullOrEmpty()]
        $FilePath,
        [ValidateNotNullOrEmpty()]
        $ArchiveFilePath
    )

    . .\Scripts\IncludeScripts\Test-PathExistence.ps1
    Test-PathExistence -Path $FilePath

    $sourceItem = Get-Item $FilePath
    $sourceDir = ""

    if ($sourceItem.PSIsContainer)
    {
        $sourceDir = $sourceItem.Parent.FullName
    }
    else
    {
        $sourceDir = $sourceItem.DirectoryName
    }

    tar.exe -C $sourceDir -a -cf $ArchiveFilePath $( $sourceItem.Name )
}


<#
.SYNOPSIS
ファイルをtar.exeで解凍します

.PARAMETER ArchivePath
解凍するファイルのパス

.PARAMETER DestinationDirectory
解凍先のディレクトリ
存在しなければ自動的に作成します
#>
function Expand-File()
{
    param
    (
        [ValidateNotNullOrEmpty()]
        $ArchiveFilePath,
        [ValidateNotNullOrEmpty()]
        $DestinationDirectory
    )

    #. .\Scripts\IncludeScripts\Test-PathExistance.ps1
    #Test-PathExistance $ArchiveFilePath

    # 解凍先のディレクトリが存在しない場合、作成
    if (-not (Test-Path -Path $DestinationDirectory))
    {
        New-Item -ItemType Directory -Path $DestinationDirectory > $null
    }

    # tarコマンドを使用して解凍
    Write-Output "tar.exe -xf `"$ArchiveFilePath`" -C $DestinationDirectory"
    tar.exe -xf "`"$archiveItem`"" -C "`"$DestinationDirectory`""
}
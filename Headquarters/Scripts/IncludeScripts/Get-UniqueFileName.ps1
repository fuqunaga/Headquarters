<#
.SYNOPSIS
ユニークなファイル名を生成します
同名ファイルが存在する場合、番号を付与し既存ファイルと区別します
#>
function Get-UniqueFileName {
    param (
        [Parameter(Mandatory)]
        [string]$Directory,
        [Parameter(Mandatory)]
        [string]$FileName
    )

    # 拡張子とファイル名を分割
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($FileName)
    $extension = [System.IO.Path]::GetExtension($FileName)

    $newFileName = $FileName
    $counter = 1

    # 同名ファイルが存在する場合、新しい名前を作成
    while (Test-Path (Join-Path $Directory $newFileName)) {
        $newFileName = "{0} ({1}){2}" -f $baseName, $counter, $extension
        $counter++
    }

    return $newFileName
}
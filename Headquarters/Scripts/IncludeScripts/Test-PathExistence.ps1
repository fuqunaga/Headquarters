<#
.SYNOPSIS
ファイルの存在を確認し、存在しない場合はエラーをスローします
#>
function Test-PathExistence {
    param
    (
        [string]$Path
    )

    if (-not (Test-Path -Path $path)) {
        throw "ファイルが見つかりません : [$Path]"
    }
}
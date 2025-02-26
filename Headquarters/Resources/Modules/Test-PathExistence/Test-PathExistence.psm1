<#
.SYNOPSIS
ファイルの存在を確認し、存在しない場合はエラーをスローします

.DESCRIPTION
ファイルの存在を確認し、存在しない場合はエラーをスローします
ValidateScript属性でエラーメッセージがわかりやすくなります
ex: [ValidateScript({Test-PathExistence $_})]
#>
function Test-PathExistence {
    param
    (
        [string]$Path
    )

    if (Test-Path -Path $path) {
        return $true
    }

    throw "ファイルが見つかりません : [$Path]"
}
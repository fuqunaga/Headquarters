<#
.SYNOPSIS
ファイルの存在を確認し、存在しない場合はエラーをスローします

.DESCRIPTION
ファイルの存在を確認し、存在しない場合はエラーをスローします
指定されたパスが存在するかどうかを確認しまファイルの存在を確認し、存在しない場合はエラーをスローします
主に引数のValidateScript属性でエラーメッセージがわかりやすくなります
例: param([ValidateScript({Test-PathExistence $_})] $Path)
#>

function Test-PathExistence 
{
    [CmdletBinding()]
    param
    (
        [string]$Path
    )

    if (Test-Path -Path $path) {
        return $true
    }

    throw "ファイルが見つかりません : [$Path]"
}
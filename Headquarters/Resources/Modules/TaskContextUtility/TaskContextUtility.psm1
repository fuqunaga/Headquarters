<#
.SYNOPSIS
TaskContextを利用するためのユーティリティ
#>

# サブモジュールをインポート
$moduleRoot = $PSScriptRoot
Import-Module "$moduleRoot\Session.psm1" -Force
Import-Module "$moduleRoot\Unc.psm1" -Force
Import-Module "$moduleRoot\Pool.psm1" -Force
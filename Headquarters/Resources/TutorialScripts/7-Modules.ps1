<#
.SYNOPSIS
PowerShellモジュールについて

.DESCRIPTION
PowerShellモジュールを利用することでスクリプトを再利用することができます
Headquartersは$env:PSModulePathに以下のディレクトリを追加して実行します
- '.\HeadquartersData\Modules': Headquarters同梱のモジュール
- '.\HeadquartersData\Profile\Scripts\Modules': プロファイルに追加できるモジュール（デフォルトでは空）
#>


$targetFolder = Resolve-Path ".\HeadquartersData\Modules"
$modules = Get-Module -ListAvailable


# 指定フォルダ以下のモジュールのみをフィルタリングして出力
foreach ($module in $modules) {
    if ($module.Path -like "$targetFolder*") {
        Write-Host "Module Name: $($module.Name)
Path: $($module.Path)
        "
    }
}

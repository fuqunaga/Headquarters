<#
.SYNOPSIS
パラメーターのUIを確認するためのスクリプトです

.DESCRIPTION
パラメーターのAttributeなどでUIの表示を変更することができます
UIはヘルプ記述順になります
ヘルプに記述のないパラメーターは記述のあるパラメーターの後に表示されます

.PARAMETER DefaultValueParameter
デフォルト値
param(
    $DefaultValueParameter = "Default Value"
)

.PARAMETER ComboBoxParameter
コンボボックス
param(
    [ValidateSet("Option1", "Option2", "Option3")]
    $ComboBoxParameter
)

.PARAMETER PathWithFileOpenDialogParameter
ファイル選択ダイアログ付きパス
param(
    [Headquarters.Path()]
    $PathWithFileOpenDialogParameter
)
#>


param(
    $NoHelpParameter,
    $DefaultValueParameter = "Default Value",
    [ValidateSet("Option1", "Option2", "Option3")]
    $ComboBoxParameter,
    [Headquarters.Path()]
    $PathWithFileOpenDialogParameter
)

Write-Host "NoHelpParameter: [$NoHelpParameter]
DefaultValueParameter: [$DefaultValueParameter]
ComboBoxParameter: [$ComboBoxParameter]
PathWithFileOpenDialogParameter: [$PathWithFileOpenDialogParameter]"




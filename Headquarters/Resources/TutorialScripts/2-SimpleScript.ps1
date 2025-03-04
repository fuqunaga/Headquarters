<#
.SYNOPSIS
シンプルなスクリプト例です

.DESCRIPTION
スクリプトはIP Listで選択されたIPごとに並列して実行されます
UIからパラメータの値を入力し、右上の実行ボタン（▶️）で実行するのが基本的な流れです

.PARAMETER Parameter
パラメーター
スクリプトのパラメーターが自動的にUIに反映されます
IP Listに同名のパラメーターがある場合、IP Listの値が優先され各IPごとに異なる値を指定できます
#>


param(
    $Parameter = "This is a parameter"
)

Write-Host "Parameter: $Parameter"
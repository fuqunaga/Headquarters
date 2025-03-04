<#
.SYNOPSIS
この項目は.SYNOPSISです
スクリプトのコマンドベースのヘルプの反映のされかたを確認するためのスクリプトです
スクリプト選択画面と、選択後のスクリプト画面の上部に表示されます

.DESCRIPTION
この項目は.DESCRIPTIONです
スクリプト選択後の画面上部に折りたたまれた状態で表示されます
日本語を表示する場合はBOM付きUTF-8で保存してください

.PARAMETER Parameter
この項目は.PARAMETER Parameterの１行目です
この項目は.PARAMETER Parameterの２行目です
１行目のみ強調表示されます
#>


param(
    $Parameter
)

Write-Host "Parameter: [$Parameter]"
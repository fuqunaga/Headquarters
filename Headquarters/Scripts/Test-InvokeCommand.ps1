<#
.SYNOPSIS
リモートPC上でコマンドを実行できるかテストします

.PARAMETER RemotePath
リモートPC上のファイルパス
このパスが存在するか確認するコマンドを実行します
#>

param($Session, $RemotePath)

Invoke-Command -Session $session -ScriptBlock { 
    "hostname: $(hostname)"
    "Test-Path [$using:RemotePath]: $(Test-Path $using:RemotePath)"
}
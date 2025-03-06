<#
.SYNOPSIS
リモートPC上でコマンドを実行できるかテストします

.PARAMETER RemotePath
リモートPC上のファイルパス
このパスが存在するか確認するコマンドを実行します
#>

param($RemotePath, $TaskContext)

Invoke-Command -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential -ScriptBlock { 
    "hostname: $(hostname)"
    "Test-Path [$using:RemotePath]: $(Test-Path $using:RemotePath)"
}
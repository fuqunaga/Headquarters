<#
.SYNOPSIS
リモートPC上のプロセスを停止します

.PARAMETER ProcessName
停止するプロセス名
#>

param($Session, $ProcessName)

Invoke-Command $Session -ScriptBlock {
    Stop-Process -Name $using:ProcessName -Force
}
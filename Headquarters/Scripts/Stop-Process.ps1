<#
.SYNOPSIS
リモートPC上のプロセスを停止します

.PARAMETER ProcessName
停止するプロセス名
#>

param($ProcessName, $TaskContext)

Invoke-Command -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential -ScriptBlock {
    Stop-Process -Name $using:ProcessName -Force
}
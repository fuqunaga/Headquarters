<#
.SYNOPSIS
リモートPC上のアプリケーションを実行します

.PARAMETER FilePath
開始するアプリケーションのパス
#>

param(
    [ValidateNotNullOrEmpty()]
    $FilePath,
    $TaskContext
)

Invoke-Command -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential -ScriptBlock {
    if (!(Test-Path $using:FilePath)) {
        Write-Error "ファイルが見つかりません：[$using:FilePath]"
        return
    }

    $taskName = "Start-Process_TempTask"
    $action = New-ScheduledTaskAction -Execute $using:FilePath

    Register-ScheduledTask -TaskName $taskName -Action $action -Force > $null
    Start-ScheduledTask -TaskName $taskName
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}
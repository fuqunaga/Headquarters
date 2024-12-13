<#
.SYNOPSIS
アプリケーションを実行します

.PARAMETER FilePath
開始するアプリケーションのパス
#>

param(
    [ValidateNotNullOrEmpty()]
    $FilePath,
    $Session
)


Invoke-Command $Session -ScriptBlock {
    if (!(Test-Path $using:FilePath)) {
        Write-Error "ファイル[$using:FilePath]が見つかりません。"
        return
    }

    $taskName = "Start-Process_TempTask"
    $action = New-ScheduledTaskAction -Execute $using:FilePath

    Register-ScheduledTask -TaskName $taskName -Action $action > $null
    Start-ScheduledTask -TaskName $taskName
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}
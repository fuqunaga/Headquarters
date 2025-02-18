<#
.SYNOPSIS
リモートPC上のプロセスを停止します

.PARAMETER ProcessName
停止するプロセス名

.PARAMETER Force
プロセスを強制停止する
チェックしない場合はプロセス自身が終了処理を行うことができますが、フリーズしている場合など確実に停止しないケースがあり得ます
#>

param(
    [ValidateNotNullOrEmpty()]
    $ProcessName,
    [switch]$Force = $True,
    $TaskContext)

Invoke-Command -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential -ScriptBlock {
    # 引数のプロセス名が存在するか確認
    if (!(Get-Process -Name $using:ProcessName -ErrorAction SilentlyContinue)) {
        Write-Error "プロセス[$using:ProcessName]が見つかりません"
        return
    }

    if ($using:Force) {
        Stop-Process -Name $using:ProcessName -Force
        return
    }


    # taskkill /F なしでWM_CLOSEメッセージが送信されるらしい
    # ただしリモートプロセスに対しては /F がないと動作しないっぽいので
    # タスクスケジューラでローカルプロセスとして実行する

    # $ProcessNameの末尾に.exeがない場合は追加
    $exeName = $using:ProcessName

    if ($exeName -notlike "*.exe")
    {
        $exeName += ".exe"
    }

    $taskName = "Stop-Process_TempTask"
    $action = New-ScheduledTaskAction -Execute "taskkill" -Argument "/IM $exeName"

    Register-ScheduledTask -TaskName $taskName -Action $action > $null
    Start-ScheduledTask -TaskName $taskName
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false

    # 3秒待機
    Start-Sleep -Seconds 3
    
    # プロセスが停止したか確認
    if (Get-Process -Name $using:ProcessName -ErrorAction SilentlyContinue) {
        Write-Error "停止命令後にプロセス[$using:ProcessName]が見つかりました"
    }
}
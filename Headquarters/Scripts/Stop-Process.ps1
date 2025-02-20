<#
.SYNOPSIS
リモートPC上のプロセスを停止します

.PARAMETER ProcessName
停止するプロセス名

.PARAMETER Force
プロセスを強制停止する
ONにすると、プロセスを確実に停止できます。
OFFにすると、プロセスは一般的な終了処理を行うことができ、
終了時にログファイルを出力する場合などに有用です
ただし、フリーズしている場合や確認ダイアログが表示される場合など、
確実に終了しないことがあります
#>

param(
    [ValidateNotNullOrEmpty()]
    $ProcessName,
    [switch]$Force = $True,
    $TaskContext)

Invoke-Command -ComputerName $TaskContext.IpAddress -Credential $TaskContext.Credential -ScriptBlock {
    # 引数のプロセス名が存在するか確認
    if (!(Get-Process -Name $using:ProcessName -ErrorAction SilentlyContinue)) {
        Write-Output "プロセスが見つかりません：[$using:ProcessName]"
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

    # TODO:一瞬ウィンドウが表示されるのを抑制したいがいい方法が見つからず
    Register-ScheduledTask -TaskName $taskName -Action $action -Force > $null
    Start-ScheduledTask -TaskName $taskName
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}
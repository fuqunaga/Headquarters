<#
.SYNOPSIS
リモートPC上のプロセスを停止します

.PARAMETER ProcessName
停止するプロセス名

.PARAMETER Force
プロセスを強制停止する
ONにするとプロセスを確実に停止できます
OFFにするとプロセスは一般的な終了処理を行うことができ、
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
        Write-Host "プロセスが見つかりません：[$using:ProcessName]"
        return
    }

    if ($using:Force) {
        Stop-Process -Name $using:ProcessName -Force
        return
    }


    # taskkill /F なしでWM_CLOSEメッセージが送信されるらしい
    # PowerShellからの実行は/Fがないと動作しない（対話的ではないから？）ので
    # タスクスケジューラでローカルプロセスとして実行する
    # 
    # ダメだった他の方法
    # - $process.CloseMainWindow() も対話的でないとダメそう
    # - Win32APIでSendMessage() は対象が確認ダイアログを出した場合処理が返ってこない（ユーザーがダイアログを操作してもダメだった）

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
param($session, $processName)

Invoke-Command $session -ScriptBlock {
    param($processName)
    Stop-Process -Name $processName -Force
} -ArgumentList ($processName)
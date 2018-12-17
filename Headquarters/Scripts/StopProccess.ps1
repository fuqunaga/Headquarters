param($session, $process)

Invoke-Command $session -ScriptBlock {
    param($process)
    Stop-Process -Name $process -Force
} -ArgumentList ($process)
param($session)

Invoke-Command $session -ScriptBlock {
   shutdown /f /s
}
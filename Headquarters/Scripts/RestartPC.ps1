param($session)

Invoke-Command $session -ScriptBlock {
   shutdown /r
}
param($session, $path)

Invoke-Command -Session $session -ScriptBlock { 
    param($path)
    hostname
    Write-Output "Is path[$path] Exist? "
    Test-Path $path
} -ArgumentList $path
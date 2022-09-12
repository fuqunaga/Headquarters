param($session,$localPath,$remotePath,$processName)


Invoke-Command $session -ScriptBlock {
    param($remotePath, $processName) 
	$exeInstance = Get-Process $processName -ErrorAction SilentlyContinue
	if ($exeInstance) 
	{
		Stop-Process -Name $processName -Force
		Sleep 2
	}
} -ArgumentList ($remotePath, $processName)
Copy-Item -ToSession $session -Recurse -Force -Path $localPath -Destination $remotePath
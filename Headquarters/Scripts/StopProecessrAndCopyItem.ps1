param($session,$localPath,$remotePath,$processName)


Invoke-Command $session -ScriptBlock {
    param($remotePath, $processName) 
	$exeInstance = Get-Process $processName -ErrorAction SilentlyContinue
	if ($exeInstance) 
	{
		Stop-Process -Name $processName -Force
		Sleep 2
	}
	if(Test-Path $remotePath)
	{
		Remove-Item $remotePath -Recurse
	}
} -ArgumentList ($remotePath, $processName)
Copy-Item -Recurse -ToSession $session -Path $localPath -Destination $remotePath
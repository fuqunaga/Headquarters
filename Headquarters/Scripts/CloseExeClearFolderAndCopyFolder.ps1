param($session,$localPath,$remotePath,$exeName)


Invoke-Command $session -ScriptBlock {
    param($remotePath, $exeName) 
	$exeInstance = Get-Process $exeName -ErrorAction SilentlyContinue
	if ($exeInstance) 
	{
		Stop-Process -Name $exeName -Force
		Sleep 2
	}
	if(Test-Path $remotePath)
	{
		Remove-Item $remotePath -Recurse
	}
} -ArgumentList ($remotePath, $exeName)
Copy-Item -Recurse -ToSession $session -Path $localPath -Destination $remotePath
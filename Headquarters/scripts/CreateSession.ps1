   $ps = ConvertTo-SecureString "Pass123" -AsPlainText -Force
   $cred = New-Object -TypeName System.Management.Automation.PSCredential ("teamlab", $ps)
   $session = New-PSSession -ComputerName $ip -Cred $cred
   New-PSSession -ComputerName $ComputerName -Cred $cred
   $session = New-PSSession -ComputerName $ip -Cred $cred
param($session,$localPath,$remotePath)

Copy-Item -ToSession $session -Path $localPath -Destination $remotePath
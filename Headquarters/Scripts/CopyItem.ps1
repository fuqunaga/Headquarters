param($session,$localPath,$remotePath)

Copy-Item -ToSession $session -Recurse -Force -Path $localPath -Destination $remotePath
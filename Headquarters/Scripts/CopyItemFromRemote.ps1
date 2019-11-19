param($session,$remotePath,$localPath)

# Check if localPath is a folder, create if not exist
if (![System.IO.Path]::GetExtension($localPath))
{
    if (!(Test-Path $localPath))
    {
        New-Item $localPath -ItemType Directory
    }
}

Copy-Item -FromSession $session -Recurse -Force -Path $remotePath -Destination $localPath
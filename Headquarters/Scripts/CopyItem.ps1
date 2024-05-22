param($session,$localPath,$remotePath)

$remotePath = $remotePath.Replace("`$(IP)", $ip)
$remotelPathInfo = [System.Uri]$remotePath

if ($remotelPathInfo.IsUnc)
{
    Copy-Item -Recurse -Force -Path $localPath -Destination $remotePath
}
else
{
    Copy-Item -ToSession $session -Recurse -Force -Path $localPath -Destination $remotePath
}
param($ip, $sourceDir, $destinationDir, $file, $robocopyOptions)

$src = $sourceDir.Replace("`$IP", $ip)
$dst = $destinationDir.Replace("`$IP", $ip)

$subdirOption = "/s"
if ( [string]::IsNullOrEmpty($file) )
{
    $subdirOption = "/e"
} 

robocopy $src $dst $file $subdirOption /b /r:1 /compress /njh /njs /nfl /ndl /np $robocopyOptions
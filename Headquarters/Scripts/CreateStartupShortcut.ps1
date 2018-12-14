param($session, $shortcutName, $targetPath)

Invoke-Command $session -ScriptBlock {
     param($shortcutName, $targetPath)
      $item = Get-Item $targetPath 
      $WSH = New-Object -ComObject 'WScript.Shell'
      $lnk = $WSH.CreateShortcut("C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup\$shortcutName.lnk")
      $lnk.TargetPath = $item.FullName
      $lnk.WorkingDirectory = $item.DirectoryName
      $lnk.Save()
} -ArgumentList ($shortcutName, $targetPath)
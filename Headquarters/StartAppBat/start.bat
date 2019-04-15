@echo off
For /f "tokens=1-3 delims=/ " %%a in ('date /t') do (set mydate=%%a-%%b-%%c)
For /f "tokens=1-2 delims=/:" %%a in ('time /t') do (set mytime=%%a%%b)
rem echo %mydate%_%mytime%

set userName=YourWindowsUserName
set password=YourWindowsPassword
set workingDir=C:\ExeFolderInRemoteMachine
set exePath=C:\ExeFolderInRemoteMachine\ExeToRun.exe
rem log path is case sencitive
set args=-logfile Log\%mydate%_%mytime%.log

rem Remove following comment to run application
rem .\PsExec64.exe -n 3 -d -i -h -w %workingDir% \\192.168.17.128 -u %userName% -p %password% %exePath% %args%
rem change remote.txt for list of remote machine ip address
.\PsExec64.exe -n 3 -d -i -h -w %workingDir% @remote.txt -u %userName% -p %password% %exePath% %args%
pause

:: Run this if you did not unblock the zip file before extracting.
cd /d "%~dp0"
powershell -command Get-ChildItem ^| Unblock-File

@echo off
echo when no error:
.\ExeWithReturnCode.exe
echo %errorlevel%

echo when error:
.\ExeWithReturnCode shitty args breaking stuff oh no
echo %errorlevel%

echo Returning ERRORLEVEL to the caller!
exit %errorlevel%
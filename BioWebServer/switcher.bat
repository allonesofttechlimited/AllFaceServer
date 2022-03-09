@echo off
SETLOCAL EnableDelayedExpansion

pushd %~dp0
set script_dir=%CD%

set platforms_count=0
set availablePlatforms=
set selected_platform=
set arg1=%~1
for /d %%D in (%script_dir%\*) do (
	set dir_path=%%~D
	if exist !dir_path!\bin\ (
	    if exist !dir_path!\lib\ (
		    set dir_name=%%~nD
		    call set "availablePlatforms=%%availablePlatforms%%, !dir_name!"
		    set /A platforms_count+=1
		)
	)
)


if "%platforms_count%" == "0" (
    echo Not found platforms!
	goto out
)

echo.
echo "Installed FaceSDK platforms:"

set i=0
for %%a in (%availablePlatforms%) do (
	echo !i!. %%~a
	set /A i+=1
)
echo.

if "%platforms_count%" == "1" (
	set index=0
) else (
	set /p index="Select platform (enter index): "
)
set /a index=%index% + 0

set i=0
for %%a in (%availablePlatforms%) do (
	if !i! EQU  !index! (
		set selected_platform=%%a
		call :make_symlinks !selected_platform!
		goto break
	)
	set /A i+=1
)
:break

if not defined selected_platform ECHO Wrong index!
goto out


:make_symlinks
	set selected_platform=%~1
	if "%selected_platform%" NEQ "" (
		if exist %script_dir%\bin\ (
			if exist %script_dir%\lib\ (
				if "%arg1%" == "auto" (
					ECHO Auto mode. Folders found!
					goto break2
				)
			)
		)

		set answer=yes
		if "%arg1%" == "auto" (
			ECHO .
		) else (
			set /p answer="Operation will delete your changes in bin and lib folders... Continue? (yes/no) "
		)

		if "!answer!" == "yes" (
			rmdir /Q /S %script_dir%\bin 2> nul
			rmdir /Q /S %script_dir%\lib 2> nul
			rmdir /Q /S %script_dir%\apk 2> nul
			xcopy /S /E %script_dir%\%selected_platform%\bin\*.* %script_dir%\bin\*.* > nul
			xcopy /S /E %script_dir%\%selected_platform%\lib\*.* %script_dir%\lib\*.* > nul
			if exist %script_dir%\%selected_platform%\apk\ (
			    xcopy /S /E %script_dir%\%selected_platform%\apk\*.* %script_dir%\apk\*.* > nul
			)
			REM rmdir %script_dir%\bin 2> nul
			REM rmdir %script_dir%\lib 2> nul
			REM mklink /J %script_dir%\bin %script_dir%\%selected_platform%\bin > nul
			REM mklink /J %script_dir%\lib %script_dir%\%selected_platform%\lib > nul
			echo Success. Selected platform: !selected_platform!
		)
	)
	:break2
EXIT /B 0

:out

if "%arg1%" NEQ "auto" (
	pause
)

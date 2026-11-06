@echo off
chcp 65001 >nul
setlocal
rem ============================================================
rem  一键：导入资源 + 校验 + 构建新候选 + 把桌面快捷方式指向新版本
rem  双击运行即可（不需要管理员权限）。每一步的日志都在 Logs\ 下。
rem ============================================================
set "ED=C:\Program Files\Tuanjie\Hub\Editor\2022.3.62t16\Editor\Tuanjie.exe"
set "PROJ=%USERPROFILE%\Duzhe"
cd /d "%PROJ%"
if not exist "%ED%" (
  echo [FAIL] 找不到编辑器: %ED%
  pause
  exit /b 1
)

echo ============================================================
echo  [1/4] 导入资源并装配场景（阴间背景/船/漩涡/任务物 + R12 + R06 + UI 皮肤）
echo ============================================================
"%ED%" -batchmode -quit -projectPath "%PROJ%" -logFile "Logs\c10c_import.log" -executeMethod Duzhe.EditorTools.ProjectSetupUtility.RunFromCommandLine
echo   editor exit code = %errorlevel%
findstr /C:"DZ_VALIDATION" "Logs\duzhe_validation.txt"

echo ============================================================
echo  [2/4] 冒烟测试（含 BG01/PRP01/PRP02/IT01/IT02 + R12 + R06 断言）
echo ============================================================
"%ED%" -batchmode -quit -projectPath "%PROJ%" -logFile "Logs\c10c_smoke.log" -executeMethod Duzhe.EditorTools.DevSmokeTest.RunFromCommandLine
echo   editor exit code = %errorlevel%
findstr /C:"DZ_SMOKE" /C:"R12:" "Logs\duzhe_smoke.txt"

echo ============================================================
echo  [3/4] 构建新候选（不覆盖旧版本）
echo ============================================================
"%ED%" -batchmode -quit -projectPath "%PROJ%" -logFile "Logs\c10c_build.log" -executeMethod Duzhe.EditorTools.DuzheBuildTools.BuildWindowsFromCommandLine
echo   editor exit code = %errorlevel%
findstr /C:"Build result" /C:"Build output" /C:"Build id" "Logs\duzhe_build.txt"

echo ============================================================
echo  [4/4] 把桌面快捷方式指向最新候选版本
echo ============================================================
powershell -NoProfile -ExecutionPolicy Bypass -Command "$root = Join-Path $env:USERPROFILE 'Duzhe\Builds\Candidates'; $exe = Get-ChildItem -Path $root -Recurse -Filter Duzhe.exe -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1; if ($null -eq $exe) { Write-Host '  [FAIL] no Duzhe.exe under Builds\Candidates'; exit 1 }; Write-Host ('  newest build: ' + $exe.FullName); $desk = [Environment]::GetFolderPath('Desktop'); $lnk = Get-ChildItem -Path $desk -Filter '*Duzhe*.lnk' -File -ErrorAction SilentlyContinue | Select-Object -First 1; if ($null -eq $lnk) { $lnkPath = Join-Path $desk 'Duzhe.lnk' } else { $lnkPath = $lnk.FullName }; $ws = New-Object -ComObject WScript.Shell; $sc = $ws.CreateShortcut($lnkPath); $sc.TargetPath = $exe.FullName; $sc.WorkingDirectory = $exe.DirectoryName; $sc.IconLocation = $exe.FullName + ',0'; $sc.Save(); Write-Host ('  OK  shortcut: ' + $lnkPath); Write-Host ('      target  : ' + $exe.FullName)"

echo.
echo 完成。请把上面四段输出（或 Logs\duzhe_validation.txt / duzhe_smoke.txt / duzhe_build.txt）发回。
pause

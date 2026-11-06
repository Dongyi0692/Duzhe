$ErrorActionPreference = 'Stop'
$projectRoot = 'C:\Users\東壹\Duzhe'
$editorPath = 'C:\Program Files\Tuanjie\Hub\Editor\2022.3.62t16\Editor\Tuanjie.exe'
$summary = Join-Path $projectRoot 'Logs\codex_pipeline.txt'
$steps = @(



    @('CH04PlayTest','ch04_playtest.txt','DZ_CH04_PLAY=PASS','play'),
    @('CH05PlayTest','ch05_playtest.txt','DZ_CH05_PLAY=PASS','play'),
    @('ChapterFlowPlayTest','chapter_flow_playtest.txt','DZ_CHAPTER_FLOW=PASS','play')
)
Set-Content -LiteralPath $summary -Value ('Started ' + (Get-Date -Format s))
foreach ($step in $steps) {
    $name=$step[0]; $report=Join-Path $projectRoot ('Logs\'+$step[1]); $log=Join-Path $projectRoot ('Logs\codex_'+$name+'.log')
    $started=Get-Date
    Add-Content -LiteralPath $summary -Value ('RUN '+$name+' '+$started.ToString('s'))
    $modeArgs = if ($step[3] -eq 'batch') { '-batchmode -quit ' } else { '' }
    $argsText = $modeArgs + '-projectPath "'+$projectRoot+'" -logFile "'+$log+'" -executeMethod Duzhe.EditorTools.'+$name+'.RunFromCommandLine'
    $proc=Start-Process -FilePath $editorPath -ArgumentList $argsText -WindowStyle Hidden -PassThru
    if (-not $proc.WaitForExit(720000)) { Add-Content -LiteralPath $summary -Value ('TIMEOUT '+$name); throw 'Editor check timed out; inspect the running editor before retrying.' }
    $proc.Refresh()
    if ($proc.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $report)) { Add-Content -LiteralPath $summary -Value ('FAIL '+$name+' exit='+$proc.ExitCode); exit 1 }
    $item=Get-Item -LiteralPath $report
    if ($item.LastWriteTime -lt $started -or -not (Select-String -LiteralPath $report -Pattern $step[2] -SimpleMatch -Quiet)) { Add-Content -LiteralPath $summary -Value ('FAIL '+$name+' (report stale or not PASS)'); exit 1 }
    Add-Content -LiteralPath $summary -Value ('PASS '+$name+' '+(Get-Date -Format s))
}
Add-Content -LiteralPath $summary -Value 'ALL_CHECKS_PASS'
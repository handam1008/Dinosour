param([string]$Run = 'Run1', [string]$Project = (Get-Location).Path, [string]$Build = 'Builds/Flow/Game.exe')
$ErrorActionPreference = 'Stop'
$testRoot = Join-Path $Project ('Logs/Flow/' + $Run)
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$gamePath = Join-Path $Project $Build
$seq = @{ host = 0; client = 0 }
$checks = [Collections.Generic.List[string]]::new()
function Snapshot($peer) {
    try { return Get-Content -LiteralPath "$testRoot/$peer.json" -Raw | ConvertFrom-Json }
    catch { return $null }
}
function Send($peer, $op, $value = 0) {
    $seq[$peer]++
    $json = @{ seq = $seq[$peer]; op = $op; value = $value } | ConvertTo-Json -Compress
    [IO.File]::WriteAllText("$testRoot/$peer.cmd.json", $json)
}
function Await($description, [scriptblock]$condition, $seconds = 25) {
    $until = [DateTime]::UtcNow.AddSeconds($seconds)
    do {
        $script:h = Snapshot 'host'
        $script:c = Snapshot 'client'
        if ($script:h.error) { throw "Host error: $($script:h.error)" }
        if ($script:c.error) { throw "Client error: $($script:c.error)" }
        if ($script:h -and $script:c -and (& $condition)) { return }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $until)
    throw "Timeout: $description; host=$($script:h.phase)/$($script:h.set)/$($script:h.round), client=$($script:c.phase)/$($script:c.set)/$($script:c.round)"
}
function Check($condition, $message) {
    if (-not $condition) { throw $message }
    $checks.Add("PASS $message")
    [IO.File]::WriteAllLines("$testRoot/checks.txt", $checks)
}
function Own($snapshot) { return @($snapshot.players | Where-Object owner)[0] }
function ThreeChoices($peer) {
    $offer = (Own (Snapshot $peer)).offer
    return $offer.x -ge 0 -and $offer.y -ge 0 -and $offer.z -ge 0 -and (@($offer.x,$offer.y,$offer.z) | Select-Object -Unique).Count -eq 3
}
$hostProcess = Start-Process -FilePath $gamePath -WorkingDirectory $Project -ArgumentList @('--net-mode','host','--net-job','Witch','--net-port','7791','--net-probe',"$testRoot/host",'--net-name','Host','-logFile',"$testRoot/host.log",'-screen-fullscreen','0','-screen-width','1280','-screen-height','720') -WindowStyle Hidden -PassThru
$clientProcess = Start-Process -FilePath $gamePath -WorkingDirectory $Project -ArgumentList @('--net-mode','client','--net-job','Magician','--net-port','7791','--net-probe',"$testRoot/client",'--net-name','Client','-logFile',"$testRoot/client.log",'-screen-fullscreen','0','-screen-width','1280','-screen-height','720') -WindowStyle Hidden -PassThru
@{ host = $hostProcess.Id; client = $clientProcess.Id } | ConvertTo-Json | Set-Content "$testRoot/pids.json"
try {
    Await 'initial common draft' { $h.phase -eq 'Draft' -and $c.phase -eq 'Draft' -and (Own $h).draftView -and (Own $c).draftView } 60
    Check ((Own $h).augments.Count -eq 0 -and (Own $c).augments.Count -eq 0) 'No random job granted at match start'
    Check ((ThreeChoices host) -and (ThreeChoices client)) 'Three distinct initial common choices on both peers'
    Check ((Own $h).seconds -gt 8 -and (Own $c).seconds -gt 8) 'Both initial choices start with ten seconds'
    Send host choose 8
    Start-Sleep -Milliseconds 350
    Check ((Own (Snapshot host)).augments.Count -eq 0) 'Invalid draft slot rejected'
    Send host choose 0
    Await 'waiting for opponent' { $h.draftStatus -and $h.draftLabel -eq '상대가 증강을 고르는 중...' }
    Check ($c.draftLabel -eq '공용 증강 선택') 'Choosing peer sees common choice label'
    Check ($h.draftTime -eq $c.draftTime) 'Waiting and choosing peers display the same timer'
    Await 'first combat' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' }
    Check ((Own $h).augments.Count -eq 1 -and (Own $c).augments.Count -eq 1) 'Initial timeout selects exactly one common'
    $counts = @{ host = 1; client = 1 }
    $losses = @{ host = 0; client = 0 }
    for ($set = 1; $set -le 7; $set++) {
        $setWinner = if ($set % 2 -eq 1) { 'host' } else { 'client' }
        $roundWinners = if ($set -eq 1) { @('host','client','host') } else { @($setWinner, $setWinner) }
        $firstMarks = 0
        $secondMarks = 0
        for ($index = 0; $index -lt $roundWinners.Count; $index++) {
            $round = $index + 1
            Await 'combat ready' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' -and $h.set -eq $set -and $c.set -eq $set -and $h.round -eq $round }
            Check ((Own $h).augments.Count -eq $counts.host -and (Own $c).augments.Count -eq $counts.client) "Set $set round $round retains augments"
            Check (-not $h.wipe -and -not $c.wipe -and -not $h.draftStatus -and -not $c.draftStatus) "Set $set round $round enters combat without draft or banner"
            Check ([Math]::Abs((Own $h).hp - (Own $h).max) -lt 0.01 -and [Math]::Abs((Own $c).hp - (Own $c).max) -lt 0.01) "Set $set round $round starts at full health"
            Check ($h.shots.Count -eq 0 -and $c.shots.Count -eq 0) "Set $set round $round has no old projectiles"
            $winner = $roundWinners[$index]
            if ($winner -eq 'host') { $firstMarks = $firstMarks -bor (1 -shl $index); Send host remote 100000 }
            else { $secondMarks = $secondMarks -bor (1 -shl $index); Send host damage 100000 }
            Await 'round scored' { $h.phase -in @('RoundEnd','SetEnd') -and $c.phase -eq $h.phase -and $h.firstMarks -eq $firstMarks -and $c.firstMarks -eq $firstMarks -and $h.secondMarks -eq $secondMarks -and $c.secondMarks -eq $secondMarks }
            Check ($h.firstSets -eq $c.firstSets -and $h.secondSets -eq $c.secondSets) "Set $set round $round scores synchronized"
            Check ($h.wipe -and $c.wipe -and $h.wipeTitle -eq $c.wipeTitle -and $h.wipeTitle.Contains('승리!')) "Set $set round $round shows the same winner banner on both peers"
            if ($set -eq 1 -and $round -eq 1) {
                Start-Sleep -Milliseconds 650
                Send host capture
                Send client capture
                Start-Sleep -Milliseconds 300
            }
        }
        Write-Output "Set $set complete: $($h.firstSets):$($h.secondSets)"
        if ($set -eq 7) { break }
        $loser = if ($setWinner -eq 'host') { 'client' } else { 'host' }
        $losses[$loser]++
        Await 'loser draft' { $h.phase -eq 'Draft' -and $c.phase -eq 'Draft' -and $h.set -eq ($set + 1) -and $c.set -eq ($set + 1) -and (Own (Snapshot $loser)).draftView }
        Check (-not (Own (Snapshot $setWinner)).draftView -and (Own (Snapshot $setWinner)).ready) "Set $set winner receives no draft"
        Check ($h.firstMarks -eq 0 -and $h.secondMarks -eq 0 -and $c.firstMarks -eq 0 -and $c.secondMarks -eq 0) "Set $set lamps reset for next set"
        $oldOffer = (Own (Snapshot $loser)).offer | ConvertTo-Json -Compress
        Check (ThreeChoices $loser) "$loser loss $($losses[$loser]) has three common choices"
        Check ((Own (Snapshot $loser)).seconds -gt 8) "$loser gets ten seconds for common selection"
        if ($set -ne 1) { Send $loser choose 0 }
        $counts[$loser]++
        if ($losses[$loser] % 2 -eq 1) {
            Await 'job choice after common' { $s = Snapshot $loser; $s.phase -eq 'Draft' -and (Own $s).draftView -and -not (Own $s).ready -and (Own $s).augments.Count -eq $counts[$loser] -and ((Own $s).offer | ConvertTo-Json -Compress) -ne $oldOffer }
            Check ($h.phase -eq 'Draft' -and $c.phase -eq 'Draft') "$loser loss $($losses[$loser]) waits for separate job choice"
            Check (ThreeChoices $loser) "$loser loss $($losses[$loser]) has three job choices"
            Check ((Own (Snapshot $loser)).seconds -gt 8) "$loser gets a fresh ten seconds for job selection"
            Await 'job label rendered' { (Snapshot $loser).draftLabel -eq '직업 증강 선택' }
            Check ((Snapshot $loser).draftLabel -eq '직업 증강 선택') "$loser sees the job choice label"
            if ($set -ne 1) { Send $loser choose 0 }
            $counts[$loser]++
        }
        Await 'next set combat' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' }
        Check ((Own $h).augments.Count -eq $counts.host -and (Own $c).augments.Count -eq $counts.client) "Personal loss reward $loser/$($losses[$loser]) correct"
    }
    Await 'final result' { $h.phase -eq 'Finished' -and $c.phase -eq 'Finished' }
    Check ($h.firstSets -eq 4 -and $h.secondSets -eq 3 -and $c.firstSets -eq 4 -and $c.secondSets -eq 3) 'Final match ends at 4:3 after seven sets'
    Check ($h.title -eq '승리' -and $c.title -eq '패배' -and -not $h.canResume -and -not $c.canResume) 'Opposite final results and no resume'
    $h | ConvertTo-Json -Depth 12 | Set-Content "$testRoot/final-host.json"
    $c | ConvertTo-Json -Depth 12 | Set-Content "$testRoot/final-client.json"
    Write-Output "PASS $($checks.Count) checks"
}
catch {
    [IO.File]::WriteAllText("$testRoot/failure.txt", $_.ToString())
    throw
}
finally {
    Send host quit
    Send client quit
}

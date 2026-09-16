param([string]$Run = 'Wait1', [string]$Project = (Get-Location).Path, [string]$Build = 'Builds/Wait/Game.exe')
$ErrorActionPreference = 'Stop'
$root = Join-Path $Project ('Logs/Watch/' + $Run)
New-Item -ItemType Directory -Path $root -Force | Out-Null
$seq = @{ host = 0; client = 0 }
$checks = [Collections.Generic.List[string]]::new()
function Read($peer) {
    for ($attempt=0; $attempt -lt 5; $attempt++) {
        try { return Get-Content "$root/$peer.json" -Raw | ConvertFrom-Json } catch { Start-Sleep -Milliseconds 10 }
    }
    return $null
}
function Own($s) { @($s.players | Where-Object owner)[0] }
function Other($s) { @($s.players | Where-Object { -not $_.owner })[0] }
function Send($peer, $op, $value = 0, $x = 0, $y = 0) {
    $seq[$peer]++
    [IO.File]::WriteAllText("$root/$peer.cmd.json", (@{seq=$seq[$peer];op=$op;value=$value;x=$x;y=$y} | ConvertTo-Json -Compress))
}
function Await($label, [scriptblock]$condition, $seconds = 15) {
    $until = [DateTime]::UtcNow.AddSeconds($seconds)
    do {
        $script:h = Read host
        $script:c = Read client
        if ($h.error) { throw $h.error }
        if ($c.error) { throw $c.error }
        if ($h -and $c -and (& $condition)) { return }
        Start-Sleep -Milliseconds 50
    } while ([DateTime]::UtcNow -lt $until)
    throw "Timeout: $label"
}
function Check($condition, $label) {
    if (-not $condition) { throw $label }
    $checks.Add("PASS $label")
    [IO.File]::WriteAllLines("$root/checks.txt", $checks)
}
function SameOffer($a, $b) { ($a | ConvertTo-Json -Compress) -eq ($b | ConvertTo-Json -Compress) }
function Hold($peer, $count, $label) {
    $offer = (Own (Read $peer)).offer
    $until = [DateTime]::UtcNow.AddSeconds(11.5)
    do {
        $state = Read $peer
        if ($state.error) { throw $state.error }
        if ($state -and ($state.phase -ne 'Draft' -or (Own $state).ready -or (Own $state).augments.Count -ne $count)) { throw "Unexpected automatic choice: $label" }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $until)
    Check ((SameOffer $offer (Own $state).offer) -and (Own $state).draftView) "$label remains unchanged beyond the old ten-second limit"
}
function Spectate($chooser, $viewer, $set, $previous) {
    Await 'spectator views' { $h.phase -eq 'Draft' -and $c.phase -eq 'Draft' -and $h.set -eq $set -and (Own (Read $chooser)).draftView -and (Other (Read $viewer)).spectating }
    $a = Read $chooser
    $b = Read $viewer
    Check (-not (Own $a).portraitRight -and (Other $b).portraitRight) "$chooser selects with left portrait and $viewer watches right portrait"
    Check (SameOffer (Own $a).offer (Other $b).watchOffer) "$viewer sees exactly the same common cards"
    Check (-not (Own $b).draftView -and (Own $b).ready) "$viewer has no selectable reward"
    Hold $chooser $previous "$chooser common choice"
    Await 'cards unlocked' { (Own (Read $chooser)).draftUnlocked }
    Send $chooser aim 0 0.3125 0.4815
    Await 'left hover mirrored' { (Own (Read $chooser)).hover -eq 0 -and (Other (Read $viewer)).hover -eq 0 } 3
    Check ((Other (Read $viewer)).hover -eq 0) "$chooser first card hover is mirrored"
    for ($i=1; $i -le 6; $i++) {
        Send $chooser aim 0 (0.3125 + 0.375 * $i / 6) 0.4815
        Start-Sleep -Milliseconds 120
    }
    Await 'right hover and trail' { $script:pointed = Own (Read $chooser); $script:watched = Other (Read $viewer); $pointed.hover -eq 2 -and $watched.hover -eq 2 -and $watched.trail -gt 0 -and [Math]::Abs($pointed.cursor.x - $watched.cursor.x) -lt 0.04 } 3
    Check ([Math]::Abs($pointed.cursor.x - $watched.cursor.x) -lt 0.04) "$viewer receives cursor position and white trail"
    Send $viewer watchclick 0
    Start-Sleep -Milliseconds 200
    Check ((Own (Read $chooser)).augments.Count -eq $previous -and -not (Own (Read $chooser)).ready) "$viewer cannot select the other player's card"
    $position = (Own (Read $viewer)).position | ConvertTo-Json -Compress
    Send $viewer move 0 1 0
    Start-Sleep -Milliseconds 180
    Check (((Own (Read $viewer)).position | ConvertTo-Json -Compress) -eq $position) "$viewer cannot move while watching"
    Send $viewer move 0 0 0
    Send $chooser draftclick 2
    Await 'selected card mirrored' { (Other (Read $viewer)).watchPick -eq 2 } 3
    Check ((Other (Read $viewer)).watchPick -eq 2) "$viewer sees the selected common card"
    Await 'job views' { (Own (Read $chooser)).augments.Count -eq ($previous + 1) -and (Other (Read $viewer)).spectating -and (Other (Read $viewer)).watchPick -eq -1 }
    $a = Read $chooser
    $b = Read $viewer
    Check (SameOffer (Own $a).offer (Other $b).watchOffer) "$viewer sees the next job cards"
    Hold $chooser ($previous + 1) "$chooser job choice"
    Await 'job unlocked' { (Own (Read $chooser)).draftUnlocked }
    Send $chooser aim 0 -0.1 0.5
    Await 'cursor leaves window' { (Own (Read $chooser)).cursor.x -eq -1 -and (Other (Read $viewer)).cursor.x -eq -1 } 3
    Await 'dust fades away' { (Other (Read $viewer)).trail -eq 0 } 3
    Check ((Other (Read $viewer)).trail -eq 0) "$viewer cursor disappears outside the window and its dust fades away"
    $jobOffer = (Own (Read $chooser)).offer.y
    Send $chooser draftclick 1
    Await 'next combat' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' }
    Check ((Own (Read $chooser)).augments.Count -eq ($previous + 2)) "$chooser receives exactly one common and one job augment"
    Check ((Own (Read $chooser)).augments -contains $jobOffer) "$chooser late manual pick is preserved through its animation"
    Check (-not (Other (Read $viewer)).draftView -and -not (Own (Read $chooser)).draftView) 'Both draft views close before combat'
}
$game = Join-Path $Project $Build
foreach ($peer in @('host','client')) {
    $job = if ($peer -eq 'host') { 'Witch' } else { 'Magician' }
    Start-Process -FilePath $game -WorkingDirectory $Project -ArgumentList @('--net-mode',$peer,'--net-job',$job,'--net-port','7794','--net-name',$peer,'--net-probe',"$root/$peer",'-logFile',"$root/$peer.log",'-screen-fullscreen','0','-screen-width','1280','-screen-height','720') -WindowStyle Hidden | Out-Null
}
try {
    Await 'initial draft' { $h.phase -eq 'Draft' -and $c.phase -eq 'Draft' -and (Own $h).draftView -and (Own $c).draftView } 60
    Check (-not (Other $h).draftView -and -not (Other $c).draftView) 'Initial choices are never shown to the opponent'
    Check ((Own $h).watchOffer.x -eq -1 -and (Own $c).watchOffer.x -eq -1 -and (Other $h).watchOffer.x -eq -1 -and (Other $c).watchOffer.x -eq -1) 'Initial card offers are not sent through spectator state'
    Check (-not (Own $h).portraitRight -and -not (Own $c).portraitRight) 'Both players see their own portrait on the left initially'
    Hold host 0 'Initial host choice'
    Check ((Own (Read client)).augments.Count -eq 0 -and -not (Own (Read client)).ready) 'Initial client choice also has no automatic grant'
    Await 'initial cards unlocked' { (Own $h).draftUnlocked -and (Own $c).draftUnlocked }
    Send host draftclick 0
    Await 'initial waiting' { (Own $h).ready -and $h.phase -eq 'Draft' }
    Check (-not (Other $h).draftView) 'Finishing the initial choice does not reveal the opponent choice'
    Hold client 0 'Opponent initial choice after host has selected'
    Check ((Read host).draftLabel -eq '상대가 증강을 고르는 중...' -and (Read host).draftStatus) 'Waiting notice stays visible without a countdown'
    Send client draftclick 0
    Await 'initial combat' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' }
    for ($round=1; $round -le 2; $round++) {
        Send host remote 100000
        if ($round -eq 1) { Await 'round two' { $h.phase -eq 'Playing' -and $h.round -eq 2 } }
    }
    Spectate client host 2 1
    for ($round=1; $round -le 2; $round++) {
        Send host damage 100000
        if ($round -eq 1) { Await 'round two' { $h.phase -eq 'Playing' -and $h.round -eq 2 } }
    }
    Spectate host client 3 1
    $h | ConvertTo-Json -Depth 12 | Set-Content "$root/final-host.json"
    $c | ConvertTo-Json -Depth 12 | Set-Content "$root/final-client.json"
    Write-Output "PASS $($checks.Count) spectator checks"
}
catch {
    $_ | Out-String | Set-Content "$root/failure.txt"
    throw
}
finally {
    Send host quit
    Send client quit
}

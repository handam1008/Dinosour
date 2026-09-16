param([string]$Run = 'Timeout1', [string]$Project = (Get-Location).Path, [string]$Build = 'Builds/Skate/Game.exe')
$ErrorActionPreference = 'Stop'
$root = Join-Path $Project ('Logs/Skate/' + $Run)
New-Item -ItemType Directory -Path $root -Force | Out-Null
$seq = @{ host = 0; client = 0 }
$checks = [Collections.Generic.List[string]]::new()
function Read($peer) {
    try { Get-Content "$root/$peer.json" -Raw | ConvertFrom-Json } catch { $null }
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
function Cards($peer) { $offer=(Own (Read $peer)).offer; @($offer.x,$offer.y,$offer.z) }
$game = Join-Path $Project $Build
foreach ($peer in @('host','client')) {
    $job = if ($peer -eq 'host') { 'Witch' } else { 'Magician' }
    Start-Process -FilePath $game -WorkingDirectory $Project -ArgumentList @('--net-mode',$peer,'--net-job',$job,'--net-port','7796','--net-name',$peer,'--net-probe',"$root/$peer",'-logFile',"$root/$peer.log",'-screen-fullscreen','0','-screen-width','1280','-screen-height','720') -WindowStyle Hidden | Out-Null
}
try {
    Await 'initial draft' { $h.phase -eq 'Draft' -and $c.phase -eq 'Draft' -and (Own $h).draftView -and (Own $c).draftView } 60
    $offers = @{ host = (Cards host); client = (Cards client) }
    Await 'initial automatic selection' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' } 15
    foreach ($peer in @('host','client')) {
        $owned = (Own (Read $peer)).augments
        Check ($owned.Count -eq 1 -and $owned[0] -in $offers[$peer]) "$peer initial timeout grants one offered card and starts combat"
    }
    foreach ($loser in @('client','host')) {
        $op = if ($loser -eq 'client') { 'remote' } else { 'damage' }
        $set = if ($loser -eq 'client') { 2 } else { 3 }
        Send host $op 100000
        Await 'second round' { $h.phase -eq 'Playing' -and $h.round -eq 2 }
        Send host $op 100000
        Await 'loser common draft' { $h.phase -eq 'Draft' -and $h.set -eq $set -and (Own (Read $loser)).draftView }
        $before = @((Own (Read $loser)).augments)
        $common = Cards $loser
        Await 'automatic common then job' { (Own (Read $loser)).augments.Count -eq ($before.Count + 1) -and (Read $loser).draftLabel -eq '직업 증강 선택' } 15
        $grant = @((Own (Read $loser)).augments | Where-Object { $_ -notin $before })
        Check ($grant.Count -eq 1 -and $grant[0] -in $common) "$loser common timeout selects exactly one offered card"
        Check ((Own (Read $loser)).seconds -gt 8) "$loser job draft receives a separate ten seconds"
        $job = Cards $loser
        $afterCommon = @((Own (Read $loser)).augments)
        Await 'automatic job then next combat' { $h.phase -eq 'Playing' -and $c.phase -eq 'Playing' -and $h.set -eq $set } 15
        $grant = @((Own (Read $loser)).augments | Where-Object { $_ -notin $afterCommon })
        Check ($grant.Count -eq 1 -and $grant[0] -in $job) "$loser job timeout selects exactly one offered card and starts combat"
        Check (@($h.players | Where-Object draftView).Count -eq 0 -and @($c.players | Where-Object draftView).Count -eq 0) 'Automatic selection closes both local and spectator screens'
    }
    Write-Output "PASS $($checks.Count) automatic selection checks"
} finally {
    Send host quit
    Send client quit
}

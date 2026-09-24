param([string]$HostJob='Swordsman',[string]$ClientJob='Assassin',[string]$Run='AudioMelee24',[int]$Port=7792)
$ErrorActionPreference='Stop'
$root="D:/unity_project/Mushrooms/Logs/Jobs24/$Run"
$checks=[Collections.Generic.List[string]]::new()
function Check($value,$label){if(-not $value){throw $label};$checks.Add("PASS $label");[IO.File]::WriteAllLines("$root/audio-checks.txt",$checks)}
function ReadPeer($side){Get-Content "$root/$side.json" -Raw | ConvertFrom-Json}
function Count($state,$name){$items=@($state.audio.cues|Where-Object {$_.name -eq $name});if($items.Count -eq 0){return 0};return $items[0].count}
try{
    & "$PSScriptRoot/../Jobs~/Start.ps1" -HostJob $HostJob -ClientJob $ClientJob -Run $Run -Port $Port | Out-Null
    . "$PSScriptRoot/../Jobs~/Probe.ps1" -Root $root -Peer host
    Send warp 0 -6.6 -3.16; Send warp 1 -5.1 -3.16
    foreach($Peer in @('host','client')){Send sound 1 1 0.7; Send sound 2 1; Send sound 0}
    $names=@{Swordsman='SwordSwing';Assassin='KnifeSwing';Gunner='GunShot';Gambler='CoinThrow'}
    $jobs=@{host=$HostJob;client=$ClientJob}
    foreach($Peer in @('host','client')){
        $direction=if($Peer -eq 'host'){1}else{-1}
        Send cast 1 $direction 0; Send release 0 $direction 0
        $cue=$names[$jobs[$Peer]]
        Await {(Count (ReadPeer host) $cue) -eq 1 -and (Count (ReadPeer client) $cue) -eq 1} 8
        Check ($true) "$($jobs[$Peer]) attack is heard once by both players"
        Start-Sleep -Milliseconds 300
    }
    Await {(Count (ReadPeer host) 'Impact') -ge 2 -and (Count (ReadPeer host) 'Impact') -eq (Count (ReadPeer client) 'Impact')} 8
    Check ($true) 'Melee or projectile impacts agree on both players'
    foreach($side in @('host','client')){
        $state=ReadPeer $side
        Check ($state.audio.peak -gt 0.001 -and $state.audio.effectsPeak -gt 0.001) "$side produces actual combat output"
        Check ($state.audio.sources -eq 25 -and $state.audio.listeners -eq 1) "$side has a bounded pool and one listener"
    }
    if($HostJob -eq 'Swordsman'){
        foreach($Peer in @('host','client')){Send heal 80}
        $Peer='host';Send parry
        Await {(Count (ReadPeer host) 'Parry') -eq 1 -and (Count (ReadPeer client) 'Parry') -eq 1} 5
        Check ($true) 'Parry activation is heard once by both players'
        $Peer='client';Send cycle 1 0 -1;Send cycle 0 0 -1
        Await {(Count (ReadPeer host) 'KnifeThrow') -eq 1 -and (Count (ReadPeer client) 'KnifeThrow') -eq 1} 5
        Check ($true) 'Guest dagger skill is heard once by both players'
        $Peer='host';Send cycle 1 -1 0;Send cycle 0 -1 0
        Await {(Count (ReadPeer host) 'Dash') -eq 1 -and (Count (ReadPeer client) 'Dash') -eq 1} 5
        Check ($true) 'Host dash is heard once by both players'
    }else{
        $beforeHost=@((ReadPeer host).audio.cues | ForEach-Object {$_.count} | Measure-Object -Sum)[0].Sum
        foreach($Peer in @('host','client')){Send cycle 1 0 1;Send cycle 0 0 1}
        Start-Sleep -Milliseconds 350
        $afterHost=@((ReadPeer host).audio.cues | ForEach-Object {$_.count} | Measure-Object -Sum)[0].Sum
        Check ($beforeHost -eq $afterHost) 'Unimplemented cycle actions produce no false reload sound'
    }
    foreach($side in @('host','client')){
        $state=ReadPeer $side
        Check ((Count $state $names[$HostJob]) -eq 1 -and (Count $state $names[$ClientJob]) -eq 1) "$side never replays predicted attacks"
        Check (-not $state.error) "$side has no runtime exception"
        $state|ConvertTo-Json -Depth 12|Set-Content "$root/audio-$side.json"
    }
    Write-Output "PASS $($checks.Count) audio checks for $HostJob / $ClientJob"
}catch{
    $_|Out-String|Set-Content "$root/audio-failure.txt"
    throw
}finally{
    foreach($Peer in @('client','host')){
        if(Test-Path "$root/$Peer.json"){
            try{Send sound 7;Send quit}catch{Write-Warning $_}
        }
    }
}

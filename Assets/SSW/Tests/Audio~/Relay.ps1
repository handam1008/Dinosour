param([string]$Run='Audit01',[string]$Project=(Get-Location).Path,[int]$CreateTimeout=45,[ValidateSet('Witch','Magician','Swordsman','Assassin','Gambler','Gunner')][string]$HostJob='Witch',[ValidateSet('Witch','Magician','Swordsman','Assassin','Gambler','Gunner')][string]$ClientJob='Magician',[switch]$EditorClient)
if($EditorClient){throw 'Audio validation uses two standalone players so Editor mute cannot affect measurement'}
$ErrorActionPreference='Stop'
$root="$Project/Logs/Relay/$Run"
if(Test-Path "$root/host.json"){throw 'Use a fresh Run name'}
New-Item -ItemType Directory -Path $root -Force | Out-Null
$menuSeq=@{host=0;client=0}; $netSeq=@{host=0;client=0}
$checks=[Collections.Generic.List[string]]::new()
$processes=@{}
$progress=[Collections.Generic.List[object]]::new()
$editorStarted=$false
function Editor($command,[string[]]$arguments=@()){
    $response=& "$env:LOCALAPPDATA/Unity/bin/unity.exe" command $command @arguments --project-path $Project --caller plugin --skill unity-cli --format json | ConvertFrom-Json
    if(-not $response.success){throw ($response|ConvertTo-Json -Depth 6)}
    $value=$response.data.result
    if($value.success -eq $false){throw ($value|ConvertTo-Json -Depth 6)}
    return $value
}
function WriteCommand($path,$json){
    $end=[DateTime]::UtcNow.AddSeconds(2)
    while($true){
        try{[IO.File]::WriteAllText($path,$json);return}
        catch [IO.IOException]{if([DateTime]::UtcNow -gt $end){throw};Start-Sleep -Milliseconds 15}
    }
}
function Snapshot($path){
    for($i=0;$i -lt 25;$i++){
        try{$state=Get-Content $path -Raw|ConvertFrom-Json;if($state){return $state}}catch{}
        Start-Sleep -Milliseconds 10
    }
    return $null
}
function Menu($peer){Snapshot "$root/$peer.menu.json"}
function Read($peer){Snapshot "$root/$peer.json"}
function MenuSend($peer,$op,$target='',$text='',$value=0){
    $menuSeq[$peer]++
    WriteCommand "$root/$peer.menu.cmd.json" (@{seq=$menuSeq[$peer];op=$op;target=$target;text=$text;value=$value}|ConvertTo-Json -Compress)
    $end=[DateTime]::UtcNow.AddSeconds(8)
    while((Menu $peer).seq -lt $menuSeq[$peer]){if([DateTime]::UtcNow -gt $end){throw "Menu command timeout: $peer $op $target"};Start-Sleep -Milliseconds 30}
    if((Menu $peer).error){throw (Menu $peer).error}
}
function Send($peer,$op,$value=0,$x=0,$y=0){
    $netSeq[$peer]++
    WriteCommand "$root/$peer.cmd.json" (@{seq=$netSeq[$peer];op=$op;value=$value;x=$x;y=$y}|ConvertTo-Json -Compress)
    if($op -eq 'quit'){return}
    $end=[DateTime]::UtcNow.AddSeconds(8)
    while((Read $peer).seq -lt $netSeq[$peer]){if([DateTime]::UtcNow -gt $end){throw "Game command timeout: $peer $op"};Start-Sleep -Milliseconds 30}
}
function Await($label,[scriptblock]$condition,$seconds=30){
    $end=[DateTime]::UtcNow.AddSeconds($seconds)
    $next=[DateTime]::UtcNow
    do{
        $script:h=Menu host;$script:c=Menu client
        $script:nh=Read host;$script:nc=Read client
        if([DateTime]::UtcNow -ge $next){
            $progress.Add(@{at=[DateTime]::UtcNow.ToString('o');label=$label;host=@{busy=$h.busy;joined=$h.joined;status=$h.status;server=$nh.server;connected=$nh.connected};client=@{busy=$c.busy;joined=$c.joined;connected=$nc.connected}})
            $progress | ConvertTo-Json -Depth 5 | Set-Content "$root/progress.json"
            $next=[DateTime]::UtcNow.AddSeconds(1)
        }
        if($h.error -or $c.error){throw "Menu error: $($h.error) $($c.error)"}
        if($h -and $c -and (& $condition)){return}
        Start-Sleep -Milliseconds 50
    }while([DateTime]::UtcNow -lt $end)
    throw "Timeout: $label; host=$($h.status); client=$($c.status); runtime=$($nh.error) $($nc.error)"
}
function Button($peer,$name){@((Menu $peer).buttons|Where-Object {$_.name -eq $name -and $_.enabled}).Count -gt 0}
function Check($condition,$label){if(-not $condition){throw $label};$checks.Add("PASS $label");[IO.File]::WriteAllLines("$root/checks.txt",$checks)}
try{
    foreach($peer in @('host','client')){
        if($EditorClient -and $peer -eq 'client'){
            if((Editor editor_status).playMode -ne 'stopped'){throw 'The Editor must be in Edit mode before this test'}
            Editor editor_play | Out-Null
            $editorStarted=$true
            Start-Sleep -Seconds 2
            $until=[DateTime]::UtcNow.AddSeconds(45)
            do{
                $state=$null
                try{$state=Editor editor_status}catch{}
                if($state.playMode -eq 'playing' -and -not $state.domainReloadInProgress -and -not $state.compiling){break}
                if([DateTime]::UtcNow -gt $until){throw 'Editor domain reload timed out'}
                Start-Sleep -Milliseconds 500
            }while($true)
            $path="$root/client"|ConvertTo-Json -Compress
            $code="var game = SSW.NetGame.GetOrCreate(); game.gameObject.AddComponent<SSW.NetProbe>().Init($path, false); game.gameObject.AddComponent<SSW.MenuProbe>().Init($path, false); return UnityEngine.Application.runInBackground;"
            Editor eval @($code) | Out-Null
        }else{
            $processes[$peer]=Start-Process -FilePath "$Project/Builds/Local/Game.exe" -WorkingDirectory $Project -ArgumentList @('--net-profile',"audit-$peer",'--net-probe',"$root/$peer",'--net-name',"Audit-$peer",'-logFile',"$root/$peer.log",'-screen-fullscreen','0','-screen-width','1280','-screen-height','720') -WindowStyle Hidden -PassThru
        }
    }
    Await 'menus' { (Button host 'Button_Play') -and (Button client 'Button_Play') } 45
    Send host fps 60; Send client fps 60
    $jobs=@{Witch=1;Magician=2;Swordsman=3;Assassin=4;Gambler=5;Gunner=6}
    MenuSend host job '' '' $jobs[$HostJob]; MenuSend client job '' '' $jobs[$ClientJob]
    foreach($peer in @('host','client')){MenuSend $peer click 'Button_Play'}
    Await 'play menus' {(Button host 'Button_방 만들기') -and (Button client 'Button_방 들어가기')}
    MenuSend host click 'Button_방 만들기'
    Await 'create form' {(Button host '방 만들기Button')}
    MenuSend host text '방 이름Input' '멀티 검증'
    MenuSend host toggle '방 목록에서 숨기기Toggle' '' 1
    MenuSend host click '방 만들기Button'
    Await 'private relay room' {$h.joined -and $h.host -and $h.code.Length -gt 0 -and -not $h.busy} $CreateTimeout
    Check ($true) 'Real Unity Services private room created'
    MenuSend client click 'Button_방 들어가기'
    Await 'join form' {(Button client '코드 참가Button')}
    MenuSend client text '참가 코드Input' $h.code
    MenuSend client click '코드 참가Button'
    Await 'relay peers ready' {$h.canStart -and $c.joined -and $h.count -eq 2 -and $c.count -eq 2} 90
    Check ($true) 'Second process joins through Relay with a separate authentication profile'
    MenuSend host click '게임 시작Button'
    Await 'initial draft over relay' {$nh.phase -eq 'Draft' -and $nc.phase -eq 'Draft'} 45
    Send host choose 0; Send client choose 0
    Await 'relay combat' {$nh.phase -eq 'Playing' -and $nc.phase -eq 'Playing'}
    Send host warp 0 -6.6 -3.16; Send host warp 1 -5.1 -3.16
    function Count($state,$name) { $entry=@($state.audio.cues | Where-Object {$_.name -eq $name}); if($entry.Count -eq 0){return 0}; return $entry[0].count }
    foreach($peer in @('host','client')){Send $peer sound 1 1 0.7; Send $peer sound 2 1; Send $peer sound 0; Send $peer sound 3}
    Await 'both music outputs' {$nh.audio.musicPeak -gt 0.001 -and $nc.audio.musicPeak -gt 0.001 -and $nh.audio.peak -gt 0.001 -and $nc.audio.peak -gt 0.001}
    Check ($true) 'Both Relay peers produce real BGM output'
    foreach($peer in @('host','client')){Send $peer sound 2 0; Send $peer sound 0}
    Await 'music independent from SFX' {$nh.audio.peak -gt 0.001 -and $nc.audio.peak -gt 0.001}
    Check ($true) 'Muting SFX preserves BGM on both peers'
    foreach($peer in @('host','client')){Send $peer sound 4; Send $peer sound 2 1}
    Start-Sleep -Milliseconds 250
    foreach($peer in @('host','client')){Send $peer sound 0}
    Await 'witch stock ready' {$nh.players[0].potion -ge 0}
    Send host cast 1 0 -1; Send host release 0 0 -1
    Await 'host potion sound' {(Count $nh 'PotionThrow') -eq 1 -and (Count $nc 'PotionThrow') -eq 1}
    Await 'potion impact sound' {(Count $nh 'PotionBreak') -gt 0 -and (Count $nh 'PotionBreak') -eq (Count $nc 'PotionBreak')}
    Check ($true) 'Host potion launch and impact play once on each peer'
    Send client cast 1 0 -1
    Start-Sleep -Milliseconds 100
    Send client release 0 0 -1
    Await 'guest card sound' {(Count $nh 'CardThrow') -eq 1 -and (Count $nc 'CardThrow') -eq 1}
    Await 'card impact sound' {(Count $nh 'Impact') -gt 0 -and (Count $nh 'Impact') -eq (Count $nc 'Impact')}
    Start-Sleep -Milliseconds 650
    $nh=Read host; $nc=Read client
    Check ((Count $nh 'CardThrow') -eq 1 -and (Count $nc 'CardThrow') -eq 1) 'Guest prediction and server confirmation do not double-play the launch'
    Check ($nh.audio.effectsPeak -gt 0.001 -and $nc.audio.effectsPeak -gt 0.001 -and $nh.audio.peak -gt 0.001 -and $nc.audio.peak -gt 0.001) 'Both Relay peers output audible combat samples'
    Check ($nh.audio.listeners -eq 1 -and $nc.audio.listeners -eq 1 -and $nh.audio.sources -eq 25 -and $nc.audio.sources -eq 25) 'Both peers keep one listener and the fixed source pool'
    Await 'second card ready' {$nc.players[1].readyIn -le 0}
    Send host lag 80 20 2; Send client lag 80 20 2
    Send client cast 1 0 -1; Send client release 0 0 -1
    Await 'card sound under delay' {(Count $nh 'CardThrow') -eq 2 -and (Count $nc 'CardThrow') -eq 2}
    Start-Sleep -Milliseconds 750
    Check ((Count (Read host) 'CardThrow') -eq 2 -and (Count (Read client) 'CardThrow') -eq 2) 'Delayed packets do not replay a predicted launch'
    Send host lag 0 0 0; Send client lag 0 0 0
    $nh=Read host;$nc=Read client
    $nh|ConvertTo-Json -Depth 12|Set-Content "$root/combat-host.json"
    $nc|ConvertTo-Json -Depth 12|Set-Content "$root/combat-client.json"
    Check (-not $nh.error -and -not $nc.error) 'Combat and audio have no runtime exception'
    foreach($peer in @('host','client')){Send $peer sound 7}
    Send client exit
    Await 'host wins disconnect' {$nh.phase -eq 'Finished' -and $nh.reason -eq 'Left'}
    Check ($true) 'Leaving Relay produces a synchronized disconnect result'
    Send host exit
    Await 'private room released' {-not $h.joined -and -not $c.joined -and $nh.scene -eq 'MainMenu' -and $nc.scene -eq 'MainMenu'}
    Check ($true) 'Both peers leave and release the private session'
    Write-Output "PASS $($checks.Count) live Relay checks"
}
catch{
    $_|Out-String|Set-Content "$root/failure.txt"
    (Read host)|ConvertTo-Json -Depth 12|Set-Content "$root/failure-host.json"
    (Read client)|ConvertTo-Json -Depth 12|Set-Content "$root/failure-client.json"
    throw
}
finally{
    $cleanup=@{}
    foreach($peer in @('client','host')){
        try{
            if((Menu $peer).busy -and -not (Menu $peer).joined -and (Button $peer '뒤로Button')){
                MenuSend $peer click '뒤로Button'
                $end=[DateTime]::UtcNow.AddSeconds(60)
                do{
                    $state=Menu $peer
                    if($state -and -not $state.busy){break}
                    Start-Sleep -Milliseconds 100
                }while([DateTime]::UtcNow -lt $end)
            }
            if((Menu $peer).joined){
                if(Button $peer '방 나가기Button'){MenuSend $peer click '방 나가기Button'}
                else{Send $peer exit}
                $end=[DateTime]::UtcNow.AddSeconds(30)
                do{
                    $state=Menu $peer
                    if($state -and -not $state.joined -and -not $state.busy){break}
                    Start-Sleep -Milliseconds 100
                }while([DateTime]::UtcNow -lt $end)
            }
            $state=Menu $peer
            $cleanup[$peer]=@{joined=$state.joined;busy=$state.busy;status=$state.status}
            if($state.joined){Write-Warning "Session leave incomplete: $peer"}
            if(-not ($EditorClient -and $peer -eq 'client')){Send $peer quit}
        }catch{Write-Warning $_}
    }
    $cleanup|ConvertTo-Json -Depth 5|Set-Content "$root/cleanup.json"
    if($editorStarted){Editor editor_stop|Out-Null}
}

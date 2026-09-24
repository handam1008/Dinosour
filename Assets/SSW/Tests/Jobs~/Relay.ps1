param([string]$Run='Audit01',[string]$Project=(Get-Location).Path,[int]$CreateTimeout=45,[ValidateSet('Witch','Magician','Swordsman','Assassin','Gambler','Gunner')][string]$HostJob='Gambler',[ValidateSet('Witch','Magician','Swordsman','Assassin','Gambler','Gunner')][string]$ClientJob='Assassin',[switch]$EditorClient)
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
    Send host grant 42 1
    Send host grant 44 1
    Send host grant 45 1
    Send host grant 48 1
    Send host grant 49 1
    Send host grant 50 1
    Send host grant 51 1
    $before=Read host
    Send client cast 1 -1 0
    Send client release 0 -1 0
    Await 'assassin hit and concealment' {$nc.players[1].hidden -and $nh.players[0].hp -lt $before.players[0].hp}
    Check ($true) 'Guest assassin melee and Ambush apply on both peers'
    Send client cycle 1 0 -1
    Await 'blindness' {$nh.players[0].blind -and $nc.players[0].blind}
    Check ($true) 'Blackout status reaches both peers'
    Start-Sleep -Milliseconds 200
    Send client cycle 1 0 -1
    Start-Sleep -Milliseconds 800
    $nh=Read host;$nc=Read client
    Check ($nc.players[1].position.y -gt -3.3 -and $nc.players[1].grounded) 'Dagger return keeps the capsule above the floor'
    Check ($nc.players[1].matches -gt 0) 'Guest dagger preview joins the authoritative projectile'
    $nh|ConvertTo-Json -Depth 12|Set-Content "$root/combat-host.json"
    $nc|ConvertTo-Json -Depth 12|Set-Content "$root/combat-client.json"
    if($EditorClient){
        $capturePath="$Project/Assets/SSW/Tests/Jobs~/assassin.png"
        Editor capture_game_view @('--source','screen','--save_path','Assets/SSW/Tests/Jobs~/assassin.png') | Out-Null
        Copy-Item -LiteralPath $capturePath -Destination "$root/assassin.png" -Force
        Remove-Item -LiteralPath $capturePath
    }
    Check (-not $nh.error -and -not $nc.error) 'Combat has no runtime exception'
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

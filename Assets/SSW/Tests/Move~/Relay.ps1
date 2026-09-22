param([string]$Run='Audit01',[string]$Project=(Get-Location).Path,[int]$CreateTimeout=45,[ValidateSet('Witch','Magician')][string]$HostJob='Witch',[ValidateSet('Witch','Magician')][string]$ClientJob='Magician')
$ErrorActionPreference='Stop'
$root="$Project/Logs/Relay/$Run"
if(Test-Path "$root/host.json"){throw 'Use a fresh Run name'}
New-Item -ItemType Directory -Path $root -Force | Out-Null
$menuSeq=@{host=0;client=0}; $netSeq=@{host=0;client=0}
$checks=[Collections.Generic.List[string]]::new()
$processes=@{}
$progress=[Collections.Generic.List[object]]::new()
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
foreach($peer in @('host','client')){
    $processes[$peer]=Start-Process -FilePath "$Project/Builds/Local/Game.exe" -WorkingDirectory $Project -ArgumentList @('--net-profile',"audit-$peer",'--net-probe',"$root/$peer",'--net-name',"Audit-$peer",'-logFile',"$root/$peer.log",'-screen-fullscreen','0','-screen-width','1280','-screen-height','720') -WindowStyle Hidden -PassThru
}
try{
    Await 'menus' { (Button host 'Button_Play') -and (Button client 'Button_Play') } 45
    Send host fps 60; Send client fps 60
    $jobs=@{Witch=1;Magician=2}
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
    Send client measure 0 1 0
    Await 'relay movement response' {$nc.viewDelay -ge 0 -and $nc.bodyDelay -ge 0}
    Send client move 0 0 0
    Check ($nc.viewDelay -lt 0.1) "Relay guest movement feedback $([Math]::Round($nc.viewDelay*1000)) ms; RTT $($nc.rtt) ms"
    Check (@($nc.players|Where-Object owner)[0].job -eq $ClientJob) "Relay guest keeps the selected $ClientJob job"
    Send host trace 4 3; Send client trace 4 3
    Send client cast 1 0 1
    if($ClientJob -eq 'Magician'){
        Start-Sleep -Milliseconds 200
        Send client cast 0 0 1
    }
    $nativeAction=@((Read client).players|Where-Object owner)[0].action
    Await 'native spawn shot trace' {(Test-Path "$root/host.trace.4.json") -and (Test-Path "$root/client.trace.4.json")}
    $nh|ConvertTo-Json -Depth 12|Set-Content "$root/native-host.json"
    $nc|ConvertTo-Json -Depth 12|Set-Content "$root/native-client.json"
    Await 'native shot acknowledged' {@($nc.players|Where-Object owner)[0].confirmed -ge $nativeAction}
    Check ($nativeAction -gt 0 -and @($nc.players|Where-Object owner)[0].confirmed -ge $nativeAction -and @($nc.players|Where-Object owner)[0].rejected -eq 0) 'Native spawn attack is accepted by the Relay host'
    $shotX=if($ClientJob -eq 'Witch'){-18}else{-10}
    Send host warp 0 13 0; Send host warp 1 $shotX 0
    Await 'open flight range' {$p=@($nc.players|Where-Object owner)[0];$p.epoch -gt 0 -and $p.grounded -and [Math]::Abs($p.position.x-$shotX) -lt 0.1 -and $p.readyIn -le 0 -and ($ClientJob -ne 'Witch' -or $p.heldView -ge 0)}
    $matches=@($nc.players|Where-Object owner)[0].matches
    Send host trace 5 4; Send client trace 5 4
    Send client cast 1 1 0
    if($ClientJob -eq 'Magician'){
        Start-Sleep -Milliseconds 200
        Send client cast 0 1 0
    }
    Await 'relay shot handoff' {@($nc.players|Where-Object owner)[0].matches -gt $matches}
    Await 'flight trace' {(Test-Path "$root/host.trace.5.json") -and (Test-Path "$root/client.trace.5.json")}
    Check ($true) "Guest $ClientJob prediction joins the real Relay projectile in open flight"
    $nh|ConvertTo-Json -Depth 12|Set-Content "$root/combat-host.json"
    $nc|ConvertTo-Json -Depth 12|Set-Content "$root/combat-client.json"
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
            Send $peer quit
        }catch{Write-Warning $_}
    }
    $cleanup|ConvertTo-Json -Depth 5|Set-Content "$root/cleanup.json"
}

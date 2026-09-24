param([string]$HostJob='Gunner',[string]$ClientJob='Gambler',[string]$Run='guncoin',[int]$Port=7791)
$ErrorActionPreference='Stop'
$root="D:/unity_project/Mushrooms/Logs/Jobs24/$Run"
if(Test-Path "$root/host.json"){throw "Use a fresh Run name"}
New-Item -ItemType Directory -Path $root -Force | Out-Null
$exe='D:/unity_project/Mushrooms/Builds/Local/Game.exe'
$processes=@()
foreach($side in @('host','client')){
 $job=if($side -eq 'host'){$HostJob}else{$ClientJob}
 $args=@('--net-mode',$side,'--net-address','127.0.0.1','--net-port',"$Port",'--net-job',$job,'--net-probe',"$root/$side",'--net-profile',"jobs24-$Run-$side",'--net-name',$job,'-logFile',"$root/$side.log",'-screen-width','960','-screen-height','540','-screen-fullscreen','0')
 $processes+=Start-Process -FilePath $exe -ArgumentList $args -WindowStyle Hidden -PassThru
 if($side -eq 'host'){Start-Sleep -Seconds 2}
}
$processes.Id | ConvertTo-Json | Set-Content "$root/pids.json"
. "$PSScriptRoot/Probe.ps1" -Root $root -Peer host
Await {Test-Path "$root/client.json"} 40
Await {(Snap).phase -eq 'Draft'} 40
Send choose 0
$Peer='client'
Await {(Snap).phase -eq 'Draft'} 10
Send choose 0
Await {(Snap).phase -eq 'Playing'} 20
$Peer='host'
Await {(Snap).phase -eq 'Playing'} 10
Send fps 60
$Peer='client'
Send fps 60
foreach($Peer in @('host','client')){Snap | ConvertTo-Json -Depth 10 | Set-Content "$root/$Peer-start.json"}
@{root=$root;pids=$processes.Id;phase=(Snap).phase;error=(Snap).error} | ConvertTo-Json

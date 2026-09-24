param([string]$Peer='client',[string]$Root='D:/unity_project/Mushrooms/Logs/Peer23')
$ErrorActionPreference='Stop'
function Snap([switch]$Menu){
    $suffix=if($Menu){'.menu.json'}else{'.json'}
    $path="$Root/$Peer$suffix"
    for($n=0;$n -lt 25;$n++){
        try{return [IO.File]::ReadAllText($path)|ConvertFrom-Json}catch{Start-Sleep -Milliseconds 10}
    }
    throw "Cannot read $path"
}
function Send([string]$Op,[int]$Value=0,[double]$X=0,[double]$Y=0){
    $seq=(Snap).seq+1
    $json=@{seq=$seq;op=$Op;value=$Value;x=$X;y=$Y}|ConvertTo-Json -Compress
    WriteProbe "$Root/$Peer.cmd.json" $json
    if($Op -eq 'quit'){return}
    $end=[DateTime]::UtcNow.AddSeconds(8)
    do{$s=Snap;if($s.seq -ge $seq){return};Start-Sleep -Milliseconds 20}while([DateTime]::UtcNow -lt $end)
    throw "Probe timeout: $Op $seq"
}
function Menu([string]$Op,[string]$Target='',[string]$Text='',[int]$Value=0){
    $seq=(Snap -Menu).seq+1
    $json=@{seq=$seq;op=$Op;target=$Target;text=$Text;value=$Value}|ConvertTo-Json -Compress
    WriteProbe "$Root/$Peer.menu.cmd.json" $json
    $end=[DateTime]::UtcNow.AddSeconds(8)
    do{$s=Snap -Menu;if($s.seq -ge $seq){if($s.error){throw $s.error};return};Start-Sleep -Milliseconds 20}while([DateTime]::UtcNow -lt $end)
    throw "Menu timeout: $Op $Target $seq"
}
function WriteProbe([string]$Path,[string]$Json){
    $end=[DateTime]::UtcNow.AddSeconds(2)
    do{try{[IO.File]::WriteAllText($Path,$Json);return}catch [IO.IOException]{Start-Sleep -Milliseconds 15}}while([DateTime]::UtcNow -lt $end)
    throw "Cannot write $Path"
}
function Await([scriptblock]$Condition,[int]$Seconds=30){
    $end=[DateTime]::UtcNow.AddSeconds($Seconds)
    do{if(& $Condition){return};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow -lt $end)
    throw 'Probe wait timed out'
}
function Button([string]$Name){@((Snap -Menu).buttons|Where-Object {$_.name -eq $Name -and $_.enabled}).Count -gt 0}

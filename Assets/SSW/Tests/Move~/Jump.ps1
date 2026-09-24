param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/../Jobs~/Probe.ps1" -Root $Root -Peer host
$checks=[Collections.Generic.List[object]]::new()
function Fixture([string]$Kind,[int]$Angle){
    foreach($script:Peer in @('host','client')){Send move; Send $Kind $Angle 1000 0}
    $script:Peer='host'
    Send warp 0 1000 5
    Send warp 1 1000 5
}
function Place([string]$Owner,[double]$X=1000){
    $script:Peer='host'
    $index=if($Owner -eq 'host'){0}else{1}
    Send warp $index $X 5
    foreach($script:Peer in @('host','client')){
        Await { $p=@((Snap).players|Where-Object {$_.id -eq $index})[0]; $p.grounded -and [Math]::Abs($p.position.x-$X) -lt 0.1 } 5
        Send metrics
    }
}
function Sample([double]$Seconds){
    $rows=[Collections.Generic.List[object]]::new()
    $until=[DateTime]::UtcNow.AddSeconds($Seconds)
    do{
        foreach($script:Peer in @('host','client')){
            $s=Snap
            if($s.error){throw $s.error}
            if($s.phase -ne 'Playing'){throw "Unexpected phase $($s.phase)"}
            $rows.Add(@{peer=$Peer;time=$s.time;players=$s.players})
        }
        Start-Sleep -Milliseconds 20
    }while([DateTime]::UtcNow -lt $until)
    return ,$rows
}
function Result([string]$Name,[string]$Owner,$Rows,[bool]$Jump){
    $id=if($Owner -eq 'host'){0}else{1}
    $result=@{name=$Name;owner=$Owner;pass=$true;views=@{}}
    foreach($view in @('host','client')){
        $states=@($Rows|Where-Object {$_.peer -eq $view}|ForEach-Object {$_.players|Where-Object {$_.id -eq $id}})
        $vy=($states.velocity.y|Measure-Object -Maximum).Maximum
        $correction=($states.maxCorrection|Measure-Object -Maximum).Maximum
        $ok=if($Jump){$vy -gt 4}else{$vy -le 0}
        if($Name -like '*edge*'){
            $left=@($states|Where-Object {!$_.grounded -and $_.position.x -gt 1004.3}).Count -gt 0
            $ok=$ok -and $left
        }
        $result.views[$view]=@{maxVelocityY=$vy;maxCorrection=$correction;pass=$ok}
        $result.pass=$result.pass -and $ok
    }
    $checks.Add($result)
    $checks|ConvertTo-Json -Depth 10|Set-Content "$Root/jump-checks.json"
    $Rows|ConvertTo-Json -Depth 12|Set-Content "$Root/$Name-$Owner-samples.json"
    $result|ConvertTo-Json -Depth 6 -Compress
    if(!$result.pass){throw "Failed $Name $Owner"}
}
try{
    foreach($kind in @('ground','platform')){
        $angle=if($kind -eq 'ground'){45}else{-45}
        Fixture $kind $angle
        foreach($owner in @('host','client')){
            Place $owner
            $script:Peer=$owner
            Send measure 1
            $rows=Sample 0.7
            Result "$kind-jump" $owner $rows $true
        }
    }
    Fixture ground 0
    foreach($owner in @('host','client')){
        foreach($ticks in @(3,8)){
            Place $owner 1003.5
            $script:Peer=$owner
            $side=(@((Snap).players|Where-Object {$_.owner})[0]).side
            Send edgejump $ticks
            Send move 0 $side 0
            $rows=Sample 0.65
            Result "edge-$ticks" $owner $rows ($ticks -eq 3)
            $script:Peer=$owner
            Send move
            Place $owner
        }
    }
    foreach($script:Peer in @('host','client')){Send lag 60 10 1}
    Fixture platform 45
    Place client
    $script:Peer='client'
    Send measure 1
    $rows=Sample 0.8
    Result 'lag-slope' client $rows $true
    Fixture ground 0
    Place client 1003.5
    $script:Peer='client'
    $side=(@((Snap).players|Where-Object {$_.owner})[0]).side
    Send edgejump 3
    Send move 0 $side 0
    $rows=Sample 0.65
    Result 'lag-edge' client $rows $true
    @{passed=$checks.Count;output="$Root/jump-checks.json"}|ConvertTo-Json -Compress
}
finally{
    foreach($script:Peer in @('client','host')){try{Send quit}catch{Write-Warning $_}}
}

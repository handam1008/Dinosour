param([string]$Run='Visual2')
$ErrorActionPreference='Stop'
$project=(Get-Location).Path
$root="$project/Logs/Move/$Run"
if(Test-Path "$root/client.json"){throw 'Use a fresh Run name'}
New-Item -ItemType Directory -Path $root -Force|Out-Null
$seq=@{host=0;client=0}
function Eval($code){
    $r=unity command eval --code $code --project-path $project --format json|ConvertFrom-Json
    if(-not $r.success -or -not $r.data.result.success){throw ($r|ConvertTo-Json -Depth 8)}
    $r.data.result.result
}
function Read($peer){try{Get-Content "$root/$peer.json" -Raw|ConvertFrom-Json}catch{return $null}}
function Send($peer,$op,$value=0,$x=0,$y=0){
    $seq[$peer]++
    [IO.File]::WriteAllText("$root/$peer.cmd.json",(@{seq=$seq[$peer];op=$op;value=$value;x=$x;y=$y}|ConvertTo-Json -Compress))
}
function Await($phase){
    $until=[DateTime]::UtcNow.AddSeconds(50)
    do{
        $h=Read host;$c=Read client
        if($h.error -or $c.error){throw "$($h.error) $($c.error)"}
        if($h.phase -eq $phase -and $c.phase -eq $phase){return}
        Start-Sleep -Milliseconds 100
    }while([DateTime]::UtcNow -lt $until)
    throw "Timeout $phase"
}
try{
    Start-Process -FilePath "$project/Builds/Move/Game.exe" -WorkingDirectory $project -ArgumentList @('--net-mode','host','--net-job','Witch','--net-port','7797','--net-name','Host','--net-probe',"$root/host",'-logFile',"$root/host.log",'-screen-fullscreen','0') -WindowStyle Hidden|Out-Null
    $path=("$root/client".Replace('\','/')|ConvertTo-Json -Compress)
    Eval ('var game=SSW.NetGame.GetOrCreate(); game.SetProfile(SSW.Fighter.Create("Client")); game.gameObject.AddComponent<SSW.NetProbe>().Init('+$path+'); game.StartLocal(false,"127.0.0.1",SSW.PlayerJob.Magician,7797); return "Client started";')|Write-Output
    Await Draft
    Send host choose;Send client choose
    Await Playing
    Send host lag 100 10 1;Send client lag 100 10 1
    Start-Sleep -Milliseconds 1000
    Send client measure 0 1 0
    $trace=[Collections.Generic.List[object]]::new()
    for($i=0;$i -lt 10;$i++){
        Start-Sleep -Milliseconds 100
        $s=Read client
        $trace.Add([pscustomobject]@{time=$s.time;player=@($s.players|Where-Object owner)[0];viewDelay=$s.viewDelay;bodyDelay=$s.bodyDelay})
    }
    Send client move
    Start-Sleep -Milliseconds 1000
    $trace|ConvertTo-Json -Depth 10|Set-Content "$root/trace.json"
    (Read client)|ConvertTo-Json -Depth 10|Set-Content "$root/final-client.json"
    unity command capture_game_view --source screen --save_path SSW/Tests/Move.png --width 1280 --height 720 --project-path $project --format json|Write-Output
    Eval 'var p=SSW.NetGame.Current.Local; var visual=p.GetComponentInChildren<SSW.DinosaurVisualController>(); return new { linked=visual.transform.IsChildOf(p.View), render=visual.transform.position, view=p.View.position, body=p.Body.position };'|ConvertTo-Json -Depth 4|Write-Output
}
finally{
    Send host quit
    unity command editor_stop --project-path $project --format json|Out-Null
}

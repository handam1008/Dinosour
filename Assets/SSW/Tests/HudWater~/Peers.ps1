param([string]$Run=('Run'+(Get-Date -Format 'yyyyMMddHHmmss')),[string]$Project=(Get-Location).Path,[string]$Build='Builds/HudWater28/Game.exe',[switch]$RopeOnly)
$ErrorActionPreference='Stop'
$Project=[IO.Path]::GetFullPath($Project).Replace('\','/')
$root="$Project/Logs/HudWater28/$Run"
if(Test-Path -LiteralPath $root){throw 'Use a new run name'}
[IO.Directory]::CreateDirectory($root)|Out-Null
$cli=Join-Path $env:LOCALAPPDATA 'Unity/bin/unity.exe'
$previous=$null
$clientProcess=$null
$evalIndex=0
$seq=@{host=0;client=0}
function Eval([string]$code){
    $script:evalIndex++
    $path="$root/Eval$evalIndex.cs"
    [IO.File]::WriteAllText($path,$code)
    $reply=& $cli command eval_file --file $path --caller plugin --skill unity-cli --project-path $Project --format json|ConvertFrom-Json
    $reply|ConvertTo-Json -Depth 14|Set-Content -LiteralPath "$root/Eval$evalIndex.json"
    if(-not $reply.success -or -not $reply.data.result.success){throw ($reply|ConvertTo-Json -Depth 9)}
    $value=$reply.data.result.result
    if($value -is [string] -and ($value.StartsWith('{') -or $value.StartsWith('['))){return $value|ConvertFrom-Json}
    return $value
}
function Read($peer){
    for($n=0;$n -lt 5;$n++){
        try{$snapshot=[IO.File]::ReadAllText("$root/$peer.json")|ConvertFrom-Json;if($snapshot -and $snapshot.probeVersion){return $snapshot}}catch{}
        Start-Sleep -Milliseconds 15
    }
    return $null
}
function Await([string]$label,[scriptblock]$condition,[int]$timeout=10){
    $until=[DateTime]::UtcNow.AddSeconds($timeout)
    do{
        $script:h=Read host;$script:c=Read client
        if(-not [string]::IsNullOrWhiteSpace([string]$h.error) -or -not [string]::IsNullOrWhiteSpace([string]$c.error)){throw "Probe error: $($h.error) $($c.error)"}
        if($h -and $c -and (& $condition)){return}
        Start-Sleep -Milliseconds 30
    }while([DateTime]::UtcNow -lt $until)
    @{label=$label;host=$h;client=$c}|ConvertTo-Json -Depth 15|Set-Content -LiteralPath "$root/Timeout.json"
    throw "Timeout: $label"
}
function Send($peer,$op,$value=0,$x=0,$y=0){
    $seq[$peer]=[Math]::Max($seq[$peer],[int](Read $peer).seq)+1
    [IO.File]::WriteAllText("$root/$peer.cmd.json",(@{seq=$seq[$peer];op=$op;value=$value;x=$x;y=$y}|ConvertTo-Json -Compress))
    if($op -eq 'quit'){return}
    $until=[DateTime]::UtcNow.AddSeconds(5)
    while((Read $peer).seq -lt $seq[$peer]){if([DateTime]::UtcNow -gt $until){throw "Command timeout: $peer $op"};Start-Sleep -Milliseconds 20}
}
try{
    $previous=Eval 'var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(s.isDirty||UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Saved stopped scene required");var r=new{scene=s.path,job=SSW.PlayerJobStorage.Load().ToString()};UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/SSW/Tests/MapStart.unity");return Newtonsoft.Json.JsonConvert.SerializeObject(r);'
    $previous|ConvertTo-Json|Set-Content -LiteralPath "$root/Previous.json"
    & $cli command editor_play --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    $until=[DateTime]::UtcNow.AddSeconds(45)
    do{
        & $cli command editor_status --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
        try{$scene=Eval 'return UnityEditor.EditorApplication.isPlaying&&!UnityEditor.EditorApplication.isCompiling?UnityEngine.SceneManagement.SceneManager.GetActiveScene().name:"";'}catch{$scene=''}
        if($scene -eq 'MapStart'){break};Start-Sleep -Milliseconds 300
    }while([DateTime]::UtcNow -lt $until)
    if($scene -ne 'MapStart'){throw 'Menu did not become ready'}
    Eval ('var g=SSW.NetGame.GetOrCreate();g.StartLocal(true,"127.0.0.1",SSW.PlayerJob.Swordsman,30721);g.gameObject.AddComponent<SSW.NetProbe>().Init("'+$root+'/host");return g.Manager.NetworkConfig.ProtocolVersion;')|Out-Null
    $clientArgs=@('--net-mode','client','--net-address','127.0.0.1','--net-port','30721','--net-job','Swordsman','--net-probe',"$root/client",'-logFile',"$root/client.log",'-screen-fullscreen','0','-screen-width','1280','-screen-height','720')
    $clientProcess=Start-Process -FilePath (Join-Path $Project $Build) -WorkingDirectory $Project -ArgumentList $clientArgs -WindowStyle Hidden -PassThru
    Await 'both drafts' {
        if((Test-Path -LiteralPath "$root/host.config.json") -and (Test-Path -LiteralPath "$root/client.config.json")){
            $hostConfig=Get-Content -LiteralPath "$root/host.config.json" -Raw|ConvertFrom-Json
            $clientConfig=Get-Content -LiteralPath "$root/client.config.json" -Raw|ConvertFrom-Json
            if($hostConfig.hash -ne $clientConfig.hash){throw 'Network config mismatch; compare both config reports and rebuild against the current prefabs'}
        }
        $h.phase -eq 'Draft' -and $c.phase -eq 'Draft'
    } 60
    foreach($peer in @('host','client')){Send $peer lag 60 10 0;Send $peer isolate;Send $peer wideground}
    Eval 'var g=SSW.NetGame.Current;typeof(SSW.BattleMap).GetField("_fallY",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(g.Arena.Map,-1000f);foreach(var p in g.Players){p.Drive.Teleport(new UnityEngine.Vector2(p.Side*-3f,3f));p.Draft.Restore(System.Array.Empty<int>());}g.Match.Picked();return true;'|Out-Null
    Await 'both playing' {$h.phase -eq 'Playing' -and $c.phase -eq 'Playing'} 30
    foreach($peer in @('host','client')){Send $peer block 1}
    $cornerCount=Eval ([IO.File]::ReadAllText("$Project/Assets/SSW/Tests/HudWater~/Corner.cs").Replace('@ROOT@',$root))
    Write-Output "PASS $cornerCount edge cases"
    Eval ([IO.File]::ReadAllText("$Project/Assets/SSW/Tests/HudWater~/Run.cs").Replace('@ROOT@',$root).Replace('@FULL@',(-not $RopeOnly).ToString().ToLowerInvariant()))|Out-Null
    $until=[DateTime]::UtcNow.AddSeconds(400)
    $stage=''
    do{
        if(Test-Path -LiteralPath "$root/Result.json"){break}
        if(Test-Path -LiteralPath "$root/Progress.json"){
            try{$progress=Get-Content -LiteralPath "$root/Progress.json" -Raw|ConvertFrom-Json;if($stage -ne $progress.stage){$stage=$progress.stage;Write-Output "$stage ($($progress.count) checks)"}}catch{}
        }
        Start-Sleep -Milliseconds 300
    }while([DateTime]::UtcNow -lt $until)
    if(-not (Test-Path -LiteralPath "$root/Result.json")){throw 'Runtime checks timed out'}
    $result=Get-Content -LiteralPath "$root/Result.json" -Raw|ConvertFrom-Json
    if(-not $result.success){throw $result.error}
    Write-Output "PASS $($result.count) checks: $root"
}catch{
    @{success=$false;error=$_.ToString();host=(Read host);client=(Read client)}|ConvertTo-Json -Depth 15|Set-Content -LiteralPath "$root/Failure.json"
    throw
}finally{
    if($clientProcess -and -not $clientProcess.HasExited){try{Send client quit}catch{};if(-not $clientProcess.WaitForExit(3000)){Stop-Process -Id $clientProcess.Id}}
    & $cli command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    if($previous){
        Start-Sleep -Milliseconds 800
        & $cli command editor_status --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
        Eval ('if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Still playing");SSW.PlayerJobStorage.Save(SSW.PlayerJob.'+$previous.job+');var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(s.isDirty)throw new System.InvalidOperationException("Unsaved scene");UnityEditor.SceneManagement.EditorSceneManager.OpenScene("'+$previous.scene+'");return true;')|Out-Null
    }
}

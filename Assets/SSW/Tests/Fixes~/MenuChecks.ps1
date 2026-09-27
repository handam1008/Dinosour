param([string]$Project=(Get-Location).Path)
$ErrorActionPreference='Stop'
$Project=[IO.Path]::GetFullPath($Project).Replace('\','/')
$root="$Project/Logs/AugmentLink27"
function Eval([string]$path){
    $reply=unity command eval_file --file $path --caller plugin --skill unity-cli --project-path $Project --format json|ConvertFrom-Json
    if(-not $reply.success -or -not $reply.data.result.success){throw ($reply|ConvertTo-Json -Depth 5)}
    return $reply.data.result.result
}
$setup=@'
if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Existing Play session");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty || scene.path!="Assets/SSW/MainMenu.unity")throw new System.InvalidOperationException("Clean MainMenu required");
return true;
'@
[IO.File]::WriteAllText("$root/MenuState.cs",$setup)
Eval "$root/MenuState.cs"|Out-Null
try{
    unity command editor_play --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    $readyAt=[DateTime]::UtcNow.AddSeconds(45)
    [IO.File]::WriteAllText("$root/MenuReady.cs",'return UnityEditor.EditorApplication.isPlaying && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu";')
    do{
        Start-Sleep -Milliseconds 500
        try{$ready=Eval "$root/MenuReady.cs"}catch{$ready=$false}
        if([DateTime]::UtcNow -ge $readyAt){throw 'Menu did not enter Play'}
    }while(-not $ready)
    [IO.File]::WriteAllText("$root/MenuProbe.cs",'var game=SSW.NetGame.GetOrCreate();if(game.GetComponent<SSW.NetProbe>()==null)game.gameObject.AddComponent<SSW.NetProbe>().Init("Logs/AugmentLink27/menu");return true;')
    try{Eval "$root/MenuProbe.cs"|Out-Null}catch{
        Start-Sleep -Seconds 1
        Eval "$root/MenuProbe.cs"|Out-Null
    }
    Eval "$PSScriptRoot/RoomScore.cs"|Out-Null
    $until=[DateTime]::UtcNow.AddMinutes(3)
    while(-not (Test-Path "$root/RoomScore.json")){
        if([DateTime]::UtcNow -ge $until){throw 'Room score run timeout'}
        Start-Sleep -Milliseconds 300
    }
    $result=Get-Content "$root/RoomScore.json" -Raw|ConvertFrom-Json
    $result|ConvertTo-Json -Depth 4
    if($result.error){throw 'Room score check failed; inspect saved report'}
    Eval "$PSScriptRoot/ProfileScore.cs"|ConvertTo-Json -Depth 4
}finally{
    unity command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    [IO.File]::WriteAllText("$root/MenuClean.cs",'var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();return new{playing=UnityEditor.EditorApplication.isPlaying,scene=scene.path,dirty=scene.isDirty};')
    Eval "$root/MenuClean.cs"|ConvertTo-Json|Set-Content "$root/MenuCleanup.json"
}

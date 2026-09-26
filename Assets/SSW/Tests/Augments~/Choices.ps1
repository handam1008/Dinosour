param([string]$Project=(Get-Location).Path,[string]$Run=('Choices'+(Get-Date -Format 'yyyyMMddHHmmss')))
$ErrorActionPreference='Stop'
$Project=[IO.Path]::GetFullPath($Project).Replace('\','/')
$root="$Project/Logs/Augments/$Run"
if(Test-Path $root){throw 'Existing choices report must be preserved.'}
[IO.Directory]::CreateDirectory($root)|Out-Null
function Eval($code){
    [IO.File]::WriteAllText("$root/Eval.cs",$code)
    $r=unity command eval_file --file "$root/Eval.cs" --caller plugin --skill unity-cli --project-path $Project --format json|ConvertFrom-Json
    if(-not $r.success -or -not $r.data.result.success){throw ($r|ConvertTo-Json -Depth 10)}
    return $r.data.result.result
}
try{
    $saved=Eval 'if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Play session active");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new System.InvalidOperationException("Scene dirty");var result=new{scene=scene.path,job=(int)SSW.PlayerJobStorage.Load()};UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/SSW/MainMenu.unity");SSW.PlayerJobStorage.Save(SSW.PlayerJob.Witch);return result;'
    Eval ('UnityEditor.SessionState.SetString("augments.choices","'+$root+'");return true;')|Out-Null
    unity command editor_play --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    $ready=[DateTime]::UtcNow.AddSeconds(50)
    do{
        try{$ok=Eval 'return UnityEditor.EditorApplication.isPlaying && UnityEngine.Object.FindFirstObjectByType<SSW.MainMenuController>()!=null;'}catch{$ok=$false}
        if($ok){break}
        Start-Sleep -Milliseconds 500
    }while([DateTime]::UtcNow -lt $ready)
    if(-not $ok){throw 'Main menu did not start'}
    $open=unity command eval_file --file 'Assets/SSW/Tests/Sandbox~/Open.cs' --caller plugin --skill unity-cli --project-path $Project --format json|ConvertFrom-Json
    if(-not $open.data.result.success){throw ($open|ConvertTo-Json -Depth 10)}
    $ready=[DateTime]::UtcNow.AddSeconds(25)
    do{
        try{$ok=Eval 'return SSW.NetGame.Current!=null && SSW.NetGame.Current.Practice!=null && SSW.NetGame.Current.Local!=null;'}catch{$ok=$false}
        if($ok){break}
        Start-Sleep -Milliseconds 500
    }while([DateTime]::UtcNow -lt $ready)
    if(-not $ok){throw 'Practice did not start'}
    $start=unity command eval_file --file 'Assets/SSW/Tests/Augments~/Choices.cs' --caller plugin --skill unity-cli --project-path $Project --format json|ConvertFrom-Json
    if(-not $start.data.result.success){throw ($start|ConvertTo-Json -Depth 10)}
    $end=[DateTime]::UtcNow.AddMinutes(6)
    do{
        if(Test-Path "$root/Error.txt"){throw (Get-Content "$root/Error.txt" -Raw)}
        if((Get-Content "$root/Checks.txt" -Tail 1 -ErrorAction SilentlyContinue) -eq 'DONE'){break}
        Start-Sleep -Seconds 2
    }while([DateTime]::UtcNow -lt $end)
    if((Get-Content "$root/Checks.txt" -Tail 1) -ne 'DONE'){throw 'Choices timed out'}
    Write-Output ('PASS '+@(Get-Content "$root/Checks.txt"|Where-Object {$_ -like 'PASS *'}).Count+' choices checks')
}catch{
    $_|Out-String|Set-Content "$root/HarnessError.txt"
    throw
}finally{
    if($saved){
        unity command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
        Start-Sleep -Seconds 5
        Eval ('SSW.PlayerJobStorage.Save((SSW.PlayerJob)'+$saved.job+');UnityEditor.SceneManagement.EditorSceneManager.OpenScene("'+$saved.scene+'");return true;')|Out-Null
    }
}

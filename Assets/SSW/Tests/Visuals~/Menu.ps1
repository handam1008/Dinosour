param([string]$Run='Menu',[string]$Project=(Get-Location).Path)
$ErrorActionPreference='Stop'
$Project=[IO.Path]::GetFullPath($Project).Replace('\','/')
$root="$Project/Logs/Visuals/$Run"
if(Test-Path -LiteralPath $root){throw 'Fresh run required'}
[IO.Directory]::CreateDirectory($root)|Out-Null
function Eval([string]$code){
    [IO.File]::WriteAllText("$root/Eval.cs",$code)
    $r=unity command eval_file --file "$root/Eval.cs" --caller plugin --skill unity-cli --project-path $Project --format json|ConvertFrom-Json
    if(-not $r.success -or -not $r.data.result.success){throw ($r|ConvertTo-Json -Depth 8)}
    $r.data.result.result
}
$previous=Eval 'if(UnityEditor.EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Idle saved scene required");return UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;'
try{
    @{commit=(git rev-parse HEAD).Trim();before=$previous}|ConvertTo-Json|Set-Content "$root/Run.json"
    Eval 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/RYU/00.Scene/StartMenu.unity");return true;'|Out-Null
    unity command editor_play --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    $until=[DateTime]::UtcNow.AddSeconds(40)
    do{Start-Sleep -Milliseconds 500;try{$name=Eval 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;'}catch{$name=''}}while($name -ne 'MainMenu' -and [DateTime]::UtcNow -lt $until)
    Eval ([IO.File]::ReadAllText("$PSScriptRoot/Menu.cs").Replace('@ROOT@',$root))|ConvertTo-Json|Set-Content "$root/Launch.json"
    do{Start-Sleep -Milliseconds 300;$r=Get-Content -Raw -LiteralPath "$root/Menu.json"|ConvertFrom-Json}while($r.status -eq 'running' -and [DateTime]::UtcNow -lt $until)
    if($r.status -ne 'completed' -or -not $r.restored){throw ($r|ConvertTo-Json -Depth 6)}
    "PASS Menu $($r.count) checks"
}finally{
    unity command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    $until=[DateTime]::UtcNow.AddSeconds(30)
    do{Start-Sleep -Milliseconds 300;$playing=Eval 'return UnityEditor.EditorApplication.isPlaying;'}while($playing -and [DateTime]::UtcNow -lt $until)
    if($playing){throw 'Editor did not stop'}
    Eval ('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("'+$previous+'");var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();return new{path=s.path,dirty=s.isDirty,playing=UnityEditor.EditorApplication.isPlaying};')|ConvertTo-Json|Set-Content "$root/Cleanup.json"
}

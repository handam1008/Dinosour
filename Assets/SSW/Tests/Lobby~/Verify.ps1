param([string]$Run=(Get-Date -Format yyyyMMddHHmmss),[string]$Build='Builds/Lobby28/Game.exe',[switch]$SkipPreview)
. "$PSScriptRoot/../Rooms~/Common.ps1"
$root="$Project/Logs/Lobby28/$Run"
if(Test-Path -LiteralPath $root){throw 'Use a fresh run name'}
[IO.Directory]::CreateDirectory($root)|Out-Null
$checks=[Collections.Generic.List[string]]::new()
$player=$null
$previous=$null
$ready=$false
function Check($Condition,[string]$Name){
    if(-not $Condition){throw $Name}
    $checks.Add($Name)
    [IO.File]::WriteAllLines("$root/Checks.txt",$checks)
    Write-Output "PASS $Name"
}
function OpenPlay([string]$Peer){
    $m=Menu $Peer
    if(@($m.buttons|Where-Object name -eq Button_Play).Count){Control $Peer click Button_Play}
    elseif(@($m.buttons|Where-Object name -eq '뒤로Button').Count){Control $Peer click '뒤로Button'}
    Await "$Peer play choices" {@((Menu $Peer).buttons|Where-Object {$_.name -eq 'Button_방 만들기' -and $_.enabled}).Count -eq 1}
}
function Create([string]$Peer){
    OpenPlay $Peer
    Control $Peer click 'Button_방 만들기'
    Control $Peer text '방 이름Input' "Lobby28-$Run-$Peer"
    Control $Peer toggle '방 목록에서 숨기기Toggle' '' 1
    Control $Peer click '방 만들기Button'
    Await "$Peer room created" {(Menu $Peer).joined -and (Menu $Peer).host -and -not (Menu $Peer).busy} 55
}
function Join([string]$Guest,[string]$Owner){
    if(-not @((Menu $Guest).buttons|Where-Object name -eq '코드 참가Button').Count){OpenPlay $Guest;Control $Guest click 'Button_방 들어가기'}
    Control $Guest text '참가 코드Input' (Menu $Owner).code
    Control $Guest click '코드 참가Button'
    Start-Sleep -Seconds 2
    foreach($peer in @('host','client')){
        $n=Get-Content "$root/$peer.json" -Raw|ConvertFrom-Json
        [IO.File]::WriteAllText("$root/$peer.cmd.json",(@{seq=[int]$n.seq+1;op='config'}|ConvertTo-Json -Compress))
    }
    Await "$Guest connects" {(Menu $Guest).joined -and (Menu $Owner).canStart -and -not (Menu $Guest).busy} 55
    Await 'roster replicated' {(Menu host).members.Count -eq 2 -and (Menu client).members.Count -eq 2}
    Check ((Menu host).config -eq (Menu client).config) 'network configurations match'
}
function Leave([string]$Peer){
    if(-not (Menu $Peer).joined){return}
    Await "$Peer idle" {-not (Menu $Peer).busy}
    Control $Peer click '방 나가기Button'
    Await "$Peer left" {-not (Menu $Peer).joined -and -not (Menu $Peer).listening -and -not (Menu $Peer).busy} 40
}
function Roster([string]$Owner,[string]$HostJob,[string]$ClientJob){
    foreach($peer in @('host','client')){
        $m=Menu $peer
        Check ($m.members.Count -eq 2 -and $m.memberRows.Count -eq 2) "$peer displays two participants"
        foreach($other in @('host','client')){
            $expected=Menu $other
            $member=@($m.members|Where-Object id -eq $expected.playerId)[0]
            $name=($expected.playerName -split '#')[0]
            $job=if($other -eq 'host'){$HostJob}else{$ClientJob}
            Check ($member.name -eq $name -and $member.job -eq $job) "$peer receives $other actual name and job"
            Check (@($m.memberRows|Where-Object {$_.text.Contains($name)}).Count -eq 1) "$peer renders $other name"
        }
        Check ($m.members[0].id -eq (Menu $Owner).playerId -and $m.members[0].host) "$peer sorts the actual host first"
        Check (@($m.buttons|Where-Object name -eq '추방Button').Count -eq [int]($peer -eq $Owner)) "$peer kick control follows authority"
        $m|ConvertTo-Json -Depth 8|Set-Content "$root/$Owner-$peer-roster.json"
    }
}
function Kick([string]$Owner,[string]$Guest){
    Control $Owner click '추방Button'
    Await "$Guest kicked" {-not (Menu $Guest).joined -and -not (Menu $Guest).listening -and -not (Menu $Guest).busy} 40
    (Menu $Guest)|ConvertTo-Json -Depth 8|Set-Content "$root/$Guest-kicked.json"
    Check ((Menu $Guest).status -like '*추방*') "$Guest sees kick notification"
    Await 'owner remains alone' {(Menu $Owner).joined -and (Menu $Owner).count -eq 1 -and -not (Menu $Owner).busy}
    Check (-not (Menu $Owner).canStart -and (Menu $Owner).listening) "$Owner retains room and cannot start alone"
    Check (@((Menu $Owner).buttons|Where-Object name -eq '추방Button').Count -eq 0) "$Owner removes departed row and kick button"
}
try{
    $previous=Eval 'var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(s.isDirty||UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Saved stopped scene required");var r=new{scene=s.path,hadJob=UnityEngine.PlayerPrefs.HasKey(SSW.PlayerJobStorage.PreferenceKey),job=UnityEngine.PlayerPrefs.GetInt(SSW.PlayerJobStorage.PreferenceKey)};UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/SSW/MainMenu.unity");return r;'
    $previous|ConvertTo-Json|Set-Content "$root/Previous.json"
    & $Cli command editor_play --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    Start-Sleep -Seconds 4
    for($attempt=0;$attempt -lt 8;$attempt++){
        try{$ready=Eval 'return UnityEditor.EditorApplication.isPlaying;';if($ready){break}}catch{}
        Start-Sleep -Seconds 2
    }
    if(-not $ready){throw 'Editor did not enter play mode'}
    Eval ('var g=SSW.NetGame.GetOrCreate();g.gameObject.AddComponent<SSW.MenuProbe>().Init("'+$root+'/host");g.gameObject.AddComponent<SSW.NetProbe>().Init("'+$root+'/host");return true;')|Out-Null
    if(-not $SkipPreview){
        Eval ([IO.File]::ReadAllText("$PSScriptRoot/Preview.cs").Replace('@ROOT@',$root))|Out-Null
        Await 'customization preview checks' {Test-Path "$root/Preview.json"} 55
        $preview=Get-Content "$root/Preview.json" -Raw|ConvertFrom-Json
        Check ($preview.success -and $preview.restored) "preview suite passed $($preview.checks.Count) checks with preferences restored"
    }
    Control host profile '' 'lobby-editor28'
    Await 'host isolated authentication' {(Menu host).profile -eq 'lobby-editor28' -and (Menu host).signedIn -and -not (Menu host).busy} 40
    $player=Start-Process -FilePath "$Project/$Build" -WorkingDirectory $Project -ArgumentList @('--net-profile','lobby-client28','--net-probe',"$root/client",'-logFile',"$root/client.log",'-screen-fullscreen','0','-screen-width','1280','-screen-height','720') -WindowStyle Hidden -PassThru
    $player.Id|Set-Content "$root/PlayerPid.txt"
    Await 'client menu' {Test-Path "$root/client.menu.json"} 40
    Control client profile '' 'lobby-client28'
    Await 'client isolated authentication' {(Menu client).profile -eq 'lobby-client28' -and (Menu client).signedIn -and -not (Menu client).busy} 40
    Control host job '' '' 1
    Control client job '' '' 3
    Create host
    Join client host
    Roster host Witch Swordsman
    Async guards 'var m=SSW.MultiplayerSessionManager.Current;bool self=false,missing=false;try{await m.KickPlayerAsync(Unity.Services.Authentication.AuthenticationService.Instance.PlayerId);}catch(System.ArgumentException){self=true;}try{await m.KickPlayerAsync("missing");}catch(System.ArgumentException){missing=true;}if(!self||!missing)throw new System.Exception("Invalid kick target accepted")'
    Check $true 'self and unknown participant kick rejected'
    $seq=[int](Menu client).seq+1
    [IO.File]::WriteAllText("$root/client.menu.cmd.json",(@{seq=$seq;op='kick';text=(Menu host).playerId}|ConvertTo-Json -Compress))
    Await 'guest kick denied' {(Menu client).error -like '*방장만*'}
    Control client clear
    Check ((Menu host).count -eq 2 -and (Menu client).joined) 'guest cannot kick host through direct API'
    Control host capture
    Start-Sleep -Milliseconds 500
    Copy-Item "$root/host.menu.png" "$root/Lobby.png"
    Kick host client
    Async service 'var m=SSW.MultiplayerSessionManager.Current;var lobby=await Unity.Services.Lobbies.LobbyService.Instance.GetLobbyAsync(m.RoomId);if(lobby.Players.Count!=1||lobby.Players[0].Id!=Unity.Services.Authentication.AuthenticationService.Instance.PlayerId)throw new System.Exception("Service membership was not removed")'
    Check $true 'UGS service membership confirms the kick'
    Control client job '' '' 6
    Join client host
    Roster host Witch Gunner
    Leave client
    Leave host
    Create client
    Join host client
    Roster client Witch Gunner
    Kick client host
    Join host client
    Leave host
    Leave client
    Check (-not (Menu host).error -and -not (Menu client).error) 'no unexpected probe errors'
    foreach($peer in @('host','client')){
        $state=Get-Content "$root/$peer.json" -Raw|ConvertFrom-Json
        Check (-not $state.error) "$peer has no runtime error"
    }
    @{checks=$checks;previewChecks=$preview.checks.Count;scope='same PC editor and development player, real UGS Lobby and Relay';twoPc=$false}|ConvertTo-Json -Depth 6|Set-Content "$root/Result.json"
}finally{
    if($player -and -not $player.HasExited){
        try{Leave client;$n=Get-Content "$root/client.json" -Raw|ConvertFrom-Json;[IO.File]::WriteAllText("$root/client.cmd.json",(@{seq=[int]$n.seq+1;op='quit'}|ConvertTo-Json -Compress));Await 'test player exits' {$player.HasExited} 12}catch{if(-not $player.HasExited){Stop-Process -Id $player.Id}}
    }
    if($ready){try{Async cleanup 'if(SSW.MultiplayerSessionManager.Current!=null)await SSW.MultiplayerSessionManager.Current.CancelAsync()'}catch{Write-Output $_.Exception.Message}}
    & $Cli command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    if($previous){
        Start-Sleep -Seconds 2
        $restore=if($previous.hadJob){'UnityEngine.PlayerPrefs.SetInt(SSW.PlayerJobStorage.PreferenceKey,'+$previous.job+');'}else{'UnityEngine.PlayerPrefs.DeleteKey(SSW.PlayerJobStorage.PreferenceKey);'}
        Eval ($restore+'UnityEngine.PlayerPrefs.Save();UnityEditor.SceneManagement.EditorSceneManager.OpenScene("'+$previous.scene+'");return true;')|Out-Null
    }
}

param([string]$Run=(Get-Date -Format yyyyMMddHHmmss),[string]$Build='Builds/Rooms/Game.exe',[switch]$SkipAbrupt,[ValidateSet('all','failures','recovery','shutdown')][string]$Stage='all')
. "$PSScriptRoot/Common.ps1"
$root="$Project/Logs/Room27/$Run"
if(Test-Path $root){throw 'Use a new run name'}
[IO.Directory]::CreateDirectory($root)|Out-Null
$root|Set-Content "$Project/Logs/Room27/AfterRoot.txt"
$checks=[Collections.Generic.List[string]]::new()
$owned=[Collections.Generic.List[string]]::new()
$player=$null
$ready=$false
function Check($Condition,[string]$Name){
    if(-not $Condition){throw $Name}
    $checks.Add($Name)
    [IO.File]::WriteAllLines("$root/Checks.txt",$checks)
    Write-Output "PASS $Name"
}
function Net([string]$Peer,[string]$Op){
    $state=Get-Content "$root/$Peer.json" -Raw|ConvertFrom-Json
    [IO.File]::WriteAllText("$root/$Peer.cmd.json",(@{seq=[int]$state.seq+1;op=$Op}|ConvertTo-Json -Compress))
}
function Launch {
    foreach($suffix in @('.cmd.json','.menu.cmd.json','.json','.menu.json')){
        $path="$root/client$suffix"
        if(Test-Path $path){Copy-Item -LiteralPath $path -Destination "$path.$([DateTime]::UtcNow.Ticks)";Remove-Item -LiteralPath $path}
    }
    $script:player=Start-Process -FilePath "$Project/$Build" -WorkingDirectory $Project -ArgumentList @('--net-profile','room-runtime27','--net-probe',"$root/client",'-logFile',"$root/client-$([DateTime]::UtcNow.Ticks).log",'-screen-fullscreen','0','-screen-width','960','-screen-height','540') -WindowStyle Hidden -PassThru
    $player.Id|Set-Content "$root/PlayerPid.txt"
    Await 'player probe' {Test-Path "$root/client.json"} 40
    Await 'player started' {(Get-Item "$root/client.json").LastWriteTimeUtc -gt $player.StartTime.ToUniversalTime()} 40
    if((Menu client).scene -ne 'MainMenu'){Net client exit}
    Await 'player MainMenu' {@((Menu client).buttons|Where-Object name -eq Button_Play).Count -gt 0} 45
    Control client profile '' 'room-runtime27'
    Await 'isolated player login' {(Menu client).profile -eq 'room-runtime27' -and (Menu client).signedIn -and -not (Menu client).busy} 40
}
function OpenPlay([string]$Peer){
    Await "$Peer visible navigation" {@((Menu $Peer).buttons|Where-Object {$_.enabled -and $_.name -in @('Button_Play','Button_방 만들기','뒤로Button')}).Count -gt 0}
    $m=Menu $Peer
    if(@($m.buttons|Where-Object name -eq Button_Play).Count){Control $Peer click Button_Play}
    elseif(@($m.buttons|Where-Object name -eq '뒤로Button').Count){Control $Peer click '뒤로Button'}
    Await "$Peer play choices" {@((Menu $Peer).buttons|Where-Object {$_.name -eq 'Button_방 만들기' -and $_.enabled}).Count -eq 1}
}
function Create([string]$Peer,[string]$Name){
    OpenPlay $Peer
    Control $Peer click 'Button_방 만들기'
    Control $Peer text '방 이름Input' "Room27-$Run-$Name"
    Control $Peer toggle '방 목록에서 숨기기Toggle' '' 0
    Control $Peer click '방 만들기Button'
    Await "$Peer created $Name" {(Menu $Peer).joined -and (Menu $Peer).host -and -not (Menu $Peer).busy} 60
    $id=(Menu $Peer).roomId
    if($Peer -eq 'client'){$owned.Add($id)}
    return $id
}
function Browse([string]$Peer){
    if(@((Menu $Peer).buttons|Where-Object name -eq '새로고침Button').Count){return}
    OpenPlay $Peer
    Control $Peer click 'Button_방 들어가기'
    Await 'browse ready' {-not (Menu $Peer).busy}
}
function Join([string]$Guest,[string]$Owner){
    Browse $Guest
    Control $Guest text '참가 코드Input' (Menu $Owner).code
    Control $Guest click '코드 참가Button'
    try {
        Await "$Guest joins $Owner" {(Menu $Guest).joined -and (Menu $Guest).listening -and (Menu $Owner).canStart} 60
    } catch {
        @{guest=(Menu $Guest);owner=(Menu $Owner)}|ConvertTo-Json -Depth 8|Set-Content "$root/JoinFailure.json"
        throw
    }
    Check ((Menu $Guest).config -eq (Menu $Owner).config) 'matching network configurations after join'
}
function Leave([string]$Peer){
    if(-not (Menu $Peer).joined){return}
    Await "$Peer ready to leave" {-not (Menu $Peer).joined -or -not (Menu $Peer).busy}
    if(-not (Menu $Peer).joined){return}
    Control $Peer click '방 나가기Button'
    Await "$Peer leaves" {-not (Menu $Peer).joined -and -not (Menu $Peer).listening -and -not (Menu $Peer).busy} 35
}
function Service([string]$Name,[string]$Id){
    $out="$root/$Name-service.json"
    $code='bool exists=true;int players=0;try{var lobby=await Unity.Services.Lobbies.LobbyService.Instance.GetLobbyAsync("'+$Id+'");players=lobby.Players.Count;}catch(Unity.Services.Lobbies.LobbyServiceException e)when(e.Reason==Unity.Services.Lobbies.LobbyExceptionReason.LobbyNotFound){exists=false;}var q=await Unity.Services.Multiplayer.MultiplayerService.Instance.QuerySessionsAsync(new Unity.Services.Multiplayer.QuerySessionsOptions{Count=100});bool listed=System.Linq.Enumerable.Any(q.Sessions,s=>s.Id=="'+$Id+'");System.IO.File.WriteAllText("'+$out+'",Newtonsoft.Json.JsonConvert.SerializeObject(new{exists,listed,players}))'
    for($attempt=0;$attempt -lt 5;$attempt++){
        try {Async "$Name-request-$attempt" $code;break}
        catch {if($attempt -eq 4 -or $_.Exception.Message -notmatch '429|Too Many Requests|RateLimit'){throw};Start-Sleep -Seconds 2}
    }
    Get-Content $out -Raw|ConvertFrom-Json
}
try {
    & $Cli command editor_play --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
    Start-Sleep -Seconds 4
    for($attempt=0;$attempt -lt 8;$attempt++){
        try {$ready=Eval 'return UnityEditor.EditorApplication.isPlaying;';if($ready){break}}catch{}
        Start-Sleep -Seconds 2
    }
    if(-not $ready){throw 'Editor did not enter play mode'}
    Async init 'await Unity.Services.Core.UnityServices.InitializeAsync();var auth=Unity.Services.Authentication.AuthenticationService.Instance;if(auth.Profile!="room-editor27"){auth.SignOut();auth.SwitchProfile("room-editor27");}if(!auth.IsSignedIn)await auth.SignInAnonymouslyAsync()'
    Eval ('UnityEngine.Application.runInBackground=true;SSW.MultiplayerSessionManager.GetOrCreate();var g=SSW.NetGame.GetOrCreate();g.gameObject.AddComponent<SSW.MenuProbe>().Init("'+$root+'/host");g.gameObject.AddComponent<SSW.NetProbe>().Init("'+$root+'/host");return true;')|Out-Null
    Launch
    Check ((Menu host).protocol -eq (Menu client).protocol) 'matching editor and player protocols'
    if($Stage -eq 'all'){
    $solo=Create host solo
    Browse client
    Await 'solo appears' {@((Menu client).rooms|Where-Object id -eq $solo).Count -eq 1} 15
    Leave host
    Check (-not (Service solo $solo).exists) 'last participant normal leave deletes the service room'
    Await 'automatic stale row removal' {@((Menu client).rooms|Where-Object id -eq $solo).Count -eq 0} 15
    Check $true 'open browser removes departed rooms without pressing refresh'
    $pair=Create host pair
    Join client host
    Leave host
    Await 'guest session ends' {-not (Menu client).joined -and -not (Menu client).listening -and -not (Menu client).busy} 30
    Check (-not (Service pair $pair).exists) 'host leave with a guest deletes the room and guest session'
    $repeat=Create host repeat
    Join client host
    Leave client
    Await 'host remains available' {(Menu host).count -eq 1 -and (Menu host).host -and (Menu host).listening}
    $state=Service guestLeave $repeat
    Check ($state.exists -and $state.listed -and $state.players -eq 1) 'guest leave preserves the healthy host room'
    Join client host
    Check $true 'guest rejoins the same healthy room'
    Leave client
    Leave host
    Check (-not (Service finalLeave $repeat).exists) 'last participant deletes a reused room'
    }
    if($Stage -eq 'all' -or $Stage -eq 'failures'){
    foreach($after in @($false,$true)){
        $name=if($after){'lostResponse'}else{'failedRequest'}
        $id=Create host $name
        Async $name ('var m=SSW.MultiplayerSessionManager.Current;var f=typeof(SSW.MultiplayerSessionManager).GetField("_session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var s=(Unity.Services.Multiplayer.ISession)f.GetValue(m);SSW.SessionFault.Arm(s,'+$after.ToString().ToLowerInvariant()+',"DeleteLobbyAsync");bool failed=false;try{await m.LeaveRoomAsync();}catch(Unity.Services.Multiplayer.SessionException){failed=true;}if(!failed||!m.IsInSession)throw new System.InvalidOperationException("Failed deletion lost its retry handle")')
        Check ((Menu host).joined -or $after -and -not (Menu host).listening) "$name retains retry state or completes a confirmed deletion"
        Check ((Service "$name-service" $id).exists -ne $after) "$name preserves the actual service outcome"
        if((Menu host).joined){Leave host}
        Await 'deletion completed' {-not (Menu host).joined -and -not (Menu host).busy}
        Check (-not (Service "$name-retry" $id).exists) "$name can retry deletion and create again"
    }
    $stale=Create client stale
    Browse host
    Await 'stale target cached' {@((Menu host).rooms|Where-Object id -eq $stale).Count -eq 1} 15
    Leave client
    Async staleJoin ('bool failed=false;try{await SSW.MultiplayerSessionManager.Current.JoinRoomAsync("'+$stale+'");}catch(Unity.Services.Multiplayer.SessionException){failed=true;}if(!failed)throw new System.InvalidOperationException("Deleted room accepted a join")')
    Check (@((Menu host).rooms|Where-Object id -eq $stale).Count -eq 0 -and -not (Menu host).joined) 'failed stale join removes its cached row and preserves recovery'
    }
    if($Stage -ne 'shutdown'){
    OpenPlay host
    Async cancelCreate ('var service=Unity.Services.Multiplayer.MultiplayerService.Instance;var before=await service.GetJoinedSessionIdsAsync();var m=SSW.MultiplayerSessionManager.Current;var create=m.CreateRoomAsync(new SSW.MultiplayerRoomRequest("Room27-cancelled",null,true));var cancel=m.CancelAsync();try{await create;}catch(System.OperationCanceledException){}await cancel;await System.Threading.Tasks.Task.Delay(1200);var joined=await service.GetJoinedSessionIdsAsync();var added=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(joined,id=>!before.Contains(id)));System.IO.File.WriteAllText("'+$root+'/Cancellation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{before,joined,added,local=m.IsInSession,connected=SSW.NetGame.Current.Connected}));if(m.IsInSession||SSW.NetGame.Current.Connected||added.Length!=0)throw new System.InvalidOperationException("Cancelled creation left a room or connection")')
    Check $true 'cancelling private room creation leaves no service membership or connection'
    Async queryRace 'var m=SSW.MultiplayerSessionManager.Current;var query=m.RefreshRoomsAsync(true);var create=m.CreateRoomAsync(new SSW.MultiplayerRoomRequest("Room27-query-race",null,true));await System.Threading.Tasks.Task.WhenAll(query,create);if(!m.IsInSession||m.Status.Length!=0)throw new System.InvalidOperationException("Old query overwrote the new room");await m.LeaveRoomAsync()'
    Check $true 'background query does not block or overwrite room creation'
    }
    $quit=Create client quit
    Join host client
    Net client quit
    Await 'normal player quit' {$player.HasExited} 15
    Await 'quit guest cleaned' {-not (Menu host).joined -and -not (Menu host).listening} 35
    Check (-not (Service normalQuit $quit).exists) 'normal application quit deletes the hosted room'
    if(-not $SkipAbrupt){
        Launch
        $killed=Create client killed
        Join host client
        Stop-Process -Id $player.Id
        Await 'host loss cleans guest' {-not (Menu host).joined -and -not (Menu host).listening} 55
        Check $true 'forced host termination releases the connected guest'
        $gone=$false
        for($i=0;$i -lt 12;$i++){
            $state=Service "expiry-$i" $killed
            if(-not $state.listed){$gone=$true;break}
            Start-Sleep -Seconds 5
        }
        Check $gone 'abandoned room leaves the active service query after expiry'
        $state|ConvertTo-Json|Set-Content "$root/Expiry.json"
        Launch
        foreach($id in $owned){Control client delete '' $id}
        Start-Sleep -Seconds 2
        Check (-not (Service ownCleanup $killed).exists) 'only the owned abandoned test room is explicitly deleted'
    }
    else {Launch}
    $last=Create host recovery
    Join client host
    Leave client
    Leave host
    Check (-not (Service recovery $last).exists) 'create join and leave still work after all lifecycle failures'
    @{source=(git rev-parse HEAD);protocol=(Menu host).protocol;stage=$Stage;checks=$checks;scope='same PC editor and development player, real UGS Lobby and Relay';editorLoopDriven=$true;twoPc=$false}|ConvertTo-Json -Depth 5|Set-Content "$root/Result.json"
}finally{
    if($player -and -not $player.HasExited){
        try {if((Menu client).joined){Leave client};foreach($id in $owned){Control client delete '' $id};Net client quit;Await 'test player exit' {$player.HasExited} 12}catch{if(-not $player.HasExited){Stop-Process -Id $player.Id}}
    }
    if($ready){try {Async cleanup 'if(SSW.MultiplayerSessionManager.Current!=null)await SSW.MultiplayerSessionManager.Current.CancelAsync()'}catch{Write-Output $_.Exception.Message}}
    & $Cli command editor_stop --caller plugin --skill unity-cli --project-path $Project --format json|Out-Null
}

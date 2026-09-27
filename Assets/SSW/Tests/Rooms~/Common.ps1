$ErrorActionPreference='Stop'
$Project=(Get-Location).Path.Replace('\','/')
$Cli=Join-Path $env:LOCALAPPDATA 'Unity/bin/unity.exe'
function Eval([string]$Code) {
    $file="$root/Eval-$([Guid]::NewGuid().ToString('N')).cs"
    [IO.File]::WriteAllText($file,$Code)
    $reply=& $Cli command eval_file --file $file --caller plugin --skill unity-cli --project-path $Project --format json | ConvertFrom-Json
    if(-not $reply.success -or -not $reply.data.result.success){throw ($reply|ConvertTo-Json -Depth 8)}
    $value=$reply.data.result.result
    if($value -is [string] -and ($value.StartsWith('{') -or $value.StartsWith('['))){return $value|ConvertFrom-Json}
    return $value
}
function Async([string]$Name,[string]$Code,[int]$Timeout=55) {
    $file="$root/$Name.json"
    $cs='async System.Threading.Tasks.Task Run(){try{'+$Code+';System.IO.File.WriteAllText("'+$file+'",Newtonsoft.Json.JsonConvert.SerializeObject(new{ok=true}));}catch(System.Exception e){System.IO.File.WriteAllText("'+$file+'",Newtonsoft.Json.JsonConvert.SerializeObject(new{ok=false,error=e.Message,type=e.GetType().Name}));}} _=Run();return true;'
    Eval $cs|Out-Null
    Await $Name {Test-Path -LiteralPath $file} $Timeout
    $result=Get-Content -LiteralPath $file -Raw|ConvertFrom-Json
    if(-not $result.ok){throw "$Name : $($result.error)"}
}
function Await([string]$Name,[scriptblock]$Test,[int]$Timeout=35) {
    $end=[DateTime]::UtcNow.AddSeconds($Timeout)
    do {if(& $Test){return};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow -lt $end)
    throw "Timeout: $Name"
}
function Menu([string]$Peer){
    for($attempt=0;$attempt -lt 5;$attempt++){
        try{$value=Get-Content -LiteralPath "$root/$Peer.menu.json" -Raw|ConvertFrom-Json;if($value){return $value}}catch{}
        Start-Sleep -Milliseconds 10
    }
    return $null
}
function Control([string]$Peer,[string]$Op,[string]$Target='',[string]$Text='',[int]$Value=0){
    if($Op -eq 'click'){Await "button $Peer $Target" {@((Menu $Peer).buttons|Where-Object {$_.name -eq $Target -and $_.enabled}).Count -gt 0}}
    $n=[int](Menu $Peer).seq+1
    [IO.File]::WriteAllText("$root/$Peer.menu.cmd.json",(@{seq=$n;op=$Op;target=$Target;text=$Text;value=$Value}|ConvertTo-Json -Compress))
    Await "$Peer $Op" {(Menu $Peer).seq -ge $n}
    if((Menu $Peer).error){throw (Menu $Peer).error}
}
function Session {
    Eval 'var m=SSW.MultiplayerSessionManager.Current;var f=typeof(SSW.MultiplayerSessionManager).GetField("_session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var s=(Unity.Services.Multiplayer.ISession)f.GetValue(m);return Newtonsoft.Json.JsonConvert.SerializeObject(new{id=s?.Id,host=s?.IsHost,state=s?.State.ToString(),network=s?.Network.State.ToString(),joined=m.IsInSession,count=m.PlayerCount,busy=m.IsBusy,status=m.Status,listening=SSW.NetGame.Current.Manager.IsListening,server=SSW.NetGame.Current.Manager.IsServer,connected=SSW.NetGame.Current.Connected,registered=Unity.Services.Multiplayer.MultiplayerService.Instance.Sessions.Count});'
}
function Query([string]$Name,[string]$Id){
    $out="$root/$Name-query.json"
    Async $Name ('var q=await Unity.Services.Multiplayer.MultiplayerService.Instance.QuerySessionsAsync(new Unity.Services.Multiplayer.QuerySessionsOptions{Count=100});System.IO.File.WriteAllText("'+$out+'",Newtonsoft.Json.JsonConvert.SerializeObject(System.Linq.Enumerable.Where(q.Sessions,s=>s.Id=="'+$Id+'")))')
    @(Get-Content -LiteralPath $out -Raw|ConvertFrom-Json)
}


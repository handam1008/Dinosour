if($VisualFile -notmatch '^[A-Za-z]+$'){throw 'Invalid visual test name'}
Map 0|Out-Null
Eval 'foreach(var p in SSW.NetGame.Current.Players){p.Block(true);p.Drive.Teleport(new UnityEngine.Vector2(p.IsOwner?-2f:2f,4f));p.Drive.Freeze(15f);}return true;'|Out-Null
foreach($peer in @('host','client')){Send $peer isolate;Send $peer wideground 0 0 3;Send $peer lag 60 10 0}
Eval ([IO.File]::ReadAllText("$PSScriptRoot/$VisualFile.cs").Replace('@ROOT@',$root).Replace('@QUICK@','true'))|ConvertTo-Json -Depth 5|Set-Content "$root/Launch.json"
$until=[DateTime]::UtcNow.AddMinutes(8)
do{
    $h=Read host;$c=Read client
    if($h.error -or $c.error){throw "Runtime error: $($h.error) $($c.error)"}
    $result=$null
    try{$result=Get-Content -Raw -LiteralPath "$root/$VisualFile.json" -ErrorAction Stop|ConvertFrom-Json}catch{}
    if($result.status -in @('completed','failed')){break}
    if([DateTime]::UtcNow -gt $until){throw 'Visual trial timeout'}
    Start-Sleep -Milliseconds 300
}while($true)
if($result.status -ne 'completed'){throw (@{error=$result.error;failures=$result.failures}|ConvertTo-Json -Depth 8)}
@{commit=$head;trial=$result;host=$h;client=$c}|ConvertTo-Json -Depth 20|Set-Content "$root/Result.json"
"PASS $VisualFile $($result.count) checks"

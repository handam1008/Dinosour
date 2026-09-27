if($EffectFile -notmatch '^[A-Za-z]+$'){throw 'Invalid effect test name'}
if($EffectFilter -notmatch '^[A-Za-z0-9:,]*$'){throw 'Invalid effect filter'}
Map 0|Out-Null
foreach($peer in @('host','client')){Send $peer isolate;Send $peer wideground 0 0 3;Send $peer lag 60 10 0}
$code=[IO.File]::ReadAllText("$PSScriptRoot/$EffectFile.cs").Replace('@ROOT@',$root).Replace('@FILTER@',$EffectFilter)
Eval $code|ConvertTo-Json -Depth 5|Set-Content "$root/Launch.json"
$until=[DateTime]::UtcNow.AddMinutes(12)
do{
    $h=Read host;$c=Read client
    if($h.error -or $c.error){throw "Runtime error: $($h.error) $($c.error)"}
    $result=$null
    try{$result=Get-Content "$root/$EffectFile.json" -Raw -ErrorAction Stop|ConvertFrom-Json}catch{}
    if($result.status -in @('completed','failed')){break}
    if([DateTime]::UtcNow -gt $until){throw 'Effect trial timeout'}
    Start-Sleep -Milliseconds 500
}while($true)
if($result.status -ne 'completed'){throw (@{error=$result.error;failures=$result.failures}|ConvertTo-Json -Depth 8)}
@{commit=$head;trial=$result;host=$h;client=$c}|ConvertTo-Json -Depth 20|Set-Content "$root/Result.json"
Write-Output "PASS $EffectFile $($result.count) checks: $root"

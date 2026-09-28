$introStart=@{}
$introEnd=@{}
$samples=[Collections.Generic.List[object]]::new()
$until=[DateTime]::UtcNow.AddSeconds(90)
do{
    $h=Read host;$c=Read client
    foreach($peer in @('host','client')){
        $value=if($peer -eq 'host'){$h}else{$c}
        if(-not $value){continue}
        if($value.error){throw $value.error}
        if($value.intro -and -not $value.introClosing -and -not $introStart.ContainsKey($peer)){$introStart[$peer]=$value.time}
        if($introStart.ContainsKey($peer) -and -not $introEnd.ContainsKey($peer) -and $value.phase -eq 'Draft'){$introEnd[$peer]=$value.time}
    }
    $samples.Add(@{host=$h;client=$c})
    if($introEnd.Count -eq 2){break}
    Start-Sleep -Milliseconds 40
}while([DateTime]::UtcNow -lt $until)
Check ($introEnd.Count -eq 2) 'both peers complete the VS introduction'
foreach($peer in @('host','client')){
    Check ($introEnd[$peer]-$introStart[$peer] -ge 3.5) "$peer VS remains for entrance plus three second hold"
}
@{start=$introStart;end=$introEnd;samples=$samples}|ConvertTo-Json -Depth 20|Set-Content "$root/Intro.json"

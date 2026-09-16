var state = new SSW.MatchState { First = 3, Second = 9, Set = 1, Round = 1, Phase = SSW.MatchPhase.Playing };
var draw = SSW.Rounds.Award(state, 0, true);
if (draw.FirstWins != 0 || draw.SecondWins != 0 || draw.Phase != SSW.MatchPhase.RoundEnd) throw new System.Exception("Draw counted");
if (!SSW.Rounds.Award(state, 77, false).Equals(state)) throw new System.Exception("Invalid winner counted");
state = SSW.Rounds.Award(state, 3, false);
if (state.FirstWins != 1 || state.FirstMarks != 1 || state.FirstSets != 0) throw new System.Exception("First round score wrong");
if (!SSW.Rounds.Award(state, 3, false).Equals(state)) throw new System.Exception("Duplicate death counted");
state.Phase = SSW.MatchPhase.Playing;
state.Round = 2;
state = SSW.Rounds.Award(state, 9, false);
state.Phase = SSW.MatchPhase.Playing;
state.Round = 3;
state = SSW.Rounds.Award(state, 3, false);
if (state.FirstMarks != 5 || state.SecondMarks != 2 || state.FirstSets != 1 || state.Phase != SSW.MatchPhase.SetEnd) throw new System.Exception("2:1 set score wrong");
state = SSW.Rounds.NextSet(state);
if (state.Set != 2 || state.Round != 1 || state.FirstMarks != 0 || state.FirstWins != 0 || state.FirstSets != 1) throw new System.Exception("Set reset wrong");
for (int loss = 1; loss <= 4; loss++)
    if (SSW.Rounds.JobReward(loss) != (loss == 1 || loss == 3)) throw new System.Exception("Personal loss rewards wrong");
foreach (ulong winner in new ulong[] { 9, 3, 9, 3, 9, 3 })
{
    for (byte round = 1; round <= 2; round++)
    {
        state.Phase = SSW.MatchPhase.Playing;
        state.Round = round;
        state = SSW.Rounds.Award(state, winner, false);
    }
    if (SSW.Rounds.Complete(state)) break;
    state = SSW.Rounds.NextSet(state);
}
if (state.FirstSets != 4 || state.SecondSets != 3 || !SSW.Rounds.Complete(state) || state.Set != 7) throw new System.Exception("Seven set finish wrong");
if (!SSW.Rounds.NextSet(state).Equals(state)) throw new System.Exception("Continued after match finish");
return "PASS: draw, invalid winner, duplicate death, 2:1 lamps, set reset, alternating personal loss rewards, 4:3 finish";

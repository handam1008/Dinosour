namespace SSW
{
    public static class Rounds
    {
        public const int ToWin = 2;
        public const int SetsToWin = 4;

        public static bool Complete(MatchState state) =>
            state.FirstSets >= SetsToWin || state.SecondSets >= SetsToWin;

        public static bool JobReward(int losses) => losses > 0 && losses % 2 == 1;

        public static bool Reportable(MatchState state) => state.Phase == MatchPhase.Finished
            && state.Reason == MatchEnd.Knockout && state.First != state.Second
            && (state.Winner == state.First && state.FirstSets == SetsToWin && state.SecondSets < SetsToWin
                || state.Winner == state.Second && state.SecondSets == SetsToWin && state.FirstSets < SetsToWin);

        public static MatchState Award(MatchState state, ulong winner, bool draw)
        {
            if (state.Phase != MatchPhase.Playing || Complete(state)) return state;
            if (!draw && winner != state.First && winner != state.Second) return state;
            state.Phase = MatchPhase.RoundEnd;
            state.Reason = draw ? MatchEnd.Draw : MatchEnd.Knockout;
            state.Winner = winner;
            if (draw) return state;
            if (winner == state.First)
            {
                state.FirstWins++;
                state.FirstMarks |= (byte)(1 << (state.Round - 1));
                if (state.FirstWins == ToWin)
                {
                    state.FirstSets++;
                    state.Phase = MatchPhase.SetEnd;
                }
            }
            else
            {
                state.SecondWins++;
                state.SecondMarks |= (byte)(1 << (state.Round - 1));
                if (state.SecondWins == ToWin)
                {
                    state.SecondSets++;
                    state.Phase = MatchPhase.SetEnd;
                }
            }
            return state;
        }

        public static MatchState NextSet(MatchState state)
        {
            if (state.Phase != MatchPhase.SetEnd || Complete(state)) return state;
            state.Set++;
            state.Round = 1;
            state.FirstWins = 0;
            state.SecondWins = 0;
            state.FirstMarks = 0;
            state.SecondMarks = 0;
            state.Phase = MatchPhase.Draft;
            return state;
        }
    }
}

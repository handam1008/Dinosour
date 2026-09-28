namespace RYU._01.Script.Leaderboard
{
    // 순위(0부터) → 티어. 인덱스 순서는 티어 아이콘 배열과 같다 (0 원숭이 ~ 5 멸종)
    public static class RankTier
    {
        public static readonly string[] Names = { "원숭이급", "공룡급", "마그마급", "메테오급", "빙하기급", "멸종급" };
        public const string Unranked = "알급";

        public static int FromRank(int rank)
        {
            if (rank < 1) return 5;
            if (rank < 6) return 4;
            if (rank < 16) return 3;
            if (rank < 31) return 2;
            if (rank < 48) return 1;
            return 0;
        }
    }
}

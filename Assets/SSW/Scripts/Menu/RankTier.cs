namespace SSW
{
    public static class RankTier
    {
        static readonly string[] Keys = { "Monkey", "Dino", "Magma", "Meteor", "IceAge", "Extinct" };

        public static string Key(int rank) => Keys[Index(rank)];

        public static int Index(int rank)
        {
            if (rank < 1) return 5;
            if (rank < 6) return 4;
            if (rank < 16) return 3;
            if (rank < 31) return 2;
            return rank < 48 ? 1 : 0;
        }
    }
}

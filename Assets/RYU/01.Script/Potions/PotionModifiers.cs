namespace RYU._01.Script.Potions
{
    public readonly struct PotionModifiers
    {
        public readonly float Splash;
        public readonly float TickCount;
        public readonly float Power;
        public readonly bool LeaveZone;
        public readonly int Bounce;

        public PotionModifiers(float splash, float tickCount, float power, bool leaveZone, int bounce)
        {
            Splash = splash;
            TickCount = tickCount;
            Power = power;
            LeaveZone = leaveZone;
            Bounce = bounce;
        }

        public static PotionModifiers None => new PotionModifiers(1f, 1f, 1f, false, 0);

        public PotionModifiers ForZone(float powerScale)
        {
            return new PotionModifiers(Splash, TickCount, Power * powerScale, false, 0);
        }
    }
}

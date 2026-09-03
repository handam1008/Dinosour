namespace RYU._01.Script.Potions
{
    // 증강으로 바뀌는 값들을 한 묶음으로 들고 다닌다.
    // 증강이 늘어나면 파라미터가 아니라 이 구조체에 필드만 추가하면 된다.
    public readonly struct PotionModifiers
    {
        public readonly float Splash;    // 넓은 살포: 폭발 반경 배율
        public readonly float TickCount; // 진한 농도: 지속효과 틱 횟수 배율
        public readonly float Power;     // 정밀 조제: 개당 위력 배율
        public readonly bool LeaveZone;  // 잔류형: 바닥 장판 생성 여부
        public readonly int Bounce;      // 깨진 유리병: 추가로 튕기는 횟수

        public PotionModifiers(float splash, float tickCount, float power, bool leaveZone, int bounce)
        {
            Splash = splash;
            TickCount = tickCount;
            Power = power;
            LeaveZone = leaveZone;
            Bounce = bounce;
        }

        // 증강이 하나도 없을 때의 기본값
        public static PotionModifiers None => new PotionModifiers(1f, 1f, 1f, false, 0);

        // 장판용: 위력을 줄이고, 장판이 또 장판을 만들지 않게 막는다
        public PotionModifiers ForZone(float powerScale)
        {
            return new PotionModifiers(Splash, TickCount, Power * powerScale, false, 0);
        }
    }
}

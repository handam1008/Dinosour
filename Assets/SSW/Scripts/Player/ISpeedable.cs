namespace SSW
{
    /// <summary>이동속도 증가 효과를 받을 수 있는 대상입니다.</summary>
    public interface ISpeedable
    {
        /// <param name="amount">0.2는 이동속도 20% 증가를 의미합니다.</param>
        /// <param name="duration">효과 지속 시간(초)입니다.</param>
        void ApplySpeed(float amount, float duration);
    }
}

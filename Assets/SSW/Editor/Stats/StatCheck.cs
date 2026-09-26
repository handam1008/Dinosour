using System;
using System.Reflection;
using UnityEngine;

namespace SSW
{
    public static class StatCheck
    {
        public static void Validate(FighterStats stats)
        {
            if (stats.Job == PlayerJob.None || !Enum.IsDefined(typeof(PlayerJob), stats.Job))
                throw new InvalidOperationException($"지원하지 않는 직업 수치: {stats.Job}");
            foreach (FieldInfo field in typeof(FighterStats).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(float)) Finite((float)field.GetValue(stats), stats.Job, field.Name);
            }
            foreach (FieldInfo field in typeof(FlightStats).GetFields(BindingFlags.Public | BindingFlags.Instance))
                Finite((float)field.GetValue(stats.Flight), stats.Job, "Flight." + field.Name);
            Finite(stats.HitOffset.x, stats.Job, "HitOffset.x");
            Finite(stats.HitOffset.y, stats.Job, "HitOffset.y");
            Finite(stats.HitSize.x, stats.Job, "HitSize.x");
            Finite(stats.HitSize.y, stats.Job, "HitSize.y");
            Require(stats.Health > 0f, stats.Job, "Health는 0보다 커야 합니다.");
            Require(stats.MoveSpeed >= 0f && stats.JumpSpeed >= 0f && stats.Gravity >= 0f,
                stats.Job, "이동·점프·중력은 음수일 수 없습니다.");
            Require(stats.Coyote >= 0f && stats.Decay >= 0f && stats.Brake >= 0f,
                stats.Job, "이동 보정 값은 음수일 수 없습니다.");
            Require(stats.Damage >= 0f && stats.SkillDamage >= 0f && stats.DashDamage >= 0f,
                stats.Job, "피해는 음수일 수 없습니다.");
            Require(stats.AttackInterval >= 0f && stats.AttackTime >= 0f && stats.SkillCooldown >= 0f
                && stats.DashTime >= 0f && stats.DashCooldown >= 0f && stats.ParryTime >= 0f
                && stats.ParryCooldown >= 0f, stats.Job, "공격·스킬 시간은 음수일 수 없습니다.");
            Require(stats.Capacity >= 0 && stats.Capacity <= 15, stats.Job, "Capacity는 네트워크 한계인 15를 넘을 수 없습니다.");
            Require(stats.Flight.Speed >= 0f && stats.Flight.Gravity >= 0f && stats.Flight.Life >= 0f
                && stats.Flight.Scale >= 0f, stats.Job, "투사체 수치는 음수일 수 없습니다.");
            Require(stats.Flight.Aspect > 0f, stats.Job, "투사체 세로/가로 비율은 0보다 커야 합니다.");
            Require(stats.GravityDelay >= 0f && stats.ExtraGravity >= 0f && stats.Splash >= 0f,
                stats.Job, "지연 중력·폭발 반경은 음수일 수 없습니다.");
            if (stats.Job != PlayerJob.Swordsman)
                Require(stats.Flight.Life > 0f && stats.Flight.Scale > 0f, stats.Job, "투사체 수명·크기가 필요합니다.");
            if (stats.Job == PlayerJob.Gunner || stats.Job == PlayerJob.Gambler)
                Require(stats.Capacity > 0 && stats.Reload > 0f, stats.Job, "탄수와 충전 시간은 0보다 커야 합니다.");
            if (stats.Job == PlayerJob.Gunner)
                Require(stats.ChargeTime > 0f && stats.ChargeDamage >= 0f, stats.Job, "강화 시간·배율을 확인하세요.");
            if (stats.Job == PlayerJob.Witch)
                Require(stats.BrewTime > 0f && stats.Splash > 0f && stats.Capacity > 0,
                    stats.Job, "물약 생성 시간·폭발 반경·보유 수를 확인하세요.");
            if (stats.Job == PlayerJob.Magician)
                Require(stats.RollInterval > 0f && stats.MirrorScale > 0f, stats.Job, "카드 변경 간격·복제 크기를 확인하세요.");
            if (stats.Job == PlayerJob.Assassin || stats.Job == PlayerJob.Swordsman)
                Require(stats.HitSize.x > 0f && stats.HitSize.y > 0f, stats.Job, "근접 공격 크기는 0보다 커야 합니다.");
            Require(stats.DashSpeed >= 0f && stats.DashRadius >= 0f && stats.ReflectSpeed >= 0f,
                stats.Job, "대쉬·패링 수치는 음수일 수 없습니다.");
        }

        static void Finite(float value, PlayerJob job, string field)
            => Require(!float.IsNaN(value) && !float.IsInfinity(value), job, field + " 값이 유효하지 않습니다.");

        static void Require(bool valid, PlayerJob job, string message)
        {
            if (!valid) throw new InvalidOperationException(job + ": " + message);
        }
    }
}

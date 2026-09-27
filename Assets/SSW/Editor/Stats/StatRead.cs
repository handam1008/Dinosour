using System;
using System.Collections.Generic;
using KDH.Scripts.Bullet;
using KDH.Scripts.Gun;
using NKY.Scripts;
using NKY.Scripts.Skill;
using RYU._01.Script.Potions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SSW
{
    public sealed class StatRead
    {
        public FighterStats Stats { get; private set; }
        public AbstractPotion[] Potions { get; private set; } = Array.Empty<AbstractPotion>();
        public string Source { get; private set; }
        public string Report => Source + "\n" + JsonUtility.ToJson(Stats) + "\n" + string.Join("\n", _notes);
        readonly List<string> _notes = new List<string>();

        public static StatRead Read(StatSources.Entry entry)
        {
            if ((entry.Prefab != null) == (entry.Scene != null))
                throw new InvalidOperationException($"{entry.Job}: Prefab 또는 Scene 중 하나를 지정하세요.");
            var result = new StatRead();
            if (entry.Prefab != null)
            {
                result.Source = entry.Job + ": " + AssetDatabase.GetAssetPath(entry.Prefab);
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(entry.Prefab)) || !PrefabUtility.IsPartOfPrefabAsset(entry.Prefab))
                    throw new InvalidOperationException(result.Source + " 프리팹 에셋이 필요합니다.");
                result.Extract(entry.Prefab, entry.Job);
                return result;
            }

            string path = AssetDatabase.GetAssetPath(entry.Scene);
            Scene open = SceneManager.GetSceneByPath(path);
            if (open.IsValid() && open.isLoaded && open.isDirty)
                throw new InvalidOperationException(path + " 원본 씬에 저장하지 않은 변경이 있습니다. 저장 후 수치를 갱신하세요.");
            result.Source = entry.Job + ": " + path + "/" + entry.Root;
            Scene preview = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                result.Extract(Root(preview, entry.Root), entry.Job);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
            return result;
        }

        void Extract(GameObject root, PlayerJob job)
        {
            using var health = new StatValue(StatValue.Root<Health>(root));
            using var motion = new StatValue(StatValue.Root<PlayerController>(root));
            var stats = new FighterStats
            {
                Job = job,
                Health = health.Number("maxHealth"),
                MoveSpeed = motion.Number("_moveSpeed"),
                JumpSpeed = motion.Number("_jumpForce"),
                Gravity = Gravity(root),
                Coyote = motion.Number("_coyoteTime"),
                Decay = motion.Number("_externalVelocityDecay"),
                Brake = motion.Number("_counterMoveBrake"),
                Flight = FlightStats.Default
            };
            switch (job)
            {
                case PlayerJob.Magician: Magician(root, ref stats); break;
                case PlayerJob.Witch: Witch(root, ref stats); break;
                case PlayerJob.Gunner: Gunner(root, ref stats); break;
                case PlayerJob.Gambler: Gambler(root, ref stats); break;
                case PlayerJob.Assassin: Assassin(root, ref stats); break;
                case PlayerJob.Swordsman: Sword(root, ref stats); break;
                default: throw new InvalidOperationException("지원하지 않는 원본 직업: " + job);
            }
            StatCheck.Validate(stats);
            Stats = stats;
        }

        void Magician(GameObject root, ref FighterStats stats)
        {
            using var roller = new StatValue(StatValue.One<NumberRoller>(root));
            stats.AttackInterval = roller.Number("_cooldownDuration");
            stats.RollInterval = roller.Number("_tickInterval");
            stats.MirrorScale = roller.Number("_mirrorScaleMultiplier");
            stats.Damage = 5f;
            stats.Flight = new FlightStats
            {
                Speed = roller.Number("_flySpeed"),
                Gravity = roller.Number("_flyGravityScale"),
                Life = roller.Number("_flyMaxLifetime"),
                Spin = roller.Number("_flySpin"),
                Scale = roller.Number("_flyScale"),
                Aspect = 1f
            };
            _notes.Add("유지: Damage=5는 동적으로 생성되는 FlyingCard의 코드 기본값. 문양별 효과 수치는 기존 FlyingCard에서 소비하며 이 모델에 복사하지 않음.");
        }

        void Witch(GameObject root, ref FighterStats stats)
        {
            using var potion = new StatValue(StatValue.One<RandomPotion>(root));
            Potions = potion.References<AbstractPotion>("potions");
            if (Potions.Length == 0) throw new InvalidOperationException("Witch: 기본 포션 목록이 비었습니다.");
            stats.AttackInterval = 0.15f;
            stats.Capacity = 2;
            stats.BrewTime = potion.Number("cycleTime");
            float spread = potion.Number("spreadAngle");
            stats.Spread = spread > 0f ? spread : 24f;
            float speed = potion.Number("potionSpeed");
            stats.ThrowLift = potion.Number("Angle") * speed;
            Vector2 scale = Vector2.zero;
            for (int i = 0; i < Potions.Length; i++)
            {
                using var data = new StatValue(Potions[i]);
                GameObject prefab = data.Reference<GameObject>("PotionPrefab");
                using var splash = new StatValue(StatValue.One<Potion>(prefab));
                float radius = splash.Number("splashRadious");
                Vector2 size = Scale(prefab);
                if (i > 0 && (!Mathf.Approximately(stats.Splash, radius)
                    || !Mathf.Approximately(scale.x, size.x) || !Mathf.Approximately(scale.y, size.y)))
                    throw new InvalidOperationException("Witch: 포션별 반경·크기가 다릅니다. 공통 FlightStats로 값을 손실 없이 표현할 수 없습니다.");
                stats.Splash = radius;
                scale = size;
            }
            stats.Flight = new FlightStats { Speed = speed, Gravity = 3f, Life = 6f, Spin = -360f,
                Scale = scale.x, Aspect = scale.y / scale.x };
            _notes.Add("유지: Capacity=2와 Gravity=3은 RandomPotion 코드의 보유 한도·발사 덮어쓰기. AttackInterval=.15, Life=6, Spin=-360은 대응 원본 설정이 없는 기존 네트워크 값.");
            _notes.Add("포션 효과·weight는 원본 SO 직접 참조. 기본 목록만 NetStock._base에 연결하며 기존 _potions ID 순서는 보존.");
        }

        void Gunner(GameObject root, ref FighterStats stats)
        {
            using var gun = new StatValue(StatValue.One<KDH_Gun>(root));
            GameObject prefab = gun.Reference<GameObject>("<BulletPrefab>k__BackingField");
            using var bullet = new StatValue(StatValue.One<KDH_Bullet>(prefab));
            Vector2 size = Scale(prefab);
            stats.Capacity = gun.Integer("<MaxAmmo>k__BackingField");
            int slots = gun.At("<AmmoPrefabs>k__BackingField").arraySize;
            if (slots != stats.Capacity) throw new InvalidOperationException($"Gunner: MaxAmmo {stats.Capacity}와 원본 AmmoPrefabs 슬롯 {slots}가 다릅니다.");
            stats.Reload = gun.Number("<ChargeSpeed>k__BackingField");
            stats.ChargeTime = 3f;
            stats.ChargeDamage = bullet.Number("<UpgradValue>k__BackingField");
            stats.Damage = bullet.Number("<Damage>k__BackingField");
            stats.Flight = new FlightStats
            {
                Speed = bullet.Number("<Speed>k__BackingField"),
                Gravity = Gravity(prefab),
                Life = 3f,
                Scale = size.x,
                Aspect = size.y / size.x
            };
            _notes.Add("유지: AttackInterval=0은 원본에 발사 간격 제한이 없기 때문. KDH_Gun.AttackSpeed/ReloadSpeed는 원본 미사용 필드라 무시. ChargeTime=3은 KDH_Ammo 코드값, Life=3은 대응 원본 설정이 없는 네트워크 수명.");
        }

        void Gambler(GameObject root, ref FighterStats stats)
        {
            using var shooter = new StatValue(StatValue.One<GamblerCoinShooter>(root));
            using var magazine = new StatValue(shooter.Reference<GamblerMagazine>("magazine"));
            using var pool = new StatValue(shooter.Reference<GamblerCoinPool>("coinPool"));
            GamblerCoinProjectile normal = pool.Reference<GamblerCoinProjectile>("normalCoinPrefab");
            GamblerCoinProjectile roulette = pool.Reference<GamblerCoinProjectile>("rouletteCoinPrefab");
            stats.Damage = shooter.Number("coinDamage") * shooter.Number("damageMultiplier");
            stats.AttackInterval = shooter.Number("fireCooldown");
            stats.Capacity = magazine.Integer("magazineSize");
            stats.Reload = magazine.Number("reloadTime");
            stats.Flight = Coin(normal, out float delay, out float extra);
            FlightStats gold = Coin(roulette, out float goldDelay, out float goldExtra);
            if (!stats.Flight.Equals(gold) || !delay.Equals(goldDelay) || !extra.Equals(goldExtra))
                throw new InvalidOperationException("Gambler: 일반·룰렛 코인의 비행 수치가 다릅니다. 공통 FlightStats로 표현할 수 없습니다.");
            stats.GravityDelay = delay;
            stats.ExtraGravity = extra;
            _notes.Add("유지: Spin=720은 애니메이션으로 회전하는 원본 코인과 구분되는 기존 네트워크 시각 값. 코인 프리팹에 남은 비직렬화 coinDamage 대신 발사기 coinDamage*damageMultiplier를 사용. ExtraGravity는 원본 force/mass로 환산한 가속도.");
            _notes.Add("미포함: 잭팟 확률·추가 효과는 FighterStats 계약 밖이며 기존 CoinCast 경로를 유지.");
        }

        void Assassin(GameObject root, ref FighterStats stats)
        {
            AssassinMeleeAttack weapon = StatValue.One<AssassinMeleeAttack>(root);
            using var attack = new StatValue(weapon);
            using var module = new StatValue(StatValue.One<PlayerSkillModule>(root));
            using var skill = new StatValue(module.Reference<AssassinNormalSkillSo>("_skillData"));
            AssassinNormalSkill projectile = skill.Reference<AssassinNormalSkill>("_skillPrefab");
            stats.Damage = attack.Number("damage") * attack.Number("damageMultiplier");
            stats.AttackInterval = attack.Number("attackCooldown");
            Vector2 localOrigin = root.transform.InverseTransformPoint(weapon.transform.position);
            Vector2 worldSize = attack.Vector("hitboxSize");
            Vector3 scale = root.transform.lossyScale;
            if (Mathf.Abs(scale.x) < 0.0001f || Mathf.Abs(scale.y) < 0.0001f)
                throw new InvalidOperationException("Assassin: 플레이어 크기가 0입니다.");
            stats.HitSize = new Vector2(worldSize.x / Mathf.Abs(scale.x), worldSize.y / Mathf.Abs(scale.y));
            stats.HitOffset = localOrigin + new Vector2(attack.Number("offset") / Mathf.Abs(scale.x), 0f);
            stats.SkillCooldown = skill.Number("<SkillCooldown>k__BackingField");
            stats.SkillDamage = skill.Number("<Damage>k__BackingField");
            Vector2 size = Scale(projectile.gameObject);
            stats.Flight = new FlightStats
            {
                Speed = skill.Number("<ThrowSpeed>k__BackingField"),
                Gravity = Gravity(projectile.gameObject),
                Life = skill.Number("<DestroyTime>k__BackingField"),
                Scale = size.x,
                Aspect = size.y / size.x
            };
            _notes.Add($"근접: 원본 OverlapBox 월드 크기를 플레이어 좌표로 변환. +X 조준 기준 HitSize={stats.HitSize}, HitOffset={stats.HitOffset}. 고정 무기 원점={localOrigin}; 회전 각도·애니메이션 시간은 모델 밖.");
        }

        void Sword(GameObject root, ref FighterStats stats)
        {
            using var attack = new StatValue(StatValue.One<KHG_nomalAttack>(root));
            using var dash = new StatValue(StatValue.One<KHG_Dash>(root));
            using var parry = new StatValue(StatValue.One<KHG_Paring>(root));
            BoxCollider2D box = attack.Reference<BoxCollider2D>("attackCollider");
            Box(root.transform, box, out stats.HitSize, out stats.HitOffset);
            stats.Damage = attack.Number("damage");
            stats.AttackInterval = attack.Number("attackCooldown");
            stats.AttackTime = attack.Number("attackDuration");
            stats.DashSpeed = dash.Number("_dashSpeed");
            stats.DashTime = dash.Number("_dashDuration");
            stats.DashCooldown = dash.Number("_dashCooldown");
            stats.DashDamage = dash.Number("damage");
            stats.DashRadius = dash.Number("hitRadius");
            stats.ParryTime = parry.Number("parryTime");
            stats.ParryCooldown = parry.Number("parryCooldown");
            stats.ReflectSpeed = parry.Number("reflectSpeed");
            _notes.Add($"근접: 원본 collider 네 모서리를 플레이어 좌표로 변환한 정지 자세 AABB. HitSize={stats.HitSize}, HitOffset={stats.HitOffset}. 회전한 원본 OBB·애니메이션 스윕과 동일 판정은 아님.");
            _notes.Add("미포함: 패링 박스 크기·offset은 FighterStats에 대응 필드가 없어 기존 서버 판정을 유지.");
        }

        static FlightStats Coin(GamblerCoinProjectile coin, out float delay, out float extra)
        {
            using var source = new StatValue(coin);
            using var gravity = new StatValue(StatValue.One<CoinGravity>(coin.gameObject));
            using var body = new StatValue(source.Reference<Rigidbody2D>("body"));
            float mass = body.Number("m_Mass");
            if (!float.IsFinite(mass) || mass <= 0f) throw new InvalidOperationException(coin.name + ": 코인 질량은 0보다 커야 합니다.");
            delay = gravity.Number("gravityDelay");
            extra = gravity.Number("extraGravity") / mass;
            Vector2 size = Scale(coin.gameObject);
            return new FlightStats { Speed = source.Number("speed"), Gravity = body.Number("m_GravityScale"),
                Life = source.Number("lifetime"), Spin = 720f, Scale = size.x, Aspect = size.y / size.x };
        }

        static float Gravity(GameObject root)
        {
            using var source = new StatValue(StatValue.Root<Rigidbody2D>(root));
            return source.Number("m_GravityScale");
        }

        static Vector2 Scale(GameObject root)
        {
            using var source = new StatValue(root.transform);
            Vector3 scale = source.At("m_LocalScale").vector3Value;
            if (!float.IsFinite(scale.x) || !float.IsFinite(scale.y) || scale.x == 0f || scale.y == 0f)
                throw new InvalidOperationException(root.name + $": 투사체 크기 ({scale.x}, {scale.y})가 유효하지 않습니다.");
            return new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        }

        static void Box(Transform root, BoxCollider2D box, out Vector2 size, out Vector2 offset)
        {
            using var source = new StatValue(box);
            Vector2 half = source.Vector("m_Size") * 0.5f;
            Vector2 center = source.Vector("m_Offset");
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    Vector2 point = root.InverseTransformPoint(box.transform.TransformPoint(center + new Vector2(half.x * x, half.y * y)));
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
            }
            size = max - min;
            offset = (max + min) * 0.5f;
        }

        static GameObject Root(Scene scene, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("씬 원본의 Root 경로가 필요합니다.");
            string[] parts = path.Split('/');
            GameObject match = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != parts[0]) continue;
                if (match != null) throw new InvalidOperationException("씬에 같은 이름의 루트가 여러 개입니다: " + parts[0]);
                match = root;
            }
            if (match == null) throw new InvalidOperationException("씬 원본 루트가 없습니다: " + path);
            for (int i = 1; i < parts.Length; i++)
            {
                Transform child = null;
                foreach (Transform item in match.transform)
                {
                    if (item.name != parts[i]) continue;
                    if (child != null) throw new InvalidOperationException("씬 원본 경로가 중복됐습니다: " + path);
                    child = item;
                }
                if (child == null) throw new InvalidOperationException("씬 원본 경로가 없습니다: " + path);
                match = child.gameObject;
            }
            return match;
        }
    }
}

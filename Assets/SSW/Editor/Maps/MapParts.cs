using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SSW
{
    internal sealed class MapParts
    {
        readonly MapRead _read;
        readonly BattleMap _map;
        readonly int _number;
        readonly List<Object> _moving;
        readonly List<Object> _server;
        readonly Dictionary<MonoBehaviour, MapTint> _tints = new Dictionary<MonoBehaviour, MapTint>();
        readonly Dictionary<Object, MapBreath> _phases = new Dictionary<Object, MapBreath>();

        public MapParts(MapRead read, BattleMap map, int number, List<Object> moving, List<Object> server)
        {
            _read = read;
            _map = map;
            _number = number;
            _moving = moving;
            _server = server;
        }

        public void Bind()
        {
            Backgrounds();
            Orbits();
            Floods();
            Breaths();
            Areas();
            Waterfalls();
            Sakura();
            Shakes();
            Lava();
            Tempo();
            foreach (MonoBehaviour item in _read.Of("KDH_Rotation")) _server.Add(_read.Resolve(item));
            _read.RemoveAll("KDH_PlatformRiderDetector");
        }

        void Backgrounds()
        {
            foreach (MonoBehaviour raw in _read.Scripts.Where(IsTint))
            {
                var source = new SerializedObject(raw);
                int index = System.Array.IndexOf(raw.GetComponents<MonoBehaviour>().Where(IsTint).ToArray(), raw);
                MapTint tint = _read.Add<MapTint>(raw, index);
                MapRead.Edit(tint, target =>
                {
                    target.FindProperty("_sprite").objectReferenceValue = _read.Get<SpriteRenderer>(raw);
                    target.FindProperty("_start").colorValue = source.FindProperty("startColor").colorValue;
                    target.FindProperty("_end").colorValue = source.FindProperty("endColor").colorValue;
                    target.FindProperty("_duration").floatValue = source.FindProperty("transitionTime").floatValue;
                    target.FindProperty("_hold").floatValue = source.FindProperty("rotationTime").floatValue;
                    target.FindProperty("_once").boolValue = raw.GetType().Name == "KDH_VolcanoBackgroundVisual";
                    target.FindProperty("_playOnAwake").boolValue = false;
                });
                _tints.Add(raw, tint);
                if (_number != 17)
                {
                    MapFlash flash = _read.Add<MapFlash>(raw, index);
                    MapRead.Edit(flash, target => target.FindProperty("_tint").objectReferenceValue = tint);
                    _read.Replace(raw, flash);
                }
                else _read.Replace(raw, tint);
            }
        }

        static bool IsTint(MonoBehaviour item) => item.GetType().Name == "KDH_BackgroundVisual" || item.GetType().Name == "KDH_VolcanoBackgroundVisual";

        void Orbits()
        {
            foreach (MonoBehaviour raw in _read.Of("KDH_RotatingWheelPivot"))
            {
                var source = new SerializedObject(raw);
                var parts = source.FindProperty("parts");
                var bodies = new List<Object>();
                for (int i = 0; i < parts.arraySize; i++)
                {
                    var part = (Transform)_read.Resolve(parts.GetArrayElementAtIndex(i).objectReferenceValue);
                    Rigidbody2D body = part.GetComponent<Rigidbody2D>();
                    if (body == null)
                    {
                        body = part.gameObject.AddComponent<Rigidbody2D>();
                        body.bodyType = RigidbodyType2D.Kinematic;
                        body.gravityScale = 0f;
                        body.interpolation = RigidbodyInterpolation2D.None;
                    }
                    if (body.bodyType == RigidbodyType2D.Static)
                        throw new InvalidOperationException(raw.name + ": 공전 발판 Rigidbody가 Static입니다.");
                    bodies.Add(body);
                    _moving.Remove(part);
                }
                MapOrbit orbit = _read.Add<MapOrbit>(raw);
                MapRead.Edit(orbit, target =>
                {
                    target.FindProperty("_pivot").objectReferenceValue = _read.At(raw);
                    target.FindProperty("_speed").floatValue = source.FindProperty("rotationSpeed").floatValue;
                    target.FindProperty("_level").boolValue = source.FindProperty("keepPartsLevel").boolValue;
                    MapRead.Array(target, "_parts", bodies);
                });
                _read.Replace(raw, orbit);
            }
        }

        void Floods()
        {
            foreach (MonoBehaviour raw in _read.Of("KDH_Flood"))
            {
                var source = new SerializedObject(raw);
                var water = (GameObject)_read.Resolve(source.FindProperty("water").objectReferenceValue);
                foreach (Collider2D shape in water.GetComponents<Collider2D>()) shape.isTrigger = true;
                MapFlood flood = _read.Add<MapFlood>(raw);
                MapRead.Edit(flood, target =>
                {
                    target.FindProperty("_water").objectReferenceValue = water.transform;
                    target.FindProperty("_target").objectReferenceValue = _read.Resolve(source.FindProperty("targetTrm").objectReferenceValue);
                    target.FindProperty("_duration").floatValue = source.FindProperty("duration").floatValue;
                });
                _moving.Remove(water.transform);
                _read.Replace(raw, flood);
            }
        }

        void Breaths()
        {
            foreach (MonoBehaviour raw in _read.Of("KDH_DinoBreathe"))
            {
                var source = new SerializedObject(raw);
                MapBreath breath = _read.Add<MapBreath>(raw);
                MapRead.Edit(breath, target =>
                {
                    target.FindProperty("_first").objectReferenceValue = ((Transform)_read.Resolve(source.FindProperty("target1").objectReferenceValue)).GetComponent<SpriteRenderer>();
                    target.FindProperty("_second").objectReferenceValue = ((Transform)_read.Resolve(source.FindProperty("target2").objectReferenceValue)).GetComponent<SpriteRenderer>();
                    target.FindProperty("_effect").objectReferenceValue = _read.Resolve(source.FindProperty("particles").objectReferenceValue);
                    target.FindProperty("_signal").colorValue = source.FindProperty("signalColor1").colorValue;
                    target.FindProperty("_ready").colorValue = source.FindProperty("signalColor2").colorValue;
                    target.FindProperty("_base").colorValue = source.FindProperty("baseColor").colorValue;
                    target.FindProperty("_delay").floatValue = Mathf.Max(4f, source.FindProperty("relationTime").floatValue);
                    target.FindProperty("breathSoundCue").objectReferenceValue = MapRead.Sound(source, "breatheSound", "Map_Breath");
                });
                _phases.Add(source.FindProperty("damageCaster").objectReferenceValue, breath);
                _read.Replace(raw, breath);
            }
        }

        MapArea Area(MonoBehaviour raw, float damage, float delay, float interval, bool contact = false,
            bool outside = false, float force = 0f, float slow = 0f, float slowDuration = 0f, MonoBehaviour phase = null)
        {
            Collider2D shape = _read.Get<Collider2D>(raw);
            if (shape == null) throw new InvalidOperationException(raw.name + ": 기믹 Collider2D가 없습니다.");
            MapArea area = _read.Add<MapArea>(raw);
            MapRead.Edit(area, target =>
            {
                target.FindProperty("_map").objectReferenceValue = _map;
                target.FindProperty("_shape").objectReferenceValue = shape;
                target.FindProperty("_phase").objectReferenceValue = phase;
                target.FindProperty("_damage").floatValue = damage;
                target.FindProperty("_delay").floatValue = delay;
                target.FindProperty("_interval").floatValue = interval;
                target.FindProperty("_contact").boolValue = contact;
                target.FindProperty("_outside").boolValue = outside;
                target.FindProperty("_force").floatValue = force;
                target.FindProperty("_slow").floatValue = slow;
                target.FindProperty("_slowDuration").floatValue = slowDuration;
            });
            _read.Remove(raw);
            return area;
        }

        void Areas()
        {
            foreach (MonoBehaviour raw in _read.Of("KDH_BreatheDamageCaster"))
            {
                var source = new SerializedObject(raw);
                _phases.TryGetValue(raw, out MapBreath phase);
                MapArea area = Area(raw, source.FindProperty("damage").floatValue,
                    source.FindProperty("tick").floatValue, source.FindProperty("tick").floatValue, phase: phase);
                area.enabled = phase != null || source.FindProperty("canDamage").boolValue;
            }
            foreach (MonoBehaviour raw in _read.Of("KDH_FloodDamageCaster"))
            {
                var source = new SerializedObject(raw);
                MapArea area = Area(raw, source.FindProperty("damage").floatValue,
                    source.FindProperty("requiredStayTime").floatValue, source.FindProperty("tick").floatValue);
                area.enabled = source.FindProperty("canDamage").boolValue;
            }
            foreach (MonoBehaviour raw in _read.Of("KDH_CollisionDamageCaster"))
            {
                var source = new SerializedObject(raw);
                Area(raw, source.FindProperty("damage").floatValue, 0f, 0f, contact: true,
                    force: source.FindProperty("knockbackForce").floatValue);
            }
            foreach (MonoBehaviour raw in _read.Of("KDH_IceArea"))
            {
                var source = new SerializedObject(raw);
                Area(raw, source.FindProperty("damage").floatValue, 1f, 1f, outside: true,
                    slow: source.FindProperty("slowAmount").floatValue, slowDuration: 1f);
            }
        }

        void Waterfalls()
        {
            foreach (MonoBehaviour raw in _read.Of("KDH_SmallWaterfall"))
                Waterfall(raw, new SerializedObject(raw).FindProperty("addForceAmount").floatValue, 0f, 0f);
            foreach (MonoBehaviour raw in _read.Of("KDH_Waterfall"))
            {
                var source = new SerializedObject(raw);
                if (MapBake.Patched(_number))
                {
                    Waterfall(raw, source.FindProperty("force").floatValue,
                        source.FindProperty("relationTime").floatValue, source.FindProperty("duration").floatValue);
                    continue;
                }
                LiftZone lift = _read.Get<LiftZone>(raw);
                if (lift == null)
                {
                    lift = _read.Add<LiftZone>(raw);
                    MapRead.Edit(lift, target =>
                    {
                        target.FindProperty("_riseSpeed").floatValue = 22f;
                        target.FindProperty("_acceleration").floatValue = 130f;
                    });
                }
                if (source.FindProperty("relationTime").floatValue > 0f || source.FindProperty("duration").floatValue > 0f
                    || _read.Of("KDH_WaterfallEffect").Any())
                    throw new InvalidOperationException("폭포 원본에 새 주기 설정이 있습니다. 기존 상시 상승과 구분해 연결해야 합니다.");
                _read.Replace(raw, lift);
            }
        }

        void Waterfall(MonoBehaviour raw, float force, float delay, float duration)
        {
            var effects = new List<ParticleSystem>();
            foreach (MonoBehaviour source in _read.Of("KDH_WaterfallEffect"))
            {
                if (new SerializedObject(source).FindProperty("waterfall").objectReferenceValue != raw) continue;
                ParticleSystem effect = _read.Get<ParticleSystem>(source);
                var main = effect.main;
                main.playOnAwake = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(effect);
                effects.Add(effect);
                _read.Remove(source);
            }
            LiftZone previous = _read.Get<LiftZone>(raw);
            if (previous != null) Object.DestroyImmediate(previous);
            WaterLift lift = _read.Add<WaterLift>(raw);
            MapRead.Edit(lift, target =>
            {
                target.FindProperty("_force").floatValue = force;
                target.FindProperty("_delay").floatValue = delay;
                target.FindProperty("_duration").floatValue = duration;
                MapRead.Array(target, "_effects", effects);
            });
            _read.Replace(raw, lift);
        }

        void Sakura()
        {
            var modes = _read.Of("KDH_SakuraMode").ToArray();
            if (modes.Length == 0) return;
            var pools = _read.Of("KDH_SakuraEffectPooling").ToArray();
            if (pools.Length != 1) throw new InvalidOperationException("사쿠라 원본 이펙트 풀은 하나여야 합니다.");
            Object original = new SerializedObject(pools[0]).FindProperty("effectPrefab").objectReferenceValue;
            var effect = AssetDatabase.LoadAssetAtPath<ParticleSystem>("Assets/SSW/Maps/Battle/SakuraFx.prefab");
            if (effect == null || original == null) throw new InvalidOperationException("사쿠라 효과 프리팹이 없습니다.");
            GameObject source = original is GameObject go ? go : ((Component)original).gameObject;
            if (PrefabUtility.GetCorrespondingObjectFromSource(effect.gameObject) != source)
                throw new InvalidOperationException("SakuraFx와 원본 풀의 이펙트가 다릅니다. 새 원본 효과 연결을 확인하세요.");
            foreach (MonoBehaviour raw in modes)
            {
                var settings = new SerializedObject(raw);
                ParticleSystem petals = _read.Get<ParticleSystem>(raw);
                Collisions(petals);
                SakuraField field = _read.Add<SakuraField>(raw);
                MapRead.Edit(field, target =>
                {
                    target.FindProperty("_petals").objectReferenceValue = petals;
                    target.FindProperty("_effect").objectReferenceValue = effect;
                    target.FindProperty("_duration").floatValue = settings.FindProperty("timeApplySpeed").floatValue;
                    target.FindProperty("_speed").floatValue = settings.FindProperty("speedAmount").floatValue;
                    target.FindProperty("_sound").objectReferenceValue = MapRead.Sound(settings, "sakuraSound", "Map_SakuraPickup");
                });
                _read.Replace(raw, field);
            }
            foreach (MonoBehaviour pool in pools) _read.Remove(pool);
        }

        void Shakes()
        {
            foreach (MonoBehaviour raw in _read.Of("KDH_ShakeEffect").Concat(_read.Of("KDH_Earthquake")))
            {
                var source = new SerializedObject(raw);
                bool quake = raw.GetType().Name == "KDH_Earthquake";
                Vector2 duration = quake ? Vector2.one * source.FindProperty("shakeDuration").floatValue
                    : new Vector2(source.FindProperty("shakeMinDuration").floatValue, source.FindProperty("shakeMaxDuration").floatValue);
                Vector2 amount = quake ? Vector2.one * source.FindProperty("shakeAmount").floatValue
                    : new Vector2(source.FindProperty("shakeMinAmount").floatValue, source.FindProperty("shakeMaxAmount").floatValue);
                MapShake shake = _read.Add<MapShake>(raw);
                MapRead.Edit(shake, target =>
                {
                    target.FindProperty("_target").objectReferenceValue = _read.At(raw);
                    target.FindProperty("_duration").vector2Value = duration;
                    target.FindProperty("_amount").vector2Value = amount;
                    target.FindProperty("_axis").vector3Value = quake ? Vector3.right : Vector3.one;
                });
                _moving.Add(_read.At(raw));
                if (quake)
                {
                    MapArea area = Area(raw, 0f, 0f, 0f, contact: true,
                        slow: source.FindProperty("slowAmount").floatValue, slowDuration: source.FindProperty("slowDuration").floatValue);
                    area.enabled = duration.x > 0f && amount.x > 0f;
                }
                else _read.Event(raw, shake, "onShake");
                _read.Replace(raw, shake);
            }
        }

        void Lava()
        {
            foreach (MonoBehaviour raw in _read.Of("KDH_Volcano"))
            {
                var source = new SerializedObject(raw);
                ParticleSystem effect = _read.Get<ParticleSystem>(raw);
                Collisions(effect);
                var main = effect.main;
                main.playOnAwake = false;
                effect.useAutoRandomSeed = false;
                effect.randomSeed = 1;
                Lava lava = _read.Add<Lava>(raw);
                MapRead.Edit(lava, target =>
                {
                    target.FindProperty("_effect").objectReferenceValue = effect;
                    target.FindProperty("_damage").floatValue = source.FindProperty("damage").floatValue;
                    target.FindProperty("_sound").objectReferenceValue = MapRead.Sound(source, "volcanoSound", "Map_VolcanoErupt");
                });
                PrefabUtility.RecordPrefabInstancePropertyModifications(effect);
                _read.Replace(raw, lava);
            }
        }

        void Tempo()
        {
            foreach (MonoBehaviour raw in _read.Of("KDH_TimeLineController"))
            {
                var source = new SerializedObject(raw);
                MonoBehaviour clock = _read.Of("KDH_ClockMotion").Single();
                MapTempo tempo = _map.GetComponent<MapTempo>() ?? _read.Add<MapTempo>(raw);
                MapRead.Edit(tempo, target =>
                {
                    target.FindProperty("_delay").floatValue = source.FindProperty("relationTime").floatValue;
                    target.FindProperty("_duration").floatValue = source.FindProperty("duration").floatValue;
                    target.FindProperty("_minimum").floatValue = source.FindProperty("minRange").floatValue;
                    target.FindProperty("_maximum").floatValue = source.FindProperty("maxRange").floatValue;
                    target.FindProperty("_clock").objectReferenceValue = _read.Get<SpriteRenderer>(clock);
                    target.FindProperty("_clockSize").floatValue = new SerializedObject(clock).FindProperty("maxSize").floatValue;
                    target.FindProperty("_startTint").objectReferenceValue = EventTint(source, "onTimeLineStart");
                    target.FindProperty("_endTint").objectReferenceValue = EventTint(source, "onTimeLineEnd");
                    target.FindProperty("_sound").objectReferenceValue = MapRead.Sound(source, "clockSound", "Map_TimeStop");
                });
                _read.Replace(raw, tempo);
            }
            _read.RemoveAll("KDH_ClockMotion");
            _read.RemoveAll("KDH_TimeLineEffect");
        }

        MapTint EventTint(SerializedObject source, string field)
        {
            var calls = source.FindProperty(field + ".m_PersistentCalls.m_Calls");
            for (int i = 0; i < calls.arraySize; i++)
            {
                var call = calls.GetArrayElementAtIndex(i);
                if (call.FindPropertyRelative("m_Target").objectReferenceValue is MonoBehaviour receiver
                    && _tints.TryGetValue(receiver, out MapTint tint)) return tint;
            }
            throw new InvalidOperationException(source.targetObject.name + ": " + field + " Tint 연결이 없습니다.");
        }

        static void Collisions(ParticleSystem effect)
        {
            var player = AssetDatabase.LoadAssetAtPath<NetPlayer>("Assets/SSW/Resources/Network/Player.prefab");
            if (effect == null || player == null) throw new InvalidOperationException("입자 또는 네트워크 플레이어 참조가 없습니다.");
            var collision = effect.collision;
            collision.enabled = true;
            collision.sendCollisionMessages = true;
            collision.collidesWith |= 1 << player.Collider.gameObject.layer;
            PrefabUtility.RecordPrefabInstancePropertyModifications(effect);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;

namespace SSW
{
    public sealed class StatPotions
    {
        readonly NetStock _stock;
        readonly AbstractPotion[] _potions;
        readonly int[] _base;
        readonly bool _changed;
        public string Report => "NetStock 기본 ID: [" + string.Join(", ", _base) + "] / 전체 " + _potions.Length + "개. 기존 ID 순서 보존.";

        public StatPotions(NetStock stock, AbstractPotion[] source)
        {
            if (stock == null) throw new InvalidOperationException("StatSources에 NetStock 참조가 필요합니다.");
            _stock = stock;
            using var value = new StatValue(stock);
            var potions = new List<AbstractPotion>(value.References<AbstractPotion>("_potions"));
            var unique = new HashSet<AbstractPotion>();
            foreach (AbstractPotion potion in potions)
            {
                if (!unique.Add(potion)) throw new InvalidOperationException("NetStock._potions에 같은 원본 참조가 중복됐습니다. 기존 ID를 확인하세요.");
            }
            unique.Clear();
            _base = new int[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null || !unique.Add(source[i]))
                    throw new InvalidOperationException("RandomPotion 기본 목록에 빈 참조 또는 같은 포션이 중복됐습니다.");
                int id = potions.IndexOf(source[i]);
                if (id < 0)
                {
                    id = potions.Count;
                    potions.Add(source[i]);
                }
                _base[i] = id;
            }
            _potions = potions.ToArray();
            SerializedProperty current = value.At("_base");
            _changed = value.At("_potions").arraySize != _potions.Length || current.arraySize != _base.Length;
            for (int i = 0; !_changed && i < _base.Length; i++)
                _changed = current.GetArrayElementAtIndex(i).intValue != _base[i];
        }

        public bool Apply()
        {
            if (!_changed) return false;
            using var target = new SerializedObject(_stock);
            SerializedProperty potions = target.FindProperty("_potions");
            SerializedProperty normal = target.FindProperty("_base");
            potions.arraySize = _potions.Length;
            for (int i = 0; i < _potions.Length; i++) potions.GetArrayElementAtIndex(i).objectReferenceValue = _potions[i];
            normal.arraySize = _base.Length;
            for (int i = 0; i < _base.Length; i++) normal.GetArrayElementAtIndex(i).intValue = _base[i];
            target.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_stock);
            AssetDatabase.SaveAssetIfDirty(_stock);
            return true;
        }
    }
}

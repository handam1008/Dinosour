using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SSW
{
    internal sealed class MapRead
    {
        readonly GameObject _source;
        readonly string _path;
        readonly Dictionary<Object, Object> _objects = new Dictionary<Object, Object>();
        readonly Dictionary<Object, Object> _replaced = new Dictionary<Object, Object>();
        readonly HashSet<Component> _removed = new HashSet<Component>();
        public MonoBehaviour[] Scripts { get; }

        public MapRead(GameObject source, GameObject target)
        {
            _source = source;
            _path = AssetDatabase.GetAssetPath(source);
            _objects[source] = target;
            _objects[source.transform] = target.transform;
            foreach (Transform item in target.GetComponentsInChildren<Transform>(true))
            {
                Transform original = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(item, _path);
                if (original == null) continue;
                _objects[original] = item;
                _objects[original.gameObject] = item.gameObject;
                foreach (Component component in item.GetComponents<Component>())
                {
                    if (component == null) continue;
                    Component raw = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(component, _path);
                    if (raw != null) _objects[raw] = component;
                }
            }
            Scripts = source.GetComponentsInChildren<MonoBehaviour>(true);
            if (Scripts.Any(item => item == null)) throw new InvalidOperationException(_path + ": 원본 스크립트가 누락됐습니다.");
        }

        public IEnumerable<MonoBehaviour> Of(string name) => Scripts.Where(item => item.GetType().Name == name);
        public Transform At(Component source) => (Transform)Resolve(source.transform);
        public T Get<T>(Component source) where T : Component => At(source).GetComponent<T>();
        public T Add<T>(Component source) where T : Component => At(source).GetComponent<T>() ?? At(source).gameObject.AddComponent<T>();
        public T Add<T>(Component source, int index) where T : Component
        {
            Transform target = At(source);
            T[] existing = target.GetComponents<T>();
            if (index < existing.Length) return existing[index];
            if (index != existing.Length) throw new InvalidOperationException(source.name + ": 어댑터 순서가 일치하지 않습니다.");
            return target.gameObject.AddComponent<T>();
        }
        public Object Resolve(Object source)
        {
            if (source == null) return null;
            if (_replaced.TryGetValue(source, out Object replacement)) return replacement;
            if (_objects.TryGetValue(source, out Object target) && target != null) return target;
            Transform location = source is GameObject go ? go.transform : (source as Component)?.transform;
            if (location == null || location != _source.transform && !location.IsChildOf(_source.transform)) return source;
            throw new InvalidOperationException(_path + ": 원본 연결을 찾지 못했습니다: " + source.name + "/" + source.GetType().Name);
        }

        public void Replace(Component source, Component target)
        {
            _replaced[source] = target;
            Remove(source);
        }

        public void Remove(Component source)
        {
            if (_objects.TryGetValue(source, out Object target) && target is Component component) _removed.Add(component);
        }

        public void RemoveAll(string name)
        {
            foreach (MonoBehaviour item in Of(name)) Remove(item);
        }

        public void Finish()
        {
            foreach (Component item in _removed)
                if (item != null) Object.DestroyImmediate(item);
        }

        public void Event(MonoBehaviour source, Component target, string field)
        {
            var raw = new SerializedObject(source);
            var settings = new SerializedObject(target);
            settings.CopyFromSerializedProperty(raw.FindProperty(field));
            var calls = settings.FindProperty(field + ".m_PersistentCalls.m_Calls");
            for (int i = 0; i < calls.arraySize; i++)
            {
                var call = calls.GetArrayElementAtIndex(i);
                var receiver = call.FindPropertyRelative("m_Target");
                Object original = receiver.objectReferenceValue;
                Object mapped = Resolve(original);
                receiver.objectReferenceValue = mapped;
                if (mapped != null)
                    call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = mapped.GetType().AssemblyQualifiedName;
                var method = call.FindPropertyRelative("m_MethodName");
                if (method.stringValue == "FadeColor" && (mapped is MapTint || mapped is MapFlash)) method.stringValue = "Play";
                var argument = call.FindPropertyRelative("m_Arguments.m_ObjectArgument");
                if (argument != null) argument.objectReferenceValue = Resolve(argument.objectReferenceValue);
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        public static SoundCue Sound(string name)
        {
            var cue = AssetDatabase.LoadAssetAtPath<SoundCue>("Assets/RYU/Sound/Cues/" + name + ".asset");
            if (cue == null) throw new InvalidOperationException("맵 사운드가 없습니다: " + name);
            return cue;
        }

        public static SoundCue Sound(SerializedObject source, string field, string fallback) =>
            source.FindProperty(field).objectReferenceValue as SoundCue ?? Sound(fallback);

        public static void Edit(Object target, Action<SerializedObject> apply)
        {
            var settings = new SerializedObject(target);
            apply(settings);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Array(SerializedObject target, string field, IEnumerable<Object> values)
        {
            Object[] items = values.Where(item => item != null).Distinct().ToArray();
            var array = target.FindProperty(field);
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        public static List<Object> Array(Object target, string field)
        {
            var array = new SerializedObject(target).FindProperty(field);
            var result = new List<Object>();
            for (int i = 0; i < array.arraySize; i++)
            {
                Object item = array.GetArrayElementAtIndex(i).objectReferenceValue;
                if (item != null) result.Add(item);
            }
            return result;
        }
    }
}

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SSW
{
    public sealed class StatValue : IDisposable
    {
        readonly SerializedObject _source;

        public StatValue(Object source)
        {
            if (source == null) throw new InvalidOperationException("수치 원본 참조가 없습니다.");
            _source = new SerializedObject(source);
            _source.UpdateIfRequiredOrScript();
        }

        public SerializedProperty At(string name) => _source.FindProperty(name)
            ?? throw new InvalidOperationException($"{_source.targetObject.name}/{_source.targetObject.GetType().Name}: {name} 필드가 없습니다.");

        public float Number(string name) => At(name).floatValue;
        public int Integer(string name) => At(name).intValue;
        public Vector2 Vector(string name) => At(name).vector2Value;

        public T Reference<T>(string name) where T : Object => At(name).objectReferenceValue as T
            ?? throw new InvalidOperationException($"{_source.targetObject.name}/{name}: {typeof(T).Name} 참조가 없습니다.");

        public T[] References<T>(string name) where T : Object
        {
            SerializedProperty property = At(name);
            var values = new T[property.arraySize];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = property.GetArrayElementAtIndex(i).objectReferenceValue as T;
                if (values[i] == null) throw new InvalidOperationException($"{_source.targetObject.name}/{name}[{i}]: 참조가 없습니다.");
            }
            return values;
        }

        public void Dispose() => _source.Dispose();

        public static T Root<T>(GameObject root) where T : Component
        {
            T[] matches = root.GetComponents<T>();
            if (matches.Length != 1)
                throw new InvalidOperationException($"{root.name}: 루트에 {typeof(T).Name} 컴포넌트가 {matches.Length}개입니다.");
            return matches[0];
        }

        public static T One<T>(GameObject root) where T : Component
        {
            T[] matches = root.GetComponentsInChildren<T>(true);
            if (matches.Length != 1)
                throw new InvalidOperationException($"{root.name}: {typeof(T).Name} 컴포넌트가 {matches.Length}개입니다. 원본 하나가 필요합니다.");
            return matches[0];
        }
    }
}

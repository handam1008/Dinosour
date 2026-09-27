using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevLib.SoundSystem.Runtime;
using SSW;
using UnityEditor;
using UnityEngine;

namespace RYU.Editor
{
    // Sound/New 폴더의 오디오 파일로 SoundCue 에셋을 만들고 SoundBank(_shared)에 등록한다
    public static class SoundCueBuilder
    {
        private const string ClipFolder = "Assets/RYU/Sound/New";
        private const string CueFolder = "Assets/RYU/Sound/Cues";
        private const string BankPath = "Assets/SSW/Audio/Sounds.asset";

        [MenuItem("RYU/Build Sound Cues")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(CueFolder)) AssetDatabase.CreateFolder("Assets/RYU/Sound", "Cues");

            List<SoundCue> cues = new List<SoundCue>();

            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { ClipFolder }))
            {
                string clipPath = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip == null) continue;

                ApplyImportSettings(clipPath);

                string cuePath = $"{CueFolder}/{Path.GetFileNameWithoutExtension(clipPath)}.asset";
                SoundCue cue = AssetDatabase.LoadAssetAtPath<SoundCue>(cuePath);

                if (cue == null)
                {
                    cue = ScriptableObject.CreateInstance<SoundCue>();
                    AssetDatabase.CreateAsset(cue, cuePath);
                }

                cue.audioType = DevLib.SoundSystem.Runtime.AudioType.Sfx;
                cue.clip = clip;
                cue.volume = 1f;
                cue.startTime = 0f;
                cue.endTime = clip.length;   // 0이면 소리가 바로 끊긴다
                EditorUtility.SetDirty(cue);

                cues.Add(cue);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();   // OnValidate가 돌면서 각 Cue의 Id가 채워진다

            Register(cues);

            Debug.Log($"[Sound] Cue {cues.Count}개 생성/갱신 완료");
        }

        private static void ApplyImportSettings(string path)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return;

            importer.forceToMono = true;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;

            importer.SaveAndReimport();
        }

        // SoundBank의 _shared 배열에 빠진 Cue를 추가한다
        private static void Register(List<SoundCue> cues)
        {
            SoundBank bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (bank == null)
            {
                Debug.LogError($"SoundBank를 찾지 못했습니다: {BankPath}");
                return;
            }

            SerializedObject serialized = new SerializedObject(bank);
            SerializedProperty shared = serialized.FindProperty("_shared");

            HashSet<Object> existing = new HashSet<Object>();
            for (int i = 0; i < shared.arraySize; i++)
                existing.Add(shared.GetArrayElementAtIndex(i).objectReferenceValue);

            int added = 0;
            foreach (SoundCue cue in cues.Where(cue => !existing.Contains(cue)))
            {
                shared.InsertArrayElementAtIndex(shared.arraySize);
                shared.GetArrayElementAtIndex(shared.arraySize - 1).objectReferenceValue = cue;
                added++;
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Sound] SoundBank에 {added}개 등록 (총 {shared.arraySize}개)");
        }
    }
}

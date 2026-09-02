using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SSW
{
    internal static class MagicianBloomRuntime
    {
        const string RuntimeVolumeName = "Magician Runtime Bloom";

        static Volume _runtimeVolume;

        internal static void Ensure()
        {
            EnablePostProcessingOnActiveCameras();

            if (_runtimeVolume != null || HasActiveGlobalBloom())
                return;

            CreateFallbackBloomVolume();
        }

        static void EnablePostProcessingOnActiveCameras()
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>();
            foreach (Camera camera in cameras)
            {
                if (!camera.isActiveAndEnabled)
                    continue;

                if (!camera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
                    cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();

                cameraData.renderPostProcessing = true;
            }
        }

        static bool HasActiveGlobalBloom()
        {
            Volume[] volumes = Resources.FindObjectsOfTypeAll<Volume>();
            foreach (Volume volume in volumes)
            {
                if (!volume.gameObject.scene.IsValid() ||
                    !volume.isActiveAndEnabled ||
                    !volume.isGlobal ||
                    volume.weight <= 0f)
                    continue;

                VolumeProfile profile = volume.sharedProfile;
                if (profile != null && profile.TryGet(out Bloom bloom) && bloom != null && bloom.active)
                {
                    if (volume.name == RuntimeVolumeName)
                        _runtimeVolume = volume;
                    return true;
                }
            }

            return false;
        }

        static void CreateFallbackBloomVolume()
        {
            var volumeObject = new GameObject(RuntimeVolumeName)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Object.DontDestroyOnLoad(volumeObject);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Magician Runtime Bloom Profile";
            profile.hideFlags = HideFlags.HideAndDontSave;

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(1.15f);
            bloom.scatter.Override(0.72f);
            bloom.clamp.Override(12f);
            bloom.highQualityFiltering.Override(true);

            _runtimeVolume = volumeObject.AddComponent<Volume>();
            _runtimeVolume.isGlobal = true;
            _runtimeVolume.priority = -1000f;
            _runtimeVolume.weight = 1f;
            _runtimeVolume.sharedProfile = profile;
        }
    }
}

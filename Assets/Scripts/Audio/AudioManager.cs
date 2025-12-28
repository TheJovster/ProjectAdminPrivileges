using System.Collections.Generic;
using UnityEngine;

namespace ProjectAdminPrivileges.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Pool Sizes")]
        [SerializeField] private int gunfirePoolSize = 30;
        [SerializeField] private int impactPoolSize = 20;
        [SerializeField] private int sfxPoolSize = 10;

        [Header("Impact Audio Data")]
        [Tooltip("Assign all ImpactAudioData assets here")]
        [SerializeField] private ImpactAudioData[] impactAudioDataArray;

        private List<PooledAudioSource> gunfirePool;
        private List<PooledAudioSource> impactPool;
        private List<PooledAudioSource> sfxPool;
        private AudioSource footstepSource;

        private Dictionary<SurfaceType, ImpactAudioData> impactDataLookup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePools();
            InitializeImpactLookup();
        }

        private void InitializePools()
        {
            gunfirePool = CreatePool(gunfirePoolSize, "GunfirePool");
            impactPool = CreatePool(impactPoolSize, "ImpactPool");
            sfxPool = CreatePool(sfxPoolSize, "SFXPool");

            GameObject footstepObj = new GameObject("FootstepSource");
            footstepObj.transform.SetParent(transform);
            footstepSource = footstepObj.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
        }

        private List<PooledAudioSource> CreatePool(int size, string poolName)
        {
            List<PooledAudioSource> pool = new List<PooledAudioSource>(size);
            GameObject poolParent = new GameObject(poolName);
            poolParent.transform.SetParent(transform);

            for (int i = 0; i < size; i++)
            {
                GameObject sourceObj = new GameObject($"AudioSource_{i}");
                sourceObj.transform.SetParent(poolParent.transform);
                AudioSource source = sourceObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;

                pool.Add(new PooledAudioSource(source));
            }

            return pool;
        }

        private void InitializeImpactLookup()
        {
            impactDataLookup = new Dictionary<SurfaceType, ImpactAudioData>();

            if (impactAudioDataArray == null || impactAudioDataArray.Length == 0)
            {
                Debug.LogWarning("AudioManager: No ImpactAudioData assigned! Impact sounds will not play.");
                return;
            }

            foreach (var data in impactAudioDataArray)
            {
                if (data != null)
                {
                    impactDataLookup[data.SurfaceType] = data;
                }
            }
        }

        public void PlayAudio(AudioClipData data, Vector3 position, float volumeMultiplier = 1f)
        {
            if (data == null)
            {
                Debug.LogWarning("AudioManager: Attempted to play null AudioClipData");
                return;
            }

            switch (data.Category)
            {
                case AudioCategory.Gunfire:
                    PlayFromPool(gunfirePool, data, position, volumeMultiplier, true);
                    break;
                case AudioCategory.Impact:
                    PlayFromPool(impactPool, data, position, volumeMultiplier, true);
                    break;
                case AudioCategory.SFX:
                    PlayFromPool(sfxPool, data, position, volumeMultiplier, false);
                    break;
                case AudioCategory.Footstep:
                    PlayFootstep(data, volumeMultiplier);
                    break;
                case AudioCategory.Voice:
                    Debug.LogWarning("AudioManager: Voice category should be played through VoiceManager");
                    break;
            }
        }

        public void PlayImpactSound(SurfaceType surfaceType, Vector3 position, float volumeMultiplier = 1f)
        {
            if (impactDataLookup.TryGetValue(surfaceType, out ImpactAudioData impactData))
            {
                PlayAudio(impactData.ImpactSound, position, volumeMultiplier);
            }
            else
            {
                Debug.LogWarning($"AudioManager: No ImpactAudioData found for surface type: {surfaceType}");
            }
        }

        private void PlayFromPool(List<PooledAudioSource> pool, AudioClipData data, Vector3 position, float volumeMultiplier, bool allowStealing)
        {
            AudioClip clip = data.GetRandomClip();
            if (clip == null) return;

            PooledAudioSource pooledSource = GetAvailableSource(pool, allowStealing);
            if (pooledSource == null)
            {
                Debug.LogWarning($"AudioManager: No available source in pool and stealing not allowed");
                return;
            }

            ConfigureAndPlaySource(pooledSource, data, clip, position, volumeMultiplier);
        }

        private PooledAudioSource GetAvailableSource(List<PooledAudioSource> pool, bool allowStealing)
        {
            foreach (var pooledSource in pool)
            {
                if (!pooledSource.IsPlaying)
                {
                    return pooledSource;
                }
            }

            if (allowStealing)
            {
                PooledAudioSource oldest = pool[0];
                foreach (var pooledSource in pool)
                {
                    if (pooledSource.PlayStartTime < oldest.PlayStartTime)
                    {
                        oldest = pooledSource;
                    }
                }
                oldest.Source.Stop();
                return oldest;
            }

            return null;
        }

        private void ConfigureAndPlaySource(PooledAudioSource pooledSource, AudioClipData data, AudioClip clip, Vector3 position, float volumeMultiplier)
        {
            AudioSource source = pooledSource.Source;

            source.transform.position = position;
            source.clip = clip;
            source.volume = data.GetRandomVolume() * volumeMultiplier;
            source.pitch = data.GetRandomPitch();

            // Spatial settings
            if (data.MaxDistance > 0)
            {
                source.spatialBlend = 1f; // 3D
                source.maxDistance = data.MaxDistance;
                source.rolloffMode = AudioRolloffMode.Linear;
            }
            else
            {
                source.spatialBlend = 0f; // 2D
            }

            source.Play();
            pooledSource.MarkAsPlaying();
        }

        public void PlayFootstep(AudioClipData data, float volumeMultiplier = 1f)
        {
            if (data == null) return;

            AudioClip clip = data.GetRandomClip();
            if (clip == null) return;

            footstepSource.clip = clip;
            footstepSource.volume = data.GetRandomVolume() * volumeMultiplier;
            footstepSource.pitch = data.GetRandomPitch();
            footstepSource.spatialBlend = 0f; // 2D for player footsteps
            footstepSource.Play();
        }

        private class PooledAudioSource
        {
            public AudioSource Source { get; private set; }
            public bool IsPlaying => Source.isPlaying;
            public float PlayStartTime { get; private set; }

            public PooledAudioSource(AudioSource source)
            {
                Source = source;
                PlayStartTime = 0f;
            }

            public void MarkAsPlaying()
            {
                PlayStartTime = Time.time;
            }
        }
    }
}
using UnityEngine;

namespace FourPlayerHorror.Audio
{
    public enum AudioCategory
    {
        BGM,
        Player,
        Monster,
        Environment,
        Horror
    }


    [CreateAssetMenu(
        fileName = "AudioData",
        menuName = "FourPlayerHorror/Audio/Audio Data"
    )]
    public class AudioData : ScriptableObject
    {
        [Header("Category")]
        public AudioCategory category;

        [Header("Clip")]
        public AudioClip clip;

        [Header("Settings")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.5f, 2f)]
        public float pitch = 1f;

        public bool loop;

        [Header("3D Audio")]
        public bool is3D = false;

        [Range(0f, 1f)]
        public float spatialBlend = 1f;

        public float minDistance = 2f;

        public float maxDistance = 20f;
    }
}
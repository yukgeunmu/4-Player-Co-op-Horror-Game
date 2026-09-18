using UnityEngine;

namespace FourPlayerHorror.Audio.Horror
{
    [CreateAssetMenu(
        fileName = "HorrorEventData",
        menuName = "FourPlayerHorror/Horror/Horror Event Data"
    )]
    public class HorrorEventData : ScriptableObject
    {
        [Header("Event")]
        [SerializeField] private string eventName;

        [Header("Sound")]
        [SerializeField] private AudioData sound;

        [Header("Light")]
        [SerializeField] private bool useLight;

        [SerializeField] private float lightOffDuration = 0.2f;

        public string EventName => eventName;
        public AudioData Sound => sound;
        public bool UseLight => useLight;
        public float LightOffDuration => lightOffDuration;
    }
}
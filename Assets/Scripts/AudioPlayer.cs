using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    public static AudioPlayer Instance;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] audio;

    private void Awake()
    {
        Instance = this;
    }

    public void PlayAudio(int id)
    {
        PlayAudio(id, 1.0f, 1.0f);
    }

    public void PlayAudio(int id, float volume)
    {
        PlayAudio(id, volume, 1.0f);
    }

    public void PlayAudio(int id, float volume, float pitch)
    {
        if (audioSource == null || id < 0 || id >= audio.Length || audio[id] == null)
            return;

        // Create a temporary GameObject to play the audio with specific pitch
        GameObject tempGO = new GameObject("TempAudio_" + audio[id].name);
        AudioSource tempSource = tempGO.AddComponent<AudioSource>();
        tempSource.clip = audio[id];
        tempSource.volume = volume;
        tempSource.pitch = pitch;
        tempSource.spatialBlend = 0f; // 2D Sound
        tempSource.Play();

        // Destroy the temp object after the clip completes
        float clipDuration = audio[id].length / Mathf.Max(0.01f, pitch);
        Destroy(tempGO, clipDuration + 0.1f);
    }
}
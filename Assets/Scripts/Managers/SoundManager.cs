using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    public Sound[] sounds;

    private void Awake()
    {
        if (Instance && Instance != this)
            Debug.LogError("Loi nhieu hon 1 Sound Manager");
        else
            Instance = this;

        foreach (var item in sounds)
        {
            item.source = gameObject.AddComponent<AudioSource>();

            item.source.clip = item.clip;
            item.source.volume = item.volume;
            item.source.pitch = item.pitch;
            item.source.loop = item.loop;
        }
    }

    private void Start()
    {
        PlayBGM(0);
    }

    public void PlayBGM(int s)
    {
        sounds[s].source.Play();
    }

    public void StopBGM()
    {
        sounds[0].source.Stop();
    }

    public void PlaySFX(int s)
    {
        sounds[s].source.PlayOneShot(sounds[s].source.clip);
    }
}
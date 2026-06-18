using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources globaux")]
    [SerializeField] private AudioSource musicSource; // Pour la musique de fond (Loop)
    [SerializeField] private AudioSource sfx2DSource;  // Pour les sons d'ambiance 2D (Chrono, Jingles)

    [Header("Mixer Group (pour lier à tes options)")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Reste actif d'une scène à l'autre
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Permet de lancer une musique depuis n'importe quel script (ex: LapManager ou ContreLaMontre au Start)
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        musicSource.clip = clip;
        musicSource.outputAudioMixerGroup = musicGroup;
        musicSource.loop = true;
        musicSource.Play();
    }

    // Permet de déclencher un effet sonore 2D depuis n'importe où (ex: Checkpoint, Compte à rebours)
    // Dans SoundManager.cs (assure-toi que cette méthode est publique)
    public void PlaySfx2D(AudioClip clip)
    {
        if (sfx2DSource == null || clip == null) return;
        sfx2DSource.PlayOneShot(clip);
    }
}
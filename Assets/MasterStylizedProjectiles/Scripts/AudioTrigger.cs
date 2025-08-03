using UnityEngine;

/// <summary>
/// Audio trigger component for playing audio clips
/// </summary>
public class AudioTrigger : MonoBehaviour
{
    public float time = 3;
    public AudioClip onClip;

    void Start()
    {
        var audio = gameObject.AddComponent<AudioSource>();
        if (onClip != null)
        {
            audio.clip = onClip;
            audio.Play();
        }
        AutoDestroy(time);
    }

    void AutoDestroy(float time)
    {
        Invoke("Destroy", time);
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }
}

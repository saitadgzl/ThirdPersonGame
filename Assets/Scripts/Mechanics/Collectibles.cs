using UnityEngine;
using System.Collections;
using CharacterController;

public class Collectibles : MonoBehaviour
{
    AudioSource _audioSource;
    public AudioClip _audioClip;
    public GameObject _particle;
    public int value = 1;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Get the ThirdPersonController component from player
            ThirdPersonController playerController = other.GetComponent<ThirdPersonController>();

            if (playerController != null)
            {
                // Check the tag
                if (gameObject.CompareTag("Money"))
                {
                    playerController.AddMoney(value);
                }
                else if (gameObject.CompareTag("Health"))
                {
                    playerController.AddHealth(value);
                }
            }

            // Visual and audio feedback
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
                r.enabled = false;

            _audioSource.PlayOneShot(_audioClip);
            Destroy(gameObject, _audioClip.length);
        }
    }
}
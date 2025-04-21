using UnityEngine;
using CharacterController; 

public class KnockbackTrigger : MonoBehaviour
{
    [SerializeField] private int knockbackDamage = 25;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger has the "Player" tag
        if (other.CompareTag("Player"))
        {
            // Get the ThirdPersonController 
            ThirdPersonController thirdPersonController = other.GetComponent<ThirdPersonController>();

            // If the player has a ThirdPersonController component
            if (thirdPersonController != null)
            {
                // Apply knockback damage, our position is source
                thirdPersonController.TakeKnockbackDamage(knockbackDamage, transform.position);
            }
        }
    }
}
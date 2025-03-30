using UnityEngine;

namespace CharacterController
{
    public class ThirdPersonController : ThirdPersonAnimator
    {
        #region Unity Methods

        #endregion
        [Header("Character Stats")]
        [Tooltip("Character's health")]
        public int health = 100;
        [Tooltip("Character's money")]
        public int money = 50;

        public virtual void ControlAnimatorRootMotion()
        {
            if (!this.enabled) return;

            if (inputSmooth == Vector3.zero)
            {
                transform.position = animator.rootPosition;
                transform.rotation = animator.rootRotation;
            }

            if (useRootMotion)
                MoveCharacter(moveDirection);
        }

        public virtual void ControlLocomotionType()
        {
            if (lockMovement) return;

            if (locomotionType.Equals(LocomotionType.FreeWithStrafe) && !isStrafing || locomotionType.Equals(LocomotionType.OnlyFree))
            {
                SetControllerMoveSpeed(freeSpeed);
                SetAnimatorMoveSpeed(freeSpeed);
            }
            else if (locomotionType.Equals(LocomotionType.OnlyStrafe) || locomotionType.Equals(LocomotionType.FreeWithStrafe) && isStrafing)
            {
                isStrafing = true;
                SetControllerMoveSpeed(strafeSpeed);
                SetAnimatorMoveSpeed(strafeSpeed);
            }

            if (!useRootMotion)
                MoveCharacter(moveDirection);
        }

        public virtual void ControlRotationType()
        {
            if (lockRotation) return;

            bool validInput = input != Vector3.zero || (isStrafing ? strafeSpeed.rotateWithCamera : freeSpeed.rotateWithCamera);

            if (validInput)
            {
                inputSmooth = Vector3.Lerp(inputSmooth, input, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);

                Vector3 dir = (isStrafing && (!isSprinting || sprintOnlyFree == false) || (freeSpeed.rotateWithCamera && input == Vector3.zero)) && rotateTarget ? rotateTarget.forward : moveDirection;
                RotateToDirection(dir);
            }
        }

        public virtual void UpdateMoveDirection(Transform referenceTransform = null)
        {
            if (input.magnitude <= 0.01)
            {
                moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);
                return;
            }

            if (referenceTransform && !rotateByWorld)
            {
                //get the right-facing direction of the referenceTransform
                var right = referenceTransform.right;
                right.y = 0;
                //get the forward direction relative to referenceTransform Right
                var forward = Quaternion.AngleAxis(-90, Vector3.up) * right;
                // determine the direction the player will face based on input and the referenceTransform's right and forward directions
                moveDirection = (inputSmooth.x * right) + (inputSmooth.z * forward);
            }
            else
            {
                moveDirection = new Vector3(inputSmooth.x, 0, inputSmooth.z);
            }
        }

        public virtual void Sprint(bool value)
        {
            var sprintConditions = (input.sqrMagnitude > 0.1f && isGrounded &&
                !(isStrafing && !strafeSpeed.walkByDefault && (horizontalSpeed >= 0.5 || horizontalSpeed <= -0.5 || verticalSpeed <= 0.1f)));

            // Can't sprint while crouching
            if (isCrouching)
            {
                if (isSprinting) isSprinting = false;
                return;
            }

            if (value && sprintConditions)
            {
                if (input.sqrMagnitude > 0.1f)
                {
                    if (isGrounded && useContinuousSprint)
                    {
                        isSprinting = !isSprinting;
                    }
                    else if (!isSprinting)
                    {
                        isSprinting = true;
                    }
                }
                else if (!useContinuousSprint && isSprinting)
                {
                    isSprinting = false;
                }
            }
            else if (isSprinting)
            {
                isSprinting = false;
            }
        }

        public virtual void Strafe()
        {
            isStrafing = !isStrafing;
        }

        public virtual void Jump()
        {
            // Cannot jump while crouching
            if (isCrouching) return;

            // trigger jump behaviour
            jumpCounter = jumpTimer;
            isJumping = true;

            // trigger jump animations
            if (input.sqrMagnitude < 0.1f)
                animator.CrossFadeInFixedTime("Jump", 0.1f);
            else
                animator.CrossFadeInFixedTime("JumpMove", .2f);
        }

        public virtual void DoubleJump()
        {
            if (CanDoubleJump())
            {
                // trigger double jump behaviour
                jumpCounter = jumpTimer;
                isJumping = true;
                hasDoubleJumped = true;

                // trigger jump animations - use the same animations as regular jump
                if (input.sqrMagnitude < 0.1f)
                    animator.CrossFadeInFixedTime("Jump", 0.1f);
                else
                    animator.CrossFadeInFixedTime("JumpMove", .2f);
            }
        }

        // Crouch method - toggle functionality
        public virtual void Crouch(bool value = true)
        {
            // Cannot crouch while jumping/airborne
            if (!isGrounded) return;

            // Cannot crouch while sprinting - exit sprint first
            if (value && isSprinting)
            {
                isSprinting = false;
            }

            // Set crouch state
            isCrouching = value;

            // Apply physical changes (collider height)
            ApplyCrouch(isCrouching);

            // Play appropriate animation
            if (isCrouching)
            {
                animator.CrossFadeInFixedTime("Crouch", 0.2f);
            }
            else
            {
                animator.CrossFadeInFixedTime("Stand", 0.2f);
            }
        }

        public virtual void Punch()
        {
            // Can't punch while crouching
            if (isCrouching) return;

            if (isPunching)
            {
                // Already in a punch animation, check if we can queue the next combo
                float timeSinceLastPunch = Time.time - lastPunchTime;

                // If we're within the combo window, increment the combo counter for the next punch
                if (timeSinceLastPunch <= punchComboTimeWindow && punchComboCount < 3)
                {
                    punchComboCount++;
                    lastPunchTime = Time.time;

                    if (debugPunch)
                        UnityEngine.Debug.Log("Punch combo increased to: " + punchComboCount);

                    // Don't trigger animation yet - it will play after the current one finishes
                }

                return;
            }

            // Not currently punching, start a new punch sequence
            if (isGrounded)
            {
                float timeSinceLastPunch = Time.time - lastPunchTime;

                // If it's been too long since last punch, reset combo
                if (timeSinceLastPunch > punchComboTimeWindow)
                {
                    punchComboCount = 1;
                }
                else
                {
                    // Increment combo within limits
                    punchComboCount = Mathf.Clamp(punchComboCount + 1, 1, 3);
                }

                isPunching = true;
                lastPunchTime = Time.time;

                // Update animator
                animator.SetBool(AnimatorParameters.IsPunching, true);
                animator.SetInteger(AnimatorParameters.PunchCombo, punchComboCount);

                // Choose the appropriate animation based on combo count
                string punchAnim = "Punch";
                float duration = punchDuration;

                if (punchComboCount == 2)
                {
                    punchAnim = "DoublePunch";
                    duration = doublePunchDuration;
                }
                else if (punchComboCount == 3)
                {
                    punchAnim = "TriplePunch";
                    duration = triplePunchDuration;
                    // Reset combo count after triple punch
                    punchComboCount = 0;
                }

                // Play the punch animation
                animator.CrossFadeInFixedTime(punchAnim, 0.1f);

                if (debugPunch)
                    UnityEngine.Debug.Log("Playing " + punchAnim + " animation, combo: " + punchComboCount);

                // Set a timer to end the punch state after the animation duration
                CancelInvoke("EndPunch");
                Invoke("EndPunch", duration);
            }
        }

        // Method to end the punch state
        protected virtual void EndPunch()
        {
            isPunching = false;
            animator.SetBool(AnimatorParameters.IsPunching, false);

            if (debugPunch)
                UnityEngine.Debug.Log("Punch animation ended");
        }

        // Methods to handle collectibles
        public virtual void AddHealth(int amount)
        {
            health = Mathf.Clamp(health + amount, 0, 100);
            UnityEngine.Debug.Log("Health increased by " + amount + ". Current health: " + health);
        }

        public virtual void AddMoney(int amount)
        {
            money += amount;
            UnityEngine.Debug.Log("Money increased by " + amount + ". Current money: " + money);
        }
    }
}
using UnityEngine;

namespace CharacterController
{
    public class ThirdPersonController : ThirdPersonAnimator
    {
        [Header("Character Stats")]
        [Tooltip("Character's health")]
        public int health = 100;
        [Tooltip("Character's max health")]
        public int maxHealth = 100; 
        [Tooltip("Character's money")]
        public int money = 50;

        public ThirdPersonMotor motor; 

        public void Init() 
        {
            base.Init();
            motor = this; 
            health = maxHealth; 
        }

        public virtual void ControlAnimatorRootMotion()
        {
            if (motor.isKnockback || !this.enabled) return; 

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
            if (motor.isKnockback || lockMovement) return; 

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
            if (motor.isKnockback || lockRotation) return; 

            bool validInput = input != Vector3.zero || (isStrafing ? strafeSpeed.rotateWithCamera : freeSpeed.rotateWithCamera);
            if (validInput)
            {
                inputSmooth = Vector3.Lerp(inputSmooth, input, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);
                Vector3 dir = (isStrafing && (!isSprinting || sprintOnlyFree == false) || (freeSpeed.rotateWithCamera && input == Vector3.zero)) && rotateTarget ?
                rotateTarget.forward : moveDirection;
                RotateToDirection(dir);
            }
        }

        public void UpdateMoveDirection(Transform referenceTransform = null) 
        {
            if (motor.isKnockback) 
            {
                moveDirection = Vector3.zero;
                return;
            }

            if (input.magnitude <= 0.01)
            {
                moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);
                return;
            }

            if (referenceTransform && !rotateByWorld)
            {
                var right = referenceTransform.right;
                right.y = 0;
                var forward = Quaternion.AngleAxis(-90, Vector3.up) * right;
                moveDirection = (inputSmooth.x * right) + (inputSmooth.z * forward);
            }
            else
            {
                moveDirection = new Vector3(inputSmooth.x, 0, inputSmooth.z);
            }
        }

        public virtual void Sprint(bool value)
        {
            if (motor.isKnockback || lockMovement) return; 

            var sprintConditions = (input.sqrMagnitude > 0.1f && isGrounded &&
                !(isStrafing && !strafeSpeed.walkByDefault && (horizontalSpeed >= 0.5 || horizontalSpeed <= -0.5 || verticalSpeed <= 0.1f)));

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
            if (motor.isKnockback || lockMovement) return; 
            isStrafing = !isStrafing;
        }

        public virtual void Jump()
        {
            if (motor.isKnockback || lockMovement) return; 

            if (isCrouching) return;
            jumpCounter = jumpTimer;
            isJumping = true;

            if (input.sqrMagnitude < 0.1f)
                animator.CrossFadeInFixedTime("Jump", 0.1f);
            else
                animator.CrossFadeInFixedTime("JumpMove", .2f);
        }

        public virtual void DoubleJump()
        {
            if (motor.isKnockback || lockMovement) return; 

            if (CanDoubleJump())
            {
                jumpCounter = jumpTimer;
                isJumping = true;
                hasDoubleJumped = true;

                if (input.sqrMagnitude < 0.1f)
                    animator.CrossFadeInFixedTime("Jump", 0.1f);
                else
                    animator.CrossFadeInFixedTime("JumpMove", .2f);
            }
        }

        public virtual void Crouch(bool value = true)
        {
            if (motor.isKnockback || lockMovement) return; 

            if (!isGrounded) return;
            if (value && isSprinting)
            {
                isSprinting = false;
            }

            motor.ApplyCrouch(value); 
   
            if (motor.isCrouching == value) 
            {
                if (motor.isCrouching)
                {
                    animator.CrossFadeInFixedTime("Crouch", 0.2f);
                }
                else
                {
                    animator.CrossFadeInFixedTime("Stand", 0.2f);
                }
            }
        }


        public virtual void Punch()
        {
            if (motor.isKnockback || lockMovement) return; 

            if (isCrouching) return;

            if (isPunching)
            {
                float timeSinceLastPunch = Time.time - lastPunchTime;
                if (timeSinceLastPunch <= punchComboTimeWindow && punchComboCount < 3)
                {
                    punchComboCount++;
                    lastPunchTime = Time.time;
                    if (debugPunch) UnityEngine.Debug.Log("Punch combo increased to: " + punchComboCount);
                }
                return;
            }

            if (isGrounded)
            {
                float timeSinceLastPunch = Time.time - lastPunchTime;
                if (timeSinceLastPunch > punchComboTimeWindow)
                {
                    punchComboCount = 1;
                }
                else
                {
                    punchComboCount = Mathf.Clamp(punchComboCount + 1, 1, 3);
                }

                isPunching = true;
                lastPunchTime = Time.time;

                animator.SetBool(AnimatorParameters.IsPunching, true);
                animator.SetInteger(AnimatorParameters.PunchCombo, punchComboCount);

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
                    punchComboCount = 0;
                }

                animator.CrossFadeInFixedTime(punchAnim, 0.1f);
                if (debugPunch) UnityEngine.Debug.Log("Playing " + punchAnim + " animation, combo: " + punchComboCount);
                CancelInvoke("EndPunch");
                Invoke("EndPunch", duration);
            }
        }

        protected virtual void EndPunch()
        {
            isPunching = false;
            animator.SetBool(AnimatorParameters.IsPunching, false);
            if (debugPunch) UnityEngine.Debug.Log("Punch animation ended");
        }

        public virtual void TakeDamage(int amount)
        {
            if (health <= 0) return;

            health -= amount;
            health = Mathf.Clamp(health, 0, maxHealth);

            UnityEngine.Debug.Log("Damage taken: " + amount + ". Current health: " + health);

            if (health <= 0)
            {
                Die();
            }

        }

        public virtual void TakeKnockbackDamage(int amount, Vector3 sourcePosition)
        {
            if (health <= 0) return;

            TakeDamage(amount);

            if (health > 0)
            {
                motor.ApplyKnockback(amount, sourcePosition);
            }
        }

        protected virtual void Die()
        {
            UnityEngine.Debug.Log("Character Died!");
        }

        public virtual void AddHealth(int amount)
        {
            if (health <= 0) return; 
            health = Mathf.Clamp(health + amount, 0, maxHealth); 
            UnityEngine.Debug.Log("Health increased by " + amount + ". Current health: " + health);
        }

        public virtual void AddMoney(int amount)
        {
            if (health <= 0) return; 
            money += amount;
            UnityEngine.Debug.Log("Money increased by " + amount + ". Current money: " + money);
        }
    }
}
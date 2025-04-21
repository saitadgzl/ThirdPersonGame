using UnityEngine;
using System.Collections; 

namespace CharacterController
{
    public class ThirdPersonMotor : MonoBehaviour
    {
        [Header("- Movement")]
        [Tooltip("Turn off if you have 'in place' animations and use this values above to move the character, or use with root motion as extra speed")]
        public bool useRootMotion = false;
        [Tooltip("Use this to rotate the character using the World axis, or false to use the camera axis - CHECK for Isometric Camera")]
        public bool rotateByWorld = false;
        [Tooltip("Check This to use sprint on press button to your Character run until the stamina finish or movement stops\nIf uncheck your Character will sprint as long as the SprintInput is pressed or the stamina finishes")]
        public bool useContinuousSprint = true;
        [Tooltip("Check this to sprint always in free movement")]
        public bool sprintOnlyFree = true;
        public enum LocomotionType
        {
            FreeWithStrafe,
            OnlyStrafe,
            OnlyFree,
        }
        public LocomotionType locomotionType = LocomotionType.FreeWithStrafe;
        public MovementSpeed freeSpeed, strafeSpeed;

        [Header("- Airborne")]
        [Tooltip("Use the currently Rigidbody Velocity to influence on the Jump Distance")]
        public bool jumpWithRigidbodyForce = false;
        [Tooltip("Rotate or not while airborne")]
        public bool jumpAndRotate = true;
        [Tooltip("How much time the character will be jumping")]
        public float jumpTimer = 0.3f;
        [Tooltip("Add Extra jump height, if you want to jump only with Root Motion leave the value with 0.")]
        public float jumpHeight = 4f;
        [Tooltip("Enable double jump")]
        public bool doubleJumpEnabled = true;
        [Tooltip("Height of the second jump")]
        public float doubleJumpHeight = 3f;
        [Tooltip("Speed that the character will move while airborne")]
        public float airSpeed = 5f;
        [Tooltip("Smoothness of the direction while airborne")]
        public float airSmooth = 6f;
        [Tooltip("Apply extra gravity when the character is not grounded")]
        public float extraGravity = -10f;
        [HideInInspector]
        public float limitFallVelocity = -15f;

        [Header("- Ground")]
        [Tooltip("Layers that the character can walk on")]
        public LayerMask groundLayer = 1 << 0;
        [Tooltip("Distance to became not grounded")]
        public float groundMinDistance = 0.25f;
        public float groundMaxDistance = 0.5f;
        [Tooltip("Max angle to walk")]
        [Range(30, 80)] public float slopeLimit = 75f;

        [Header("- Crouch")]
        [Tooltip("Speed reduction when crouching")]
        [Range(0.1f, 1f)] public float crouchSpeedMultiplier = 0.5f;
        [Tooltip("Height of collider when crouching")]
        public float crouchColliderHeight = 1.0f;

        [Header("- Knockback")]
        [Tooltip("Knockback mesafesi (Hasar < 50)")]
        public float lightKnockbackDistance = 2f;
        [Tooltip("Knockback mesafesi (Hasar >= 50)")]
        public float hardKnockbackDistance = 5f;
        [Tooltip("Knockback hareketinin süresi (saniye)")]
        public float knockbackDuration = 0.3f;
        [Tooltip("Knockback eþik deðeri")]
        public float knockbackThreshold = 50f;

        internal Animator animator;
        internal Rigidbody _rigidbody;
        internal PhysicMaterial frictionPhysics, maxFrictionPhysics, slippyPhysics;
        internal CapsuleCollider _capsuleCollider;

        internal bool isJumping;
        internal bool hasDoubleJumped;
        internal bool isPunching;
        internal bool isCrouching;
        internal bool isKnockback;
        internal int punchComboCount;
        internal float lastPunchTime;
        internal bool isStrafing
        {
            get { return _isStrafing; }
            set { _isStrafing = value; }
        }
        internal bool isGrounded { get; set; }
        internal bool isSprinting { get; set; }
        public bool stopMove { get; protected set; }

        internal float inputMagnitude;
        internal float verticalSpeed;
        internal float horizontalSpeed;
        internal float moveSpeed;
        internal float verticalVelocity;
        internal float colliderRadius, colliderHeight;
        internal float heightReached;
        internal float jumpCounter;
        internal float groundDistance;
        internal RaycastHit groundHit;
        internal bool lockMovement = false;
        internal bool lockRotation = false;
        internal bool _isStrafing;
        internal Transform rotateTarget;
        internal Vector3 input;
        internal Vector3 colliderCenter;
        internal Vector3 inputSmooth;
        internal Vector3 moveDirection;
        internal float originalColliderHeight;

        private Coroutine knockbackCoroutine; 
        private ThirdPersonAnimator _animatorController; 

        public virtual void Init() 
        {
            animator = GetComponent<Animator>();
            _animatorController = GetComponent<ThirdPersonAnimator>(); 
            animator.updateMode = AnimatorUpdateMode.Fixed;

            frictionPhysics = new PhysicMaterial { name = "frictionPhysics", staticFriction = .25f, dynamicFriction = .25f, frictionCombine = PhysicMaterialCombine.Multiply };
            maxFrictionPhysics = new PhysicMaterial { name = "maxFrictionPhysics", staticFriction = 1f, dynamicFriction = 1f, frictionCombine = PhysicMaterialCombine.Maximum };
            slippyPhysics = new PhysicMaterial { name = "slippyPhysics", staticFriction = 0f, dynamicFriction = 0f, frictionCombine = PhysicMaterialCombine.Minimum };

            _rigidbody = GetComponent<Rigidbody>();
            _capsuleCollider = GetComponent<CapsuleCollider>();
            colliderCenter = _capsuleCollider.center;
            colliderRadius = _capsuleCollider.radius;
            colliderHeight = _capsuleCollider.height;
            originalColliderHeight = colliderHeight;

            isGrounded = true;
            isPunching = false;
            isCrouching = false;
            isKnockback = false; 
            punchComboCount = 0;
            lastPunchTime = 0f;
        }

        public virtual void UpdateMotor()
        {
            if (isKnockback) return;

            CheckGround();
            CheckSlopeLimit();
            ControlJumpBehaviour();
            AirControl();
        }

        public virtual void SetControllerMoveSpeed(MovementSpeed speed)
        {
            if (isKnockback) return; 

            float targetSpeed;
            if (speed.walkByDefault)
                targetSpeed = isSprinting ? speed.runningSpeed : speed.walkSpeed;
            else
                targetSpeed = isSprinting ? speed.sprintSpeed : speed.runningSpeed;

            if (isCrouching)
                targetSpeed *= crouchSpeedMultiplier;
            moveSpeed = Mathf.Lerp(moveSpeed, targetSpeed, speed.movementSmooth * Time.deltaTime);
        }

        public virtual void MoveCharacter(Vector3 _direction)
        {
            if (isKnockback || lockMovement) return; 

            inputSmooth = Vector3.Lerp(inputSmooth, input, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);
            if (!isGrounded || isJumping) return;

            _direction.y = 0;
            _direction.x = Mathf.Clamp(_direction.x, -1f, 1f);
            _direction.z = Mathf.Clamp(_direction.z, -1f, 1f);
            if (_direction.magnitude > 1f)
                _direction.Normalize();

            Vector3 targetPosition = (useRootMotion ? animator.rootPosition : _rigidbody.position) + _direction * (stopMove ? 0 : moveSpeed) * Time.deltaTime;
            Vector3 targetVelocity = (targetPosition - transform.position) / Time.deltaTime;

            bool useVerticalVelocity = true;
            if (useVerticalVelocity) targetVelocity.y = _rigidbody.velocity.y;
            _rigidbody.velocity = targetVelocity;
        }

        public virtual void CheckSlopeLimit()
        {
            if (isKnockback || input.sqrMagnitude < 0.1) return; 

            RaycastHit hitinfo;
            var hitAngle = 0f;

            if (Physics.Linecast(transform.position + Vector3.up * (_capsuleCollider.height * 0.5f), transform.position + moveDirection.normalized * (_capsuleCollider.radius + 0.2f), out hitinfo, groundLayer))
            {
                hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);
                var targetPoint = hitinfo.point + moveDirection.normalized * _capsuleCollider.radius;
                if ((hitAngle > slopeLimit) && Physics.Linecast(transform.position + Vector3.up * (_capsuleCollider.height * 0.5f), targetPoint, out hitinfo, groundLayer))
                {
                    hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);
                    if (hitAngle > slopeLimit && hitAngle < 85f)
                    {
                        stopMove = true;
                        return;
                    }
                }
            }
            stopMove = false;
        }

        public virtual void RotateToPosition(Vector3 position)
        {
            if (isKnockback || lockRotation) return; 
            Vector3 desiredDirection = position - transform.position;
            RotateToDirection(desiredDirection.normalized);
        }

        public virtual void RotateToDirection(Vector3 direction)
        {
            if (isKnockback || lockRotation) return; 
            RotateToDirection(direction, isStrafing ? strafeSpeed.rotationSpeed : freeSpeed.rotationSpeed);
        }

        public virtual void RotateToDirection(Vector3 direction, float rotationSpeed)
        {
            if (isKnockback || lockRotation || (!jumpAndRotate && !isGrounded)) return; 

            direction.y = 0f;
            if (direction.normalized.magnitude < 0.01f) return; 
            Vector3 desiredForward = Vector3.RotateTowards(transform.forward, direction.normalized, rotationSpeed * Time.deltaTime, .1f);
            Quaternion _newRotation = Quaternion.LookRotation(desiredForward);
            transform.rotation = _newRotation;
        }

        protected virtual void ControlJumpBehaviour()
        {
            if (isKnockback || !isJumping) return; 

            jumpCounter -= Time.deltaTime;
            if (jumpCounter <= 0)
            {
                jumpCounter = 0;
                isJumping = false;
            }

            var vel = _rigidbody.velocity;
            vel.y = hasDoubleJumped ? doubleJumpHeight : jumpHeight;
            _rigidbody.velocity = vel;
        }

        public virtual void AirControl()
        {
            if (isKnockback || (isGrounded && !isJumping)) return; 

            if (transform.position.y > heightReached) heightReached = transform.position.y;
            inputSmooth = Vector3.Lerp(inputSmooth, input, airSmooth * Time.deltaTime);

            if (jumpWithRigidbodyForce && !isGrounded)
            {
                _rigidbody.AddForce(moveDirection * airSpeed * Time.deltaTime, ForceMode.VelocityChange);
                return;
            }

            moveDirection.y = 0;
            moveDirection.x = Mathf.Clamp(moveDirection.x, -1f, 1f);
            moveDirection.z = Mathf.Clamp(moveDirection.z, -1f, 1f);

            Vector3 targetPosition = _rigidbody.position + (moveDirection * airSpeed) * Time.deltaTime;
            Vector3 targetVelocity = (targetPosition - transform.position) / Time.deltaTime;

            targetVelocity.y = _rigidbody.velocity.y;
            _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, targetVelocity, airSmooth * Time.deltaTime);
        }


        protected virtual bool jumpFwdCondition
        {
            get
            {
                if (isKnockback) return false; // Eklendi
                Vector3 p1 = transform.position + _capsuleCollider.center + Vector3.up * -_capsuleCollider.height * 0.5F;
                Vector3 p2 = p1 + Vector3.up * _capsuleCollider.height;
                return Physics.CapsuleCastAll(p1, p2, _capsuleCollider.radius * 0.5f, transform.forward, 0.6f, groundLayer).Length == 0;
            }
        }

        public virtual bool CanDoubleJump()
        {
            if (isKnockback) return false; // Eklendi
            return doubleJumpEnabled && !isGrounded && !hasDoubleJumped && !isJumping;
        }


        protected virtual void CheckGround()
        {
            // if (isKnockback) return; // Knockback sýrasýnda yer kontrolü farklý yönetilebilir

            CheckGroundDistance();
            ControlMaterialPhysics();

            if (groundDistance <= groundMinDistance)
            {
                isGrounded = true;
                if (!isJumping && !isKnockback && groundDistance > 0.05f) // Güncellendi
                    _rigidbody.AddForce(transform.up * (extraGravity * 2 * Time.deltaTime), ForceMode.VelocityChange);
                heightReached = transform.position.y;
                hasDoubleJumped = false;
            }
            else
            {
                if (groundDistance >= groundMaxDistance)
                {
                    isGrounded = false;
                    verticalVelocity = _rigidbody.velocity.y;
                    if (!isJumping && !isKnockback) // Güncellendi
                    {
                        _rigidbody.AddForce(transform.up * extraGravity * Time.deltaTime, ForceMode.VelocityChange);
                    }
                }
                else if (!isJumping && !isKnockback) // Güncellendi
                {
                    _rigidbody.AddForce(transform.up * (extraGravity * 2 * Time.deltaTime), ForceMode.VelocityChange);
                }
            }
        }

        protected virtual void ControlMaterialPhysics()
        {
            if (isKnockback) // Eklendi
            {
                _capsuleCollider.material = slippyPhysics;
                return;
            }

            _capsuleCollider.material = (isGrounded && GroundAngle() <= slopeLimit + 1) ? frictionPhysics : slippyPhysics;

            if (isGrounded && input == Vector3.zero)
                _capsuleCollider.material = maxFrictionPhysics;
            else if (isGrounded && input != Vector3.zero)
                _capsuleCollider.material = frictionPhysics;
            else
                _capsuleCollider.material = slippyPhysics;
        }

        protected virtual void CheckGroundDistance()
        {
            // if (isKnockback) return; // Gerekirse knockback için farklý kontrol

            if (_capsuleCollider != null)
            {
                float radius = _capsuleCollider.radius * 0.9f;
                var dist = 10f;
                Ray ray2 = new Ray(transform.position + new Vector3(0, colliderHeight / 2, 0), Vector3.down);
                if (Physics.Raycast(ray2, out groundHit, (colliderHeight / 2) + dist, groundLayer) && !groundHit.collider.isTrigger)
                    dist = transform.position.y - groundHit.point.y;

                if (dist >= groundMinDistance)
                {
                    Vector3 pos = transform.position + Vector3.up * (_capsuleCollider.radius);
                    Ray ray = new Ray(pos, -Vector3.up);
                    if (Physics.SphereCast(ray, radius, out groundHit, _capsuleCollider.radius + groundMaxDistance, groundLayer) && !groundHit.collider.isTrigger)
                    {
                        Physics.Linecast(groundHit.point + (Vector3.up * 0.1f), groundHit.point + Vector3.down * 0.15f, out groundHit, groundLayer);
                        float newDist = transform.position.y - groundHit.point.y;
                        if (dist > newDist) dist = newDist;
                    }
                }
                groundDistance = (float)System.Math.Round(dist, 2);
            }
        }

        public virtual float GroundAngle()
        {
            var groundAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            return groundAngle;
        }

        public virtual float GroundAngleFromDirection()
        {
            var dir = isStrafing && input.magnitude > 0 ?
                (transform.right * input.x + transform.forward * input.z).normalized : transform.forward;
            var movementAngle = Vector3.Angle(dir, groundHit.normal) - 90;
            return movementAngle;
        }

        public virtual void ApplyCrouch(bool state)
        {
            if (isKnockback) return; // Eklendi

            if (state)
            {
                _capsuleCollider.height = crouchColliderHeight;
                Vector3 center = colliderCenter;
                center.y = colliderCenter.y - (originalColliderHeight - crouchColliderHeight) * 0.5f;
                _capsuleCollider.center = center;
                isCrouching = true; // Durumu burada güncelle
            }
            else
            {
                if (CheckHeadObstruction())
                {
                    isCrouching = true; // Engel varsa eðik kal, durumu güncelleme
                    return;
                }
                _capsuleCollider.height = originalColliderHeight;
                _capsuleCollider.center = colliderCenter;
                isCrouching = false; // Durumu burada güncelle
            }
        }

        protected virtual bool CheckHeadObstruction()
        {
            float heightDifference = originalColliderHeight - crouchColliderHeight;
            Vector3 rayStart = transform.position + _capsuleCollider.center + Vector3.up * (crouchColliderHeight * 0.5f); 
            return Physics.Raycast(rayStart, Vector3.up, heightDifference, groundLayer, QueryTriggerInteraction.Ignore);
        }

        public virtual void ApplyKnockback(float damageAmount, Vector3 sourcePosition)
        {
            if (isKnockback) return;

            float distance = damageAmount < knockbackThreshold ? lightKnockbackDistance : hardKnockbackDistance;
            Vector3 knockbackDirection = (transform.position - sourcePosition).normalized;
            knockbackDirection.y = 0;

            if (knockbackDirection.sqrMagnitude < 0.01f)
            {
                knockbackDirection = -transform.forward;
            }

            if (_animatorController)
            {
                if (distance == lightKnockbackDistance)
                    _animatorController.TriggerLightKnockback();
                else
                    _animatorController.TriggerHardKnockback();
            }

            if (knockbackCoroutine != null)
            {
                StopCoroutine(knockbackCoroutine);
            }
            knockbackCoroutine = StartCoroutine(KnockbackCoroutine(knockbackDirection, distance, knockbackDuration));
        }

        public IEnumerator KnockbackCoroutine(Vector3 direction, float distance, float duration)
        {
            isKnockback = true;
            lockMovement = true;
            lockRotation = true;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.useGravity = false;

            Vector3 startPosition = transform.position;
            Vector3 targetPosition = startPosition + direction * distance;
            float originalDistance = distance; // Engel kontrolü için orijinal mesafeyi sakla
            float elapsedTime = 0f;

            if (Physics.Linecast(startPosition + _capsuleCollider.center + Vector3.up * 0.1f, targetPosition + _capsuleCollider.center + Vector3.up * 0.1f, out RaycastHit hit, groundLayer, QueryTriggerInteraction.Ignore))
            {
                targetPosition = hit.point - direction * _capsuleCollider.radius * 1.1f; // Kapsül merkezi ve yarýçapý kullan
                distance = Vector3.Distance(startPosition, targetPosition);
                if (distance < _capsuleCollider.radius) // Çok yakýnsa veya iç içe geçtiyse
                {
                    yield return new WaitForSeconds(duration); // Sadece animasyon süresi kadar bekle
                    isKnockback = false;
                    lockMovement = false;
                    lockRotation = false;
                    _rigidbody.useGravity = true;
                    knockbackCoroutine = null;
                    yield break;
                }
                // Süreyi yeni mesafeye göre ayarla
                if (originalDistance > 0.01f) // Sýfýra bölme hatasýný önle
                {
                    duration *= (distance / originalDistance);
                }
                else
                {
                    duration = 0; // Mesafe sýfýrsa süre de sýfýr olmalý
                }

            }

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                _rigidbody.MovePosition(Vector3.Lerp(startPosition, targetPosition, t));
                elapsedTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            if (duration > 0) // Süre sýfýr deðilse son pozisyona ayarla
                _rigidbody.MovePosition(targetPosition);


            isKnockback = false;
            lockMovement = false;
            lockRotation = false;
            _rigidbody.useGravity = true;
            knockbackCoroutine = null;
        }


        [System.Serializable]
        public class MovementSpeed
        {
            [Range(1f, 20f)]
            public float movementSmooth = 6f;
            [Range(0f, 1f)]
            public float animationSmooth = 0.2f;
            [Tooltip("Rotation speed of the char")]
            public float rotationSpeed = 16f;
            [Tooltip("Character will limit movement to walk instead of running")]
            public bool walkByDefault = false;
            [Tooltip("Rotate with the Camera forward when idle")]
            public bool rotateWithCamera = false;
            [Tooltip("Speed to Walk using rigidbody or extra speed if you're using RootMotion")]
            public float walkSpeed = 2f;
            [Tooltip("Speed to Run using rigidbody or extra speed if you're using RootMotion")]
            public float runningSpeed = 4f;
            [Tooltip("Speed to Sprint using rigidbody or extra speed if you're using RootMotion")]
            public float sprintSpeed = 6f;
        }
    }
}
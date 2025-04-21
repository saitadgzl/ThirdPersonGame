using System.Diagnostics;
using UnityEngine;

namespace CharacterController
{
    public class ThirdPersonInput : MonoBehaviour
    {
        [Header("Controller Input")]
        public string horizontalInput = "Horizontal";
        public string verticallInput = "Vertical";
        public KeyCode jumpInput = KeyCode.Space;
        public KeyCode strafeInput = KeyCode.Tab;
        public KeyCode sprintInput = KeyCode.LeftShift;
        public KeyCode punchInput = KeyCode.Mouse0;
        public KeyCode crouchInput = KeyCode.LeftControl;

        [Header("Camera Input")]
        public string rotateCameraXInput = "Mouse X";
        public string rotateCameraYInput = "Mouse Y";

        [Header("Character Abilities")]
        [Tooltip("Enable or disable double jump capability")]
        public bool doubleJumpEnabled = true;

        [HideInInspector] public ThirdPersonController cc;
        [HideInInspector] public ThirdPersonCamera tpCamera;
        [HideInInspector] public Camera cameraMain;


        protected virtual void Start()
        {
            InitilizeController();
            InitializeTpCamera();

            if (cc != null)
            {
                cc.doubleJumpEnabled = doubleJumpEnabled;
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!cc) return; 
            cc.UpdateMotor();
            cc.ControlLocomotionType();
            cc.ControlRotationType();
        }

        protected virtual void Update()
        {
            if (!cc) return; 
            InputHandle();
            cc.UpdateAnimator();
        }

        public virtual void OnAnimatorMove()
        {
            if (!cc) return; 
            cc.ControlAnimatorRootMotion();
        }

        protected virtual void InitilizeController()
        {
            cc = GetComponent<ThirdPersonController>();
            if (cc != null)
                cc.Init();
        }

        protected virtual void InitializeTpCamera()
        {
            if (tpCamera == null)
            {
                tpCamera = FindAnyObjectByType<ThirdPersonCamera>();
                if (tpCamera == null)
                    return;
                if (tpCamera)
                {
                    tpCamera.SetMainTarget(this.transform);
                    tpCamera.Init();
                }
            }
        }

        protected virtual void InputHandle()
        {
            if (cc == null || (cc.motor != null && cc.motor.isKnockback) || cc.lockMovement)
            {
                if (cc != null) cc.input = Vector3.zero; 
                return;
            }

            MoveInput();
            CameraInput();
            SprintInput();
            StrafeInput();
            JumpInput();
            PunchInput();
            CrouchInput();
        }

        public virtual void MoveInput()
        {
            cc.input.x = Input.GetAxis(horizontalInput);
            cc.input.z = Input.GetAxis(verticallInput);
        }

        protected virtual void CameraInput()
        {
            if (!cameraMain)
            {
                if (!Camera.main) UnityEngine.Debug.Log("Missing a Camera with the tag MainCamera, please add one.");
                else
                {
                    cameraMain = Camera.main;
                    cc.rotateTarget = cameraMain.transform;
                }
            }

            if (cameraMain)
            {
                cc.UpdateMoveDirection(cameraMain.transform);
            }

            if (tpCamera == null)
                return;
            var Y = Input.GetAxis(rotateCameraYInput);
            var X = Input.GetAxis(rotateCameraXInput);

            tpCamera.RotateCamera(X, Y);
        }

        protected virtual void StrafeInput()
        {
            if (Input.GetKeyDown(strafeInput))
                cc.Strafe();
        }

        protected virtual void SprintInput()
        {
            if (Input.GetKeyDown(sprintInput))
                cc.Sprint(true);
            else if (Input.GetKeyUp(sprintInput))
                cc.Sprint(false);
        }

        protected virtual void PunchInput()
        {
            if (Input.GetKeyDown(punchInput))
            {
                cc.Punch();
            }
        }

        public virtual void CrouchInput()
        {
            if (Input.GetKeyDown(crouchInput))
            {
                if (cc != null && cc.motor != null)
                {
                    cc.Crouch(!cc.motor.isCrouching);
                }
            }
        }

        protected virtual bool JumpConditions()
        {
            return cc.isGrounded && cc.GroundAngle() < cc.slopeLimit && !cc.isJumping && !cc.stopMove;
        }

        protected virtual void JumpInput()
        {
            if (Input.GetKeyDown(jumpInput))
            {
                if (JumpConditions())
                {
                    cc.Jump();
                }
                else if (cc.CanDoubleJump()) 
                {
                    cc.DoubleJump();
                }
            }
        }
    }
}
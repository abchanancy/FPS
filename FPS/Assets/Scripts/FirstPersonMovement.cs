using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonMovement : MonoBehaviour
{
    //public
    public float speed = 100;
    public float crouchSpeed = 50;
    public float airSpeed = 10;
    public float slideSpeed = 150;
    public float mouseSpeed = 1f;
    public float jumpForce = 800f;
    public float crouchJumpForce = 400f;
    public float gravityMod = 5;
    public int crouchIterations = 8;
    public float playerHeight = 1.5f;
    public GroundCollider groundCol;
    public BoxCollider crouchingBox;
    public float maxSpeed = 100;
    public float maxSlideSpeed = 150;
    public float maxCrouchSpeed = 50;
    public float threshold = 0.01f;
    public float slideCounterMovement = 0.2f;
    public float counterMovement = 0.175f;
    public PhysicMaterial globalPhysMat;

    //bools
    [HideInInspector] public bool crouching = false;
    [HideInInspector] public bool crouchCoRunning = false;
    [HideInInspector] public bool airCrouch = false;
    [HideInInspector] public bool sliding = false;
    [HideInInspector] public bool jumpSquat = false;
    [HideInInspector] public bool airSlide = false;

    //accessable
    [HideInInspector] public float mousePosX;
    [HideInInspector] public float mousePosY;
    [HideInInspector] public float cameraChange;
    [HideInInspector] public Vector3 cameraPosTarget;
    [HideInInspector] public float currentSpeed;


    //private
    PlayerActions inputAction;
    Rigidbody rgd;
    private BoxCollider boxCol;
    private bool jumpTrigger = false;
    private bool slideTrigger = false;
    private Vector3 dirVec;
    private Coroutine crouchCouroutine = null;
    private float crouchingHiehgt;
    private Vector3 storedSlideVelocity = Vector3.zero;


    //Input
    Vector2 movementInput;
    [HideInInspector] public Vector2 mouseInput;


    private void Awake()
    {
        inputAction = new PlayerActions();
        inputAction.PlayerControlls.Move.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
        inputAction.PlayerControlls.Look.performed += ctx => mouseInput = ctx.ReadValue<Vector2>();
        inputAction.PlayerControlls.Jump.started += ctx => jumpTrigger = true;
        inputAction.PlayerControlls.Jump.canceled += ctx => jumpTrigger = false;
        inputAction.PlayerControlls.Crouch.canceled += ctx => slideTrigger = false;
        inputAction.PlayerControlls.Crouch.canceled += ctx => CancelSlide();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    void Start()
    {
        rgd = GetComponent<Rigidbody>();
        boxCol = GetComponent<BoxCollider>();
        mouseSpeed /= 10;
        crouchingHiehgt = 2 * playerHeight/3;
        currentSpeed = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log("collecting garbage");
            System.GC.Collect();
        }
        //Debug

        //Direction of movement
        dirVec = new Vector3(movementInput.x, 0f, movementInput.y);
        dirVec = transform.TransformDirection(dirVec).normalized;
        //Inputs
        Debug.Log(isGrounded());
        //Grounded
        if (isGrounded())
        {
            if(dirVec != Vector3.zero)
            {
                inputAction.PlayerControlls.Crouch.started += ctx => slideTrigger = true;
            }
        }
        //Airborne
        else
        {

        }
        inputAction.PlayerControlls.Crouch.started += ctx => Crouch();
        inputAction.PlayerControlls.Crouch.canceled += ctx => StandUp();

        //Camera
        CalculateCameraPosition();
    }

    private void FixedUpdate()
    {
        //UPDATE RGD
        if (!isGrounded())
        {
            if (airSlide)
            {
                rgd.AddForce(dirVec * Time.fixedDeltaTime * airSpeed/4, ForceMode.VelocityChange);
            }
            else
            {
                rgd.AddForce(dirVec * Time.fixedDeltaTime * airSpeed, ForceMode.VelocityChange);
            }
        }
        else
        {
            if (sliding)
            {
                if(airSlide && !jumpSquat)
                {
                    rgd.velocity += storedSlideVelocity.magnitude/4 * transform.forward;
                    airSlide = false;
                }
                rgd.AddForce(dirVec * Time.fixedDeltaTime * crouchSpeed, ForceMode.VelocityChange);
            }
            else
            {
                airSlide = false;
                if (crouching)
                {
                    rgd.AddForce(dirVec * Time.fixedDeltaTime * crouchSpeed, ForceMode.VelocityChange);
                }
                else
                {
                    rgd.AddForce(dirVec * Time.fixedDeltaTime * speed, ForceMode.VelocityChange);
                }
            }
        }

        //JUMP
        if (isGrounded() && jumpTrigger)
        {
            rgd.angularVelocity = Vector3.zero;
            if (sliding)
            {
                airSlide = true;
                storedSlideVelocity = rgd.velocity;
            }
            rgd.velocity = Vector3.zero;

            if (crouching)
            {
                if(sliding)
                {
                    rgd.AddForce((transform.up) * crouchJumpForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
                    rgd.velocity += storedSlideVelocity;
                }
                else
                {
                    rgd.AddForce((transform.up + dirVec) * crouchJumpForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
                }
            }
            else
            {
                rgd.AddForce((transform.up + dirVec) * jumpForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
            }
            jumpSquat = true;
            jumpTrigger = false;
        }
        if(jumpSquat || !isGrounded())
        {
            RemoveOtherVelocity(transform.forward);
        }
        //SLIDE
        if (isGrounded() && slideTrigger)
        {
            rgd.AddForce(dirVec * slideSpeed * Time.fixedDeltaTime, ForceMode.VelocityChange);
            sliding = true;
            slideTrigger = false;
            globalPhysMat.dynamicFriction = 0.9f;
        }

        CounterSteer(movementInput.x, movementInput.y, FindVelRelativeToLook());

        if (sliding)
        {
            CheckMaxSpeed(maxSlideSpeed);
        }
        else if (crouching && !airCrouch || airCrouch && isGrounded())
        {
            CheckMaxSpeed(maxCrouchSpeed);
        }
        else
        {
            CheckMaxSpeed(maxSpeed);
        }

        //Add gravity after all movement and speed calcs
        rgd.AddForce(transform.up * Physics.gravity.y * gravityMod * Time.fixedDeltaTime * speed, ForceMode.Acceleration);

        //UPDATE CAMERA        
        float mouseX = mouseInput.x * mouseSpeed;
        float mouseY = mouseInput.y * mouseSpeed;
        mousePosX += mouseX;
        mousePosY += mouseY;
        mousePosY = Mathf.Clamp(mousePosY, -80, 80);       
        transform.Rotate(0, mouseX, 0);
    }

    private void RemoveOtherVelocity(Vector3 direction)
    {
        Vector3 velocityInLookDir = Quaternion.Euler(0, mouseInput.x * mouseSpeed, 0) * rgd.velocity;
        rgd.velocity = new Vector3(velocityInLookDir.x, rgd.velocity.y, velocityInLookDir.z);
        rgd.angularVelocity = Vector3.zero;
    }

    private void CancelSlide()
    {
        sliding = false;
        globalPhysMat.dynamicFriction = 0.05f;
    }

    private void CounterSteer(float x, float y, Vector2 mag)
    {
        //frames where jump has been initiated, but we are still concidered grounded
        if (jumpSquat || !isGrounded())
        {
            if(!isGrounded())
            {
                jumpSquat = false;
            }
            return;
        }
        if(sliding || crouching && isGrounded())
        {
            if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
            {
                rgd.AddForce(speed * transform.right * Time.fixedDeltaTime * -mag.x * slideCounterMovement);
            }
            if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
            {
                rgd.AddForce(speed * transform.forward * Time.fixedDeltaTime * -mag.y * slideCounterMovement);
            }
        }
        else
        {
            //Counter movement
            if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
            {
                rgd.AddForce(speed * transform.right * Time.fixedDeltaTime * -mag.x * counterMovement);
            }
            if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
            {
                rgd.AddForce(speed * transform.forward * Time.fixedDeltaTime * -mag.y * counterMovement);
            }
        }
        //slow down diagnal
        if(Mathf.Abs(dirVec.x) >= 0.7 && Mathf.Abs(dirVec.z) >= 0.7)
        {
            rgd.AddForce(speed * transform.forward * Time.fixedDeltaTime * -mag.y/7 * counterMovement);
            rgd.AddForce(speed * transform.right * Time.fixedDeltaTime * -mag.x/7 * counterMovement);
        }
    }

    private void CheckMaxSpeed(float max)
    {
        if(rgd.velocity.magnitude <= maxCrouchSpeed && isGrounded())
        {
            CancelSlide();
            return;
        }
        if(rgd.velocity.x > max)
        {
            rgd.velocity = new Vector3(max, rgd.velocity.y, rgd.velocity.z);
        }
        if (rgd.velocity.z > max)
        {
            rgd.velocity = new Vector3(rgd.velocity.x, rgd.velocity.y, max);
        }
        if (rgd.velocity.x < -max)
        {
            rgd.velocity = new Vector3(-max, rgd.velocity.y, rgd.velocity.z);
        }
        if (rgd.velocity.z < -max)
        {
            rgd.velocity = new Vector3(rgd.velocity.x, rgd.velocity.y, -max);
        }
    }

    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rgd.velocity.x, rgd.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rgd.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private void LateUpdate()
    {

    }

    private void OnEnable()
    {
        inputAction.Enable();
    }

    private void OnDisable()
    {
        inputAction.Disable();
    }

    public bool isGrounded()
    {
        return groundCol.grounded;
    }

    /*
    public bool isRayGrounded()
    {
        return groundCol.rayGrounded;
    }
    */

    void Crouch()
    {
        crouching = true;
        if (crouchCouroutine != null)
        {
            StopCoroutine(crouchCouroutine);
        }

        groundCol.GetComponent<BoxCollider>().center = new Vector3(0, crouchingBox.center.y - crouchingHiehgt / 2, 0);
        if (isGrounded())
        {
            float crouchTarget = 0;
            crouchCouroutine = StartCoroutine(CameraLerp(crouchTarget, crouchIterations));
        }
        //dont play animation, just change hibox
        else
        {
            airCrouch = true;
            slideTrigger = false;
            crouchingBox.center = new Vector3(0, 0, 0);
        }
        crouchingBox.enabled = true;
        boxCol.enabled = false;
    }

    void StandUp()
    {
        crouching = false;

        if (crouchCouroutine != null)
        {
            StopCoroutine(crouchCouroutine);
        }
        if (isGrounded())
        {
            float standTarget = playerHeight / 3;
            crouchCouroutine = StartCoroutine(CameraLerp(standTarget, crouchIterations));
        }
        airCrouch = false;
        crouchingBox.enabled = false;
        boxCol.enabled = true;
        crouchingBox.center = new Vector3(0, -crouchingHiehgt/2, 0);
        groundCol.GetComponent<BoxCollider>().center = new Vector3(0, boxCol.center.y - playerHeight / 2, 0);

    }

    void CalculateCameraPosition()
    {
        cameraPosTarget = transform.position;
        if(airCrouch)
        {
            cameraPosTarget.y += crouchingHiehgt/2;
        }
        else if (crouchCoRunning)
        {
            cameraPosTarget = new Vector3(cameraPosTarget.x, 0, cameraPosTarget.z);
            cameraPosTarget += new Vector3(0, cameraChange, 0);
        }
        else if(!crouching)
        {
            cameraPosTarget.y += playerHeight / 3;
        }
    }

    IEnumerator CameraLerp(float target, float iterations)
    {
        crouchCoRunning = true;
        float speed = 1 / iterations;
        for (float t = 0f; t <= 1f; t += speed)
        {
            cameraChange = Mathf.Lerp(Camera.main.transform.position.y, transform.position.y + target, t);
            yield return null;
        }
        crouchCoRunning = false;
    }
}

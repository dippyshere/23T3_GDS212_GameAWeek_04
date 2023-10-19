using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform visualTransform;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI bellText;

    private float groundCheckRadius = 0.3f;
    private float speed = 8;
    private float jumpForce = 30f;
    private bool jumped = false;
    private bool isGrounded;
    private Rigidbody rigidBody;
    private Vector3 direction;
    private Vector3 moveDirection = Vector3.forward;
    private float horizontalInput;
    private float verticalInput;
    private bool canMove = true;
    private int coins = 0;
    private int bells = 0;
    
    public GravityBody gravityBody;

    void Start()
    {
        rigidBody = transform.GetComponent<Rigidbody>();
        gravityBody = transform.GetComponent<GravityBody>();
    }

    void Update()
    {
        direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);

        if (!isGrounded && !jumped)
        {
            jumped = true;
            animator.SetTrigger("Jump");
            animator.ResetTrigger("Land");
        }
        else if (isGrounded && jumped)
        {
            jumped = false;
            animator.SetTrigger("Land");
            animator.ResetTrigger("Jump");
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && canMove)
        {
            rigidBody.AddForce(-gravityBody.GravityDirection * jumpForce, ForceMode.Impulse);
        }
        if (canMove)
        {
            visualTransform.rotation = Quaternion.Slerp(visualTransform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(moveDirection, gravityBody.GravityDirection).normalized, -gravityBody.GravityDirection), Time.deltaTime * 10f);
        }
    }

    void FixedUpdate()
    {
        bool isRunning = direction.magnitude > 0.1f;

        if (isRunning && canMove)
        {
            //Vector3 viewDir = transform.position - Camera.main.transform.position;
            //orientation.forward = viewDir.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
            orientation.rotation = targetRotation;
            moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
            moveDirection = Vector3.ProjectOnPlane(moveDirection, gravityBody.GravityDirection).normalized;
            rigidBody.AddForce(moveDirection.normalized * speed * 10f, ForceMode.Force);

            // rigidBody.MovePosition(rigidBody.position + moveDirection * (speed * Time.fixedDeltaTime));

            if (!isGrounded)
            {
                animator.SetFloat("Run", 0);
            }
            else
            {
                animator.SetFloat("Run", direction.magnitude * rigidBody.velocity.magnitude / 5);
            }
        }
        else
        {
            animator.SetFloat("Run", 0);
        }
    }

    private void OnDrawGizmos()
    {
        bool isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawSphere(groundCheck.position, groundCheckRadius);
        Gizmos.color = Color.blue;
        if (gravityBody != null)
        {
            Gizmos.DrawLine(transform.position, transform.position - gravityBody.GravityDirection * 2f);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + moveDirection);
        Gizmos.color = Color.red;
        if (gravityBody != null)
        {
            Gizmos.DrawLine(transform.position, transform.position + Vector3.ProjectOnPlane(moveDirection, gravityBody.GravityDirection).normalized);
        }
    }

    public void UpdateCoins()
    {
        coinText.text = coins.ToString("N0", CultureInfo.InvariantCulture);
    }

    public void UpdateBells()
    {
        bellText.text = bells.ToString("N0", CultureInfo.InvariantCulture);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            coins++;
            UpdateCoins();
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Bell"))
        {
            bells++;
            UpdateBells();
            Destroy(other.gameObject);
            StartCoroutine(BeginBellDance());
        }
    }

    IEnumerator BeginBellDance()
    {
        canMove = false;
        Quaternion previousRotation = visualTransform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
        targetRotation = Quaternion.AngleAxis(180f, -gravityBody.GravityDirection) * targetRotation;
        Debug.Log("targetRotation: " + targetRotation.eulerAngles);
        // slerp the visual transform to the camera
        while (true)
        {
            visualTransform.rotation = Quaternion.Slerp(visualTransform.rotation, targetRotation, Time.deltaTime * 10f);
            if (Quaternion.Angle(visualTransform.rotation, targetRotation) < 1f)
            {
                break;
            }
            yield return null;
        }
        animator.SetTrigger("Spell");
        yield return new WaitForSeconds(0.8f);
        while (true)
        {
            visualTransform.rotation = Quaternion.Slerp(visualTransform.rotation, previousRotation, Time.deltaTime * 15f);
            if (Quaternion.Angle(visualTransform.rotation, previousRotation) < 1f)
            {
                break;
            }
            yield return null;
        }
        canMove = true;
        yield return null;
    }
}
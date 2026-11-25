using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Рух")]
    [SerializeField, Range(1f, 20f)]  private float maxSpeed = 8f;
    [SerializeField, Range(5f, 200f)] private float acceleration = 70f;
    [SerializeField, Range(5f, 200f)] private float deceleration = 90f;

    [Header("Стрибок")]
    [SerializeField, Range(0.5f, 10f)] private float jumpHeight = 3f;
    [SerializeField, Range(0f, 0.5f)]  private float coyoteTime = 0.12f;
    [SerializeField, Range(0f, 0.5f)]  private float jumpBufferTime = 0.12f;
    [SerializeField, Range(0.1f, 1f)]  private float jumpCutMultiplier = 0.5f;

    [Header("Земля")]
    [SerializeField] private Transform groundCheck;
    [SerializeField, Range(0.05f, 0.5f)] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D body;
    private PlayerControls controls;
    private float moveInput;
    private bool isGrounded;
    private float coyoteCounter;
    private float jumpBufferCounter;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();
    }

    // Без Enable введення не приходить — і помилки при цьому не буде
    private void OnEnable()  => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();
    private void OnDestroy() => controls.Dispose();

    // Коло перекриває всю ширину ніг, а не одну точку
    private bool CheckGround() =>
        Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

    // Без візуалізації датчик налагоджувати неможливо
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Application.isPlaying && isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    private void Update()
    {
        isGrounded = CheckGround();

        // Введення читають у Update: FixedUpdate може пропустити короткий натиск
        moveInput = controls.Player.Move.ReadValue<Vector2>().x;

        coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;

        if (controls.Player.Jump.WasPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Змінна висота стрибка: якщо відпустити кнопку під час підйому, ріжемо швидкість угору
        if (controls.Player.Jump.WasReleasedThisFrame() && body.linearVelocity.y > 0f)
        {
            body.linearVelocity = new Vector2(
                body.linearVelocity.x,
                body.linearVelocity.y * jumpCutMultiplier
            );
        }
    }

    private void FixedUpdate()
    {
        float target = moveInput * maxSpeed;

        // Гальмуємо ШВИДШЕ, ніж розганяємось — саме це дає відчуття контролю
        float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;

        float x = Mathf.MoveTowards(body.linearVelocity.x, target,
                                    rate * Time.fixedDeltaTime);
        float y = body.linearVelocity.y;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
            y = Mathf.Sqrt(2f * gravity * jumpHeight);

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        body.linearVelocity = new Vector2(x, y);
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, PlayerInputSystem.IPlayerActions
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float screenPadding = 0.5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Camera cam;

    private Vector2 minBounds;
    private Vector2 maxBounds;

    private PlayerInputSystem inputSystem;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;

        rb.gravityScale = 0;
        rb.freezeRotation = true;

        CalculateBounds();

        // Инициализируем систему ввода
        inputSystem = new PlayerInputSystem();
        inputSystem.player.AddCallbacks(this);
        inputSystem.player.Enable();
    }

    private void OnDestroy()
    {
        if (inputSystem != null)
        {
            inputSystem.player.RemoveCallbacks(this);
            inputSystem.Dispose();
        }
    }

    private void FixedUpdate()
    {
        if (moveInput != Vector2.zero) // Проверка на нулевое значение
        {
            Vector2 nextPos =
                rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;

            nextPos.x = Mathf.Clamp(nextPos.x, minBounds.x, maxBounds.x);
            nextPos.y = Mathf.Clamp(nextPos.y, minBounds.y, maxBounds.y);

            rb.MovePosition(nextPos);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>().normalized;
    }

    private void CalculateBounds()
    {
        Vector3 bottomLeft =
            cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));

        Vector3 topRight =
            cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

        minBounds = new Vector2(
            bottomLeft.x + screenPadding,
            bottomLeft.y + screenPadding);

        maxBounds = new Vector2(
            topRight.x - screenPadding,
            topRight.y - screenPadding);
    }
}

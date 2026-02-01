using UnityEngine;

public class Eater : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float zigzagAmplitude = 2f; // Амплитуда зигзага
    [SerializeField] private float zigzagFrequency = 2f; // Частота зигзага
    [SerializeField] private bool moveDownwards = true; // true = движение вниз, false = вверх
    
    [Header("Target Settings")]
    [SerializeField] private Transform playerTransform; // Ссылка на игрока
    
    [Header("Sprite Settings")]
    [SerializeField] private Transform spriteTransform; // Объект со спрайтом (если спрайт на дочернем объекте)
    
    [Header("Despawn Settings")]
    [SerializeField] private float despawnDistance = 25f; // Дистанция за границей экрана, после которой враг исчезает
    
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 10f; // Максимальное здоровье врага
    private float currentHealth;
    
    [Header("Hit Feedback Settings")]
    [SerializeField] private float hitSlowdownDuration = 0.2f; // Длительность замедления при попадании
    [SerializeField] private float hitSlowdownAmount = 0.5f; // Насколько замедляется (0.5 = 50% от скорости)
    [SerializeField] private float hitFlashDuration = 0.1f; // Длительность эффекта белого цвета
    [SerializeField] private Color hitFlashColor = Color.white; // Цвет при попадании
    
    [Header("Collision Settings")]
    [SerializeField] private float damageToPlayer = 10f;
    [SerializeField] private bool canDamagePlayer = true;
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float zigzagTimer = 0f;
    private bool hasCollidedWithPlayer = false;
    private Color originalColor;
    private float currentSpeedMultiplier = 1f; // Текущий множитель скорости

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Если спрайтрендерер не найден на этом объекте, ищем его на указанном объекте
        if (spriteRenderer == null && spriteTransform != null)
        {
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        }
        
        // Если всё ещё не найден, ищем в дочерних объектах
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        // Инициализируем здоровье
        currentHealth = maxHealth;

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning("Eater: SpriteRenderer не найден на этом объекте или в дочерних объектах!");
        }
        else
        {
            // Сохраняем оригинальный цвет спрайта
            originalColor = spriteRenderer.color;
        }

        // Настройка физики
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        // Поиск игрока, если не назначен
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
    }

    private void OnDestroy()
    {
        hasCollidedWithPlayer = false;
    }

    private void Update()
    {
        // Обновляем таймер зигзага
        zigzagTimer += Time.deltaTime;

        // Проверяем дистанцию для исчезновения
        CheckDespawnDistance();
    }

    private void FixedUpdate()
    {
        if (playerTransform == null)
            return;

        // Враг движется либо вниз (moveDownwards = true), либо вверх (moveDownwards = false)
        float verticalDirection = moveDownwards ? -1f : 1f;

        float playerYDiff = playerTransform.position.y - transform.position.y;
        
        // Зигзагообразное горизонтальное движение ВСЕГДА
        float horizontalMovement = Mathf.Sin(zigzagTimer * zigzagFrequency) * zigzagAmplitude;

        // Логика привлечения к игроку
        // Если moveDownwards = true: привлекаемся, если игрок ПОД врагом (playerYDiff < 0)
        // Если moveDownwards = false: привлекаемся, если игрок НАД врагом (playerYDiff > 0)
        bool shouldAttractToPlayer = moveDownwards ? (playerYDiff < -0.5f) : (playerYDiff > 0.5f);

        if (shouldAttractToPlayer)
        {
            // Горизонтальное движение с привлечением к игроку
            float playerXDiff = playerTransform.position.x - transform.position.x;
            float targetHorizontal = Mathf.Clamp(playerXDiff * 0.3f, -1f, 1f) * moveSpeed;
            horizontalMovement += targetHorizontal * 0.5f;
        }

        // Обновляем положение
        Vector2 newPosition = rb.position + new Vector2(horizontalMovement * Time.fixedDeltaTime * currentSpeedMultiplier, verticalDirection * moveSpeed * Time.fixedDeltaTime * currentSpeedMultiplier);
        rb.MovePosition(newPosition);

        // Отражаем спрайт в зависимости от направления движения
        if (horizontalMovement > 0.1f)
        {
            spriteRenderer.flipX = true; // Движемся вправо - отражаем спрайт
        }
        else if (horizontalMovement < -0.1f)
        {
            spriteRenderer.flipX = false; // Движемся влево - спрайт нормально
        }
    }

    private void CheckDespawnDistance()
    {
        // Проверяем дистанцию за границей экрана
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 screenPos = cam.WorldToViewportPoint(transform.position);
        
        // Если враг далеко за границей экрана
        if (screenPos.y < -despawnDistance / 10f || screenPos.y > 1f + despawnDistance / 10f ||
            screenPos.x < -despawnDistance / 10f || screenPos.x > 1f + despawnDistance / 10f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Взаимодействие с пулей
        if (collision.CompareTag("Bullet") || collision.GetComponent<Bullet>() != null)
        {
            Bullet bulletComponent = collision.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                TakeDamage(bulletComponent.GetDamage());
            }
            else
            {
                TakeDamage(5f); // Урон по умолчанию
            }
            Destroy(collision.gameObject); // Уничтожаем пулю
            return;
        }

        // Взаимодействие с игроком
        if (collision.CompareTag("Player") || collision.GetComponent<PlayerController>() != null)
        {
            if (canDamagePlayer && !hasCollidedWithPlayer)
            {
                hasCollidedWithPlayer = true;
                // Наносим урон игроку
                PlayerController playerCtrl = collision.GetComponent<PlayerController>();
                if (playerCtrl != null)
                {
                    // Здесь можно добавить метод TakeDamage в PlayerController
                    Debug.Log($"Враг нанёс {damageToPlayer} урона игроку!");
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Взаимодействие с пулей через обычный коллайдер
        if (collision.gameObject.CompareTag("Bullet") || collision.gameObject.GetComponent<Bullet>() != null)
        {
            Bullet bulletComponent = collision.gameObject.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                TakeDamage(bulletComponent.GetDamage());
            }
            else
            {
                TakeDamage(5f); // Урон по умолчанию
            }
            Destroy(collision.gameObject); // Уничтожаем пулю
            return;
        }

        // Взаимодействие с игроком
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PlayerController>() != null)
        {
            if (canDamagePlayer && !hasCollidedWithPlayer)
            {
                hasCollidedWithPlayer = true;
                PlayerController playerCtrl = collision.gameObject.GetComponent<PlayerController>();
                if (playerCtrl != null)
                {
                    Debug.Log($"Враг нанёс {damageToPlayer} урона игроку!");
                }
            }
        }
    }

    private void Die()
    {
        Debug.Log("Враг уничтожен!");
        Destroy(gameObject);
    }

    /// <summary>
    /// Нанести урон враку
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"Враг получил {damage} урона. Оставшееся здоровье: {currentHealth}");
        
        // Запускаем визуальный эффект попадания
        StartCoroutine(HitFlashEffect());
        StartCoroutine(HitSlowdownEffect());
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Эффект белого цвета при попадании
    /// </summary>
    private System.Collections.IEnumerator HitFlashEffect()
    {
        if (spriteRenderer != null)
        {
            // Устанавливаем цвет эффекта
            spriteRenderer.color = hitFlashColor;
            
            // Если у материала есть свойство _Color, меняем и его
            if (spriteRenderer.material.HasProperty("_Color"))
            {
                spriteRenderer.material.color = hitFlashColor;
            }
            
            // Ждём указанное время
            yield return new WaitForSeconds(hitFlashDuration);
            
            // Возвращаем оригинальный цвет из Awake
            spriteRenderer.color = originalColor;
            
            if (spriteRenderer.material.HasProperty("_Color"))
            {
                spriteRenderer.material.color = originalColor;
            }
        }
    }

    /// <summary>
    /// Эффект замедления при попадании
    /// </summary>
    private System.Collections.IEnumerator HitSlowdownEffect()
    {
        currentSpeedMultiplier = hitSlowdownAmount;
        yield return new WaitForSeconds(hitSlowdownDuration);
        currentSpeedMultiplier = 1f;
    }

    /// <summary>
    /// Установить максимальное здоровье враку
    /// </summary>
    public void SetMaxHealth(float health)
    {
        maxHealth = Mathf.Max(1f, health);
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Установить направление движения врага
    /// </summary>
    public void SetMovementDirection(bool moveDown)
    {
        moveDownwards = moveDown;
    }

    /// <summary>
    /// Установить игрока как цель
    /// </summary>
    public void SetPlayerTarget(Transform player)
    {
        playerTransform = player;
    }
}

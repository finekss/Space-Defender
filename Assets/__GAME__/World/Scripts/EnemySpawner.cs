using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab(s)")]
    [SerializeField] private GameObject[] enemyPrefabs; // Массив префабов для спавна
    
    [Header("Spawn Settings")]
    [SerializeField] private int enemiesToSpawn = 5; // Количество врагов для спавна
    [SerializeField] private Vector2 spawnZoneMin = new Vector2(-5f, -5f); // Минимальная позиция зоны спавна
    [SerializeField] private Vector2 spawnZoneMax = new Vector2(5f, 5f); // Максимальная позиция зоны спавна
    
    [Header("Trigger Settings")]
    [SerializeField] private Collider2D triggerCollider; // Триггер, при соприкосновении с которым происходит спавн
    [SerializeField] private string triggerTag = "Player"; // Тег объекта для спавна (если triggerCollider не назначен)

    private bool hasSpawned = false; // Флаг, чтобы спавнить только один раз
    private Transform playerTransform;

    private void Start()
    {
        // Ищем игрока
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        // Если триггер не назначен, создаём его сами
        if (triggerCollider == null)
        {
            Collider2D collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = true;
                triggerCollider = boxCollider;
            }
            else
            {
                collider.isTrigger = true;
                triggerCollider = collider;
            }

            // Добавляем Rigidbody2D если его нет
            if (GetComponent<Rigidbody2D>() == null)
            {
                Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.isKinematic = true;
            }
        }

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("EnemySpawner: enemyPrefabs не назначены!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверяем, это ли нужный триггер
        if (!hasSpawned && CheckTrigger(collision))
        {
            hasSpawned = true;
            SpawnEnemies();
        }
    }

    private bool CheckTrigger(Collider2D collision)
    {
        // Если триггер назначен, проверяем совпадение
        if (triggerCollider != null)
        {
            return collision == triggerCollider;
        }

        // Иначе проверяем по тегу
        return collision.CompareTag(triggerTag);
    }

    private void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("EnemySpawner: Нет префабов для спавна!");
            return;
        }

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // Выбираем случайный префаб из массива
            GameObject prefabToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            // Генерируем случайную позицию в зоне спавна
            Vector3 randomPos = new Vector3(
                Random.Range(spawnZoneMin.x, spawnZoneMax.x),
                Random.Range(spawnZoneMin.y, spawnZoneMax.y),
                0f
            );

            // Спавним врага
            GameObject newEnemy = Instantiate(prefabToSpawn, transform.position + randomPos, Quaternion.identity);

            // Устанавливаем цель (игрока) для врага
            if (playerTransform != null)
            {
                var setTargetMethod = newEnemy.GetComponent("Eater")?.GetType().GetMethod("SetPlayerTarget");
                if (setTargetMethod != null)
                {
                    setTargetMethod.Invoke(newEnemy.GetComponent("Eater"), new object[] { playerTransform });
                }
            }
        }

        Debug.Log($"EnemySpawner: Спавнено {enemiesToSpawn} врагов!");
    }

    /// <summary>
    /// Сбросить флаг спавна, чтобы спавнить врагов снова
    /// </summary>
    public void ResetSpawner()
    {
        hasSpawned = false;
    }
}

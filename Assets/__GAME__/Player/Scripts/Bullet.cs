using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector3 startPosition;
    private float maxDistance = 20f;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        // Проверяем пройденное расстояние
        float distanceTraveled = Vector3.Distance(transform.position, startPosition);
        
        if (distanceTraveled >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Установить максимальную дистанцию полёта пули
    /// </summary>
    public void SetMaxDistance(float distance)
    {
        maxDistance = Mathf.Max(0.1f, distance);
    }
}

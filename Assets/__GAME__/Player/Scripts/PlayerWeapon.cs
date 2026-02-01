using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private float fireRate = 0.1f; // Время между выстрелами (в секундах)
    [SerializeField] private Transform shootPoint; // Точка, откуда вылетают снаряды
    [SerializeField] private GameObject bulletPrefab; // Префаб снаряда
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float maxBulletDistance = 20f; // Максимальная дистанция полёта пули
    [SerializeField] private float bulletDamage = 5f; // Урон пули
    
    [Header("Auto Shoot")]
    [SerializeField] private bool autoShootEnabled = true; // Авто стрельба как в Star Soldier

    private float shootCooldown = 0f;

    private void Update()
    {
        // Уменьшаем перезарядку
        if (shootCooldown > 0)
        {
            shootCooldown -= Time.deltaTime;
        }

        // Авто стрельба
        if (autoShootEnabled && shootCooldown <= 0)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("PlayerWeapon: bulletPrefab не назначен!");
            return;
        }

        Vector3 spawnPos = shootPoint != null ? shootPoint.position : transform.position;

        // Создаём снаряд
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        
        // Добавляем компонент для отслеживания дистанции
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent == null)
        {
            bulletComponent = bullet.AddComponent<Bullet>();
        }
        bulletComponent.SetMaxDistance(maxBulletDistance);
        bulletComponent.SetDamage(bulletDamage);

        // Если есть Rigidbody2D, даём ему скорость
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = new Vector2(0, bulletSpeed); // Стреляем вверх
        }

        shootCooldown = fireRate;
    }

    /// <summary>
    /// Включить/выключить автоматическую стрельбу
    /// </summary>
    public void SetAutoShoot(bool enabled)
    {
        autoShootEnabled = enabled;
    }

    /// <summary>
    /// Установить скорость огня (выстрелов в секунду)
    /// </summary>
    public void SetFireRate(float rate)
    {
        fireRate = Mathf.Max(0.01f, rate);
    }

    /// <summary>
    /// Установить урон пули
    /// </summary>
    public void SetBulletDamage(float damage)
    {
        bulletDamage = Mathf.Max(0.1f, damage);
    }
}

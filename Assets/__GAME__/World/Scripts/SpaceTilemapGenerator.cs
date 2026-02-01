using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class SpaceTilemapGenerator : MonoBehaviour
{
    [Header("Tilemap Settings")]
    public Tilemap spaceTilemap; // Основной тайлмап для космоса
    
    [Header("Star Tiles")]
    public Tile[] starTiles; // Массив тайлов звёзд (разные размеры/яркость)
    public Tile emptyTile; // Пустой тайл космоса (можно null)
    
    [Header("Generation Settings")]
    [Range(0f, 1f)]
    public float starDensity = 0.05f; // Плотность звёзд (5% тайлов будут звёздами)
    public int chunkWidth = 16;
    public int chunkHeight = 16;
    public int seed = 12345; // Сид для детерминированной генерации
    
    // Кэш созданных чанков
    private Dictionary<Vector2Int, GameObject> chunkObjects = new Dictionary<Vector2Int, GameObject>();
    
    /// <summary>
    /// Создаёт чанк с процедурно-генерированными звёздами
    /// </summary>
    public GameObject CreateChunk(int chunkX, int chunkY)
    {
        Vector2Int chunkCoord = new Vector2Int(chunkX, chunkY);
        
        // Если чанк уже существует, не создаём
        if (chunkObjects.ContainsKey(chunkCoord))
            return chunkObjects[chunkCoord];

        // Создаём GameObject для чанка
        GameObject chunkObj = new GameObject($"SpaceChunk_{chunkX}_{chunkY}");
        chunkObj.transform.parent = transform;
        chunkObj.transform.localPosition = Vector3.zero;

        // Добавляем компоненты
        Grid grid = chunkObj.AddComponent<Grid>();
        Tilemap chunkTilemap = chunkObj.AddComponent<Tilemap>();
        TilemapRenderer renderer = chunkObj.AddComponent<TilemapRenderer>();
        
        // Копируем настройки рендерера
        if (spaceTilemap != null && spaceTilemap.GetComponent<TilemapRenderer>() != null)
        {
            TilemapRenderer sourceRenderer = spaceTilemap.GetComponent<TilemapRenderer>();
            renderer.sortingLayerID = sourceRenderer.sortingLayerID;
            renderer.sortingOrder = sourceRenderer.sortingOrder;
            renderer.material = sourceRenderer.material;
        }

        // Генерируем звёзды в чанке
        GenerateStarsInChunk(chunkTilemap, chunkX, chunkY);

        chunkObjects[chunkCoord] = chunkObj;
        return chunkObj;
    }

    /// <summary>
    /// Генерирует звёзды в конкретном чанке используя детерминированный Random
    /// </summary>
    private void GenerateStarsInChunk(Tilemap tilemap, int chunkX, int chunkY)
    {
        int minX = chunkX * chunkWidth;
        int minY = chunkY * chunkHeight;
        int maxX = minX + chunkWidth;
        int maxY = minY + chunkHeight;

        // Создаём детерминированный Random для этого чанка
        // Одинаковые координаты чанка всегда дадут одинаковый результат
        int chunkSeed = seed + chunkX * 73856093 + chunkY * 19349663;
        Random.State oldState = Random.state;
        Random.InitState(chunkSeed);

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                
                // Проверяем, нужно ли поставить звезду
                if (Random.value < starDensity)
                {
                    // Выбираем случайный тайл звезды
                    Tile starTile = starTiles[Random.Range(0, starTiles.Length)];
                    tilemap.SetTile(pos, starTile);
                }
                else if (emptyTile != null)
                {
                    // Ставим пустой тайл (или оставляем null)
                    tilemap.SetTile(pos, emptyTile);
                }
            }
        }

        // Восстанавливаем предыдущее состояние Random
        Random.state = oldState;
    }

    /// <summary>
    /// Удаляет чанк из сцены
    /// </summary>
    public void DestroyChunk(int chunkX, int chunkY)
    {
        Vector2Int chunkCoord = new Vector2Int(chunkX, chunkY);
        
        if (chunkObjects.TryGetValue(chunkCoord, out GameObject chunkObj))
        {
            Destroy(chunkObj);
            chunkObjects.Remove(chunkCoord);
        }
    }

    /// <summary>
    /// Проверяет, загружен ли чанк
    /// </summary>
    public bool IsChunkLoaded(int chunkX, int chunkY)
    {
        return chunkObjects.ContainsKey(new Vector2Int(chunkX, chunkY));
    }

    /// <summary>
    /// Очищает все чанки
    /// </summary>
    public void ClearAllChunks()
    {
        foreach (var chunk in chunkObjects.Values)
        {
            if (chunk != null)
                Destroy(chunk);
        }
        chunkObjects.Clear();
    }
}

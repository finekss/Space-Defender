using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    [Header("Terrain Tilemap")]
    [SerializeField] private DualGridTilemap dualGridTilemap;
    
    [Header("Space Background")]
    [SerializeField] private SpaceTilemapGenerator spaceTilemapGenerator;
    
    [Header("Chunk Settings")]
    [SerializeField] private float chunkHeight = 16f; // Должен соответствовать chunkHeight в DualGridTilemap
    [SerializeField] private int chunksToLoadAboveScreen = 3; // Сколько чанков загружать выше
    [SerializeField] private int chunksToLoadBelowScreen = 2; // Сколько чанков загружать ниже
    [SerializeField] private int minChunkX = -5; // Минимальный X чанка (для покрытия всей ширины карты)
    [SerializeField] private int maxChunkX = 5; // Максимальный X чанка
    [SerializeField] private bool debugMode = true;

    private Camera cam;
    private HashSet<Vector2Int> loadedChunks = new HashSet<Vector2Int>(); // Кэш загруженных чанков (X и Y)
    private int currentMinChunkY = 0;
    private int currentMaxChunkY = 0;

    private void Start()
    {
        if (dualGridTilemap == null)
            dualGridTilemap = GetComponent<DualGridTilemap>();

        if (spaceTilemapGenerator == null)
            spaceTilemapGenerator = GetComponent<SpaceTilemapGenerator>();

        if (dualGridTilemap == null && spaceTilemapGenerator == null)
        {
            Debug.LogError("MapManager: Ни DualGridTilemap, ни SpaceTilemapGenerator не найдены!");
            return;
        }

        cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("MapManager: Main Camera не найдена!");
            return;
        }

        // Загружаем начальные чанки
        UpdateLoadedChunks();
    }

    private void Update()
    {
        UpdateLoadedChunks();
    }

    void UpdateLoadedChunks()
    {
        if ((dualGridTilemap == null && spaceTilemapGenerator == null) || cam == null) return;

        // Получаем позицию тайлмапа (используем DualGrid если есть, иначе SpaceGenerator)
        float tilemapY = 0;
        if (dualGridTilemap != null && dualGridTilemap.displayTilemap != null)
            tilemapY = dualGridTilemap.displayTilemap.transform.position.y;
        else if (spaceTilemapGenerator != null)
            tilemapY = spaceTilemapGenerator.transform.position.y;
        
        // Получаем границы видимости камеры в мировых координатах
        Vector3 camTop = cam.ViewportToWorldPoint(new Vector3(0.5f, 1, cam.nearClipPlane));
        Vector3 camBottom = cam.ViewportToWorldPoint(new Vector3(0.5f, 0, cam.nearClipPlane));
        
        // Преобразуем границы экрана в координаты тайлмапа (вычитаем позицию тайлмапа)
        float screenBottomInTilemapCoords = camBottom.y - tilemapY;
        float screenTopInTilemapCoords = camTop.y - tilemapY;

        // Вычисляем какие чанки видны на экране
        int minChunkY = Mathf.FloorToInt(screenBottomInTilemapCoords / chunkHeight) - chunksToLoadBelowScreen;
        int maxChunkY = Mathf.CeilToInt(screenTopInTilemapCoords / chunkHeight) + chunksToLoadAboveScreen;

        if (debugMode)
            Debug.Log($"Tilemap Y: {tilemapY:F2} | Screen world: {camBottom.y:F2} - {camTop.y:F2} | " +
                     $"Screen tilemap: {screenBottomInTilemapCoords:F2} - {screenTopInTilemapCoords:F2} | " +
                     $"Chunks: {minChunkY} - {maxChunkY} | Loaded: {loadedChunks.Count}");

        // Если диапазон изменился, обновляем чанки
        if (minChunkY != currentMinChunkY || maxChunkY != currentMaxChunkY)
        {
            if (debugMode)
                Debug.Log($"★ Chunk range changed! Old: {currentMinChunkY}-{currentMaxChunkY} New: {minChunkY}-{maxChunkY}");
            
            UnloadChunks(minChunkY, maxChunkY);
            LoadChunks(minChunkY, maxChunkY);

            currentMinChunkY = minChunkY;
            currentMaxChunkY = maxChunkY;
        }
    }

    void LoadChunks(int minChunkY, int maxChunkY)
    {
        for (int chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
        {
            for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
            {
                Vector2Int chunkCoord = new Vector2Int(chunkX, chunkY);
                
                if (!loadedChunks.Contains(chunkCoord))
                {
                    if (debugMode)
                        Debug.Log($"★ Loading chunk ({chunkX}, {chunkY})");

                    // Загружаем terrain чанк если есть DualGridTilemap
                    if (dualGridTilemap != null)
                        dualGridTilemap.CreateChunk(chunkX, chunkY);
                    
                    // Загружаем space чанк если есть SpaceTilemapGenerator
                    if (spaceTilemapGenerator != null)
                        spaceTilemapGenerator.CreateChunk(chunkX, chunkY);

                    loadedChunks.Add(chunkCoord);
                }
            }
        }
    }

    void UnloadChunks(int minChunkY, int maxChunkY)
    {
        // Удаляем чанки которые больше не видны
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();

        foreach (Vector2Int chunkCoord in loadedChunks)
        {
            if (chunkCoord.y < minChunkY || chunkCoord.y > maxChunkY)
            {
                chunksToRemove.Add(chunkCoord);
            }
        }

        foreach (Vector2Int chunkCoord in chunksToRemove)
        {
            if (debugMode)
                Debug.Log($"★ Unloading and destroying chunk ({chunkCoord.x}, {chunkCoord.y})");

            // Удаляем terrain чанк
            if (dualGridTilemap != null)
                dualGridTilemap.DestroyChunk(chunkCoord.x, chunkCoord.y);
            
            // Удаляем space чанк
            if (spaceTilemapGenerator != null)
                spaceTilemapGenerator.DestroyChunk(chunkCoord.x, chunkCoord.y);

            loadedChunks.Remove(chunkCoord);
        }
    }
}

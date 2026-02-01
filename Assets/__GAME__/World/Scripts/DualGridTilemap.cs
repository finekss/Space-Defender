using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TileType;

public class DualGridTilemap : MonoBehaviour {
    protected static Vector3Int[] NEIGHBOURS = new Vector3Int[] {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0)
    };

    protected static Dictionary<Tuple<TileType, TileType, TileType, TileType>, Tile> neighbourTupleToTile;

    // Provide references to each tilemap in the inspector
    public Tilemap placeholderTilemap;
    public Tilemap displayTilemap;

    // Provide the dirt and grass placeholder tiles in the inspector
    public Tile grassPlaceholderTile;
    public Tile dirtPlaceholderTile;

    // Provide the 16 tiles in the inspector
    public Tile[] tiles;

    // Chunk settings
    public int chunkWidth = 16;
    public int chunkHeight = 16;

    // Кэш плейсхолдер тайлов для быстрого доступа
    private Dictionary<Vector3Int, TileType> placeholderCache = new Dictionary<Vector3Int, TileType>();
    
    // Чанки (key = Vector2Int(chunkX, chunkY), value = GameObject чанка)
    private Dictionary<Vector2Int, GameObject> chunkObjects = new Dictionary<Vector2Int, GameObject>();

    void Start() {
        // This dictionary stores the "rules", each 4-neighbour configuration corresponds to a tile
        // |_1_|_2_|
        // |_3_|_4_|
        neighbourTupleToTile = new() {
            {new (Grass, Grass, Grass, Grass), tiles[6]},
            {new (Dirt, Dirt, Dirt, Grass), tiles[13]}, // OUTER_BOTTOM_RIGHT
            {new (Dirt, Dirt, Grass, Dirt), tiles[0]}, // OUTER_BOTTOM_LEFT
            {new (Dirt, Grass, Dirt, Dirt), tiles[8]}, // OUTER_TOP_RIGHT
            {new (Grass, Dirt, Dirt, Dirt), tiles[15]}, // OUTER_TOP_LEFT
            {new (Dirt, Grass, Dirt, Grass), tiles[1]}, // EDGE_RIGHT
            {new (Grass, Dirt, Grass, Dirt), tiles[11]}, // EDGE_LEFT
            {new (Dirt, Dirt, Grass, Grass), tiles[3]}, // EDGE_BOTTOM
            {new (Grass, Grass, Dirt, Dirt), tiles[9]}, // EDGE_TOP
            {new (Dirt, Grass, Grass, Grass), tiles[5]}, // INNER_BOTTOM_RIGHT
            {new (Grass, Dirt, Grass, Grass), tiles[2]}, // INNER_BOTTOM_LEFT
            {new (Grass, Grass, Dirt, Grass), tiles[10]}, // INNER_TOP_RIGHT
            {new (Grass, Grass, Grass, Dirt), tiles[7]}, // INNER_TOP_LEFT
            {new (Dirt, Grass, Grass, Dirt), tiles[14]}, // DUAL_UP_RIGHT
            {new (Grass, Dirt, Dirt, Grass), tiles[4]}, // DUAL_DOWN_RIGHT
            {new (Dirt, Dirt, Dirt, Dirt), tiles[12]},
        };
        
        // Не вызываем RefreshDisplayTilemap, теперь чанки создаются по требованию
    }

    public void SetCell(Vector3Int coords, Tile tile) {
        placeholderTilemap.SetTile(coords, tile);
        setDisplayTile(coords);
    }

    private TileType getPlaceholderTileTypeAt(Vector3Int coords) {
        // Проверяем кэш
        if (placeholderCache.TryGetValue(coords, out TileType cachedType))
            return cachedType;

        TileType type;
        TileBase tile = placeholderTilemap.GetTile(coords);
        
        if (tile == null)
            type = None; // Пустая ячейка
        else if (tile == grassPlaceholderTile)
            type = Grass;
        else if (tile == dirtPlaceholderTile)
            type = Dirt;
        else
            type = None; // Неизвестный тайл

        placeholderCache[coords] = type;
        return type;
    }

    protected Tile calculateDisplayTile(Vector3Int coords) {
        // 4 neighbours
        TileType topRight = getPlaceholderTileTypeAt(coords - NEIGHBOURS[0]);
        TileType topLeft = getPlaceholderTileTypeAt(coords - NEIGHBOURS[1]);
        TileType botRight = getPlaceholderTileTypeAt(coords - NEIGHBOURS[2]);
        TileType botLeft = getPlaceholderTileTypeAt(coords - NEIGHBOURS[3]);

        // Если все соседи пустые - не рисуем ничего
        if (topRight == None && topLeft == None && botRight == None && botLeft == None)
            return null;

        // Для расчёта переходов заменяем None на Dirt (земля по умолчанию)
        if (topRight == None) topRight = Dirt;
        if (topLeft == None) topLeft = Dirt;
        if (botRight == None) botRight = Dirt;
        if (botLeft == None) botLeft = Dirt;

        Tuple<TileType, TileType, TileType, TileType> neighbourTuple = new(topLeft, topRight, botLeft, botRight);

        // Если такой комбинации нет в словаре, не рисуем ничего
        if (!neighbourTupleToTile.ContainsKey(neighbourTuple))
            return null;

        return neighbourTupleToTile[neighbourTuple];
    }

    protected void setDisplayTile(Vector3Int pos) {
        for (int i = 0; i < NEIGHBOURS.Length; i++) {
            Vector3Int newPos = pos + NEIGHBOURS[i];
            displayTilemap.SetTile(newPos, calculateDisplayTile(newPos));
        }
    }

    // The tiles on the display tilemap will recalculate themselves based on the placeholder tilemap
    public void RefreshDisplayTilemap() {
        for (int i = -50; i < 50; i++) {
            for (int j = -50; j < 50; j++) {
                setDisplayTile(new Vector3Int(i, j, 0));
            }
        }
    }

    /// <summary>
    /// Создаёт чанк (отдельный GameObject с Tilemap) и заполняет его тайлами
    /// </summary>
    public GameObject CreateChunk(int chunkX, int chunkY) {
        Vector2Int chunkCoord = new Vector2Int(chunkX, chunkY);
        
        // Если чанк уже существует, не создаём
        if (chunkObjects.ContainsKey(chunkCoord))
            return chunkObjects[chunkCoord];

        // Проверяем есть ли вообще тайлы в placeholder в этой области
        int minX = chunkX * chunkWidth;
        int minY = chunkY * chunkHeight;
        int maxX = minX + chunkWidth;
        int maxY = minY + chunkHeight;
        
        bool hasAnyTiles = false;
        for (int x = minX; x < maxX && !hasAnyTiles; x++) {
            for (int y = minY; y < maxY; y++) {
                if (placeholderTilemap.HasTile(new Vector3Int(x, y, 0))) {
                    hasAnyTiles = true;
                    break;
                }
            }
        }
        
        // Если в placeholder нет тайлов в этой области - не создаём чанк
        if (!hasAnyTiles) {
            return null;
        }

        // Создаём GameObject для чанка
        GameObject chunkObj = new GameObject($"Chunk_{chunkX}_{chunkY}");
        chunkObj.transform.parent = transform;
        chunkObj.transform.localPosition = Vector3.zero;

        // Добавляем компоненты
        Grid grid = chunkObj.AddComponent<Grid>();
        Tilemap chunkTilemap = chunkObj.AddComponent<Tilemap>();
        TilemapRenderer renderer = chunkObj.AddComponent<TilemapRenderer>();
        
        // Копируем настройки рендерера
        if (displayTilemap != null && displayTilemap.GetComponent<TilemapRenderer>() != null)
        {
            TilemapRenderer sourceRenderer = displayTilemap.GetComponent<TilemapRenderer>();
            renderer.sortingLayerID = sourceRenderer.sortingLayerID;
            renderer.sortingOrder = sourceRenderer.sortingOrder;
            renderer.material = sourceRenderer.material;
        }

        // Заполняем чанк тайлами
        for (int x = minX; x < maxX; x++) {
            for (int y = minY; y < maxY; y++) {
                Vector3Int pos = new Vector3Int(x, y, 0);
                Tile tile = calculateDisplayTile(pos);
                if (tile != null) {
                    chunkTilemap.SetTile(pos, tile);
                }
            }
        }

        chunkObjects[chunkCoord] = chunkObj;
        return chunkObj;
    }

    /// <summary>
    /// Удаляет чанк из сцены
    /// </summary>
    public void DestroyChunk(int chunkX, int chunkY) {
        Vector2Int chunkCoord = new Vector2Int(chunkX, chunkY);
        
        if (chunkObjects.TryGetValue(chunkCoord, out GameObject chunkObj))
        {
            Destroy(chunkObj);
            chunkObjects.Remove(chunkCoord);
        }
    }

    /// <summary>
    /// Обновляет только тайлы в указанном чанке для оптимизации (устаревший метод)
    /// </summary>
    public void RefreshChunk(int chunkX, int chunkY) {
        // Теперь создаём чанк если его нет
        CreateChunk(chunkX, chunkY);
    }

    /// <summary>
    /// Сбрасывает кэш плейсхолдер тайлов (используй если меняешь тайлмап в рантайме)
    /// </summary>
    public void ClearCache() {
        placeholderCache.Clear();
    }
}

public enum TileType {
    None,
    Grass,
    Dirt
}

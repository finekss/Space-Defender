using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class TilemapChunkCreator
{
    [MenuItem("Tools/Tilemap/Split Into Chunks")]
    static void SplitTilemap()
    {
        Tilemap source = Selection.activeGameObject?.GetComponent<Tilemap>();

        if (source == null)
        {
            Debug.LogError("Выбери объект с Tilemap!");
            return;
        }

        int chunkWidth = 16;
        int chunkHeight = 16;

        var bounds = source.cellBounds;

        for (int cx = bounds.xMin; cx < bounds.xMax; cx += chunkWidth)
        {
            for (int cy = bounds.yMin; cy < bounds.yMax; cy += chunkHeight)
            {
                GameObject chunkObj = new GameObject($"Chunk_{cx}_{cy}");
                chunkObj.transform.position = source.transform.position;

                Grid grid = chunkObj.AddComponent<Grid>();
                Tilemap tilemap = chunkObj.AddComponent<Tilemap>();
                chunkObj.AddComponent<TilemapRenderer>();

                for (int x = 0; x < chunkWidth; x++)
                {
                    for (int y = 0; y < chunkHeight; y++)
                    {
                        Vector3Int pos =
                            new Vector3Int(cx + x, cy + y, 0);

                        TileBase tile = source.GetTile(pos);

                        if (tile != null)
                            tilemap.SetTile(pos, tile);
                    }
                }
            }
        }

        Debug.Log("Tilemap разделена на чанки.");
    }
}

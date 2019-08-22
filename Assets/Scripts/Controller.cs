using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kultie.ProcedualDungeon;
using System.Threading;
using System;
using SimpleJSON;

public class Controller : MonoBehaviour
{
    Dungeon dungeon;
    public GameObject cellsContainer;
    public SpriteRenderer mapCellPrefab;
    public float cellSize;
    public int dungeonWidth = 200;
    public int dungeonHeight = 200;
    DungeonCell[,] map;
    public Sprite[] sprites;
    int fillSpace;

    public TextAsset textAsset;

    JSONNode lookupTable;
    //{ 2 = 1, 8 = 2, 10 = 3, 11 = 4, 16 = 5, 18 = 6, 22 = 7, 24 = 8, 26 = 9, 27 = 10, 30 = 11, 31 = 12, 64 = 13, 66 = 14, 72 = 15, 74 = 16, 75 = 17, 80 = 18, 82 = 19, 86 = 20, 88 = 21, 90 = 22, 91 = 23, 94 = 24, 95 = 25, 104 = 26, 106 = 27, 107 = 28, 120 = 29, 122 = 30, 123 = 31, 126 = 32, 127 = 33, 208 = 34, 210 = 35, 214 = 36, 216 = 37, 218 = 38, 219 = 39, 222 = 40, 223 = 41, 248 = 42, 250 = 43, 251 = 44, 254 = 45, 255 = 46, 0 = 47 }

    private void Start()
    {
        ObjectPool.CreatePool(mapCellPrefab, 1000);
        lookupTable = JSON.Parse(textAsset.ToString());
    }
    // Use this for initialization
    void CreateMap()
    {

        fillSpace = 0;
        StartCoroutine(GenerateMap(() =>
        {
            map = dungeon.GetDungeonGrid();
            //DrawMap();
            DrawMap();
        }));
    }

    IEnumerator GenerateMap(Action callback)
    {
        bool done = false;
        Thread thread = new Thread(() =>
        {
            dungeon = new Dungeon(dungeonWidth, dungeonHeight);
            while (!done)
            {
                done = dungeon.CreateMap();
            }
        });
        thread.Start();
        while (!done)
        {
            yield return null;
        }
        callback();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CreateMap();
        }
    }

    void DrawMap()
    {
        ObjectPool.RecycleAll(mapCellPrefab);
        for (int i = 0; i < dungeonWidth; i++)
        {
            for (int j = 0; j < dungeonHeight; j++)
            {
                SpriteRenderer cell = ObjectPool.Spawn(mapCellPrefab, cellsContainer.transform);
                DrawMapCell(cell, new Vector2(i, j), map[i, j]);
            }
        }
    }

    void DrawMapCell(SpriteRenderer mapCell, Vector2 position, DungeonCell data)
    {
        Transform cellTransform = mapCell.transform;
        mapCell.sprite = null;
        cellTransform.localScale = Vector3.one;
        cellTransform.localPosition = position - new Vector2(dungeonWidth / 2, dungeonHeight / 2);
        Color mapCellColor = Color.white;
        if (data.cellType == DungeonCellType.PATH)
        {
            //mapCell.sprite = sprites[lookupTable[data.spriteValue.ToString()].AsInt];
            mapCell.sprite = sprites[data.spriteValue];
        }
        else if (data.cellType == DungeonCellType.WALL)
        {
            mapCellColor = new Color(0, 1 * data.pathValue, 1, 1);
        }
        mapCell.color = mapCellColor;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kultie.ProcedualDungeon;
using System.Threading;
using System;

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

    private void Start()
    {
        ObjectPool.CreatePool(mapCellPrefab, 1000);
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
        cellTransform.localScale = Vector3.one;
        cellTransform.localPosition = position - new Vector2(dungeonWidth / 2, dungeonHeight / 2);
        Color mapCellColor = Color.white;
        if (data.cellType == DungeonCellType.PATH)
        {
            mapCellColor = new Color(0, 1 * data.pathValue, 0, 1);
        }
        else if (data.cellType == DungeonCellType.WALL)
        {
            mapCell.sprite = sprites[data.spriteValue];
        }
        mapCell.color = mapCellColor;
    }
}

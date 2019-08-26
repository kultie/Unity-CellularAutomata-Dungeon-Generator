using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kultie.AutoTileSystem;
using Kultie.ProcedualDungeon;
using System.Threading;
using System;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    Dungeon dungeon;
    public GameObject cellsContainer;
    public MapTile mapCellPrefab;
    public float cellSize;
    public int dungeonWidth = 200;
    public int dungeonHeight = 200;
    DungeonCell[,] map;
    int fillSpace;
    public Sprite[] sprites;
    public Camera mainCam;
    float camWidth;
    float camHeight;
    Vector2 leftTopTile;
    Vector2 rightBottomTile;

    Vector3 mapCenter;

    private void Awake()
    {
        camHeight = 2f * mainCam.orthographicSize;
        camWidth = camHeight * mainCam.aspect;
        ObjectPool.CreatePool(mapCellPrefab, 20);
    }
    private void Start()
    {

    }
    // Use this for initialization
    void CreateMap()
    {
        mapCenter = new Vector3(dungeonWidth / 2, dungeonHeight / 2, 0);
        StartCoroutine(GenerateMap(() =>
        {
            map = dungeon.GetDungeonGrid();
            //DrawMapFull();
        }));
    }

    IEnumerator GenerateMap(Action callback)
    {
        bool done = false;
        Thread thread = new Thread(() =>
        {
            dungeon = new Dungeon(dungeonWidth, dungeonHeight);
            dungeon.recreateMapCount = 0;
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
        Debug.Log(dungeon.recreateMapCount);
        callback();
    }
    Vector3 oldCamPos;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CreateMap();
        }
        if (map != null)
        {
            Vector3 camPos = ClamCamPos(mainCam.transform.position);
            mainCam.transform.position = camPos;
            //Vector3 camPos = mainCam.transform.position;
            leftTopTile = PosToTile(camPos + mapCenter, -camWidth / 2 - 1, camHeight / 2 + 1);
            rightBottomTile = PosToTile(camPos + mapCenter, camWidth / 2 + 1, -camHeight / 2 - 1);
            if(Vector3.SqrMagnitude(oldCamPos - camPos) > 1){
                DrawMap();
                oldCamPos = camPos;
            }           
        }
    }

    Vector3 ClamCamPos(Vector3 camPos)
    {
        Vector3 currentCamPos = camPos;
        float minCamPosX = Mathf.Round(-mapCenter.x + camWidth / 2);
        float maxCamPosX = Mathf.Round(mapCenter.x - camWidth / 2);

        float minCamPosY = Mathf.Round(-mapCenter.y + camHeight / 2);
        float maxCamPosY = Mathf.Round(mapCenter.y - camHeight / 2);


        float x = Mathf.Clamp(camPos.x, minCamPosX - 1/mainCam.aspect, maxCamPosX - 1/mainCam.aspect);
        float y = Mathf.Clamp(camPos.y, minCamPosY - 1/mainCam.aspect, maxCamPosY - 1/mainCam.aspect);
        return new Vector3(x, y, -10);
    }

    Vector2 PosToTile(Vector2 pos, float offSetX, float offSetY)
    {
        float x = pos.x;
        float y = pos.y;

        x = Mathf.RoundToInt(pos.x + offSetX);
        y = Mathf.RoundToInt(pos.y + offSetY);

        x = Math.Max(0, x);
        y = Math.Min(dungeonHeight - 1, y);
        x = Math.Min(dungeonWidth - 1, x);
        y = Math.Max(0, y);

        return new Vector2(x, y);
    }

    DungeonCell GetTile(Vector2 pos)
    {
        return map[(int)pos.x, (int)pos.y];
    }

    void DrawMap()
    {
        ObjectPool.RecycleAll(mapCellPrefab);
        for (int i = (int)leftTopTile.x; i <= (int)rightBottomTile.x; i++)
        {
            for (int j = (int)rightBottomTile.y; j <= (int)leftTopTile.y; j++)
            {
                MapTile cell = ObjectPool.Spawn(mapCellPrefab, cellsContainer.transform);
                DrawMapCell(cell, new Vector2(i, j), map[i, j]);
            }
        }
    }

    void DrawMapFull()
    {
        ObjectPool.RecycleAll(mapCellPrefab);
        for (int i = 0; i < dungeonWidth; i++)
        {
            for (int j = 0; j < dungeonHeight; j++)
            {
                MapTile cell = ObjectPool.Spawn(mapCellPrefab, cellsContainer.transform);
                DrawMapCell(cell, new Vector2(i, j), map[i, j]);
            }
        }
    }

    void DrawMapCell(MapTile mapCell, Vector3 position, DungeonCell data)
    {
        Transform cellTransform = mapCell.transform;
        cellTransform.localScale = Vector3.one;
        cellTransform.localPosition = position - mapCenter;
        Color mapCellColor = Color.white;
        mapCell.Reset();
        if (data.cellType == DungeonCellType.PATH)
        {
            //mapCell.sprite = sprites[lookupTable[data.spriteValue.ToString()].AsInt];
            //mapCell.sprite = sprites[];

            //mapCellColor = new Color(0, 1 * data.pathValue, 1, 1);

            //mapCell.SetSprite(sprites[EightBitAutoTile.GetTileIndex(data.spriteValue)]);
        }
        else if (data.cellType == DungeonCellType.WALL)
        {
            int[] tiles = VXAutoTile.GetTile(data.spriteValue);
            for (int i = 0; i < 4; i++)
            {
                mapCell.SetSprite(i, sprites[tiles[i]]);
            }
            //mapCellColor = new Color(0, 1 * data.wallValue, 0, 1);
        }
        mapCell.SetColor(mapCellColor);
    }
}

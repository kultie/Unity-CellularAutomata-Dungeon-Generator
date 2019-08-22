using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kultie.ProcedualDungeon;
using System.Threading;
using System;

public class Controller : MonoBehaviour {
    Dungeon dungeon;
    public GameObject cellsContainer;
    public SpriteRenderer mapCellPrefab;
    public float cellSize;
    public int dungeonWidth = 200;
    public int dungeonHeight = 200;
    DungeonCell[,] map;

    int fillSpace;

    Dictionary<string, SpriteRenderer> cacheImg = new Dictionary<string, SpriteRenderer>();
    // Use this for initialization
    void CreateMap () {
       
        fillSpace = 0;
        StartCoroutine(GenerateMap(()=>{
            map = dungeon.GetDungeonGrid();
            DrawMap();
            FloodFill(map, dungeonWidth / 2, dungeonHeight / 2, Color.white);
            ClearAllLeftOver();
            DrawMap();
            AddTerrain();
            float filledRate = fillSpace * 1f / (dungeonWidth * dungeonHeight);
            if (filledRate < 0.4f)
            {
                CreateMap();
            }
        }));
    }

    IEnumerator GenerateMap(Action callback){
        bool done = false;
        Thread thread = new Thread(() =>
        {
            dungeon = new Dungeon(dungeonWidth, dungeonHeight);
            done = true;
        });
        thread.Start();
        while(!done){
            yield return null;
        }
        Debug.Log("Finish generate Map");
        callback();
    }

	private void Update()
	{
        if(Input.GetKeyDown(KeyCode.Space)){
            CreateMap();
        }
        else if(Input.GetKeyDown(KeyCode.Z)){
            PlaceTresure();
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            PlaceRock();
        }
	}

	void DrawMap(){
        for (int i = 0; i < dungeonWidth; i++)
        {
            for (int j = 0; j < dungeonHeight; j++)
            {
                string key = i.ToString() + "-" + j.ToString();
                SpriteRenderer cell = null;
                if(!cacheImg.TryGetValue(key, out cell)){
                    cell = Instantiate(mapCellPrefab, cellsContainer.transform);
                    cacheImg[key] = cell;
                }
                DrawMapCell(cell, new Vector2(i, j), map[i, j].cellType == DungeonCellType.PATH);
            }
        }
    }
	
    void DrawMapCell(SpriteRenderer mapCell, Vector2 position, bool value){
        Transform cellTransform = mapCell.transform;
        cellTransform.localScale = Vector3.one;
        cellTransform.localPosition = position - new Vector2(dungeonWidth/ 2,dungeonHeight/ 2);
        mapCell.color = value ? Color.blue : Color.grey;
    }

    void PlaceTresure(){
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                if(i == 0 || j == 0 || i == map.GetLength(0) - 1 || j == map.GetLength(1) - 1){
                    if (map[i, j].cellType == DungeonCellType.PATH)
                    {
                        int nbs = dungeon.CountAliveNeightbours(map, i, j);
                        if (nbs >= 3)
                        {
                            string key = i.ToString() + "-" + j.ToString();
                            cacheImg[key].color = Color.yellow;
                        }
                    }
                }
            }
        }
    }

    void PlaceRock()
    {
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                if (map[i, j].cellType == DungeonCellType.WALL)
                {
                    int nbs = dungeon.CountAliveNeightbours(map, i, j);
                    if (nbs >= 1 && nbs <= 8)
                    {
                        string key = i.ToString() + "-" + j.ToString();
                        cacheImg[key].color = Color.red;
                    }
                }
            }
        }
    }

    void AddTerrain()
    {
        Vector2 offSet = Vector2.one * UnityEngine.Random.Range(-100f, 100f);
        for (int i = 0; i < dungeonWidth; i++)
        {
            for (int j = 0; j < dungeonHeight; j++)
            {
                string key = i + "-" + j;
                if (cacheImg[key].color == Color.white || cacheImg[key].color == Color.blue)
                {
                    float data = GenerateTerrain(i, j, dungeonWidth, dungeonHeight, 2, offSet);
                    cacheImg[key].color = new Color(0.5f, 1 * data, 0.5f, 1f);
                }
                if (cacheImg[key].color == Color.grey)
                {
                    float data = GenerateTerrain(i, j, dungeonWidth, dungeonHeight, 2, offSet);
                    cacheImg[key].color = new Color(1, 1 * data, 1, 1f);
                }
            }
        }
    }

    float GenerateTerrain(int x, int y, int mapWidth, int mapHeight, float scale, Vector2 offSet)
    {
        float xCoord = (float)x / mapWidth * scale;
        float yCoord = (float)y / mapHeight * scale;
        return Mathf.PerlinNoise(xCoord + offSet.x, yCoord + offSet.y);
    }

    void FloodFillUtil(DungeonCell[,] _map, int x, int y, Color prevC, Color newC)
    {      
        if (x < 0 || x >= _map.GetLength(0) || y < 0 || y >= _map.GetLength(1))
            return;

        string key = x.ToString() + "-" + y.ToString();
        var currentNode = cacheImg[key];

        if (currentNode.color != prevC)
            return;

        currentNode.color = newC;
        fillSpace++;
        FloodFillUtil(_map, x + 1, y, prevC, newC);
        FloodFillUtil(_map, x - 1, y, prevC, newC);
        FloodFillUtil(_map, x, y + 1, prevC, newC);
        FloodFillUtil(_map, x, y - 1, prevC, newC);
    }

    void FloodFill(DungeonCell[,] _map, int x, int y, Color newC)
    {
        string key = x.ToString() + "-" + y.ToString();
        var prevC = cacheImg[key].color;
        FloodFillUtil(_map, x, y, prevC, newC);
    }

    void ClearAllLeftOver(){
        for (int i = 0; i < dungeonWidth; i++)
        {
            for (int j = 0; j < dungeonHeight; j++)
            {
                string key = i + "-" + j;
                if(map[i,j].cellType == DungeonCellType.PATH && cacheImg[key].color == Color.blue){
                    map[i, j].SetCellType(DungeonCellType.WALL);
                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kultie.ProcedualDungeon;
public class Controller : MonoBehaviour {
    Dungeon dungeon;
    public GameObject cellsContainer;
    public Image mapCellPrefab;
    public float cellSize;
    public int dungeonWidth = 200;
    public int dungeonHeight = 200;
    DungeonCell[,] map;

    int fillSpace;

    Dictionary<string, Image> cacheImg = new Dictionary<string, Image>();
    Dictionary<Image, RectTransform> cacheTransform = new Dictionary<Image, RectTransform>();
    // Use this for initialization
    void CreateMap () {
       
        fillSpace = 0;
        dungeon = new Dungeon(dungeonWidth, dungeonHeight);       
        map = dungeon.GetDungeonGrid();
        DrawMap();
        FloodFill(map, dungeonWidth / 2, dungeonHeight / 2, Color.white);
        ////ClearAllLeftOver();
        //AddTerrain();
        //float filledRate = fillSpace * 1f / (dungeonWidth * dungeonHeight);
        //if(filledRate < 0.4f){
        //    CreateMap();
        //}
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
                Image cell = null;
                if(!cacheImg.TryGetValue(key, out cell)){
                    cell = Instantiate(mapCellPrefab, cellsContainer.transform);
                    cacheImg[key] = cell;
                }
                cacheTransform[cell] = cell.GetComponent<RectTransform>();
                DrawMapCell(cell, new Vector2(i, j), map[i, j].cellType == DungeonCellType.PATH);
            }
        }
    }
	
    void DrawMapCell(Image mapCell, Vector2 position, bool value){
        RectTransform transform = cacheTransform[mapCell];
        transform.localScale = Vector3.one;
        transform.sizeDelta = Vector2.one * cellSize;
        transform.localPosition = position * cellSize - new Vector2(dungeonWidth * cellSize / 2,dungeonHeight * cellSize / 2);
        mapCell.GetComponent<Image>().color = value ? Color.blue : Color.grey;
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
        Vector2 offSet = Vector2.one * Random.Range(-100f, 100f);
        for (int i = 0; i < dungeonWidth; i++)
        {
            for (int j = 0; j < dungeonHeight; j++)
            {
                string key = i + "-" + j;
                if (cacheImg[key].color == Color.white || cacheImg[key].color == Color.blue)
                {
                    float data = GenerateTerrain(i, j, dungeonWidth, dungeonHeight, cellSize, offSet);
                    cacheImg[key].color = new Color(1f, 1 * data, 0.5f, 1f);
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
                    cacheImg[key].color = Color.black;
                }
            }
        }
    }
}

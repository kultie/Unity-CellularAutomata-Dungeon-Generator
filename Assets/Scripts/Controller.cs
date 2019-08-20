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

    bool[,] map;

    int fillSpace;

    Dictionary<string, Image> cacheImg = new Dictionary<string, Image>();
    Dictionary<Image, RectTransform> cacheTransform = new Dictionary<Image, RectTransform>();
    // Use this for initialization
    void CreateMap () {
       
        fillSpace = 0;
        dungeon = new Dungeon(dungeonWidth, dungeonHeight);
        map = dungeon.GetDungeonGrid();
        for (int i = 0; i < 12; i++)
        {
            map = dungeon.SimulationStep(map);
        }
        DrawMap();
        FloodFill(map, dungeonWidth / 2, dungeonHeight / 2, Color.white);
        ClearAllLeftOver();
        float filledRate = fillSpace * 1f / (dungeonWidth * dungeonHeight);

        if(filledRate < 0.4f){
            CreateMap();
        }
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
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                string key = i.ToString() + "-" + j.ToString();
                Image cell = null;
                if(!cacheImg.TryGetValue(key, out cell)){
                    cell = Instantiate(mapCellPrefab, cellsContainer.transform);
                    cacheImg[key] = cell;
                }
                cacheTransform[cell] = cell.GetComponent<RectTransform>();
                DrawMapCell(cell, new Vector2(i, j), map[i, j]);
            }
        }
    }
	
    void DrawMapCell(Image mapCell, Vector2 position, bool value){
        RectTransform transform = cacheTransform[mapCell];
        transform.localScale = Vector3.one;
        transform.sizeDelta = Vector2.one * cellSize;
        transform.localPosition = position * cellSize - new Vector2(dungeonWidth * cellSize / 2,dungeonHeight * cellSize / 2);
        mapCell.GetComponent<Image>().color = value ? Color.blue : Color.black;
    }

    void PlaceTresure(){
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                if(!map[i,j]){
                    int nbs = dungeon.CountAliveNeightbours(map, i, j);
                    if(nbs >= 5 && nbs < 8){
                        if(Random.Range(0,1f) < 0.25f){
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
                if (!map[i, j])
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

    void FloodFillUtil(bool[,] _map, int x, int y, Color prevC, Color newC)
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

    void FloodFill(bool[,] _map, int x, int y, Color newC)
    {
        string key = x.ToString() + "-" + y.ToString();
        var prevC = cacheImg[key].color;
        FloodFillUtil(_map, x, y, prevC, newC);
    }

    void ClearAllLeftOver(){
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                string key = i + "-" + j;
                if(map[i,j] && cacheImg[key].color == Color.blue){
                    map[i, j] = false;
                    cacheImg[key].color = Color.black;
                }
            }
        }
    }
}

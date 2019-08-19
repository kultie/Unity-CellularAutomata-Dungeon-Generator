using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kultie.ProcedualDungeon;
public class Controller : MonoBehaviour {
    Dungeon dungeon;
    public GameObject cellsContainer;
    public RectTransform mapCellPrefab;
    public float cellSize;


    bool[,] map;
    private int dungeonWidth = 75;
    private int dungeonHeight = 75;

    Dictionary<string, RectTransform> cacheObj = new Dictionary<string, RectTransform>();
	// Use this for initialization
	void CreateMap () {
        dungeon = new Dungeon(dungeonWidth, dungeonHeight);
        map = dungeon.GetDungeonGrid();
        for (int i = 0; i < 6; i++)
        {
            map = dungeon.SimulationStep(map);
        }
        DrawMap();
	}

	private void Update()
	{
        if(Input.GetKeyDown(KeyCode.Space)){
            CreateMap();
        }
        else if(Input.GetKeyDown(KeyCode.Z)){
            PlaceTresure();
        }
	}



	void DrawMap(){
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                string key = i.ToString() + "-" + j.ToString();
                RectTransform cell = null;
                if(!cacheObj.TryGetValue(key, out cell)){
                    cell = Instantiate(mapCellPrefab, cellsContainer.transform);
                    cacheObj[key] = cell;
                }
                DrawMapCell(cell, new Vector2(i, j), map[i, j]);
            }
        }
    }
	
    void DrawMapCell(RectTransform mapCell, Vector2 position, bool value){
        mapCell.localScale = Vector3.one;
        mapCell.sizeDelta = Vector2.one * cellSize;
        mapCell.transform.localPosition = position * cellSize - new Vector2(dungeonWidth * cellSize / 2,dungeonHeight * cellSize / 2);
        mapCell.GetComponent<Image>().color = value ? Color.white : Color.black;
    }

    void PlaceTresure(){
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                if(!map[i,j]){
                    int nbs = dungeon.CountAliveNeightbours(map, i, j);
                    if(nbs >= 6){
                        if(Random.Range(0f,1f) >= 0.75f){
                            string key = i.ToString() + "-" + j.ToString();
                            cacheObj[key].GetComponent<Image>().color = Color.yellow;
                        }
                    }
                }
            }
        }
    }
}

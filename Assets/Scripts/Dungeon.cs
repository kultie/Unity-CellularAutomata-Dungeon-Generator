using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kultie.ProcedualDungeon
{
    public enum DungeonCellType { PATH, WALL };
    public class Dungeon
    {
        private DungeonCell[,] dungeonGrid;
        private float chanceToStartAlive = 0.45f;

        private int overpopLimit = 8;

        private int starvationLimit = 5;

        private int birthLimit = 4;

        public Dungeon(int width, int height)
        {
            dungeonGrid = InitialiseMap(width, height);
            for (int i = 0; i < 6; i++)
            {
                dungeonGrid = SimulationStep(dungeonGrid);
            }
        }

        public void CreateMap(){
            
        }

        DungeonCell[,] InitialiseMap(int width, int height)
        {
            DungeonCell[,] dungeon = new DungeonCell[width, height];
            for (int i = 0; i < dungeon.GetLength(0); i++)
            {
                for (int j = 0; j < dungeon.GetLength(1); j++)
                {
                    float randomValue = Random.Range(0f, 1f);
                    dungeon[i, j] = new DungeonCell(randomValue < chanceToStartAlive ? DungeonCellType.PATH : DungeonCellType.WALL);

                }
            }
            return dungeon;
        }

        public DungeonCell[,] GetDungeonGrid()
        {
            return dungeonGrid;
        }

        public DungeonCell[,] SimulationStep(DungeonCell[,] oldDungeon)
        {
            DungeonCell[,] newDungeon = (DungeonCell[,])oldDungeon.Clone();
            for (int i = 0; i < oldDungeon.GetLength(0); i++)
            {
                for (int j = 0; j < oldDungeon.GetLength(1); j++)
                {
                    int neightboursCount = CountAliveNeightbours(oldDungeon, i, j);
                    if(oldDungeon[i,j].cellType == DungeonCellType.PATH){
                        newDungeon[i, j].SetCellType(neightboursCount >= overpopLimit && neightboursCount <= starvationLimit ? DungeonCellType.PATH : DungeonCellType.WALL);
                    }
                    else if (oldDungeon[i, j].cellType == DungeonCellType.WALL){
                        newDungeon[i, j].SetCellType(neightboursCount <= birthLimit ? DungeonCellType.PATH : DungeonCellType.WALL);
                    }
                }
            }
            dungeonGrid = newDungeon;
            return newDungeon;
        }

        public int CountAliveNeightbours(DungeonCell[,] dungeon, int x, int y)
        {
            int count = 0;
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    int neightbour_x = x + i;
                    int neightbour_y = y + j;
                    if (i == 0 && j == 0)
                    {

                    }
                    else if (neightbour_x < 0 || neightbour_y < 0 || neightbour_x >= dungeon.GetLength(0) || neightbour_y >= dungeon.GetLength(1))
                    {
                        
                    }
                    else if(dungeon[neightbour_x,neightbour_y].cellType == DungeonCellType.PATH){
                        count = count + 1;
                    }
                }
            }
            return count;
        }
    }

    public class DungeonCell{
        DungeonCellType _cellType;
        public DungeonCellType cellType{
            get{
                return _cellType;
            }
        }

        public DungeonCell(DungeonCellType type){
            _cellType = type;
        }

        public void SetCellType(DungeonCellType type){
            _cellType = type;
        }
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kultie.ProcedualDungeon
{
    public class Dungeon
    {
        private bool[,] dungeonGrid;
        private float chanceToStartAlive = 0.45f;

        private int overpopLimit = 8;

        private int starvationLimit = 5;

        private int birthLimit = 4;

        public Dungeon(int width, int height)
        {
            dungeonGrid = InitialiseMap(width, height);
        }

        public void CreateMap(){
            
        }

        bool[,] InitialiseMap(int width, int height)
        {
            bool[,] dungeon = new bool[width, height];
            for (int i = 0; i < dungeon.GetLength(0); i++)
            {
                for (int j = 0; j < dungeon.GetLength(1); j++)
                {
                    float randomValue = Random.Range(0f, 1f);
                    dungeon[i, j] = randomValue < chanceToStartAlive;
                }
            }
            return dungeon;
        }

        public void PrintMapValue()
        {
            for (int i = 0; i < dungeonGrid.GetLength(0); i++)
            {
                for (int j = 0; j < dungeonGrid.GetLength(1); j++)
                {
                    Debug.Log("x: " + i + " y: " + j + " value: " + dungeonGrid[i, j]);
                }
            }
        }

        public bool[,] GetDungeonGrid()
        {
            return dungeonGrid;
        }

        public bool[,] SimulationStep(bool[,] oldDungeon)
        {
            bool[,] newDungeon = new bool[oldDungeon.GetLength(0), oldDungeon.GetLength(1)];
            for (int i = 0; i < oldDungeon.GetLength(0); i++)
            {
                for (int j = 0; j < oldDungeon.GetLength(1); j++)
                {
                    int neightboursCount = CountAliveNeightbours(oldDungeon, i, j);
                    if(oldDungeon[i,j]){
                        newDungeon[i, j] = neightboursCount >= overpopLimit && neightboursCount <= starvationLimit;
                    }
                    else{
                        newDungeon[i, j] = neightboursCount <= birthLimit;
                    }
                }
            }
            dungeonGrid = newDungeon;
            return newDungeon;
        }

        public int CountAliveNeightbours(bool[,] dungeon, int x, int y)
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
                    else if(dungeon[neightbour_x,neightbour_y]){
                        count = count + 1;
                    }
                }
            }
            return count;
        }
    }
}


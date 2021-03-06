﻿using System;
using System.Collections.Generic;
using UnityEngine;
namespace Kultie.ProcedualDungeon
{
    public enum DungeonCellType { WALL, PATH };
    public enum DungeonCellFillType { FILLED, NON };
    public class Dungeon
    {
        const int threadGroupSize = 1024;

        bool useComputeShader;

        private ComputeShader shader;
        private DungeonCell[,] dungeonGrid;
        private int chanceToStartAlive = 45;

        private int overpopLimit = 8;

        private int starvationLimit = 5;

        private int birthLimit = 4;
        int fillSpace = 0;

        int mapWidth;
        int mapHeight;

        System.Random rnd;

        public Dungeon(int width, int height, ComputeShader shader, bool useComputeShader)
        {
            mapWidth = width;
            mapHeight = height;
            rnd = new System.Random();
            this.shader = shader;
            this.useComputeShader = useComputeShader;

        }
        public bool CreateMap()
        {
            fillSpace = 0;
            dungeonGrid = InitialiseMap(mapWidth, mapHeight);
            for (int i = 0; i < 12; i++)
            {
                if (useComputeShader)
                {
                    CalculateCell();
                }
                else
                {
                    dungeonGrid = SimulationStep(dungeonGrid);
                }


            }
            if (!useComputeShader)
            {
                FloodFill(dungeonGrid, mapWidth / 2, mapHeight / 2, DungeonCellFillType.FILLED);
                float filledRate = fillSpace * 1f / (mapWidth * mapHeight);

                if (filledRate < 0.4f)
                {
                    return false;
                }
                ClearAllLeftOver();
            }
            AddTerrain();
            return true;
        }

        void ClearAllLeftOver()
        {
            for (int i = 0; i < mapWidth; i++)
            {
                for (int j = 0; j < mapHeight; j++)
                {
                    string key = i + "-" + j;
                    if (dungeonGrid[i, j].cellType == DungeonCellType.PATH && dungeonGrid[i, j].fillType == DungeonCellFillType.NON)
                    {
                        dungeonGrid[i, j].SetCellType(DungeonCellType.WALL);
                    }
                }
            }
        }

        DungeonCell[,] InitialiseMap(int width, int height)
        {
            DungeonCell[,] dungeon = new DungeonCell[width, height];
            for (int i = 0; i < dungeon.GetLength(0); i++)
            {
                for (int j = 0; j < dungeon.GetLength(1); j++)
                {
                    float randomValue = rnd.Next(0, 100);
                    dungeon[i, j] = new DungeonCell(randomValue < chanceToStartAlive ? DungeonCellType.PATH : DungeonCellType.WALL);

                }
            }
            return dungeon;
        }

        public DungeonCell[,] GetDungeonGrid()
        {
            return dungeonGrid;
        }

        void CalculateCell()
        {

            int numCells = mapWidth * mapHeight;

            var oldCellData = new ComputeCelldata[numCells];
            for (int i = 0; i < numCells; i++)
            {
                int x = i % mapHeight;
                int y = Mathf.FloorToInt(i / mapHeight);
                oldCellData[i].dungeonCellType = (int)dungeonGrid[x, y].cellType;
                oldCellData[i].dungeonCellFillType = (int)dungeonGrid[x, y].cellType;
            }

            var newCellData = new ComputeCelldata[numCells];

            var oldCellBuffer = new ComputeBuffer(numCells, sizeof(int) * 2);
            var newCellBuffer = new ComputeBuffer(numCells, sizeof(int) * 2);

            oldCellBuffer.SetData(oldCellData);
            newCellBuffer.SetData(newCellData);

            shader.SetBuffer(0, "oldCells", oldCellBuffer);
            shader.SetBuffer(0, "newCells", newCellBuffer);
            shader.SetInt("numCells", mapWidth * mapHeight);
            shader.SetInt("horizontalBound", mapWidth - 1);
            shader.SetInt("verticleBound", mapHeight - 1);
            shader.SetInt("overpopLimit", overpopLimit);
            shader.SetInt("starvationLimit", starvationLimit);
            shader.SetInt("birthLimit", birthLimit);

            int threadGroups = Mathf.CeilToInt(numCells / (float)threadGroupSize);
            shader.Dispatch(0, threadGroups, 1, 1);
            newCellBuffer.GetData(newCellData);

            for (int i = 0; i < numCells; i++)
            {
                int x = i % mapHeight;
                int y = Mathf.FloorToInt(i / mapHeight);
                dungeonGrid[x, y].SetCellType((DungeonCellType)newCellData[i].dungeonCellType);
                dungeonGrid[x, y].SetFillType((DungeonCellFillType)newCellData[i].dungeonCellFillType);
            }

            oldCellBuffer.Dispose();
            newCellBuffer.Dispose();
        }

        public DungeonCell[,] SimulationStep(DungeonCell[,] oldDungeon)
        {
            DungeonCell[,] newDungeon = new DungeonCell[oldDungeon.GetLength(0), oldDungeon.GetLength(1)];
            for (int i = 0; i < oldDungeon.GetLength(0); i++)
            {
                for (int j = 0; j < oldDungeon.GetLength(1); j++)
                {
                    int neightboursCount = CountAliveNeightbours(oldDungeon, i, j);
                    if (oldDungeon[i, j].cellType == DungeonCellType.PATH)
                    {
                        newDungeon[i, j] = new DungeonCell(neightboursCount >= overpopLimit && neightboursCount <= starvationLimit ? DungeonCellType.PATH : DungeonCellType.WALL);
                    }
                    else if (oldDungeon[i, j].cellType == DungeonCellType.WALL)
                    {
                        newDungeon[i, j] = new DungeonCell(neightboursCount <= birthLimit ? DungeonCellType.PATH : DungeonCellType.WALL);
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
                    else if (dungeon[neightbour_x, neightbour_y].cellType == DungeonCellType.PATH)
                    {
                        count = count + 1;
                    }
                }
            }
            return count;
        }

        void FloodFillUtil(DungeonCell[,] _map, int x, int y, DungeonCellFillType prevC, DungeonCellFillType newC)
        {
            if (x < 0 || x >= _map.GetLength(0) || y < 0 || y >= _map.GetLength(1))
                return;

            var currentNode = _map[x, y];

            if (currentNode.fillType != prevC || currentNode.cellType == DungeonCellType.WALL)
                return;

            currentNode.SetFillType(newC);
            fillSpace++;
            FloodFillUtil(_map, x + 1, y, prevC, newC);
            FloodFillUtil(_map, x - 1, y, prevC, newC);
            FloodFillUtil(_map, x, y + 1, prevC, newC);
            FloodFillUtil(_map, x, y - 1, prevC, newC);
        }

        void FloodFill(DungeonCell[,] _map, int x, int y, DungeonCellFillType newC)
        {
            var prevC = _map[x, y].fillType;
            FloodFillUtil(_map, x, y, prevC, newC);
        }

        void AddTerrain()
        {
            int offSet = rnd.Next(-100, 100);
            for (int i = 0; i < mapWidth; i++)
            {
                for (int j = 0; j < mapHeight; j++)
                {
                    if (dungeonGrid[i, j].cellType == DungeonCellType.PATH)
                    {
                        float data = GenerateTerrain(i, j, mapWidth, mapHeight, 2, offSet);
                        dungeonGrid[i, j].pathValue = data;
                    }
                    if (dungeonGrid[i, j].cellType == DungeonCellType.WALL)
                    {
                        CalculateCellSpriteValue(i, j, DungeonCellType.WALL);
                        float data = GenerateTerrain(i, j, mapWidth, mapHeight, 2, offSet);
                        dungeonGrid[i, j].wallValue = data;
                    }
                }
            }
        }

        float GenerateTerrain(int x, int y, int width, int height, float scale, int offSet)
        {
            float xCoord = (float)x / width * scale;
            float yCoord = (float)y / height * scale;
            float value = UnityEngine.Mathf.PerlinNoise(xCoord + offSet, yCoord + offSet);
            return value;
            //if (value <= 0.25) return 0;
            //else if (value <= 0.75) return 0.5f;
            //return 1;
        }

        public void CalculateCellSpriteValue(int x, int y, DungeonCellType compareCellType)
        {
            int value = HorizontalAndVerticleSpriteValue(x, y, compareCellType) + DiagonalSpriteValue(x, y, compareCellType);
            //int value = HorizontalAndVerticleSpriteValue4Bit(x, y, compareCellType);
            dungeonGrid[x, y].spriteValue = value;
        }

        int HorizontalAndVerticleSpriteValue4Bit(int x, int y, DungeonCellType compareCellType)
        {
            bool upCell;
            bool leftCell;
            bool rightCell;
            bool downCell;

            if (y == mapHeight - 1)
            {
                upCell = false;
            }
            else
            {
                upCell = dungeonGrid[x, y + 1].cellType == compareCellType;
            }

            if (x == 0)
            {
                leftCell = false;
            }
            else
            {
                leftCell = dungeonGrid[x - 1, y].cellType == compareCellType;
            }

            if (x == mapWidth - 1)
            {
                rightCell = false;
            }
            else
            {
                rightCell = dungeonGrid[x + 1, y].cellType == compareCellType;
            }

            if (y == 0)
            {
                downCell = false;
            }
            else
            {
                downCell = dungeonGrid[x, y - 1].cellType == compareCellType;
            }

            int value = 1 * ConvertBoolToInt(upCell) + 2 * ConvertBoolToInt(leftCell) + 4 * ConvertBoolToInt(rightCell) + 8 * ConvertBoolToInt(downCell);
            return value;
        }

        int HorizontalAndVerticleSpriteValue(int x, int y, DungeonCellType compareCellType)
        {
            bool upCell;
            bool leftCell;
            bool rightCell;
            bool downCell;

            if (y == mapHeight - 1)
            {
                upCell = true;
            }
            else
            {
                upCell = dungeonGrid[x, y + 1].cellType == compareCellType;
            }

            if (x == 0)
            {
                leftCell = true;
            }
            else
            {
                leftCell = dungeonGrid[x - 1, y].cellType == compareCellType;
            }

            if (x == mapWidth - 1)
            {
                rightCell = true;
            }
            else
            {
                rightCell = dungeonGrid[x + 1, y].cellType == compareCellType;
            }

            if (y == 0)
            {
                downCell = true;
            }
            else
            {
                downCell = dungeonGrid[x, y - 1].cellType == compareCellType;
            }
            int value = 4 * ConvertBoolToInt(upCell) + 1 * ConvertBoolToInt(leftCell) + 16 * ConvertBoolToInt(rightCell) + 64 * ConvertBoolToInt(downCell);
            return value;
        }

        int DiagonalSpriteValue(int x, int y, DungeonCellType compareCellType)
        {
            bool nwCell = true;
            bool neCell = true;
            bool swCell = true;
            bool seCell = true;

            if (y == mapHeight - 1)
            {
                nwCell = true;
                neCell = true;
            }
            else
            {
                if (x == 0)
                {
                    neCell = true;
                }
                else if (x == mapWidth - 1)
                {
                    nwCell = true;
                }
                else
                {
                    nwCell = dungeonGrid[x - 1, y + 1].cellType == compareCellType;
                    neCell = dungeonGrid[x + 1, y + 1].cellType == compareCellType;
                }
            }

            if (y == 0)
            {
                swCell = true;
                seCell = true;
            }
            else
            {
                if (x == 0)
                {
                    seCell = true;
                }
                else if (x == mapWidth - 1)
                {
                    swCell = true;
                }
                else
                {
                    swCell = dungeonGrid[x - 1, y - 1].cellType == compareCellType;
                    seCell = dungeonGrid[x + 1, y - 1].cellType == compareCellType;
                }
            }

            int value = 2 * ConvertBoolToInt(nwCell) + 8 * ConvertBoolToInt(neCell) + 128 * ConvertBoolToInt(swCell) + 32 * ConvertBoolToInt(seCell);
            return value;
        }



        int ConvertBoolToInt(bool value)
        {
            return value ? 1 : 0;
        }
    }

    public class DungeonCell
    {
        DungeonCellType _cellType;
        DungeonCellFillType _fillType;

        public float pathValue = 1f;
        public float wallValue = 1f;

        public int dungeonCellType;
        public int dungeonCellFillType;

        public int spriteValue;

        public DungeonCellType cellType
        {
            get
            {
                return _cellType;
            }
        }

        public DungeonCellFillType fillType
        {
            get
            {
                return _fillType;
            }
        }

        public DungeonCell(DungeonCellType type)
        {
            _cellType = type;
            _fillType = DungeonCellFillType.NON;
        }

        public void SetCellType(DungeonCellType type)
        {
            _cellType = type;
        }

        public void SetFillType(DungeonCellFillType type)
        {
            _fillType = type;
        }
    }

    public struct ComputeCelldata
    {
        public int dungeonCellType;
        public int dungeonCellFillType;
    }
}


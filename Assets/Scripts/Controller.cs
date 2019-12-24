using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kultie.AutoTileSystem;
using Kultie.ProcedualDungeon;
using System.Threading;
using System;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;

public class Controller : MonoBehaviour
{
    Dungeon dungeon;
    public Transform mapContainer;
    public int mapWidth = 200;
    public int mapHeight = 200;

    public Texture wallTexture;
    public Texture pathTexture;
    public ComputeShader shader;

    public bool useComputeShader;

    DungeonCell[,] map;

    Vector3 mapCenter;

    

    private void Awake()
    {

    }
    private void Start()
    {
        CreateMap();
    }
    // Use this for initialization
    void CreateMap()
    {
        mapCenter = new Vector3(mapWidth / 2, mapHeight / 2, 0);
        StartCoroutine(GenerateMap(() =>
        {
            map = dungeon.GetDungeonGrid();

            CreateMapMesh("Wall", wallTexture, CreateMesh(wallTexture, 24, (DungeonCell cell) => {
                return cell.cellType == DungeonCellType.WALL;
            }, (DungeonCell cell) => {
                return VXAutoTile.GetTile(cell.spriteValue);
            }));

            CreateMapMesh("Path", pathTexture, CreateMesh(pathTexture, 24, (DungeonCell cell) => {
                return cell.cellType == DungeonCellType.PATH;
            }, (DungeonCell cell) => {
                return VXAutoTile.GetTile(0);
            }));
        }));
    }
    IEnumerator GenerateMap(Action callback) {
        if (useComputeShader)
        {
            dungeon = new Dungeon(mapWidth, mapHeight, shader, useComputeShader);
            dungeon.CreateMap();
            callback();
        }
        else {
            bool done = false;
            Thread thread = new Thread(() =>
            {
                dungeon = new Dungeon(mapWidth, mapHeight, shader, useComputeShader);
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

    }

    Mesh CreateMesh(Texture texture, int tileSize, CheckMapTile condition, GetMapTile func)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Vector4[] uvs = GenerateUV(texture, tileSize);

        int mapSize = mapHeight * mapWidth;

        Vector3[] verticies = new Vector3[4 * 4 * mapSize];

        Vector2[] uv = new Vector2[4 * 4 * mapSize];
        List<int> triangles = new List<int>();

        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                DungeonCell data = map[i, j];
                if (condition(data))
                {
                    GenerateMeshData(i, j, func(data), verticies, uv, triangles, uvs);
                }
            }
        }
        mesh.vertices = verticies;
        mesh.uv = uv;
        mesh.triangles = triangles.ToArray();
        MeshUtility.Optimize(mesh);
        return mesh;
    }

    void GenerateMeshData(int i, int j, int[] tiles, Vector3[] verticies, Vector2[] uv, List<int> triangles, Vector4[] uvs)
    {
        int index = i * mapHeight + j;
        Vector3 offSet = new Vector3(-0.5f, -1, 0);
        for (int k = 0; k < 4; k++)
        {
            Vector4 tileUV = uvs[tiles[k]];

            float rootX = i + 0.5f * (k % 2);
            float rootY = j + 1 - 0.5f * (k / 2);

            verticies[index * 16 + k * 4 + 0] = new Vector3(rootX, rootY, 0) - mapCenter + offSet;
            verticies[index * 16 + k * 4 + 1] = new Vector3(rootX, rootY + 0.5f, 0) - mapCenter + offSet;
            verticies[index * 16 + k * 4 + 2] = new Vector3(rootX + 0.5f, rootY + 0.5f, 0) - mapCenter + offSet;
            verticies[index * 16 + k * 4 + 3] = new Vector3(rootX + 0.5f, rootY, 0) - mapCenter + offSet;

            uv[index * 16 + k * 4 + 0] = new Vector2(tileUV.x, tileUV.y);
            uv[index * 16 + k * 4 + 1] = new Vector2(tileUV.x, tileUV.w);
            uv[index * 16 + k * 4 + 2] = new Vector2(tileUV.z, tileUV.w);
            uv[index * 16 + k * 4 + 3] = new Vector2(tileUV.z, tileUV.y);


            triangles.Add(index * 16 + k * 4 + 0);
            triangles.Add(index * 16 + k * 4 + 1);
            triangles.Add(index * 16 + k * 4 + 2);

            triangles.Add(index * 16 + k * 4 + 0);
            triangles.Add(index * 16 + k * 4 + 2);
            triangles.Add(index * 16 + k * 4 + 3);
        }
    }

    public Vector4[] GenerateUV(Texture texture, float tileSize)
    {
        List<Vector4> uvs = new List<Vector4>();
        float col = texture.width / tileSize;
        float row = texture.height / tileSize;
        float width = tileSize / texture.width;
        float height = tileSize / texture.height;

        float u0 = 0;
        float v0 = 1 - height;

        float u1 = width;
        float v1 = 1;

        for (int j = 0; j < row; j++)
        {
            for (int i = 0; i < col; i++)
            {
                uvs.Add(new Vector4(u0, v0, u1, v1));
                u0 += width;
                u1 += width;
            }
            u0 = 0;
            v0 -= height;
            u1 = width;
            v1 -= height;
        }

        return uvs.ToArray();
    }

    void CreateMapMesh(string gameObjectName, Texture texture, Mesh mesh) {
        GameObject mapMesh = new GameObject(gameObjectName);
        mapMesh.transform.parent = mapContainer;
        MeshFilter meshFilter = mapMesh.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = mapMesh.AddComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("Unlit/Transparent"));
        material.mainTexture = texture;
        meshRenderer.material = material;
        meshFilter.mesh = mesh;
    }

    int ConvertVector3ToID(Vector3 value) {
        return (int)(value.y * mapHeight + value.x);
    }
}

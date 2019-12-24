using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Kultie.Test
{
    public class TestComputeShader : MonoBehaviour
    {

        const int threadGroupSize = 256;

        public int mapSize;
        public Transform mapContainer;
        public ComputeShader shader;
        public Texture texture;

        DungeonCell[] cells;

        Vector3 mapCenter;
        // Use this for initialization
        void Start()
        {
            mapCenter = new Vector3(mapSize / 2, mapSize / 2, 0);
            cells = new DungeonCell[mapSize * mapSize];
            for (int i = 0; i < mapSize * mapSize; i++)
            {
                float value = Random.Range(0, 100);
                cells[i] = new DungeonCell
                {
                    dungeonCellType = value < 45 ? 1 : 0,
                    dungeonCellFillType = 0
                };
            }
            for (int i = 0; i < 12; i++)
            {
                CalculateCell();                
            }
            CreateMapMesh(texture, CreateMesh());
        }

        void CalculateCell()
        {
            int numCells = mapSize * mapSize;

            var oldCellData = new DungeonCell[numCells];
            for (int i = 0; i < numCells; i++)
            {
                oldCellData[i].dungeonCellType = cells[i].dungeonCellType;
                oldCellData[i].dungeonCellFillType = cells[i].dungeonCellFillType;
            }

            var newCellData = new DungeonCell[numCells];

            var oldCellBuffer = new ComputeBuffer(numCells, sizeof(int) * 2);
            var newCellBuffer = new ComputeBuffer(numCells, sizeof(int) * 2);

            oldCellBuffer.SetData(oldCellData);
            newCellBuffer.SetData(newCellData);

            shader.SetBuffer(0, "oldCells", oldCellBuffer);
            shader.SetBuffer(0, "newCells", newCellBuffer);
            shader.SetInt("numCells", cells.Length);
            shader.SetInt("boundLimit", mapSize - 1);

            int threadGroups = Mathf.CeilToInt(numCells / (float)threadGroupSize);
            shader.Dispatch(0, threadGroups, 8, 1);
            newCellBuffer.GetData(newCellData);

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].dungeonCellType = newCellData[i].dungeonCellType;
                cells[i].dungeonCellFillType = newCellData[i].dungeonCellFillType;
            }

            oldCellBuffer.Dispose();
            newCellBuffer.Dispose();
        }

        void CreateMapMesh(Texture texture, Mesh mesh)
        {
            GameObject mapMesh = new GameObject("Map");
            mapMesh.transform.parent = mapContainer;
            MeshFilter meshFilter = mapMesh.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = mapMesh.AddComponent<MeshRenderer>();
            Material material = new Material(Shader.Find("Unlit/Transparent"));
            material.mainTexture = texture;
            meshRenderer.material = material;
            meshFilter.mesh = mesh;
        }

        Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            Vector3[] verticies = new Vector3[4 * mapSize * mapSize];

            Vector2[] uv = new Vector2[4 * mapSize * mapSize];
            List<int> triangles = new List<int>();

            for (int i = 0; i < mapSize * mapSize; i++)
            {
                GenerateMeshData(i, cells[i].dungeonCellType, verticies, uv, triangles);
            }

            mesh.vertices = verticies;
            mesh.uv = uv;
            mesh.triangles = triangles.ToArray();
            MeshUtility.Optimize(mesh);
            return mesh;
        }

        void GenerateMeshData(int index, int cellValue, Vector3[] verticies, Vector2[] uv, List<int> triangles)
        {
            int x = index % mapSize;
            int y = Mathf.FloorToInt(index / mapSize);
            Vector4 uvs = GetUvs(cellValue);


            verticies[index * 4 + 0] = new Vector3(x, y, 0) - mapCenter;
            verticies[index * 4 + 1] = new Vector3(x, y + 1, 0) - mapCenter;
            verticies[index * 4 + 2] = new Vector3(x + 1, y + 1, 0) - mapCenter;
            verticies[index * 4 + 3] = new Vector3(x + 1, y, 0) - mapCenter;

            uv[index * 4 + 0] = new Vector2(uvs.x, uvs.y);
            uv[index * 4 + 1] = new Vector2(uvs.x, uvs.w);
            uv[index * 4 + 2] = new Vector2(uvs.z, uvs.w);
            uv[index * 4 + 3] = new Vector2(uvs.z, uvs.y);


            triangles.Add(index * 4 + 0);
            triangles.Add(index * 4 + 1);
            triangles.Add(index * 4 + 2);

            triangles.Add(index * 4 + 0);
            triangles.Add(index * 4 + 2);
            triangles.Add(index * 4 + 3);
        }

        Vector4 GetUvs(int value)
        {
            if (value == 1)
            {
                return new Vector4(0.5f, 0, 1, 1);
            }
            else
            {
                return new Vector4(0, 0, 0.5f, 1);
            }
        }
    }

    public struct DungeonCell
    {
        public int dungeonCellType;
        public int dungeonCellFillType;
    }
}

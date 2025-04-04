using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;

public class MarchingCubes : MonoBehaviour
{
    [SerializeField] private float isoLevel = 0.2f;
    [SerializeField] private int gridSize = 100;
    [SerializeField] private float SizeOfCubes = 0.1f;    
    private MeshFilter meshFilter;
    private float[,,] scalarField;
    private int gridShift;

    [SerializeField] private GameObject cameraRig;
    [SerializeField] private GameObject rotator;
    [SerializeField] private GameObject uiScript;

    private int currentShape = 0;
    private ChangeColorOnClick rotatorClass;

    private Matrix4x4 objectRotation;
    private MovePlayer movePlayer;
    private float wPos;

    private List<Vector3> verticesList;
    private List<int> trianglesList;
    private List<Vector3> normalsList;

    private Matrix4x4 rotationMatrix = Matrix4x4.identity;

    private float function1(float x, float y, float z, float w){
        return x*x + y*y + z*z + w*w - 1;
    }
    private float function2(float x, float y, float z, float w){        
        return Math.Max(x*x + y*y, z*z + w*w) - 1;
    }
    private float function3(float x, float y, float z, float w){
        return Math.Max(x*x, y*y + z*z + w*w) - 1;
    }
    private float function4(float x, float y, float z, float w){
        return Math.Max( Math.Max(x*x, y*y), Math.Max(z*z, w*w)) - 1;
    }
    private float function5(float x, float y, float z, float w){
        return x*x - y*y + z*z - w*w;
    }
    private float function6(float x, float y, float z, float w){
        return 8*(-x*x-y*y*w+z*z);
    }
    private float function7(float x, float y, float z, float w){
        return (x*x + y*y + z*z)*(x*x + y*y + z*z) - 4*(x*y*z*w) - 0.1f;
    }

    private Dictionary<Vector3, List<Vector3>> normalMap; // For smooth normals
    private bool arewerecalculating;

    private void Start()
    {
        objectRotation = Matrix4x4.identity;
        rotatorClass = rotator.GetComponent<ChangeColorOnClick>();
        movePlayer = cameraRig.GetComponent<MovePlayer>();
        
        normalMap = new Dictionary<Vector3, List<Vector3>>();

        gridShift = gridSize / 2;
        scalarField = new float[gridSize + 1, gridSize + 1, gridSize + 1];

        UpdateScalarField();
        verticesList = new List<Vector3>();
        trianglesList = new List<int>();
        normalsList = new List<Vector3>();

        meshFilter = gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
        Material mat = gameObject.GetComponent<Renderer>().material;
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        GenerateMesh();
    }

    private void Update() 
    {
        currentShape = uiScript.GetComponent<UIScript>().getCurrentShape();
        objectRotation = rotatorClass.getMatrix(); 
        UpdateScalarField();
        GenerateMesh();
    }  

    private void ProcessOneCube(int x, int y, int z){
        float[] cubeCorners = new float[8];
        Vector3[] cubePositions = new Vector3[8];

        for (int i = 0; i < 8; i++)
        {
            int xi = x + (i & 1);
            int yi = y + ((i >> 1) & 1);
            int zi = z + ((i >> 2) & 1);
            cubePositions[i] = new Vector3(xi - gridShift, yi - gridShift, zi - gridShift) * SizeOfCubes;
            cubeCorners[i] = scalarField[xi, yi, zi];
        }

        int cubeIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (cubeCorners[i] < isoLevel)
                cubeIndex |= (1 << i);
        }

        if (cubeIndex >= triTable.Length || triTable[cubeIndex].Length == 0)
            return;

        Vector3[] edgeVertices = new Vector3[12];
        //Vector3[] normalsExtra = new Vector3[12];
        for (int i = 0; i < 12; i++)
        {
            int v1 = edgeTable[i, 0];
            int v2 = edgeTable[i, 1];
            float t = 1f;
            Vector3 p1 = cubePositions[v1];
            Vector3 p2 = cubePositions[v2];
            float f1 = cubeCorners[v1];
            float f2 = cubeCorners[v2];

            if(Math.Abs(f2 - f1) > 0.001f){
                t = (isoLevel - f1) / (f2 - f1);
            }
            edgeVertices[i] = Vector3.Lerp(cubePositions[v1], cubePositions[v2], t);
            //normalsExtra[i] = ((p2 - p1).normalized * Mathf.Abs(f2 - f1));
        }

        foreach (int index in triTable[cubeIndex]) 
        {
            trianglesList.Add(verticesList.Count);
            verticesList.Add(edgeVertices[index]);
        }

        for (int i = 0; i < triTable[cubeIndex].Length; i += 3)
        {
            Vector3 v1 = edgeVertices[triTable[cubeIndex][i]];
            Vector3 v2 = edgeVertices[triTable[cubeIndex][i + 1]];
            Vector3 v3 = edgeVertices[triTable[cubeIndex][i + 2]];

            Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;

            if (!normalMap.ContainsKey(v1)) normalMap[v1] = new List<Vector3>();
            if (!normalMap.ContainsKey(v2)) normalMap[v2] = new List<Vector3>();
            if (!normalMap.ContainsKey(v3)) normalMap[v3] = new List<Vector3>();

            normalMap[v1].Add(normal);
            normalMap[v2].Add(normal);
            normalMap[v3].Add(normal);
        }
    }

    void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        verticesList.Clear();
        trianglesList.Clear();
        normalMap.Clear();

        //List<int> triangles = new List<int>();

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    ProcessOneCube(x, y, z);
                }
            }
        }

        Vector3[] normals = new Vector3[verticesList.Count];
        for (int i = 0; i < verticesList.Count; i++)
        {
            Vector3 vertex = verticesList[i];
            if (normalMap.ContainsKey(vertex))
            {
                Vector3 smoothedNormal = Vector3.zero;
                foreach (Vector3 normal in normalMap[vertex])
                {
                    smoothedNormal += normal;
                }
                normals[i] = smoothedNormal.normalized;
            }
        }

        mesh.vertices = verticesList.ToArray();
        mesh.triangles = trianglesList.ToArray();
        mesh.normals = normals;
        if (OVRInput.Get(OVRInput.Button.PrimaryThumbstick)) // Detect button press
        {
            arewerecalculating = !arewerecalculating;
        }
        if (arewerecalculating){
            mesh.RecalculateNormals();
        }
        //mesh.Optimize();
        //mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    private void UpdateScalarField(){
        wPos = movePlayer.wPos;

        for(int x = 0; x <= gridSize; x++) {
            for(int y = 0; y <= gridSize; y++) {
                for(int z=0; z <= gridSize; z++){

                    List<Func<float, float, float, float, float>> methodList = new List<Func<float, float, float, float, float>>
                    {
                        function1,
                        function2,
                        function3,
                        function4,
                        function5,
                        function6,
                        function7,
                    };

                    Vector4 gridPoint = new Vector4( (float) (x - gridShift)*SizeOfCubes, 
                        (float) (y - gridShift)*SizeOfCubes, 
                        (float) (z - gridShift)*SizeOfCubes, 
                        -wPos);
                    gridPoint = objectRotation * gridPoint;
                    //Debug.Log(currentShape);
                    scalarField[x,y,z] = methodList[currentShape](gridPoint.x, gridPoint.y, gridPoint.z, gridPoint.w);
                }
            }
        }
    }

    private static readonly int[,] edgeTable = new int[12, 2]
    {
        {0, 1}, {1, 3}, {3, 2}, {2, 0}, {4, 5}, {5, 7}, {7, 6}, {6, 4}, {0, 4}, {1, 5}, {3, 7}, {2, 6}
    };

    private static readonly int[][] triTable = new int[256][]
    {
        new int[] {}, 
        new int[] {  0, 3, 8},
        new int[] {  0, 9, 1},
        new int[] {  3, 8, 1, 1, 8, 9},
        new int[] {  2, 11, 3},
        new int[] {  8, 0, 11, 11, 0, 2},
        new int[] {  3, 2, 11, 1, 0, 9},
        new int[] {  11, 1, 2, 11, 9, 1, 11, 8, 9},
        new int[] {  1, 10, 2},
        new int[] {  0, 3, 8, 2, 1, 10},
        new int[] {  10, 2, 9, 9, 2, 0},
        new int[] {  8, 2, 3, 8, 10, 2, 8, 9, 10},
        new int[] {  11, 3, 10, 10, 3, 1},
        new int[] {  10, 0, 1, 10, 8, 0, 10, 11, 8},
        new int[] {  9, 3, 0, 9, 11, 3, 9, 10, 11},
        new int[] {  8, 9, 11, 11, 9, 10},
        new int[] {  4, 8, 7},
        new int[] {  7, 4, 3, 3, 4, 0},
        new int[] {  4, 8, 7, 0, 9, 1},
        new int[] {  1, 4, 9, 1, 7, 4, 1, 3, 7},
        new int[] {  8, 7, 4, 11, 3, 2},
        new int[] {  4, 11, 7, 4, 2, 11, 4, 0, 2},
        new int[] {  0, 9, 1, 8, 7, 4, 11, 3, 2},
        new int[] {  7, 4, 11, 11, 4, 2, 2, 4, 9, 2, 9, 1},
        new int[] {  4, 8, 7, 2, 1, 10},
        new int[] {  7, 4, 3, 3, 4, 0, 10, 2, 1},
        new int[] {  10, 2, 9, 9, 2, 0, 7, 4, 8},
        new int[] {  10, 2, 3, 10, 3, 4, 3, 7, 4, 9, 10, 4},
        new int[] {  1, 10, 3, 3, 10, 11, 4, 8, 7},
        new int[] {  10, 11, 1, 11, 7, 4, 1, 11, 4, 1, 4, 0},
        new int[] {  7, 4, 8, 9, 3, 0, 9, 11, 3, 9, 10, 11},
        new int[] {  7, 4, 11, 4, 9, 11, 9, 10, 11},
        new int[] {  9, 4, 5},
        new int[] {  9, 4, 5, 8, 0, 3},
        new int[] {  4, 5, 0, 0, 5, 1},
        new int[] {  5, 8, 4, 5, 3, 8, 5, 1, 3},
        new int[] {  9, 4, 5, 11, 3, 2},
        new int[] {  2, 11, 0, 0, 11, 8, 5, 9, 4},
        new int[] {  4, 5, 0, 0, 5, 1, 11, 3, 2},
        new int[] {  5, 1, 4, 1, 2, 11, 4, 1, 11, 4, 11, 8},
        new int[] {  1, 10, 2, 5, 9, 4},
        new int[] {  9, 4, 5, 0, 3, 8, 2, 1, 10},
        new int[] {  2, 5, 10, 2, 4, 5, 2, 0, 4},
        new int[] {  10, 2, 5, 5, 2, 4, 4, 2, 3, 4, 3, 8},
        new int[] {  11, 3, 10, 10, 3, 1, 4, 5, 9},
        new int[] {  4, 5, 9, 10, 0, 1, 10, 8, 0, 10, 11, 8},
        new int[] {  11, 3, 0, 11, 0, 5, 0, 4, 5, 10, 11, 5},
        new int[] {  4, 5, 8, 5, 10, 8, 10, 11, 8},
        new int[] {  8, 7, 9, 9, 7, 5},
        new int[] {  3, 9, 0, 3, 5, 9, 3, 7, 5},
        new int[] {  7, 0, 8, 7, 1, 0, 7, 5, 1},
        new int[] {  7, 5, 3, 3, 5, 1},
        new int[] {  5, 9, 7, 7, 9, 8, 2, 11, 3},
        new int[] {  2, 11, 7, 2, 7, 9, 7, 5, 9, 0, 2, 9},
        new int[] {  2, 11, 3, 7, 0, 8, 7, 1, 0, 7, 5, 1},
        new int[] {  2, 11, 1, 11, 7, 1, 7, 5, 1},
        new int[] {  8, 7, 9, 9, 7, 5, 2, 1, 10},
        new int[] {  10, 2, 1, 3, 9, 0, 3, 5, 9, 3, 7, 5},
        new int[] {  7, 5, 8, 5, 10, 2, 8, 5, 2, 8, 2, 0},
        new int[] {  10, 2, 5, 2, 3, 5, 3, 7, 5},
        new int[] {  8, 7, 5, 8, 5, 9, 11, 3, 10, 3, 1, 10},
        new int[] {  5, 11, 7, 10, 11, 5, 1, 9, 0},
        new int[] { 11, 5, 10, 7, 5, 11, 8, 3, 0},
        new int[] { 5, 11, 7, 10, 11, 5},
        new int[] { 6, 7, 11},
        new int[] { 7, 11, 6, 3, 8, 0},
        new int[] { 6, 7, 11, 0, 9, 1},
        new int[] { 9, 1, 8, 8, 1, 3, 6, 7, 11},
        new int[] { 3, 2, 7, 7, 2, 6},
        new int[] { 0, 7, 8, 0, 6, 7, 0, 2, 6},
        new int[] { 6, 7, 2, 2, 7, 3, 9, 1, 0},
        new int[] { 6, 7, 8, 6, 8, 1, 8, 9, 1, 2, 6, 1},
        new int[] { 11, 6, 7, 10, 2, 1},
        new int[] { 3, 8, 0, 11, 6, 7, 10, 2, 1},
        new int[] { 0, 9, 2, 2, 9, 10, 7, 11, 6},
        new int[] { 6, 7, 11, 8, 2, 3, 8, 10, 2, 8, 9, 10},
        new int[] { 7, 10, 6, 7, 1, 10, 7, 3, 1},
        new int[] { 8, 0, 7, 7, 0, 6, 6, 0, 1, 6, 1, 10},
        new int[] { 7, 3, 6, 3, 0, 9, 6, 3, 9, 6, 9, 10},
        new int[] { 6, 7, 10, 7, 8, 10, 8, 9, 10},
        new int[] { 11, 6, 8, 8, 6, 4},
        new int[] { 6, 3, 11, 6, 0, 3, 6, 4, 0},
        new int[] { 11, 6, 8, 8, 6, 4, 1, 0, 9},
        new int[] { 1, 3, 9, 3, 11, 6, 9, 3, 6, 9, 6, 4},
        new int[] { 2, 8, 3, 2, 4, 8, 2, 6, 4},
        new int[] { 4, 0, 6, 6, 0, 2},
        new int[] { 9, 1, 0, 2, 8, 3, 2, 4, 8, 2, 6, 4},
        new int[] { 9, 1, 4, 1, 2, 4, 2, 6, 4},
        new int[] { 4, 8, 6, 6, 8, 11, 1, 10, 2},
        new int[] { 1, 10, 2, 6, 3, 11, 6, 0, 3, 6, 4, 0},
        new int[] { 11, 6, 4, 11, 4, 8, 10, 2, 9, 2, 0, 9},
        new int[] { 10, 4, 9, 6, 4, 10, 11, 2, 3},
        new int[] { 4, 8, 3, 4, 3, 10, 3, 1, 10, 6, 4, 10},
        new int[] { 1, 10, 0, 10, 6, 0, 6, 4, 0},
        new int[] { 4, 10, 6, 9, 10, 4, 0, 8, 3},
        new int[] { 4, 10, 6, 9, 10, 4},
        new int[] { 6, 7, 11, 4, 5, 9},
        new int[] { 4, 5, 9, 7, 11, 6, 3, 8, 0},
        new int[] { 1, 0, 5, 5, 0, 4, 11, 6, 7},
        new int[] { 11, 6, 7, 5, 8, 4, 5, 3, 8, 5, 1, 3},
        new int[] { 3, 2, 7, 7, 2, 6, 9, 4, 5},
        new int[] { 5, 9, 4, 0, 7, 8, 0, 6, 7, 0, 2, 6},
        new int[] { 3, 2, 6, 3, 6, 7, 1, 0, 5, 0, 4, 5},
        new int[] { 6, 1, 2, 5, 1, 6, 4, 7, 8},
        new int[] { 10, 2, 1, 6, 7, 11, 4, 5, 9},
        new int[] { 0, 3, 8, 4, 5, 9, 11, 6, 7, 10, 2, 1},
        new int[] { 7, 11, 6, 2, 5, 10, 2, 4, 5, 2, 0, 4},
        new int[] { 8, 4, 7, 5, 10, 6, 3, 11, 2},
        new int[] { 9, 4, 5, 7, 10, 6, 7, 1, 10, 7, 3, 1},
        new int[] { 10, 6, 5, 7, 8, 4, 1, 9, 0},
        new int[] { 4, 3, 0, 7, 3, 4, 6, 5, 10},
        new int[] { 10, 6, 5, 8, 4, 7},
        new int[] { 9, 6, 5, 9, 11, 6, 9, 8, 11},
        new int[] { 11, 6, 3, 3, 6, 0, 0, 6, 5, 0, 5, 9},
        new int[] { 11, 6, 5, 11, 5, 0, 5, 1, 0, 8, 11, 0},
        new int[] { 11, 6, 3, 6, 5, 3, 5, 1, 3},
        new int[] { 9, 8, 5, 8, 3, 2, 5, 8, 2, 5, 2, 6},
        new int[] { 5, 9, 6, 9, 0, 6, 0, 2, 6},
        new int[] { 1, 6, 5, 2, 6, 1, 3, 0, 8},
        new int[] { 1, 6, 5, 2, 6, 1},
        new int[] { 2, 1, 10, 9, 6, 5, 9, 11, 6, 9, 8, 11},
        new int[] { 9, 0, 1, 3, 11, 2, 5, 10, 6},
        new int[] { 11, 0, 8, 2, 0, 11, 10, 6, 5},
        new int[] { 3, 11, 2, 5, 10, 6},
        new int[] { 1, 8, 3, 9, 8, 1, 5, 10, 6},
        new int[] { 6, 5, 10, 0, 1, 9},
        new int[] { 8, 3, 0, 5, 10, 6},
        new int[] { 6, 5, 10},
        new int[] { 10, 5, 6},
        new int[] { 0, 3, 8, 6, 10, 5},
        new int[] { 10, 5, 6, 9, 1, 0},
        new int[] { 3, 8, 1, 1, 8, 9, 6, 10, 5},
        new int[] { 2, 11, 3, 6, 10, 5},
        new int[] { 8, 0, 11, 11, 0, 2, 5, 6, 10},
        new int[] { 1, 0, 9, 2, 11, 3, 6, 10, 5},
        new int[] { 5, 6, 10, 11, 1, 2, 11, 9, 1, 11, 8, 9},
        new int[] { 5, 6, 1, 1, 6, 2},
        new int[] { 5, 6, 1, 1, 6, 2, 8, 0, 3},
        new int[] { 6, 9, 5, 6, 0, 9, 6, 2, 0},
        new int[] { 6, 2, 5, 2, 3, 8, 5, 2, 8, 5, 8, 9},
        new int[] { 3, 6, 11, 3, 5, 6, 3, 1, 5},
        new int[] { 8, 0, 1, 8, 1, 6, 1, 5, 6, 11, 8, 6},
        new int[] { 11, 3, 6, 6, 3, 5, 5, 3, 0, 5, 0, 9},
        new int[] { 5, 6, 9, 6, 11, 9, 11, 8, 9},
        new int[] { 5, 6, 10, 7, 4, 8},
        new int[] { 0, 3, 4, 4, 3, 7, 10, 5, 6},
        new int[] { 5, 6, 10, 4, 8, 7, 0, 9, 1},
        new int[] { 6, 10, 5, 1, 4, 9, 1, 7, 4, 1, 3, 7},
        new int[] { 7, 4, 8, 6, 10, 5, 2, 11, 3},
        new int[] { 10, 5, 6, 4, 11, 7, 4, 2, 11, 4, 0, 2},
        new int[] { 4, 8, 7, 6, 10, 5, 3, 2, 11, 1, 0, 9},
        new int[] { 1, 2, 10, 11, 7, 6, 9, 5, 4},
        new int[] { 2, 1, 6, 6, 1, 5, 8, 7, 4},
        new int[] { 0, 3, 7, 0, 7, 4, 2, 1, 6, 1, 5, 6},
        new int[] { 8, 7, 4, 6, 9, 5, 6, 0, 9, 6, 2, 0},
        new int[] { 7, 2, 3, 6, 2, 7, 5, 4, 9},
        new int[] { 4, 8, 7, 3, 6, 11, 3, 5, 6, 3, 1, 5},
        new int[] { 5, 0, 1, 4, 0, 5, 7, 6, 11},
        new int[] { 9, 5, 4, 6, 11, 7, 0, 8, 3},
        new int[] { 11, 7, 6, 9, 5, 4},
        new int[] { 6, 10, 4, 4, 10, 9},
        new int[] { 6, 10, 4, 4, 10, 9, 3, 8, 0},
        new int[] { 0, 10, 1, 0, 6, 10, 0, 4, 6},
        new int[] { 6, 10, 1, 6, 1, 8, 1, 3, 8, 4, 6, 8},
        new int[] { 9, 4, 10, 10, 4, 6, 3, 2, 11},
        new int[] { 2, 11, 8, 2, 8, 0, 6, 10, 4, 10, 9, 4},
        new int[] { 11, 3, 2, 0, 10, 1, 0, 6, 10, 0, 4, 6},
        new int[] { 6, 8, 4, 11, 8, 6, 2, 10, 1},
        new int[] { 4, 1, 9, 4, 2, 1, 4, 6, 2},
        new int[] { 3, 8, 0, 4, 1, 9, 4, 2, 1, 4, 6, 2},
        new int[] { 6, 2, 4, 4, 2, 0},
        new int[] { 3, 8, 2, 8, 4, 2, 4, 6, 2},
        new int[] { 4, 6, 9, 6, 11, 3, 9, 6, 3, 9, 3, 1},
        new int[] { 8, 6, 11, 4, 6, 8, 9, 0, 1},
        new int[] { 11, 3, 6, 3, 0, 6, 0, 4, 6},
        new int[] { 8, 6, 11, 4, 6, 8},
        new int[] { 10, 7, 6, 10, 8, 7, 10, 9, 8},
        new int[] { 3, 7, 0, 7, 6, 10, 0, 7, 10, 0, 10, 9},
        new int[] { 6, 10, 7, 7, 10, 8, 8, 10, 1, 8, 1, 0},
        new int[] { 6, 10, 7, 10, 1, 7, 1, 3, 7},
        new int[] { 3, 2, 11, 10, 7, 6, 10, 8, 7, 10, 9, 8},
        new int[] { 2, 9, 0, 10, 9, 2, 6, 11, 7},
        new int[] { 0, 8, 3, 7, 6, 11, 1, 2, 10},
        new int[] { 7, 6, 11, 1, 2, 10},
        new int[] { 2, 1, 9, 2, 9, 7, 9, 8, 7, 6, 2, 7},
        new int[] { 2, 7, 6, 3, 7, 2, 0, 1, 9},
        new int[] { 8, 7, 0, 7, 6, 0, 6, 2, 0},
        new int[] { 7, 2, 3, 6, 2, 7},
        new int[] { 8, 1, 9, 3, 1, 8, 11, 7, 6},
        new int[] { 11, 7, 6, 1, 9, 0},
        new int[] { 6, 11, 7, 0, 8, 3},
        new int[] { 11, 7, 6},
        new int[] { 7, 11, 5, 5, 11, 10},
        new int[] { 10, 5, 11, 11, 5, 7, 0, 3, 8},
        new int[] { 7, 11, 5, 5, 11, 10, 0, 9, 1},
        new int[] { 7, 11, 10, 7, 10, 5, 3, 8, 1, 8, 9, 1},
        new int[] { 5, 2, 10, 5, 3, 2, 5, 7, 3},
        new int[] { 5, 7, 10, 7, 8, 0, 10, 7, 0, 10, 0, 2},
        new int[] { 0, 9, 1, 5, 2, 10, 5, 3, 2, 5, 7, 3},
        new int[] { 9, 7, 8, 5, 7, 9, 10, 1, 2},
        new int[] { 1, 11, 2, 1, 7, 11, 1, 5, 7},
        new int[] { 8, 0, 3, 1, 11, 2, 1, 7, 11, 1, 5, 7},
        new int[] { 7, 11, 2, 7, 2, 9, 2, 0, 9, 5, 7, 9},
        new int[] { 7, 9, 5, 8, 9, 7, 3, 11, 2},
        new int[] { 3, 1, 7, 7, 1, 5},
        new int[] { 8, 0, 7, 0, 1, 7, 1, 5, 7},
        new int[] { 0, 9, 3, 9, 5, 3, 5, 7, 3},
        new int[] { 9, 7, 8, 5, 7, 9},
        new int[] { 8, 5, 4, 8, 10, 5, 8, 11, 10},
        new int[] { 0, 3, 11, 0, 11, 5, 11, 10, 5, 4, 0, 5},
        new int[] { 1, 0, 9, 8, 5, 4, 8, 10, 5, 8, 11, 10},
        new int[] { 10, 3, 11, 1, 3, 10, 9, 5, 4},
        new int[] { 3, 2, 8, 8, 2, 4, 4, 2, 10, 4, 10, 5},
        new int[] { 10, 5, 2, 5, 4, 2, 4, 0, 2},
        new int[] { 5, 4, 9, 8, 3, 0, 10, 1, 2},
        new int[] { 2, 10, 1, 4, 9, 5},
        new int[] { 8, 11, 4, 11, 2, 1, 4, 11, 1, 4, 1, 5},
        new int[] { 0, 5, 4, 1, 5, 0, 2, 3, 11},
        new int[] { 0, 11, 2, 8, 11, 0, 4, 9, 5},
        new int[] { 5, 4, 9, 2, 3, 11},
        new int[] { 4, 8, 5, 8, 3, 5, 3, 1, 5},
        new int[] { 0, 5, 4, 1, 5, 0},
        new int[] { 5, 4, 9, 3, 0, 8},
        new int[] { 5, 4, 9},
        new int[] { 11, 4, 7, 11, 9, 4, 11, 10, 9},
        new int[] { 0, 3, 8, 11, 4, 7, 11, 9, 4, 11, 10, 9},
        new int[] { 11, 10, 7, 10, 1, 0, 7, 10, 0, 7, 0, 4},
        new int[] { 3, 10, 1, 11, 10, 3, 7, 8, 4},
        new int[] { 3, 2, 10, 3, 10, 4, 10, 9, 4, 7, 3, 4},
        new int[] { 9, 2, 10, 0, 2, 9, 8, 4, 7},
        new int[] { 3, 4, 7, 0, 4, 3, 1, 2, 10},
        new int[] { 7, 8, 4, 10, 1, 2},
        new int[] { 7, 11, 4, 4, 11, 9, 9, 11, 2, 9, 2, 1},
        new int[] { 1, 9, 0, 4, 7, 8, 2, 3, 11},
        new int[] { 7, 11, 4, 11, 2, 4, 2, 0, 4},
        new int[] { 4, 7, 8, 2, 3, 11},
        new int[] { 9, 4, 1, 4, 7, 1, 7, 3, 1},
        new int[] { 7, 8, 4, 1, 9, 0},
        new int[] { 3, 4, 7, 0, 4, 3},
        new int[] { 7, 8, 4},
        new int[] { 11, 10, 8, 8, 10, 9},
        new int[] { 0, 3, 9, 3, 11, 9, 11, 10, 9},
        new int[] { 1, 0, 10, 0, 8, 10, 8, 11, 10},
        new int[] { 10, 3, 11, 1, 3, 10},
        new int[] { 3, 2, 8, 2, 10, 8, 10, 9, 8},
        new int[] { 9, 2, 10, 0, 2, 9},
        new int[] { 8, 3, 0, 10, 1, 2},
        new int[] { 2, 10, 1},
        new int[] { 2, 1, 11, 1, 9, 11, 9, 8, 11},
        new int[] { 11, 2, 3, 9, 0, 1},
        new int[] { 11, 0, 8, 2, 0, 11},
        new int[] { 3, 11, 2},
        new int[] { 1, 8, 3, 9, 8, 1},
        new int[] { 1, 9, 0},
        new int[] { 8, 3, 0},
        new int[] {}      
    };


}
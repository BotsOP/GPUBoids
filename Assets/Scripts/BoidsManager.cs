using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;



public class BoidsManager : MonoBehaviour
{
    [Header("Boid Forces")]
    [SerializeField, Range(0, 10)] private float seperationRadius = 1;
    [SerializeField, Range(0, 0.1f)] private float seperationStrength = 0.05f;
    [SerializeField, Range(0, 10)] private float alignmentRadius = 1;
    [SerializeField, Range(0, 0.1f)] private float alignmentStrength = 0.05f;
    [SerializeField, Range(0, 10)] private float cohesionRadius = 1;
    [SerializeField, Range(0, 0.1f)] private float cohesionStrength = 0.0005f;
    [SerializeField, Range(0, 0.1f)] private float moveMeStrength = 0.0005f;
    
    [Header("Boid Settings")]
    [SerializeField] private Material boidMaterial;
    [SerializeField] private Mesh boidMesh;
    [SerializeField] private int numOfBoids = 100000;
    [SerializeField] private float  spawnAreaWidth;
    [SerializeField] private Transform moveMe;
    [SerializeField, Range(0, 10)] private float  maxSpeed = 6;
    [SerializeField, Range(0, 10)] private float  minSpeed = 3;
    [SerializeField, Range(0, 5)] private float boidSpeed = 1;
    [SerializeField, Range(0, 10)] private float cellSize = 1;
    
    private ComputeShader boidsShader;
    private Boid[] boidsArray;
    private ComputeBuffer boidsBuffer;
    private ComputeBuffer boidsIndicesBuffer;
    private ComputeBuffer boidsStartIndicesBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = { 0, 0, 0, 0, 0 };
    private int updateBoidsKernel;
    private int boidStartIndexKernel;
    private int boidIndexKernel;
    private int threadX;
    private float neighbourDistance;
    private Bounds bounds;

    private void OnEnable()
    {
        boidsShader = Resources.Load<ComputeShader>("BoidsShader");

        updateBoidsKernel = boidsShader.FindKernel("UpdateBoids");
        boidStartIndexKernel = boidsShader.FindKernel("BoidStartIndex");
        boidIndexKernel = boidsShader.FindKernel("BoidIndex");
        threadX = Mathf.CeilToInt(numOfBoids / 512.0f);
        //Should fix this to accurate bounds
        bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        
        InitBoids();
        InitShader();
    }

    private void OnDisable()
    {
        boidsBuffer?.Release();
        boidsBuffer = null;
        boidsIndicesBuffer?.Release();
        boidsIndicesBuffer = null;
        boidsStartIndicesBuffer?.Release();
        boidsStartIndicesBuffer = null;
        argsBuffer?.Release();
        argsBuffer = null;
    }

    private void InitBoids()
    {
        boidsArray = new Boid[numOfBoids];

        for (int i = 0; i < numOfBoids; i++)
        {
            Vector3 pos = GetBoidSpawnPos();
            Vector3 vel = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            boidsArray[i] = new Boid(pos, vel);
        }
    }

    private Vector3 GetBoidSpawnPos()
    {
        Vector3 respawnPos;
        float randX = Random.Range(0f, spawnAreaWidth);
        float randY = Random.Range(0f, spawnAreaWidth);
        float randZ = Random.Range(0f, spawnAreaWidth);
        respawnPos.x = randX;
        respawnPos.y = randY;
        respawnPos.z = randZ;
        return respawnPos;
    }

    void InitShader()
    {
        boidsBuffer = new ComputeBuffer(numOfBoids, 7 * sizeof(float));
        boidsBuffer.SetData(boidsArray);
        
        boidsIndicesBuffer = new ComputeBuffer(numOfBoids, 3 * sizeof(int));
        boidsStartIndicesBuffer = new ComputeBuffer(numOfBoids, sizeof(int));

        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        if (boidMesh != null)
        {
            args[0] = boidMesh.GetIndexCount(0);
            args[1] = (uint)numOfBoids;
        }
        argsBuffer.SetData(args);

        boidsShader.SetBuffer(updateBoidsKernel, "Boids", boidsBuffer);
        boidsShader.SetBuffer(updateBoidsKernel, "Indices", boidsIndicesBuffer);
        boidsShader.SetBuffer(updateBoidsKernel, "StartIndices", boidsStartIndicesBuffer);
        
        boidsShader.SetBuffer(boidIndexKernel, "Boids", boidsBuffer);
        boidsShader.SetBuffer(boidIndexKernel, "Indices", boidsIndicesBuffer);
        
        boidsShader.SetBuffer(boidStartIndexKernel, "Indices", boidsIndicesBuffer);
        boidsShader.SetBuffer(boidStartIndexKernel, "StartIndices", boidsStartIndicesBuffer);
        
        boidsShader.SetInt("amountBoids", numOfBoids);
        
        boidsShader.Dispatch(boidIndexKernel, threadX, 1, 1);

        boidMaterial.SetBuffer("boidsBuffer", boidsBuffer);
    }

    private void Update()
    {
        boidsShader.SetFloat("boidSpeed", boidSpeed);
        boidsShader.SetFloat("cellSize", cellSize);
        boidsShader.SetFloat("deltaTime", Time.deltaTime);
        boidsShader.SetFloat("maxSpeed", maxSpeed);
        boidsShader.SetFloat("minSpeed", minSpeed);
        boidsShader.SetFloat("boxArea", spawnAreaWidth);
        boidsShader.SetVector("moveMePos", moveMe.position);
        
        boidsShader.SetFloat("seperationRadius", seperationRadius);
        boidsShader.SetFloat("alignmentRadius", alignmentRadius);
        boidsShader.SetFloat("cohesionRadius", cohesionRadius);
        boidsShader.SetFloat("neighbourRadius", Mathf.Max(seperationRadius, Mathf.Max(alignmentRadius, cohesionRadius)));
        
        boidsShader.SetFloat("seperationStrength", seperationStrength);
        boidsShader.SetFloat("alignmentStrength", alignmentStrength);
        boidsShader.SetFloat("cohesionStrength", cohesionStrength);
        boidsShader.SetFloat("moveMeStrength", moveMeStrength);
        
        
        // Uint3[] indices = new Uint3[numOfBoids];
        // boidsIndicesBuffer.GetData(indices);
        // uint[] startIndices = new uint[numOfBoids];
        // boidsStartIndicesBuffer.GetData(startIndices);
        boidsBuffer.GetData(boidsArray);
        
        boidsShader.Dispatch(boidIndexKernel, threadX, 1, 1);
        ComputeSorter.Sort(boidsIndicesBuffer);
        boidsShader.Dispatch(boidStartIndexKernel, threadX, 1, 1);

        boidsShader.Dispatch(updateBoidsKernel, threadX, 1, 1);
        
        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, bounds, argsBuffer);
    }
}

struct Uint3
{
    public uint x;
    public uint y;
    public uint z;
}

struct Boid
{
    public Vector3 position;
    public Vector3 direction;
    public float smth;

    public Boid(Vector3 pos, Vector3 dir)
    {
        position = pos;
        direction = dir;
        smth = 0;
    }
}
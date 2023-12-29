using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoidsManager : MonoBehaviour
{
    [SerializeField] private int numOfBoids;
    [SerializeField] private float  spawnAreaWidth;
    
    private ComputeShader boidsShader;
    private Boid[] boidsArray;
    private ComputeBuffer boidsBuffer;
    private ComputeBuffer argsBuffer;
    private Mesh boidMesh;
    private uint[] args = { 0, 0, 0, 0, 0 };
    private int updateBoidsKernel;
    private Material boidMaterial;
    private float rotationSpeed;
    private float boidSpeed;
    private float neighbourDistance;
    private Bounds bounds;

    private void OnEnable()
    {
        boidsShader = Resources.Load<ComputeShader>("BoidsShader");

        updateBoidsKernel = boidsShader.FindKernel("UpdateBoids");
        
        //Should fix this to accurate bounds
        bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        
        InitBoids();
        InitShader();
    }
    
    private void InitBoids()
    {
        boidsArray = new Boid[numOfBoids];

        for (int i = 0; i < numOfBoids; i++)
        {
            Vector3 pos = GetBoidSpawnPos();
            Quaternion rot = Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f);
            boidsArray[i] = new Boid(pos, rot.eulerAngles);
        }
    }

    private Vector3 GetBoidSpawnPos()
    {
        Vector3 respawnPos;
        float respawnWidth = 10;
        float randX = Random.Range(0f, 1f);
        float randY = Random.Range(0f, 1f);
        float randZ = Random.Range(0f, 1f);
        
        respawnPos.y = randY * (spawnAreaWidth * 2 - respawnWidth * 2) + respawnWidth;

        if(Random.Range(0f, 1f) < 0.5)
        {
            respawnPos.x = randX * spawnAreaWidth * 3;
            if(respawnPos.x > respawnWidth && respawnPos.x < spawnAreaWidth * 3 - respawnWidth)
            {
                float extraZPos = (int)(randZ * 2) * (spawnAreaWidth * 3 - respawnWidth);
                respawnPos.z = randZ * respawnWidth + extraZPos;
                return respawnPos;
            }
            respawnPos.z = randZ * spawnAreaWidth * 3;
            return respawnPos;
        }
	
        respawnPos.z = randZ * spawnAreaWidth * 3;
        if(respawnPos.z > respawnWidth && respawnPos.z < spawnAreaWidth * 3 - respawnWidth)
        {
            float extraZPos = (int)(randX * 2) * (spawnAreaWidth * 3 - respawnWidth);
            respawnPos.x = randX * respawnWidth + extraZPos;
            return respawnPos;
        }
        respawnPos.x = randX * spawnAreaWidth * 3;
        return respawnPos;
    }

    void InitShader()
    {
        boidsBuffer = new ComputeBuffer(numOfBoids, 7 * sizeof(float));
        boidsBuffer.SetData(boidsArray);

        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        if (boidMesh != null)
        {
            args[0] = boidMesh.GetIndexCount(0);
            args[1] = (uint)numOfBoids;
        }
        argsBuffer.SetData(args);

        boidsShader.SetBuffer(updateBoidsKernel, "boidsBuffer", boidsBuffer);
        boidsShader.SetFloat("rotationSpeed", rotationSpeed);
        boidsShader.SetFloat("boidSpeed", boidSpeed);
        boidsShader.SetFloat("neighbourDistance", neighbourDistance);
        boidsShader.SetInt("boidsCount", numOfBoids);

        boidMaterial.SetBuffer("boidsBuffer", boidsBuffer);
    }

    private void Update()
    {
        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, bounds, argsBuffer);
    }
}

struct Boid
{
    public Vector3 position;
    public Vector3 direction;

    public Boid(Vector3 pos, Vector3 dir)
    {
        position = pos;
        direction = dir;
    }
}

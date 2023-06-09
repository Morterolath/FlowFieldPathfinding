﻿using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
public class PathfindingManager : MonoBehaviour
{
    [SerializeField] TerrainGenerator _terrainGenerator;
    [SerializeField] int _maxCostfieldOffset;
    [SerializeField] float _agentUpdateFrequency;

    [HideInInspector] public float TileSize;
    [HideInInspector] public int RowAmount;
    [HideInInspector] public int ColumnAmount;
    [HideInInspector] public byte SectorTileAmount = 10;
    [HideInInspector] public int SectorMatrixColAmount;
    [HideInInspector] public int SectorMatrixRowAmount;

    public CostFieldProducer CostFieldProducer;
    public PathProducer PathProducer;
    public AgentDataContainer AgentDataContainer;
    public NativeArray<Vector3> TilePositions;
    public List<FlowFieldAgent> Agents;

    float _lastAgentUpdateTime = 0;
    PathfindingUpdateRoutine _pathfindingUpdateRoutine;
    private void Awake()
    {
        Agents = new List<FlowFieldAgent>();
        AgentDataContainer = new AgentDataContainer(this);
    }
    private void Start()
    {

        //!!!ORDER IS IMPORTANT!!!
        TileSize = _terrainGenerator.TileSize;
        RowAmount = _terrainGenerator.RowAmount;
        ColumnAmount = _terrainGenerator.ColumnAmount;
        SectorMatrixColAmount = ColumnAmount / SectorTileAmount;
        SectorMatrixRowAmount = RowAmount / SectorTileAmount;
        CostFieldProducer = new CostFieldProducer(_terrainGenerator.WalkabilityData, SectorTileAmount, ColumnAmount, RowAmount, SectorMatrixColAmount, SectorMatrixRowAmount);
        CostFieldProducer.StartCostFieldProduction(0, _maxCostfieldOffset, SectorTileAmount, SectorMatrixColAmount, SectorMatrixRowAmount);
        PathProducer = new PathProducer(this);
        TilePositions = new NativeArray<Vector3>(RowAmount * ColumnAmount, Allocator.Persistent);
        _pathfindingUpdateRoutine = new PathfindingUpdateRoutine(this);
        CalculateTilePositions();
        CostFieldProducer.ForceCompleteCostFieldProduction();

        SetFlowFieldUtilities();
    }
    private void Update()
    {
        PathProducer.Update();
        AgentDataContainer.OnUpdate();
        float curTime = Time.realtimeSinceStartup;
        float deltaTime = curTime - _lastAgentUpdateTime;
        if (deltaTime >= _agentUpdateFrequency)
        {
            _lastAgentUpdateTime = curTime;
            _pathfindingUpdateRoutine.Update(deltaTime);
        }
    }
    private void LateUpdate()
    {
        
    }
    void CalculateTilePositions()
    {
        for (int r = 0; r < RowAmount; r++)
        {
            for (int c = 0; c < ColumnAmount; c++)
            {
                int index = r * ColumnAmount + c;
                TilePositions[index] = new Vector3(TileSize / 2 + c * TileSize, 0f, TileSize / 2 + r * TileSize);
            }
        }
    }
    void SetFlowFieldUtilities()
    {
        FlowFieldUtilities.SectorMatrixTileAmount = SectorMatrixColAmount * SectorMatrixRowAmount;
        FlowFieldUtilities.SectorMatrixRowAmount = SectorMatrixRowAmount;
        FlowFieldUtilities.SectorMatrixColAmount = SectorMatrixColAmount;
        FlowFieldUtilities.SectorColAmount = SectorTileAmount;
        FlowFieldUtilities.SectorRowAmount = SectorTileAmount;
        FlowFieldUtilities.SectorTileAmount = SectorTileAmount * SectorTileAmount;
        FlowFieldUtilities.TileSize = TileSize;
        FlowFieldUtilities.FieldColAmount = ColumnAmount;
        FlowFieldUtilities.FieldRowAmount = RowAmount;
        FlowFieldUtilities.FieldTileAmount = ColumnAmount * RowAmount;
    }
    public Path SetDestination(NativeArray<Vector3> sources, Vector3 target)
    {
        Vector2 target2 = new Vector2(target.x, target.z);
        return PathProducer.ProducePath(sources, target2, 0);
    }
    public void Subscribe(FlowFieldAgent agent)
    {
        Agents.Add(agent);
    }
    public void UnSubscribe(FlowFieldAgent agent)
    {
        Agents.Remove(agent);
    }
}

﻿using Unity.Collections;
using UnityEngine;

public struct AStarGrid
{
    int _rowAmount;
    int _colAmount;
    public NativeArray<AStarTile> _integratedCosts;
    public NativeQueue<int> _searchQueue;

    public AStarGrid(int rowAmount, int colAmount)
    {
        _rowAmount = rowAmount;
        _colAmount = colAmount;
        _integratedCosts = new NativeArray<AStarTile>(rowAmount * colAmount, Allocator.Persistent);
        _searchQueue = new NativeQueue<int>(Allocator.Persistent);
    }
    public void DisposeNatives()
    {
        _integratedCosts.Dispose();
        _searchQueue.Dispose();
    }
    public NativeArray<AStarTile> GetIntegratedCostsFor(Sector sector, Index2 target, NativeArray<byte> costs, NativeArray<DirectionData> directions)
    {
        Reset(sector, costs);
        int targetIndex = Index2.ToIndex(target, _colAmount);

        AStarTile targetTile = _integratedCosts[targetIndex];
        targetTile.IntegratedCost = 0f;
        targetTile.Enqueued = true;
        _integratedCosts[targetIndex] = targetTile;
        Enqueue(directions[targetIndex]);

        while (!_searchQueue.IsEmpty())
        {
            int index = _searchQueue.Dequeue();
            AStarTile tile = _integratedCosts[index];
            tile.IntegratedCost = GetCost(directions[index]);
            _integratedCosts[index] = tile;
            Enqueue(directions[index]);
        }
        return _integratedCosts;
    }
    void Reset(Sector sector, NativeArray<byte> costs)
    {
        Index2 lowerBound = sector.StartIndex;
        Index2 upperBound = new Index2(sector.StartIndex.R + sector.Size - 1, sector.StartIndex.C + sector.Size - 1);
        int lowerBoundIndex = Index2.ToIndex(lowerBound, _colAmount);
        int upperBoundIndex = Index2.ToIndex(upperBound, _colAmount);

        for (int r = lowerBoundIndex; r < lowerBoundIndex + sector.Size * _colAmount; r += _colAmount)
        {
            for (int i = r; i < r + sector.Size; i++)
            {
                if (costs[i] == byte.MaxValue)
                {
                    _integratedCosts[i] = new AStarTile(float.MaxValue, true);
                    continue;
                }
                _integratedCosts[i] = new AStarTile(float.MaxValue, false);
            }
        }
        SetEdgesUnwalkable(sector, lowerBoundIndex, upperBoundIndex);
    }
    void SetEdgesUnwalkable(Sector sector, int lowerBoundIndex, int upperBoundIndex)
    {
        bool notOnBottom = !sector.IsOnBottom();
        bool notOnTop = !sector.IsOnTop(_rowAmount);
        bool notOnRight = !sector.IsOnRight(_colAmount);
        bool notOnLeft = !sector.IsOnLeft();
        if (notOnBottom)
        {
            for (int i = lowerBoundIndex - _colAmount; i < (lowerBoundIndex - _colAmount) + sector.Size; i++)
            {
                _integratedCosts[i] = new AStarTile(float.MaxValue, true);
            }
        }
        if (notOnTop)
        {
            for (int i = upperBoundIndex + _colAmount; i > upperBoundIndex + _colAmount - sector.Size; i--)
            {
                _integratedCosts[i] = new AStarTile(float.MaxValue, true);
            }
        }
        if (notOnRight)
        {
            for (int i = upperBoundIndex + 1; i >= lowerBoundIndex + sector.Size; i -= _colAmount)
            {
                _integratedCosts[i] = new AStarTile(float.MaxValue, true);
            }
        }
        if (notOnLeft)
        {
            for (int i = lowerBoundIndex - 1; i <= upperBoundIndex - sector.Size; i += _colAmount)
            {
                _integratedCosts[i] = new AStarTile(float.MaxValue, true);
            }
        }
        if (notOnRight && notOnBottom)
        {
            _integratedCosts[lowerBoundIndex + sector.Size - _colAmount] = new AStarTile(float.MaxValue, true);
        }
        if (notOnRight && notOnTop)
        {
            _integratedCosts[upperBoundIndex + _colAmount + 1] = new AStarTile(float.MaxValue, true);
        }
        if (notOnLeft && notOnBottom)
        {
            _integratedCosts[lowerBoundIndex - _colAmount - 1] = new AStarTile(float.MaxValue, true);
        }
        if (notOnLeft && notOnTop)
        {
            _integratedCosts[upperBoundIndex + _colAmount - sector.Size] = new AStarTile(float.MaxValue, true);
        }
    }
    void Enqueue(DirectionData directions)
    {
        int n = directions.N;
        int e = directions.E;
        int s = directions.S;
        int w = directions.W;
        if (!_integratedCosts[n].Enqueued)
        {
            _searchQueue.Enqueue(n);
            AStarTile tile = _integratedCosts[n];
            tile.Enqueued = true;
            _integratedCosts[n] = tile;
        }
        if (!_integratedCosts[e].Enqueued)
        {
            _searchQueue.Enqueue(e);
            AStarTile tile = _integratedCosts[e];
            tile.Enqueued = true;
            _integratedCosts[e] = tile;
        }
        if (!_integratedCosts[s].Enqueued)
        {
            _searchQueue.Enqueue(s);
            AStarTile tile = _integratedCosts[s];
            tile.Enqueued = true;
            _integratedCosts[s] = tile;
        }
        if (!_integratedCosts[w].Enqueued)
        {
            _searchQueue.Enqueue(w);
            AStarTile tile = _integratedCosts[w];
            tile.Enqueued = true;
            _integratedCosts[w] = tile;
        }
    }
    float GetCost(DirectionData directions)
    {
        float costToReturn = float.MaxValue;
        float nCost = _integratedCosts[directions.N].IntegratedCost + 1f;
        float neCost = _integratedCosts[directions.NE].IntegratedCost + 1.4f;
        float eCost = _integratedCosts[directions.E].IntegratedCost + 1f;
        float seCost = _integratedCosts[directions.SE].IntegratedCost + 1.4f;
        float sCost = _integratedCosts[directions.S].IntegratedCost + 1f;
        float swCost = _integratedCosts[directions.SW].IntegratedCost + 1.4f;
        float wCost = _integratedCosts[directions.W].IntegratedCost + 1f;
        float nwCost = _integratedCosts[directions.NW].IntegratedCost + 1.4f;
        if (nCost < costToReturn) { costToReturn = nCost; }
        if (neCost < costToReturn) { costToReturn = neCost; }
        if (eCost < costToReturn) { costToReturn = eCost; }
        if (seCost < costToReturn) { costToReturn = seCost; }
        if (sCost < costToReturn) { costToReturn = sCost; }
        if (swCost < costToReturn) { costToReturn = swCost; }
        if (wCost < costToReturn) { costToReturn = wCost; }
        if (nwCost < costToReturn) { costToReturn = nwCost; }
        return costToReturn;
    }
}
public struct AStarTile
{
    public bool Enqueued;
    public float IntegratedCost;

    public AStarTile(float integratedCost, bool enqueued)
    {
        Enqueued = enqueued;
        IntegratedCost = integratedCost;
    }
}
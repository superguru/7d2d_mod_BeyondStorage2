using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeyondStorage.Scripts.Game.Item;

// Face data structure to store painting information
public struct PaintFaceData
{
    public Vector3i BlockPos { get; set; }
    public BlockFace BlockFace { get; set; }
    public int Channel { get; set; }

    public PaintFaceData(Vector3i blockPos, BlockFace blockFace, int channel)
    {
        BlockPos = blockPos;
        BlockFace = blockFace;
        Channel = channel;
    }
}

internal class ItemActionTextureBlockExposed : ItemActionTextureBlock
{
    private const int LAYER_MASK = -555528197;

    // Store faces to paint during counting phase
    private readonly Dictionary<Guid, List<PaintFaceData>> _facesToPaint = [];

    public void CountFloodFill(World _world, ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, PersistentPlayerData _lpRelative, int _sourcePaint, Vector3 _hitPosition, Vector3 _hitFaceNormal, Vector3 _dir1, Vector3 _dir2, int _channel, Guid operationId)
    {
        // Initialize face list for this operation
        _facesToPaint[operationId] = [];

        visitedPositions.Clear();
        visitedRays.Clear();
        positionsToCheck.Clear();
        positionsToCheck.Push(new Vector2i(0, 0));

        while (positionsToCheck.Count > 0)
        {
            Vector2i vector2i = positionsToCheck.Pop();
            if (visitedRays.ContainsKey(vector2i))
            {
                continue;
            }
            visitedRays.Add(vector2i, value: true);
            Vector3 origin = _hitPosition + _hitFaceNormal * 0.2f + vector2i.x * _dir1 + vector2i.y * _dir2;
            Vector3 direction = -_hitFaceNormal * 0.3f;
            float magnitude = direction.magnitude;
            if (!Voxel.Raycast(_world, new Ray(origin, direction), magnitude, LAYER_MASK, 69, 0f))
            {
                continue;
            }
            worldRayHitInfo.CopyFrom(Voxel.voxelRayHitInfo);
            BlockValue blockValue = worldRayHitInfo.hit.blockValue;
            Vector3i blockPos = worldRayHitInfo.hit.blockPos;
            bool flag;
            if (worldRayHitInfo.hitTriangleIdx < 0 || ((flag = visitedPositions.TryGetValue(blockPos, out var value)) && !value))
            {
                continue;
            }
            if (!flag)
            {
                Vector3 _hitFaceCenter;
                Vector3 _hitFaceNormal2;
                BlockFace blockFaceFromHitInfo = GameUtils.GetBlockFaceFromHitInfo(blockPos, blockValue, worldRayHitInfo.hitCollider, worldRayHitInfo.hitTriangleIdx, out _hitFaceCenter, out _hitFaceNormal2);
                if (blockFaceFromHitInfo == BlockFace.None)
                {
                    continue;
                }
                _hitFaceNormal2 = _hitFaceNormal2.normalized;
                if ((double)(_hitFaceNormal2 - _hitFaceNormal).sqrMagnitude > 0.01)
                {
                    continue;
                }
                if (getCurrentPaintIdx(_cc, blockPos, blockFaceFromHitInfo, blockValue, _channel) != _sourcePaint)
                {
                    visitedPositions.Add(blockPos, value: false);
                    continue;
                }
                EPaintResult ePaintResult = CountPaintBlock(_world, _cc, _entityId, _actionData, blockPos, blockFaceFromHitInfo, blockValue, _lpRelative, new ChannelMask(_channel), operationId);
                if (ePaintResult == EPaintResult.CanNotPaint || ePaintResult == EPaintResult.NoPaintAvailable)
                {
                    visitedPositions.Add(blockPos, value: false);
                    continue;
                }
                visitedPositions.Add(blockPos, value: true);
            }
            positionsToCheck.Push(vector2i + Vector2i.down);
            positionsToCheck.Push(vector2i + Vector2i.up);
            positionsToCheck.Push(vector2i + Vector2i.left);
            positionsToCheck.Push(vector2i + Vector2i.right);
        }
        visitedPositions.Clear();
        visitedRays.Clear();
        positionsToCheck.Clear();
    }

    public void ExecuteFloodFill(World _world, ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, PersistentPlayerData _lpRelative, int _sourcePaint, Vector3 _hitPosition, Vector3 _hitFaceNormal, Vector3 _dir1, Vector3 _dir2, int _channel, Guid operationId)
    {
        // Use the stored faces instead of running flood fill again
        if (!_facesToPaint.TryGetValue(operationId, out var facesToPaint))
        {
            return; // No faces stored for this operation
        }

        try
        {
            // Paint faces in order until we run out of paint
            foreach (var faceData in facesToPaint)
            {
                if (!ItemTexture.ShouldPaintFace(operationId))
                {
                    break; // No more paint available
                }

                // Apply the texture
                GameManager.Instance.SetBlockTextureServer(
                    faceData.BlockPos,
                    faceData.BlockFace,
                    _actionData.idx,
                    _entityId,
                    (byte)faceData.Channel
                );
            }
        }
        finally
        {
            // Clean up the stored faces
            _facesToPaint.Remove(operationId);
        }
    }

    public EPaintResult CountPaintBlock(World _world, ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, Vector3i _blockPos, BlockFace _blockFace, BlockValue _blockValue, PersistentPlayerData _lpRelative, ChannelMask _channelMask, Guid operationId)
    {
        getParentBlock(ref _blockValue, ref _blockPos, _cc);
        if (!checkBlockCanBePainted(_world, _blockPos, _blockValue, _lpRelative))
        {
            return EPaintResult.CanNotPaint;
        }
        if (BlockToolSelection.Instance.SelectionActive && !new BoundsInt(BlockToolSelection.Instance.SelectionMin, BlockToolSelection.Instance.SelectionSize).Contains(_blockPos))
        {
            return EPaintResult.CanNotPaint;
        }
        if (!_actionData.bPaintAllSides)
        {
            return CountPaintFace(_cc, _entityId, _actionData, _blockPos, _blockFace, _blockValue, _channelMask, operationId);
        }
        int num = 0;
        for (int i = 0; i <= 5; i++)
        {
            _blockFace = (BlockFace)i;
            EPaintResult ePaintResult = CountPaintFace(_cc, _entityId, _actionData, _blockPos, _blockFace, _blockValue, _channelMask, operationId);
            switch (ePaintResult)
            {
                case EPaintResult.NoPaintAvailable:
                    return ePaintResult;
                case EPaintResult.Painted:
                    num++;
                    break;
            }
        }
        if (num == 0)
        {
            return EPaintResult.SamePaint;
        }
        return EPaintResult.Painted;
    }

    public EPaintResult CountPaintFace(ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, Vector3i _blockPos, BlockFace _blockFace, BlockValue _blockValue, ChannelMask _channelMask, Guid operationId)
    {
        EPaintResult result = EPaintResult.SamePaint;
        for (int i = 0; i < 1; i++)
        {
            if (!_channelMask.IncludesChannel(i))
            {
                continue;
            }
            int currentPaintIdx = getCurrentPaintIdx(_cc, _blockPos, _blockFace, _blockValue, i);
            if (_actionData.idx != currentPaintIdx)
            {
                // Store the face to be painted
                if (_facesToPaint.TryGetValue(operationId, out var faceList))
                {
                    faceList.Add(new PaintFaceData(_blockPos, _blockFace, i));
                }

                // Count the paint usage
                if (!ItemTexture.CountPaintUsage(operationId))
                {
                    return EPaintResult.NoPaintAvailable;
                }
                result = EPaintResult.Painted;
            }
        }
        return result;
    }

    // Legacy methods for backwards compatibility (these call the original base class methods)
    public EPaintResult CountPaintBlock(World _world, ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, Vector3i _blockPos, BlockFace _blockFace, BlockValue _blockValue, PersistentPlayerData _lpRelative, ChannelMask _channelMask)
    {
        // For legacy calls without operationId, use the original base class method
        return paintBlock(_world, _cc, _entityId, _actionData, _blockPos, _blockFace, _blockValue, _lpRelative, _channelMask);
    }

    public EPaintResult CountPaintFace(ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, Vector3i _blockPos, BlockFace _blockFace, BlockValue _blockValue, ChannelMask _channelMask)
    {
        // For legacy calls without operationId, use the original base class method
        return paintFace(_cc, _entityId, _actionData, _blockPos, _blockFace, _blockValue, _channelMask);
    }

    // Cleanup method to prevent memory leaks
    public void CleanupOperation(Guid operationId)
    {
        _facesToPaint.Remove(operationId);
    }

    // Add these methods to your existing ItemActionTextureBlockExposed class

    public void CountAreaPaint(World _world, ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, PersistentPlayerData _lpRelative, Vector3 _pos, Vector3 _origin, Vector3 _dir1, Vector3 _dir2, float _radius, Guid operationId)
    {
        // Initialize face list for this operation
        _facesToPaint[operationId] = [];

        // Iterate through area grid
        for (float x = -_radius; x <= _radius; x += 0.5f)
        {
            for (float y = -_radius; y <= _radius; y += 0.5f)
            {
                Vector3 direction = _pos + x * _dir1 + y * _dir2 - _origin;
                int hitMask = 69;

                if (Voxel.Raycast(_world, new Ray(_origin, direction), 50f /* Range */, LAYER_MASK, hitMask, 0f))
                {
                    WorldRayHitInfo hitInfo = Voxel.voxelRayHitInfo.Clone();
                    BlockValue blockValue = hitInfo.hit.blockValue;
                    Vector3i blockPos = hitInfo.hit.blockPos;

                    Vector3 hitFaceCenter;
                    Vector3 hitFaceNormal;
                    BlockFace blockFace = GameUtils.GetBlockFaceFromHitInfo(blockPos, blockValue, hitInfo.hitCollider, hitInfo.hitTriangleIdx, out hitFaceCenter, out hitFaceNormal);

                    if (blockFace != BlockFace.None)
                    {
                        CountPaintBlock(_world, _cc, _entityId, _actionData, blockPos, blockFace, blockValue, _lpRelative, _actionData.channelMask, operationId);
                    }
                }
            }
        }
    }

    public void ExecuteAreaPaint(int _entityId, ItemActionTextureBlockData _actionData, Guid operationId)
    {
        // Use the stored faces instead of running area paint again
        if (!_facesToPaint.TryGetValue(operationId, out var facesToPaint))
        {
            return; // No faces stored for this operation
        }

        try
        {
            // Paint faces in order until we run out of paint
            foreach (var faceData in facesToPaint)
            {
                if (!ItemTexture.ShouldPaintFace(operationId))
                {
                    break; // No more paint available
                }

                // Apply the texture
                GameManager.Instance.SetBlockTextureServer(
                    faceData.BlockPos,
                    faceData.BlockFace,
                    _actionData.idx,
                    _entityId,
                    (byte)faceData.Channel
                );
            }
        }
        finally
        {
            // Clean up the stored faces
            _facesToPaint.Remove(operationId);
        }
    }
}

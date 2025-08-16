using System;
using System.Collections.Generic;
using UnityEngine;
using static ItemActionTextureBlock;

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

/// <summary>
/// Wrapper class that provides enhanced painting functionality while preserving the original ItemActionTextureBlock data.
/// Uses composition instead of inheritance to maintain access to all original game object state.
/// </summary>
public class ItemActionTextureBlockExposed(ItemActionTextureBlock originalTextureBlock)
{
    private const int LAYER_MASK = -555528197;

    // Store faces to paint during counting phase
    private readonly Dictionary<Guid, List<PaintFaceData>> _facesToPaint = [];

    /// <summary>
    /// The original ItemActionTextureBlock instance containing all game state and data.
    /// </summary>
    public ItemActionTextureBlock OriginalTextureBlock => originalTextureBlock;

    // Delegate properties to the original object
    public bool InfiniteAmmo => OriginalTextureBlock.InfiniteAmmo;
    public bool HasInfiniteAmmo(ItemActionData actionData) => OriginalTextureBlock.HasInfiniteAmmo(actionData);
    public ItemValue currentMagazineItem => OriginalTextureBlock.currentMagazineItem;
    public float rayCastDelay => OriginalTextureBlock.rayCastDelay;
    public bool bRemoveTexture => OriginalTextureBlock.bRemoveTexture;
    public float Range => OriginalTextureBlock.Range;

    public void CountFloodFill(World _world, ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, PersistentPlayerData _lpRelative, int _sourcePaint, Vector3 _hitPosition, Vector3 _hitFaceNormal, Vector3 _dir1, Vector3 _dir2, int _channel, Guid operationId)
    {
        // Initialize face list for this operation
        _facesToPaint[operationId] = [];

        // Access protected/private members through the original object using reflection if needed
        // For now, we'll use the accessible members and methods
        var visitedPositions = new Dictionary<Vector3i, bool>();
        var visitedRays = new Dictionary<Vector2i, bool>();
        var positionsToCheck = new Stack<Vector2i>();
        var worldRayHitInfo = new WorldRayHitInfo();

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

                // Use reflection or accessible methods to get current paint index
                int currentPaintIdx = GetCurrentPaintIdx(_cc, blockPos, blockFaceFromHitInfo, blockValue, _channel);
                if (currentPaintIdx != _sourcePaint)
                {
                    visitedPositions.Add(blockPos, value: false);
                    continue;
                }

                var ePaintResult = CountPaintBlock(_world, _cc, _entityId, _actionData, blockPos, blockFaceFromHitInfo, blockValue, _lpRelative, new ChannelMask(_channel), operationId);
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
        // Use reflection to access protected methods from the original object
        var getParentBlockMethod = typeof(ItemActionTextureBlock).GetMethod("getParentBlock",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var checkBlockCanBePaintedMethod = typeof(ItemActionTextureBlock).GetMethod("checkBlockCanBePainted",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (getParentBlockMethod != null)
        {
            object[] parameters = { _blockValue, _blockPos, _cc };
            getParentBlockMethod.Invoke(OriginalTextureBlock, parameters);
            _blockValue = (BlockValue)parameters[0];
            _blockPos = (Vector3i)parameters[1];
        }

        if (checkBlockCanBePaintedMethod != null)
        {
            var canBePainted = (bool)checkBlockCanBePaintedMethod.Invoke(OriginalTextureBlock, new object[] { _world, _blockPos, _blockValue, _lpRelative });
            if (!canBePainted)
            {
                return EPaintResult.CanNotPaint;
            }
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

            int currentPaintIdx = GetCurrentPaintIdx(_cc, _blockPos, _blockFace, _blockValue, i);

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

    // Cleanup method to prevent memory leaks
    public void CleanupOperation(Guid operationId)
    {
        _facesToPaint.Remove(operationId);
    }

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

                if (Voxel.Raycast(_world, new Ray(_origin, direction), Range, LAYER_MASK, hitMask, 0f))
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

    /// <summary>
    /// Proper implementation of getCurrentPaintIdx that matches the original game logic.
    /// This handles the case where GetBlockFaceTexture returns 0 by falling back to the default paint.
    /// </summary>
    private int GetCurrentPaintIdx(ChunkCluster _cc, Vector3i _blockPos, BlockFace _blockFace, BlockValue _blockValue, int _channel)
    {
        int blockFaceTexture = _cc.GetBlockFaceTexture(_blockPos, _blockFace, _channel);
        if (blockFaceTexture != 0)
        {
            return blockFaceTexture;
        }

        string _name;
        return GameUtils.FindPaintIdForBlockFace(_blockValue, _blockFace, out _name, _channel);
    }
}

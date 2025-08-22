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
    private const int LAYER_MASK = -555528197; // Same magic number as original game
    private const double FACE_NORMAL_TOLERANCE = 0.01; // Magic number from original
    private const int MAX_CHANNELS = 1; // Based on original game logic
    private const int MAX_BLOCK_FACES = 6; // Block faces 0-5

    // Store faces to paint during counting phase
    private readonly Dictionary<Guid, List<PaintFaceData>> _facesToPaint = [];

    // Cache reflection methods to avoid repeated lookups
    private static readonly System.Reflection.MethodInfo s_getParentBlockMethod =
        typeof(ItemActionTextureBlock).GetMethod("getParentBlock",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    private static readonly System.Reflection.MethodInfo s_checkBlockCanBePaintedMethod =
        typeof(ItemActionTextureBlock).GetMethod("checkBlockCanBePainted",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

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

    /// <summary>
    /// Performs flood fill counting by traversing connected blocks and storing faces to paint.
    /// This is the counting phase - no actual painting occurs here.
    /// </summary>
    public void CountFloodFill(World _world, ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, PersistentPlayerData _lpRelative, int _sourcePaint, Vector3 _hitPosition, Vector3 _hitFaceNormal, Vector3 _dir1, Vector3 _dir2, int _channel, Guid operationId)
    {
        // Initialize face list for this operation
        _facesToPaint[operationId] = [];

        // State tracking for flood fill (matches original game logic)
        var visitedPositions = new Dictionary<Vector3i, bool>(); // bool = canExpand
        var visitedRays = new Dictionary<Vector2i, bool>();
        var positionsToCheck = new Stack<Vector2i>();
        var worldRayHitInfo = new WorldRayHitInfo();

        positionsToCheck.Push(new Vector2i(0, 0));

        while (positionsToCheck.Count > 0)
        {
            Vector2i rayPosition = positionsToCheck.Pop();

            if (ShouldSkipRayPosition(rayPosition, visitedRays))
            {
                continue;
            }

            visitedRays.Add(rayPosition, true);

            var raycastData = PerformFloodFillRaycast(_world, _hitPosition, _hitFaceNormal, _dir1, _dir2, rayPosition, worldRayHitInfo);
            if (!raycastData.IsValid)
            {
                continue;
            }

            var blockProcessResult = ProcessFloodFillBlock(
                _cc, raycastData, visitedPositions, _hitFaceNormal, _sourcePaint, _channel,
                _actionData, operationId);

            if (blockProcessResult.ShouldExpand)
            {
                AddAdjacentPositions(positionsToCheck, rayPosition);
            }
        }
    }

    /// <summary>
    /// Executes flood fill painting using previously stored faces.
    /// This is the execution phase - actual painting occurs here.
    /// </summary>
    public void ExecuteFloodFill(World _world, ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, PersistentPlayerData _lpRelative, int _sourcePaint, Vector3 _hitPosition, Vector3 _hitFaceNormal, Vector3 _dir1, Vector3 _dir2, int _channel, Guid operationId)
    {
        if (!_facesToPaint.TryGetValue(operationId, out var facesToPaint))
        {
            return; // No faces stored for this operation
        }

        try
        {
            ExecutePaintingPhase(facesToPaint, _actionData, _entityId, operationId);
        }
        finally
        {
            _facesToPaint.Remove(operationId);
        }
    }

    /// <summary>
    /// Counts paint requirements for a specific block, handling both single face and all faces scenarios.
    /// </summary>
    public EPaintResult CountPaintBlock(World _world, ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, Vector3i _blockPos, BlockFace _blockFace, BlockValue _blockValue, PersistentPlayerData _lpRelative, ChannelMask _channelMask, Guid operationId)
    {
        var blockData = PrepareBlockData(_blockPos, _blockFace, _blockValue, _cc);

        if (!ValidateBlockCanBePainted(_world, blockData.blockPos, blockData.blockValue, _lpRelative))
        {
            return EPaintResult.CanNotPaint;
        }

        if (!ValidateBlockSelection(blockData.blockPos))
        {
            return EPaintResult.CanNotPaint;
        }

        return _actionData.bPaintAllSides
            ? CountPaintAllFaces(_cc, _entityId, _actionData, blockData.blockPos, blockData.blockValue, _channelMask, operationId)
            : CountPaintFace(_cc, _entityId, _actionData, blockData.blockPos, blockData.blockFace, _blockValue, _channelMask, operationId);
    }

    /// <summary>
    /// Counts paint requirements for a specific face on a block.
    /// Fixed to properly handle channel iteration instead of the original's pointless loop.
    /// </summary>
    public EPaintResult CountPaintFace(ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, Vector3i _blockPos, BlockFace _blockFace, BlockValue _blockValue, ChannelMask _channelMask, Guid operationId)
    {
        EPaintResult result = EPaintResult.SamePaint;

        // Fixed: Process all channels that are included in the mask (instead of pointless loop to 1)
        for (int channel = 0; channel < MAX_CHANNELS; channel++)
        {
            if (!_channelMask.IncludesChannel(channel))
            {
                continue;
            }

            int currentPaintIdx = GetCurrentPaintIdx(_cc, _blockPos, _blockFace, _blockValue, channel);

            if (_actionData.idx != currentPaintIdx)
            {
                // Store the face to be painted
                if (_facesToPaint.TryGetValue(operationId, out var faceList))
                {
                    faceList.Add(new PaintFaceData(_blockPos, _blockFace, channel));
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

    /// <summary>
    /// Counts paint requirements for area painting (Multiple/Spray modes).
    /// </summary>
    public void CountAreaPaint(World _world, ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, PersistentPlayerData _lpRelative, Vector3 _pos, Vector3 _origin, Vector3 _dir1, Vector3 _dir2, float _radius, Guid operationId)
    {
        _facesToPaint[operationId] = [];

        // Iterate through area grid (matches original game logic)
        for (float x = -_radius; x <= _radius; x += 0.5f)
        {
            for (float y = -_radius; y <= _radius; y += 0.5f)
            {
                var raycastResult = PerformAreaRaycast(_world, _pos, _origin, _dir1, _dir2, x, y);
                if (raycastResult.IsValid)
                {
                    // Fixed: Direct face counting instead of recursive CountPaintBlock call
                    CountPaintFaceForArea(_cc, raycastResult, _actionData, _lpRelative, operationId);
                }
            }
        }
    }

    /// <summary>
    /// Executes area painting using previously stored faces.
    /// </summary>
    public void ExecuteAreaPaint(int _entityId, ItemActionTextureBlockData _actionData, Guid operationId)
    {
        if (!_facesToPaint.TryGetValue(operationId, out var facesToPaint))
        {
            return;
        }

        try
        {
            ExecutePaintingPhase(facesToPaint, _actionData, _entityId, operationId);
        }
        finally
        {
            _facesToPaint.Remove(operationId);
        }
    }

    /// <summary>
    /// Cleanup method to prevent memory leaks
    /// </summary>
    public void CleanupOperation(Guid operationId)
    {
        _facesToPaint.Remove(operationId);
    }

    #region Private Helper Methods

    // Helper struct to group related block data
    private struct BlockData
    {
        public Vector3i blockPos;
        public BlockFace blockFace;
        public BlockValue blockValue;
    }

    // Helper struct for raycast results
    private struct FloodFillRaycastData
    {
        public bool IsValid;
        public Vector3i BlockPos;
        public BlockFace BlockFace;
        public BlockValue BlockValue;
        public Vector3 HitFaceNormal;
    }

    // Helper struct for area raycast results
    private struct AreaRaycastData
    {
        public bool IsValid;
        public Vector3i BlockPos;
        public BlockFace BlockFace;
        public BlockValue BlockValue;
    }

    // Helper struct for block processing results
    private struct BlockProcessResult
    {
        public bool ShouldExpand;
    }

    private BlockData PrepareBlockData(Vector3i blockPos, BlockFace blockFace, BlockValue blockValue, ChunkCluster cc)
    {
        var result = new BlockData { blockPos = blockPos, blockFace = blockFace, blockValue = blockValue };

        // Apply getParentBlock transformation if method exists
        if (s_getParentBlockMethod != null)
        {
            var parameters = new object[] { result.blockValue, result.blockPos, cc };
            s_getParentBlockMethod.Invoke(OriginalTextureBlock, parameters);
            result.blockValue = (BlockValue)parameters[0];
            result.blockPos = (Vector3i)parameters[1];
        }

        return result;
    }

    private bool ValidateBlockCanBePainted(World world, Vector3i blockPos, BlockValue blockValue, PersistentPlayerData lpRelative)
    {
        if (s_checkBlockCanBePaintedMethod == null)
        {
            return true; // If method doesn't exist, assume it can be painted
        }

        return (bool)s_checkBlockCanBePaintedMethod.Invoke(OriginalTextureBlock, new object[] { world, blockPos, blockValue, lpRelative });
    }

    private static bool ValidateBlockSelection(Vector3i blockPos)
    {
        return !BlockToolSelection.Instance.SelectionActive ||
               new BoundsInt(BlockToolSelection.Instance.SelectionMin, BlockToolSelection.Instance.SelectionSize).Contains(blockPos);
    }

    private EPaintResult CountPaintAllFaces(ChunkCluster cc, int entityId, ItemActionTextureBlockData actionData, Vector3i blockPos, BlockValue blockValue, ChannelMask channelMask, Guid operationId)
    {
        int paintedFaces = 0;

        for (int faceIndex = 0; faceIndex <= MAX_BLOCK_FACES - 1; faceIndex++)
        {
            var currentFace = (BlockFace)faceIndex;
            var faceResult = CountPaintFace(cc, entityId, actionData, blockPos, currentFace, blockValue, channelMask, operationId);

            if (faceResult == EPaintResult.NoPaintAvailable)
            {
                return EPaintResult.NoPaintAvailable;
            }

            if (faceResult == EPaintResult.Painted)
            {
                paintedFaces++;
            }
        }

        return paintedFaces > 0 ? EPaintResult.Painted : EPaintResult.SamePaint;
    }

    private static bool ShouldSkipRayPosition(Vector2i rayPosition, Dictionary<Vector2i, bool> visitedRays)
    {
        return visitedRays.ContainsKey(rayPosition);
    }

    private FloodFillRaycastData PerformFloodFillRaycast(World world, Vector3 hitPosition, Vector3 hitFaceNormal, Vector3 dir1, Vector3 dir2, Vector2i rayPosition, WorldRayHitInfo worldRayHitInfo)
    {
        Vector3 origin = hitPosition + hitFaceNormal * 0.2f + rayPosition.x * dir1 + rayPosition.y * dir2;
        Vector3 direction = -hitFaceNormal * 0.3f;
        float magnitude = direction.magnitude;

        if (!Voxel.Raycast(world, new Ray(origin, direction), magnitude, LAYER_MASK, 69, 0f))
        {
            return new FloodFillRaycastData { IsValid = false };
        }

        worldRayHitInfo.CopyFrom(Voxel.voxelRayHitInfo);
        var blockValue = worldRayHitInfo.hit.blockValue;
        var blockPos = worldRayHitInfo.hit.blockPos;

        if (worldRayHitInfo.hitTriangleIdx < 0)
        {
            return new FloodFillRaycastData { IsValid = false };
        }

        var blockFace = GameUtils.GetBlockFaceFromHitInfo(blockPos, blockValue, worldRayHitInfo.hitCollider, worldRayHitInfo.hitTriangleIdx, out _, out var hitFaceNormal2);
        if (blockFace == BlockFace.None)
        {
            return new FloodFillRaycastData { IsValid = false };
        }

        return new FloodFillRaycastData
        {
            IsValid = true,
            BlockPos = blockPos,
            BlockFace = blockFace,
            BlockValue = blockValue,
            HitFaceNormal = hitFaceNormal2.normalized
        };
    }

    private BlockProcessResult ProcessFloodFillBlock(ChunkCluster cc, FloodFillRaycastData raycastData, Dictionary<Vector3i, bool> visitedPositions, Vector3 expectedNormal, int sourcePaint, int channel, ItemActionTextureBlockData actionData, Guid operationId)
    {
        // Check if block was already visited and marked as non-expandable
        if (visitedPositions.TryGetValue(raycastData.BlockPos, out var wasVisited) && !wasVisited)
        {
            return new BlockProcessResult { ShouldExpand = false };
        }

        // Skip if we already processed this block
        if (wasVisited)
        {
            return new BlockProcessResult { ShouldExpand = true };
        }

        // Validate face normal matches expected (from original game logic)
        if ((raycastData.HitFaceNormal - expectedNormal).sqrMagnitude > FACE_NORMAL_TOLERANCE)
        {
            visitedPositions.Add(raycastData.BlockPos, false);
            return new BlockProcessResult { ShouldExpand = false };
        }

        // Check if paint matches source paint
        int currentPaintIdx = GetCurrentPaintIdx(cc, raycastData.BlockPos, raycastData.BlockFace, raycastData.BlockValue, channel);
        if (currentPaintIdx != sourcePaint)
        {
            visitedPositions.Add(raycastData.BlockPos, false);
            return new BlockProcessResult { ShouldExpand = false };
        }

        // Store face for painting and count paint usage
        if (_facesToPaint.TryGetValue(operationId, out var faceList))
        {
            faceList.Add(new PaintFaceData(raycastData.BlockPos, raycastData.BlockFace, channel));
        }

        bool canExpand = ItemTexture.CountPaintUsage(operationId);
        visitedPositions.Add(raycastData.BlockPos, canExpand);

        return new BlockProcessResult { ShouldExpand = canExpand };
    }

    private static void AddAdjacentPositions(Stack<Vector2i> positionsToCheck, Vector2i currentPosition)
    {
        positionsToCheck.Push(currentPosition + Vector2i.down);
        positionsToCheck.Push(currentPosition + Vector2i.up);
        positionsToCheck.Push(currentPosition + Vector2i.left);
        positionsToCheck.Push(currentPosition + Vector2i.right);
    }

    private AreaRaycastData PerformAreaRaycast(World world, Vector3 pos, Vector3 origin, Vector3 dir1, Vector3 dir2, float x, float y)
    {
        Vector3 direction = pos + x * dir1 + y * dir2 - origin;
        const int hitMask = 69;

        if (!Voxel.Raycast(world, new Ray(origin, direction), Range, LAYER_MASK, hitMask, 0f))
        {
            return new AreaRaycastData { IsValid = false };
        }

        WorldRayHitInfo hitInfo = Voxel.voxelRayHitInfo.Clone();
        BlockValue blockValue = hitInfo.hit.blockValue;
        Vector3i blockPos = hitInfo.hit.blockPos;

        BlockFace blockFace = GameUtils.GetBlockFaceFromHitInfo(blockPos, blockValue, hitInfo.hitCollider, hitInfo.hitTriangleIdx, out _, out _);

        if (blockFace == BlockFace.None)
        {
            return new AreaRaycastData { IsValid = false };
        }

        return new AreaRaycastData
        {
            IsValid = true,
            BlockPos = blockPos,
            BlockFace = blockFace,
            BlockValue = blockValue
        };
    }

    private void CountPaintFaceForArea(ChunkCluster cc, AreaRaycastData raycastData, ItemActionTextureBlockData actionData, PersistentPlayerData lpRelative, Guid operationId)
    {
        // Direct face counting for area paint to avoid recursion
        for (int channel = 0; channel < MAX_CHANNELS; channel++)
        {
            if (!actionData.channelMask.IncludesChannel(channel))
            {
                continue;
            }

            int currentPaintIdx = GetCurrentPaintIdx(cc, raycastData.BlockPos, raycastData.BlockFace, raycastData.BlockValue, channel);

            if (actionData.idx != currentPaintIdx)
            {
                if (_facesToPaint.TryGetValue(operationId, out var faceList))
                {
                    faceList.Add(new PaintFaceData(raycastData.BlockPos, raycastData.BlockFace, channel));
                }

                ItemTexture.CountPaintUsage(operationId);
            }
        }
    }

    private static void ExecutePaintingPhase(List<PaintFaceData> facesToPaint, ItemActionTextureBlockData actionData, int entityId, Guid operationId)
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
                actionData.idx,
                entityId,
                (byte)faceData.Channel
            );
        }
    }

    /// <summary>
    /// Proper implementation of getCurrentPaintIdx that matches the original game logic.
    /// This handles the case where GetBlockFaceTexture returns 0 by falling back to the default paint.
    /// </summary>
    private int GetCurrentPaintIdx(ChunkCluster cc, Vector3i blockPos, BlockFace blockFace, BlockValue blockValue, int channel)
    {
        int blockFaceTexture = cc.GetBlockFaceTexture(blockPos, blockFace, channel);
        if (blockFaceTexture != 0)
        {
            return blockFaceTexture;
        }

        return GameUtils.FindPaintIdForBlockFace(blockValue, blockFace, out _, channel);
    }

    #endregion
}

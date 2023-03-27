using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_CsLo;

public static class BlockmapManager
{
    public static Dictionary<Vector2SByte,List<Thing>> blockmap = new Dictionary<Vector2SByte, List<Thing>>();
    public const byte BlockWidth = 8,BlockHeight = 8;

    private static Vector2 blockmapOffset,size;

    public static void InitBlockmap(Vector2 size,Vector2 offset)
    {
        blockmapOffset = offset;
        blockmap.Clear();

        BlockmapManager.size = size;

        for(int x = 0; x < size.X; x++)
            for(int y = 0; y < size.Y; y++)
                blockmap.Add(new Vector2SByte((sbyte)x,(sbyte)y), new List<Thing>());
    }

    public static void UpdateInBlockmap(Thing thing,Vector3 oldPos)
    {
        Vector2SByte placementInMap = new Vector2SByte((sbyte)((thing.GetPosition().X-blockmapOffset.X)/BlockWidth),(sbyte)((thing.GetPosition().Y-blockmapOffset.Y)/BlockHeight));
        Vector2SByte oldPlacementInMap = new Vector2SByte((sbyte)((oldPos.X-blockmapOffset.X)/BlockWidth),(sbyte)((oldPos.Y-blockmapOffset.Y)/BlockHeight));

        if(!blockmap.ContainsKey(oldPlacementInMap) || !blockmap.ContainsKey(placementInMap)) return;

        if(blockmap[oldPlacementInMap].Contains(thing)) blockmap[oldPlacementInMap].Remove(thing);
        if(!blockmap[placementInMap].Contains(thing)) blockmap[placementInMap].Add(thing);
    }
    public static void DebugBlockmap()
    {
        for(int x = 0; x < size.X; x++)
        {
            for(int y = 0; y < size.Y; y++)
            {
                Raylib.DrawRectangleLines(x*BlockWidth*15,y*BlockHeight*15,BlockWidth*15,BlockHeight*15,blockmap[new Vector2SByte((sbyte)x,(sbyte)y)].Count==0?Raylib.WHITE:Raylib.RED);
            }
        }
    }
    public static Thing[] GetBlockContentsFromMapPoint(int mapx, int mapy)
    {
        sbyte bx = (sbyte)((float)(mapx-blockmapOffset.X)/BlockWidth);
        sbyte by = (sbyte)((float)(mapy-blockmapOffset.Y)/BlockWidth);

        if(!blockmap.ContainsKey(new Vector2SByte(bx,by))) return null;

        return blockmap[new Vector2SByte(bx,by)].ToArray();
    }
}
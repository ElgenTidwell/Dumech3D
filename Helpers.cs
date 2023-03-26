using System;
using System.Numerics;
using Raylib_CsLo;

public struct Tex
{
    public Image rawImage;
    public string name;
    public int width,height;
    public Color[,] colors;

    public static Tex FromBitmap(string path)
    {
        Image img = Raylib.LoadImage(path);
        Tex tex = new Tex();
        tex.width = img.width;
        tex.height = img.height;

        tex.colors = new Color[tex.width,tex.height];

        for (int x = 0; x < img.width; x++)
            for (int y = 0; y < img.height; y++)
            {
                Color pixelColor = Raylib.GetImageColor(img, x, y);
                tex.colors[x,y] = pixelColor;
            }

        tex.rawImage = img;

        return tex;
    }
}

public struct Vector2SByte
{
    public sbyte x,y;
    public Vector2SByte(sbyte x, sbyte y)
    {
        this.x = x;
        this.y = y;
    }

    public static implicit operator Vector2(Vector2SByte v)
    {
        return new Vector2(v.x,v.y);
    }
    public static explicit operator Vector2SByte(Vector3 v)
    {
        return new Vector2SByte((sbyte)v.X,(sbyte)v.Y);
    }
    public static Vector2SByte operator + (Vector2SByte a,Vector2SByte b)
    {
        return new Vector2SByte((sbyte)(b.x+a.x),(sbyte)(b.y+a.y));
    }
    public static Vector2SByte operator + (Vector2SByte a,Vector3 b)
    {
        return new Vector2SByte((sbyte)(b.X+a.x),(sbyte)(b.Y+a.y));
    }
    public static Vector2SByte operator + (Vector3 a,Vector2SByte b)
    {
        return new Vector2SByte((sbyte)(b.x+a.X),(sbyte)(b.y+a.Y));
    }
    public static Vector2SByte operator * (Vector2SByte a, float b)
    {
        return new Vector2SByte((sbyte)(a.x*b),(sbyte)(a.y*b));
    }
    public static Vector2SByte operator / (Vector2SByte a, float b)
    {
        return new Vector2SByte((sbyte)(a.x*b),(sbyte)(a.y*b));
    }
}


public struct Map
{
    public int width, length, height;
    public Vector2 blockmapOffset,blockmapSize;
    public int[,,] data;

    public Thing[] things;

    public byte[] texLegend;
}

public struct LegendEntry
{
    string wallTexture,floorTexture,ceilingTexture,lowerTexture,upperTexture;
}

public struct rect
{
    public Vector3 pos, size, velocity;
}

public static class Mths
{
    public const float R2D = 180f/3.1459f;
    public const float D2R = 3.1459f/180f;
    public static T Clamp<T>(T a, T mi, T ma) where T : IComparable
    {
        if(a.CompareTo(mi)<0) return mi;
        if(a.CompareTo(ma)>0) return ma;
        return a;
    }
    public static T[,,] ResizeArray<T>(T[,,] original, int rows, int cols, int lyrs)
    {
        var newArray = new T[lyrs,rows,cols];
        int minRows = Math.Min(rows, original.GetLength(1));
        int minCols = Math.Min(cols, original.GetLength(2));
        int minLyrs = Math.Min(lyrs, original.GetLength(0));

        for(int i = 0; i < minRows; i++)
            for(int j = 0; j < minCols; j++)
                for(int k = 0; k < minLyrs; k++)
                {
                    newArray[k, i, j] = original[k, i, j];
                }

        return newArray;
    }
    public static float MoveTowards(float current, float target, float maxDelta)
	{
		if (MathF.Abs(target - current) <= maxDelta)
		{
			return target;
		}
		return current + MathF.Sign(target - current) * maxDelta;
	}
	public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
	{
		Vector3 a = target - current;
		float magnitude = a.Length();
		if (magnitude <= maxDistanceDelta || magnitude == 0f)
		{
			return target;
		}
		return current + a / magnitude * maxDistanceDelta;
	}
    public static float Lerp(float firstFloat, float secondFloat, float by)
	{
		return firstFloat * (1 - by) + secondFloat * by;
	}
}
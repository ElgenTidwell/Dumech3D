using System;
using System.Numerics;
using Raylib_CsLo;

public abstract class Thinker
{
    public Thing myThing;
    public abstract void T_Think(float deltaTime);
}
public class Thing
{
    protected Thinker thinker;
    protected rect body;
    public float dirX=-1,dirY=0;
    public int id
    {
        get; protected set;
    }

    public Vector2 GetHeading() => new Vector2(dirX,dirY);

    public rect GetBody() => body;
    public void SetBody(rect n) => body=n;

    public void SetVelocity(Vector3 vel) => body.velocity = vel;
    public void SetPosition(Vector3 vel) => body.pos = vel;
    public void SetSize(Vector3 vel) => body.size = vel;
    public void SetSizeX(float vel) => body.size.X = vel;
    public void SetSizeY(float vel) => body.size.Y = vel;
    public void SetSizeZ(float vel) => body.size.Z = vel;

    public Vector3 GetVelocity() => body.velocity;
    public Vector3 GetSize() => body.size;
    public Vector3 GetPosition() => body.pos;

    public Thinker GetThinker() => thinker;

    public static int CompareDistancesToPlayer(Thing t1,Thing t2)
    {
        var plr = Program.instance.activeThings[0];

        float distance1 = (t1.GetPosition().X-plr.GetPosition().X)*(t1.GetPosition().X-plr.GetPosition().X)+
							    (t1.GetPosition().Y-plr.GetPosition().Y)*(t1.GetPosition().Y-plr.GetPosition().Y);

        float distance2 = (t2.GetPosition().X-plr.GetPosition().X)*(t2.GetPosition().X-plr.GetPosition().X)+
							    (t2.GetPosition().Y-plr.GetPosition().Y)*(t2.GetPosition().Y-plr.GetPosition().Y);

        if(distance1 > distance2) return -1;
        if(distance1 < distance2) return 1;
        return 0;
    }
}
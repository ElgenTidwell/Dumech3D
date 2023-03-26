using System;
using System.Numerics;
using Raylib_CsLo;
using Thinkers;

public class PlayerStart : Thing 
{ 
    public PlayerStart() 
    { body = new rect{size = new Vector3(0.5f,0.5f,0.28f)}; thinker = new NullThinker(); thinker.myThing=this; id = 1;} 
}
public class Player : Thing     
{  
    public float planeX=0f,planeY=0.66f,strafeX=-1,strafeY=0,updown,cameraOffset;
    public Player() 
    { body = new rect{size = new Vector3(0.15f,0.15f,0.28f)}; thinker = new p_playermobj(); thinker.myThing=this; id = 0;} 
}
public class Zombie : Thing
{
    public Zombie()
    { body = new rect{size = new Vector3(0.15f,0.15f,0.28f)}; thinker = new p_mobj("zombie"); thinker.myThing = this; id = 2;}
}

public class NullThinker : Thinker { public override void T_Think(float deltaTime){} }
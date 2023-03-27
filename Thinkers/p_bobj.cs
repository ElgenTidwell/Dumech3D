using System;
using System.Numerics;
using Raylib_CsLo;

namespace Thinkers
{
    public class p_bobj : Thinker
    {
        protected float gravity;
        protected bool onGround, wasOnGround;
        public Tex sprite;

        public p_bobj()
        {
            P_RecalculateDir();
        }

        public override void T_Think(float deltaTime)
        {
            P_Move(deltaTime);
        }

        public void P_RecalculateDir()
        {
            if(myThing.id != 0)
            {
                myThing.dirX = MathF.Sin(myThing.angularDirection*Mths.D2R);
                myThing.dirY = MathF.Cos(myThing.angularDirection*Mths.D2R);
            }
        }
        public void P_Move(float deltaTime)
        {
            var hit = new hitInfo{onGround = true};
            var hit2 = new hitInfo{onGround = true};

            if (!onGround)
            {
                gravity -= 12f * deltaTime;
            }

            float velZ = myThing.GetVelocity().Z;
            velZ+=gravity;

            myThing.SetVelocity(new Vector3(myThing.GetVelocity().X,myThing.GetVelocity().Y,velZ*deltaTime));

            rect output;
            Program.instance.CheckWorldCollision(myThing.GetBody(), out output,out hit);
            
            myThing.SetPosition(output.pos);

            myThing.SetVelocity(output.velocity);

            Program.instance.CheckThingCollision(myThing);

            if(hit.ceilingHit) gravity = 0;
            wasOnGround = onGround;
            onGround = hit.onGround;

            P_ApplyVelocity(deltaTime);
        }
        void P_ApplyVelocity(float deltaTime)
        {
            rect updated = myThing.GetBody();
            updated.pos.X += (float)updated.velocity.X;
            updated.pos.Y += (float)updated.velocity.Y;
            updated.pos.Z += (float)updated.velocity.Z;
            updated.pos.Z = MathF.Max(updated.size.Z, updated.pos.Z);
            myThing.SetBody(updated);
        }

        public override void T_Destroy()
        {
        }
    }
}
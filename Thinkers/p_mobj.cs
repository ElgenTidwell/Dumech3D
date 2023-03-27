using System;
using System.Numerics;
using Raylib_CsLo;
using States;

namespace Thinkers
{
    public class p_mobj : p_bobj
    {
        protected float walkspeed = 1;
        bool successfulWalk = true;
        int lastTurn;
        int movecount;
        StateTracker tracker;

        int target;

        float deltaTime;

        public p_mobj(string statePath)
        {
            string fullpath = $"{Program.basePath}/States/{statePath}.wtt";
            tracker = new StateTracker(fullpath,true,true);
            tracker.tick = (short)(pRandom.GetRandom()%3);
            Console.WriteLine(tracker.tick);
            tracker.onSpriteChanged += P_SprChange;
        }
        public override void T_Destroy()
        {
            base.T_Destroy();
            tracker.onSpriteChanged -= P_SprChange;
        }

        public override void T_Think(float deltaTime)
        {
            this.deltaTime = deltaTime;
            tracker.Advance();
            tracker.ComputeDirection(myThing.GetHeading(),myThing.GetPosition());
            sprite = tracker.GetCurrentSprite();
            base.T_Think(deltaTime);
        }

        bool P_Move()
        {
            Vector3 moveDir = new Vector3(myThing.GetHeading().X,myThing.GetHeading().Y,0)*walkspeed*deltaTime;
            rect test = new rect{pos = myThing.GetPosition()+moveDir, size = myThing.GetSize()};
            Program.instance.CheckWorldCollision(test,out rect nr, out hitInfo hit);

            return !(hit.hit || Program.instance.CheckThingCollision(test,myThing));
        }

        void P_TryWalk()
        {
            myThing.SetVelocity(new Vector3(0,0,myThing.GetVelocity().Z));
            Vector3 moveDir = new Vector3(myThing.GetHeading().X,myThing.GetHeading().Y,0)*walkspeed*deltaTime;

            if(!P_Move())
            {
                int turnDir = (successfulWalk?((int)MathF.Sign(pRandom.GetRandom()/127+0.01f))*45:lastTurn);
                lastTurn = turnDir;
                myThing.angularDirection += lastTurn;
                successfulWalk = false;
                P_RecalculateDir();
                return;
            }

            movecount--;

            if(movecount <= 0)
            {
                P_NewChaseDir();
            }

            successfulWalk = true;

            myThing.SetVelocity(moveDir+Vector3.UnitZ*myThing.GetVelocity().Z);
        }
        void P_NewChaseDir()
        {
            movecount = pRandom.GetRandom()&25;

            Vector3 targetPos = Program.instance.activeThings[target].GetPosition();

            float angle = Mths.R2D * MathF.Atan2(targetPos.Y-myThing.GetPosition().Y,targetPos.X-myThing.GetPosition().X);
            if(angle > 90) angle = 450 - angle;
            else           angle = 90  - angle;

            angle = ((int)(angle/45))*45;

            float oldDir = myThing.angularDirection;

            myThing.angularDirection = angle;

            if(!P_Move())
            {
                myThing.angularDirection = oldDir;
            }

            P_RecalculateDir();
        }
        void P_Idle()
        {
            Player p = (Player)Program.instance.activeThings[0];
            if((p.GetPosition()-myThing.GetPosition()).LengthSquared() < 3)
            {
                tracker.MoveToPreDefAction("WALK");
            }
        }
        void P_SprChange()
        {
            switch(tracker.GetCurrentStateAction())
            {
                case State.MONSTER_IDLE:
                P_Idle();
                break;
                case State.MONSTER_CHASE:
                P_TryWalk();
                break;
                case State.MONSTER_ATTACK:
                break;
            }
        }
    }
}
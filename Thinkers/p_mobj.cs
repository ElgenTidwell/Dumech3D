using System;
using System.Numerics;
using Raylib_CsLo;
using States;

namespace Thinkers
{
    public class p_mobj : p_bobj
    {
        protected float walkspeed = 3;
        StateTracker tracker;

        public p_mobj(string statePath)
        {
            string fullpath = $"{Program.basePath}/States/{statePath}.wtt";
            tracker = new StateTracker(fullpath,true,true);
            tracker.tick = (int)((pRandom.GetRandom()/255f)*32);
            tracker.MoveToPreDefAction("WALK");
        }

        public override void T_Think(float deltaTime)
        {
            tracker.Advance();
            tracker.ComputeDirection(myThing.GetHeading(),myThing.GetPosition());
            sprite = tracker.GetCurrentSprite();

            switch(tracker.GetCurrentStateAction())
            {
                case State.MONSTER_IDLE:
                break;
                case State.MONSTER_CHASE:
                P_TryWalk(deltaTime);
                break;
                case State.MONSTER_ATTACK:
                break;
            }

            base.T_Think(deltaTime);
        }

        void P_TryWalk(float dt)
        {

            myThing.SetVelocity(new Vector3(myThing.GetHeading().X,myThing.GetHeading().Y,0)*walkspeed*dt);
        }
    }
}
using System;
using System.Numerics;
using Raylib_CsLo;
using States;

namespace Thinkers
{
    public class p_mobj : p_bobj
    {
        StateTracker tracker;

        public p_mobj(string statePath)
        {
            string fullpath = $"{Program.basePath}/States/{statePath}.wtt";
            tracker = new StateTracker(fullpath,true,true);
        }

        public override void T_Think(float deltaTime)
        {
            tracker.ComputeDirection(myThing.GetHeading(),myThing.GetPosition());
            sprite = tracker.GetCurrentSprite();
            base.T_Think(deltaTime);
        }
    }
}
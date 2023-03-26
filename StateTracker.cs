using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Linq;

namespace States
{
    public class StateTracker
    {
        static Dictionary<string,Dictionary<string,Tex>> allTextures = new Dictionary<string, Dictionary<string, Tex>>(); 

        Dictionary<string,State> states;
        Tex currentSprite;
        bool isDirectional;

        string myHash;

        string currentState;
        int tick = 0;
        public int directionToCamera = -1;

        public StateTracker(string path, bool isDirectional, bool loadAllTextures = false)
        {
            myHash = path;
            Dictionary<string,State> _states = new Dictionary<string,State>();
            foreach(string line in File.ReadAllLines(path))
            {
                if(string.IsNullOrWhiteSpace(line)) continue;
                if(line.StartsWith("#")) continue;
                State state = new State();
                int index = 0;
                foreach(string part in line.Split(' '))
                {
                    index++;
                    //TODO: load state
                    switch(index)
                    {
                        case 1:
                        state.stateName = part;
                        if(currentState == null) currentState = part;
                        break;
                        case 2:
                        state.baseSpriteName = part;
                        break;
                        case 3:
                        state.directional = part == "t"?true:false;
                        break;
                        case 4:
                        state.offset = int.Parse(part);
                        break;
                        case 5:
                        state.stateAction = part;
                        break;
                        case 6:
                        state.nextState = part;
                        break;
                        case 7:
                        state.ticks = int.Parse(part);
                        break;
                    }
                }
                if(loadAllTextures)
                {
                    if(!allTextures.ContainsKey(myHash)) allTextures.Add(myHash,new Dictionary<string, Tex>());
                    if(!isDirectional && !allTextures[myHash].ContainsKey(state.stateName+"0"))
                        allTextures[myHash].Add(state.stateName+"0", GetSpriteAtIndex(state,0));
                    else
                        for(int i = 0; i < 8; i ++) if(!allTextures[myHash].ContainsKey(state.stateName+i))allTextures[myHash].Add(state.stateName+i, GetSpriteAtIndex(state,i));
                }
                _states.Add(state.stateName,state);
            }
            states = _states;
            this.isDirectional = isDirectional;
        }
    
        public void Advance()
        {
            directionToCamera = Mths.Clamp(directionToCamera,0,8);
            tick++;

            //something has gone wrong, or we're looping.
            if(tick >= 250) tick = 1;
            if(tick>states[currentState].ticks)
            {
                LoadCurrentSprite(directionToCamera);
                currentState = states[currentState].nextState;
                tick = 1;
            }
        }

        public void ChangeState(string stateName)
        {
            currentState = stateName;
        }
        public string FindStateByAction(string action)
        {
            return Array.Find(states.Keys.ToArray(),e=>states[e].stateAction == action);
        }

        public void ComputeDirection(Vector2 selfFacing,Vector3 selfPosition)
        {
            Vector3 cameraFacing3 = (Program.instance.activeThings[0].GetPosition()-selfPosition);
            Vector2 cameraFacing = Vector2.Normalize(new Vector2(cameraFacing3.X,cameraFacing3.Y));

            float cos = Vector2.Dot(cameraFacing,selfFacing);
            float angle = Mths.R2D * (MathF.Atan2(selfFacing.X,selfFacing.Y)-MathF.Atan2(cameraFacing.X,cameraFacing.Y));
            if(angle > 90) angle = 450 - angle;
            else           angle = 90  - angle;

            int olddir = directionToCamera;

            directionToCamera = (int)(angle/45f);
            directionToCamera = 7-directionToCamera;

            if(directionToCamera != olddir) LoadCurrentSprite(directionToCamera);
        }
    
        public Tex LoadCurrentSprite(int direction)
        {
            if(allTextures.ContainsKey(myHash) && allTextures[myHash].ContainsKey(currentState+direction))
                currentSprite = allTextures[myHash][currentState+direction];
            else
                currentSprite = GetSpriteAtIndex(currentState,direction);

            return currentSprite;
        }
        public Tex GetCurrentSprite()
        {
            return currentSprite;
        }

        public string GetCurrentStateAction()
        {
            return states[currentState].stateAction;
        }
        public string GetCurrentState()
        {
            return currentState;
        }

        private Tex GetSpriteAtIndex(string i, int direction)
        {
            return Tex.FromBitmap($"{Program.basePath}/Sprites/{states[i].baseSpriteName}{(states[i].directional?direction:0)}{states[i].offset.ToString("000")}.png");
        }
        Tex GetSpriteAtIndex(State state, int direction)
        {
            return Tex.FromBitmap($"{Program.basePath}/Sprites/{state.baseSpriteName}{(state.directional?direction:0)}{state.offset.ToString("000")}.png");
        }
    }

    public struct State
    {
        public const string WEAPON_READY = "WEAPON_RDY";
        public const string WEAPON_PREFIRE = "WEAPON_PFR";
        public const string WEAPON_SHOOT = "WEAPON_FRE";
        public string stateName;
        public string baseSpriteName;
        public bool directional;
        public int offset;
        public string stateAction;
        public string nextState;
        public int ticks;
    }
}
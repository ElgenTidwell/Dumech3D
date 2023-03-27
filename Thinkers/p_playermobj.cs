using System;
using System.Numerics;
using Raylib_CsLo;
using States;

namespace Thinkers
{
    public class p_playermobj : p_bobj
    {
        float totalRot = 0;
        float totalTime = 0;
        float fsin,fsinh;
        float camvel,tcamvel,tcampos;
        Vector2 oldMousePos = Vector2.One*float.NegativeInfinity;
        StateTracker weapon;
        Sound weaponShoot;

        string shootAnim,readyAnim;

        public p_playermobj()
        {
            weapon = new StateTracker($"{Program.basePath}/States/shotgun.wtt", false,true);
            weaponShoot = Raylib.LoadSound($"{Program.basePath}/Sounds/shotgun/shoot.mp3");
            P_ReloadWeaponData();
        }

        void P_ReloadWeaponData()
        {
            // WEAPON_RDY is conventionally first.
            readyAnim = weapon.GetCurrentState();

            shootAnim = weapon.FindStateByAction("WEAPON_PFR");

            weapon.LoadCurrentSprite(0);
        }

        public override void T_Think(float deltaTime)
        {
            weapon.Advance();
            P_Input(deltaTime);
            base.T_Think(deltaTime);
            Player p = (Player)myThing;

            float xoff = Program.screenWidth*0.1f;
            float yoff = Program.screenHeight*-0.1f-(p.cameraOffset-Program.camOffBase-p.GetPosition().Z)*50;

            Program.instance.screenTextures.Add(new Tuple<Vector3, Tex>
                                        (new Vector3(xoff+(fsinh*14f),yoff+(fsin*7f),4f/Program.screenScalar),
                                                                                    weapon.GetCurrentSprite()));
        }

        public void P_Mouseinput()
        {
            if(oldMousePos == Vector2.One*float.NegativeInfinity) oldMousePos = Raylib.GetMousePosition();
            Vector2 newMousePos = Raylib.GetMousePosition();
            Vector2 mouseDelta = newMousePos-oldMousePos;

            Player p = (Player)myThing;

            float mxrotSpeed = -mouseDelta.X*0.002f; //the constant value is in radians/second
            float myrotSpeed = -mouseDelta.Y*0.01f; //the constant value is in radians/second

            totalRot -= mxrotSpeed;

            float _oldDirX = myThing.dirX;
            p.dirX = myThing.dirX * MathF.Cos(mxrotSpeed) - p.dirY * MathF.Sin(mxrotSpeed);
            p.dirY = _oldDirX * MathF.Sin(mxrotSpeed) + p.dirY * MathF.Cos(mxrotSpeed);

            float _oldStrafeX = p.strafeX;
            p.strafeX = p.strafeX * MathF.Cos(-mxrotSpeed) - p.strafeY * MathF.Sin(-mxrotSpeed);
            p.strafeY = _oldStrafeX * MathF.Sin(-mxrotSpeed) + p.strafeY * MathF.Cos(-mxrotSpeed);

            float _oldPlaneX = p.planeX;
            p.planeX = p.planeX * MathF.Cos(mxrotSpeed) - p.planeY * MathF.Sin(mxrotSpeed);
            p.planeY = _oldPlaneX * MathF.Sin(mxrotSpeed) + p.planeY * MathF.Cos(mxrotSpeed);

            p.updown += myrotSpeed * 40 * (Program.screenWidth/Program.screenHeight);

            p.updown = Mths.Clamp(p.updown,-210,210);

            oldMousePos = newMousePos;
        }

        void P_Input(float deltaTime)
        {
            Player p = (Player)myThing;

            totalTime += deltaTime;

            float velX = p.GetBody().velocity.X;
            float velY = p.GetBody().velocity.Y;
            float velZ = p.GetBody().velocity.Z;

            float acceleration = deltaTime*.8f;

            //speed modifiers
            float moveSpeed = deltaTime * 8.0f; //the constant value is in squares/second
            float rotSpeed = deltaTime * 3.0f; //the constant value is in radians/second
            fsin = (MathF.Sin(totalTime*6)*(MathF.Abs(velX)+MathF.Abs(velY))/8)*40;
            fsinh = (MathF.Sin(totalTime*3)*(MathF.Abs(velX)+MathF.Abs(velY))/8)*40;
            //camera stuff!
            tcampos = fsin*.1f+Program.camOffBase+camvel+p.GetPosition().Z;

            p.cameraOffset = Mths.Clamp(Mths.Lerp(p.cameraOffset,tcampos+tcamvel,.25f)-(p.updown*0.0001f),
                                        p.GetPosition().Z-p.GetBody().size.Z,p.GetPosition().Z+p.GetBody().size.Z*2);

            float length = (float)((velX*velX)+(velY*velY));
            float multiplier = 1;

            float mspeedsqr = moveSpeed*moveSpeed;

            if(length > mspeedsqr)
            {
                multiplier = (mspeedsqr)/length;
            }
            velX *= multiplier;
            velY *= multiplier;
            tcamvel = Mths.MoveTowards(tcamvel,velZ*3,deltaTime*5);
            if (onGround)
            {
                gravity = 0;
                if(Raylib.IsKeyDown(KeyboardKey.KEY_SPACE))
                {
                    gravity = 5;
                    tcamvel = -.7f;
                }

                velZ = gravity;
                onGround = false;
            }

            if(Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL))
            {
                if(p.GetSize().Z != 0.1f) 
                {
                    tcamvel = .7f;
                    p.SetSizeZ(0.1f);
                }
            }
            else
            {
                if(p.GetSize().Z != 0.28f) 
                {
                    tcamvel = -.7f;
                    p.SetSizeZ(0.28f);
                    p.SetPosition(p.GetPosition()+Vector3.UnitZ*0.18f);
                }
            }

            Vector3 fin = Mths.MoveTowards(new Vector3((float)velX,(float)velY,0),Vector3.Zero,deltaTime*0.14f);

            velX = fin.X;
            velY = fin.Y;

            if (Raylib.IsKeyDown(KeyboardKey.KEY_W))
            {
                velX += p.dirX * acceleration;
                velY += p.dirY * acceleration;
            }
            if (Raylib.IsKeyDown(KeyboardKey.KEY_S))
            {
                velX += -p.dirX * acceleration;
                velY += -p.dirY * acceleration;
            }
            if (Raylib.IsKeyDown(KeyboardKey.KEY_A))
            {
                velX += p.strafeY * acceleration;
                velY += p.strafeX * acceleration;
            }
            if (Raylib.IsKeyDown(KeyboardKey.KEY_D))
            {
                velX += -p.strafeY * acceleration;
                velY += -p.strafeX * acceleration;
            }

            p.SetVelocity(new Vector3(velX,velY,velZ));

            if(Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT) && weapon.GetCurrentStateAction() == State.WEAPON_READY)
            {
                weapon.ChangeState(shootAnim);
            }
            if(weapon.GetCurrentStateAction() == State.WEAPON_SHOOT)
            {
                Raylib.PlaySound(weaponShoot);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_CsLo;
public class LevelEditor
{
    int activeLayer = 0;
    float zoom = 6f, xoff = 0,yoff = 0;
    Map copy;
    List<Thing> things = new List<Thing>();

    int selectedThing = -1,hoveringThing;
    public void Init(Program p)
    {
        copy = new Map();
        copy.data   = p.currentMap.data;
        copy.width  = p.currentMap.width;
        copy.length = p.currentMap.length;
        copy.height = p.currentMap.height;
        things.Clear();
        things.AddRange(p.currentMap.things);
    }
    public void Apply(Program p)
    {
        copy.things = things.ToArray();
        MapLoader.t_r3m(copy,$"{Program.mapPath}map{p.GetCurrentMapIndex().ToString("00")}.r3m");
    }
    public void DrawLevelEditor()
    {
        Vector2 mousepos = Raylib.GetMousePosition();

        //map expansion buttons
        {
            if(RayGui.GuiButton(new Rectangle(0,0,100,50),"Expand X"))
            {
                copy.width ++;
                copy.data = Mths.ResizeArray(copy.data,copy.width,copy.length,copy.height);
            }
            if(RayGui.GuiButton(new Rectangle(110,0,100,50),"Shrink X"))
            {
                copy.width --;
                copy.data = Mths.ResizeArray(copy.data,copy.width,copy.length,copy.height);
            }

            if(RayGui.GuiButton(new Rectangle(220,0,100,50),"Expand Y"))
            {
                copy.length ++;
                copy.data = Mths.ResizeArray(copy.data,copy.width,copy.length,copy.height);
            }
            if(RayGui.GuiButton(new Rectangle(330,0,100,50),"Shrink Y"))
            {
                copy.length --;
                copy.data = Mths.ResizeArray(copy.data,copy.width,copy.length,copy.height);
            }
            
            if(RayGui.GuiButton(new Rectangle(440,0,100,50),"Expand Z"))
            {
                copy.height ++;
                copy.data = Mths.ResizeArray(copy.data,copy.width,copy.length,copy.height);
            }
            if(RayGui.GuiButton(new Rectangle(550,0,100,50),"Shrink Z"))
            {
                copy.height --;
                copy.data = Mths.ResizeArray(copy.data,copy.width,copy.length,copy.height);
            }
        }

        //Left panel (thing panel)
        {

        }
        
        for(int z = 0; z < copy.height; z++)
        {
            for(int x = 0; x < copy.width; x++)
            {
                for(int y = 0; y < copy.length; y++)
                {					
                    Color color;
                    switch (copy.data[z,x,y])
                    {
                        case 1: color = Raylib.RED; break; //red
                        case 2: color = Raylib.GREEN; break; //green
                        case 3: color = Raylib.BLUE; break; //blue
                        case 4: color = Raylib.WHITE; break; //white
                        default: color = Raylib.YELLOW; break; //yellow
                    }

                    color.a = (byte)(z == activeLayer?255:100);

                    if(copy.data[z,x,y] != 0)
                        Raylib.DrawRectangle((int)((x)*zoom+xoff),(int)((y)*zoom+yoff),(int)zoom,(int)zoom,color);

                    if(mousepos.X < Program.screenWidth-250 || mousepos.Y < 80 || hoveringThing != 0) continue;
                    
                    if(x == (int)((mousepos.X-xoff)/zoom) && y == (int)((mousepos.Y-yoff)/zoom) && z == activeLayer)
                    {
                        Raylib.DrawRectangle((int)((x)*zoom+xoff),(int)((y)*zoom+yoff),(int)zoom,(int)zoom,new Color(255,255,255,150));

                        if(selectedThing >= 0) continue;

                        if(Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT)) copy.data[activeLayer,x,y] = 1;
                        if(Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT)) copy.data[activeLayer,x,y] = 0;
                    }
                }
            }
        }

        Raylib.DrawRectangleLinesEx(new Rectangle(xoff,yoff,zoom*copy.width,zoom*copy.length),2,Raylib.WHITE);

        hoveringThing = 0;

        for(int i = things.Count-1; i >= 0; i--)
        {
            Vector2 offsetPos = new Vector2(things[i].GetPosition().X-things[i].GetBody().size.X,things[i].GetPosition().Y-things[i].GetBody().size.Y);
            Vector2 rectWidth = new Vector2(things[i].GetBody().size.X*2*zoom,things[i].GetBody().size.Y*2*zoom);
            
            Raylib.DrawRectangle((int)((offsetPos.X)*zoom+xoff),(int)((offsetPos.Y)*zoom+yoff),(int)rectWidth.X,(int)rectWidth.Y,new Color(0,255,155,255));
            RayGui.GuiLabel(new Rectangle((int)((offsetPos.X)*zoom+xoff),(int)((offsetPos.Y)*zoom+yoff),100,20),things[i].GetType().ToString());

            if(mousepos.X < (offsetPos.X)*zoom+xoff && mousepos.Y < (offsetPos.Y)*zoom+yoff &&
               mousepos.X > (offsetPos.X)*zoom+xoff-rectWidth.X && mousepos.Y > (offsetPos.Y)*zoom+yoff-rectWidth.Y)
            {
                hoveringThing = i;
            }
        }

        if(Raylib.IsKeyPressed(KeyboardKey.KEY_UP)) activeLayer++;
        if(Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN)) activeLayer--;

        if(Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_MIDDLE))
        {
			Vector2 mouseDelta = Raylib.GetMouseDelta();
            xoff+= mouseDelta.X;
            yoff+= mouseDelta.Y;
        }

        if(selectedThing >= 0)
        {

        }

        zoom += Raylib.GetMouseWheelMove();

        activeLayer = Mths.Clamp(activeLayer,0,copy.height-1);
    }
}
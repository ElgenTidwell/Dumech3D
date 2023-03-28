using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_CsLo;

public static class MapLoader
{
    public static Map r3m(string path)
    {
        Map map = new Map();

        string[] data = File.ReadAllLines(path);

        int mode = -1; //0 map, 1 things
        int lineIndex = 0;

        int activeZLayer = 0, activeYLayer = 0;
        int[,,] mpdatful = new int[0,0,0];
        List<Thing> things = new List<Thing>();

        foreach(string line in data)
        {
            if(string.IsNullOrWhiteSpace(line)) continue;
            if(line.StartsWith('#')) continue;
            if(line == "MPS") { mode = 0; lineIndex = 0; continue; }
            if(line == "THD") { mode = 1; lineIndex = 0; continue; }
            switch(mode)
            {
                case 0: //we're loading the map data
                {
                    if(lineIndex == 0) { map.width  = int.Parse(line); break; }
                    if(lineIndex == 1) { map.length = int.Parse(line); break; }
                    if(lineIndex == 2) 
                    {
                        map.height = int.Parse(line);

                        // we now know the size of the array, initialize it.
                        mpdatful = new int[map.height,map.width,map.length];

                        break;
                    }

                    if(line == "NXL") { activeZLayer++; activeYLayer = 0; break; }
                    if(line == "MPE") 
                    { 
                        mode = -1; 
                        map.data = mpdatful;
                        break; 
                    }
                    
                    int x = 0;
                    foreach(string mpdat in line.Split(','))
                    {
                        mpdatful[activeZLayer,x,activeYLayer] = int.Parse(mpdat);
                        x++;
                    }

                    activeYLayer++;
                }
                break;
                case 1: //we're loading a map object
                {
                    Console.WriteLine("Loading object!");
                    string[] bits = line.Split(' ');
                    int index = int.Parse(bits[0]);

                    var t = IndexToThing(index);
                    t.SetPosition(new Vector3(float.Parse(bits[1]),float.Parse(bits[2]),float.Parse(bits[3])));
                    t.angularDirection = float.Parse(bits[4]);
                    
                    if(index == 1)
                    {
                        things.Insert(0,t);//player start is always first, this is to save speed.
                    }
                    else
                        things.Add(t); //anything else goes after.

                    mode = -1;
                }
                break;
            }
            lineIndex++;
        }
        Console.WriteLine(things.Count);
        map.things = things.ToArray();

        return map;
    }
    public static void t_r3m(Map m, string path)
    {
        //start the file with the 'MPS' keyword, then add a line.
        string file = $"MPS\n";

        //now, we add the dimensions with a line break in-between each
        file += $"{m.width}\n{m.length}\n{m.height}\n";

        //oh boy, now its time to actually give this damned file the data,
        //pump it!
        for(int z = 0; z < m.height; z ++) //loop over all layers..
        {
            for(int y = 0; y < m.length; y ++) //..and the Y width of the map (vertical)..
            {
                for(int x = 0; x < m.width; x++) //..and the X width (horizontal)
                {
                    // all this does is takes the raw numbers and write them out. '0' '1' '2', you passed math.
                    file += m.data[z,x,y].ToString();

                    //then, if this isnt the last number on this row, seperate with a comma.
                    if(x != m.width-1) file += ",";
                }
                file += $"\n"; //after we've done one whole row, add a line break, and go to the next
            }
            file += $"NXL\n"; //after we've done one whole layer, add 'NXL' and a line break, and continue
        }
        file += $"MPE\n"; //jesus, finally we're done.

        //now we just write out the objects, fairly easy
        foreach(Thing thing in m.things)
        {
            //add THD plus a line break,
            //then...       V--The thing id    V-- the x position      V-- the y position      V-- and the z        V-- now the direction
            file += $"THD\n{thing.id} {thing.GetPosition().X} {thing.GetPosition().Y} {thing.GetPosition().Z} {thing.angularDirection}\n";
        }

        //hooplah!
        File.Delete(path);//get rid of the old shit
        
        File.WriteAllText(path,file); //write the new one
    }

    public static Thing IndexToThing(int index)
    {
        switch(index)
        {
            case 1: //player start
            return new PlayerStart();
            case 2: //zombie
            return new Zombie();
        }
        return null;
    }
}
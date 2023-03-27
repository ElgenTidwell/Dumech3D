using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Raylib_CsLo;
using Thinkers;
using MeltySynth;

public class Program
{
    static int sampleRate = 44800;
    static int bufferSize = 4096;
	public static Program instance;
	public static string basePath
	{
		get
		{
			return System.IO.Path.GetDirectoryName($"{System.Reflection.Assembly.GetExecutingAssembly().Location}")+"/res";
		}
	}
	public static string mapPath = $"{basePath}/Maps/";
	public const int screenHeight = 225, screenWidth = 400;
	const int tickrate = 50;
	const float tickLengthInSeconds = 1f/tickrate;
	int mapWidth = 24, mapHeight = 24, mapLayers = 3;
	int[,,] worldMap = new int[0,0,0];
	int texWidth = 64;

	static float timetotick;
	static int gametick;

	public string currentMap;

	int mapindex = 1;

	public int GetCurrentMapIndex() => mapindex;

	public enum PlayState{
		game,
		leveleditor
	};
	public PlayState state;


	public const float camOffBase = 0.3f;
	float cameraOffset = camOffBase;
	float dirX=-1, dirY=0;
	float planeX=0,	planeY=0.66f;//the 2d raycaster version of camera plane
	bool skew = false;
	float updown = 0, devZ = 0;

	bool firstframeskip = true;

	Stopwatch sw = new Stopwatch();

	double lastFrameTime, currentFrameTime;

	float[,] zbuffer = new float[screenWidth,screenHeight];

	Color[,] pixels = new Color[screenWidth,screenHeight];
	public static int screenScalar = 4;

	float deltaTime;

	Tex[] textures;
	
	Player playerObject;

	public List<Thing> activeThings;

	public List<Tuple<Vector3,Tex>> screenTextures = new List<Tuple<Vector3, Tex>>();
	static int waiting = 0;

	public void LoadMap(string path)
	{
		Map map = MapLoader.r3m(path);
		mapWidth  = map.width;
		mapHeight = map.length;
		mapLayers = map.height;
		worldMap  = map.data;
		currentMap = path;
		activeThings = new List<Thing>();
		activeThings.AddRange(map.things);

		instance.playerObject = new Player();
		instance.playerObject.SetPosition(instance.activeThings[0].GetPosition());
		instance.activeThings[0] = instance.playerObject; 

		instance.sw.Stop();
		instance.sw.Reset();
		instance.sw.Start();
		gametick = 0;
		timetotick = 0;

		BlockmapManager.InitBlockmap(new Vector2(MathF.Ceiling(mapWidth/BlockmapManager.BlockWidth)+1,MathF.Ceiling(mapHeight/BlockmapManager.BlockHeight)+1),map.blockmapOffset);
	}

	static unsafe void Main(string[] args)
	{
		Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_UNDECORATED);
		Raylib.InitWindow(screenWidth* screenScalar, screenHeight* screenScalar, "layers!");

		RayGui.GuiLoadStyleDefault();
		RayGui.GuiEnable();

		instance = new Program();
		LevelEditor l = new LevelEditor();
		Raylib.DisableCursor();
		Raylib.InitAudioDevice();

		instance.sw.Start();

		Raylib.SetAudioStreamBufferSizeDefault(bufferSize);

        var stream = Raylib.LoadAudioStream((uint)sampleRate, 16, 2);
        var buffer = new short[2 * bufferSize];

        Raylib.PlayAudioStream(stream);

        var synthesizer = new Synthesizer($"{basePath}/Soundfont/TimGM6mb.sf2", sampleRate);
        var sequencer = new MidiFileSequencer(synthesizer);
        var midiFile = new MidiFile($"{basePath}/Music/track-{instance.mapindex.ToString("00")}.mid");

		Console.WriteLine(basePath);

		string[] textures = Directory.GetFiles($"{basePath}/Textures");

		instance.textures = new Tex[textures.Length];
		for(int i = 0; i < textures.Length; i++)
		{
			instance.textures[i] = Tex.FromBitmap(textures[i]);
			instance.textures[i].name = Path.GetFileNameWithoutExtension(textures[i]);
		}
		instance.texWidth = instance.textures[0].width;

		instance.LoadMap($"{mapPath}map{instance.mapindex.ToString("00")}.r3m");

        sequencer.Play(midiFile, true);
        Raylib.SetTargetFPS(tickrate+5);

		while (!Raylib.WindowShouldClose() && !Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
		{
			if (instance.firstframeskip)
			{
				instance.firstframeskip = false;
				continue;
			}

			switch(instance.state)
			{
				case PlayState.game:

					RunGame(l);

				break;
				
				case PlayState.leveleditor:
				{
					if(Raylib.IsKeyPressed(KeyboardKey.KEY_PERIOD))						
					{
						l.Apply(instance);
						instance.state = PlayState.game;
						Raylib.DisableCursor();
						instance.LoadMap($"{mapPath}map{instance.mapindex.ToString("00")}.r3m");
					}
					Raylib.BeginDrawing();

					Raylib.ClearBackground(Raylib.BLACK);

					l.DrawLevelEditor();

					Raylib.EndDrawing();
				}
				break;
			}
			if (Raylib.IsAudioStreamProcessed(stream))
			{
				sequencer.RenderInterleavedInt16(buffer);
				fixed (short* p = buffer)
				{
					Raylib.UpdateAudioStream(stream, p, bufferSize);
				}
			}
		}

		instance.sw.Stop();
		Raylib.StopAudioStream(stream);
		Raylib.EnableCursor();
		RayGui.GuiDisable();
		Raylib.CloseWindow();
	}

	static void RunGame(LevelEditor l)
	{
		if(Raylib.IsKeyPressed(KeyboardKey.KEY_PERIOD))
		{
			instance.state = PlayState.leveleditor;
			Raylib.EnableCursor();
			l.Init(instance);
		}

		Raylib.BeginDrawing();

		((p_playermobj)(instance.activeThings[0].GetThinker())).P_Mouseinput();

		if(timetotick <= instance.sw.ElapsedMilliseconds/1000f)
		{
			instance.screenTextures.Clear();
			gametick++;
			GameTick(instance);
			float error = instance.sw.ElapsedMilliseconds/1000f-timetotick;
			timetotick = tickLengthInSeconds+instance.sw.ElapsedMilliseconds/1000f+error;
		}
		Vector2 dir = instance.activeThings[0].GetHeading();
		instance.dirX = dir.X;
		instance.dirY = dir.Y;
		instance.updown = ((Player)instance.activeThings[0]).updown;
		instance.planeX = ((Player)instance.activeThings[0]).planeX;
		instance.planeY = ((Player)instance.activeThings[0]).planeY;
		instance.cameraOffset= ((Player)instance.activeThings[0]).cameraOffset;

		instance.SpriteRender();

		for (int x = 0; x < screenWidth; x+=2)
		{
			instance.Raycast(x);
			
			for(int y = 0; y < screenHeight; y++)
			{
				if(instance.screenTextures.Count>0)
				for(int i = instance.screenTextures.Count-1; i >= 0; i --)
				{
					var tex = instance.screenTextures[i];

					if(x > tex.Item1.X && y > tex.Item1.Y && x < tex.Item2.width+tex.Item1.X && y < tex.Item2.height+tex.Item1.Y)
					{
						int xpos = (int)((x-tex.Item1.X));
						int ypos = (int)((y-tex.Item1.Y));

						if(xpos >= tex.Item2.width || ypos >= tex.Item2.height || x < 0 || y < 0) continue;

						Color col = tex.Item2.colors[(int)(xpos),(int)(ypos)];

						if(col.a < 150) continue;

						col.a = 255;

						xpos = (int)((x*tex.Item1.Z));
						ypos = (int)((y*tex.Item1.Z));

						if(xpos >= screenWidth || ypos >= screenHeight || x < 0 || y < 0) continue;

						instance.pixels[xpos,ypos] = col;
					}
				}
				Raylib.DrawRectangle(x*screenScalar,y*screenScalar,screenScalar*2,screenScalar,instance.pixels[x,y]);
				instance.zbuffer[x,y] = 1000;				
				instance.pixels[x,y] = Raylib.DARKGRAY;
			}
		}

		
		foreach(Thing thing in BlockmapManager.GetBlockContentsFromMapPoint((int)instance.activeThings[0].GetPosition().X,(int)instance.activeThings[0].GetPosition().Y))
		{
			Raylib.DrawRectangle((int)(thing.GetPosition().X*15),(int)(thing.GetPosition().Y*15),15,15,Raylib.RED);
		}

		if(Raylib.IsKeyDown(KeyboardKey.KEY_UP)) instance.devZ += 0.01f;
		if(Raylib.IsKeyDown(KeyboardKey.KEY_DOWN)) instance.devZ -= 0.01f;

		//BlockmapManager.DebugBlockmap();

		Raylib.DrawRectangle((int)(instance.activeThings[0].GetPosition().X*15),(int)(instance.activeThings[0].GetPosition().Y*15),15,15,Raylib.GREEN);

		Raylib.DrawFPS(100,100);
		Raylib.EndDrawing();
	}

	private void SpriteRender()
	{
		var plr = activeThings[0];

		foreach(Thing t in activeThings)
		{
			p_bobj bobj = (p_bobj)t.GetThinker();
			if(bobj == null) continue;
			if(bobj.sprite.colors == null) continue;
			if(bobj.sprite.colors.Length == 0) continue;

			float relX = t.GetPosition().X-plr.GetPosition().X;
			float relY = t.GetPosition().Y-plr.GetPosition().Y;

			float invDet = 1f/(planeX*dirY-dirX*planeY);

			float transformX = invDet * (dirY*relX - dirX*relY);
			float transformY = invDet * (-planeY*relX + planeX*relY); //this is more depth than Y

			int spriteScreenX = (int)((screenWidth/2)*(1+transformX/transformY));

			//calculate height of the sprite on screen
			int spriteHeight = (int)MathF.Abs((int)(screenHeight / (transformY))); //using 'transformY' instead of the real distance prevents fisheye

			float z = t.GetPosition().Z+t.GetSize().Z;

			int drawStartY = -spriteHeight/2 + screenHeight/2 + (int)((cameraOffset + devZ - z) * 2 * spriteHeight / 2 + updown);
			if(drawStartY < 0) drawStartY = 0;
			int drawEndY = spriteHeight/2 + screenHeight/2 + (int)((cameraOffset + devZ - z) * 2 * spriteHeight / 2 + updown) - 1;
			if(drawEndY >= screenHeight) drawEndY = screenHeight-1;

			//calculate width of the sprite
			int spriteWidth = (int)MathF.Abs( (int)(screenHeight / (transformY)));
			int drawStartX = -spriteWidth / 2 + spriteScreenX;
			if(drawStartX < 0) drawStartX = 0;
			int drawEndX = spriteWidth / 2 + spriteScreenX;
			if(drawEndX >= screenWidth) drawEndX = screenWidth - 1;

			//loop through every vertical stripe of the sprite on screen
     		for(int stripe = drawStartX; stripe < drawEndX; stripe++)
			{
				int texX = (int)(256 * (stripe - (-spriteWidth / 2 + spriteScreenX)) * bobj.sprite.width / spriteWidth) / 256;
				//the conditions in the if are:
				//1) it's in front of camera plane so you don't see things behind you
				//2) it's on the screen (left)
				//3) it's on the screen (right)
				//4) ZBuffer, with perpendicular distance
				if(transformY > 0 && stripe > 0 && stripe < screenWidth)
				for(int y = drawStartY; y < drawEndY; y++) //for every pixel of the current stripe
				{				
					//pinch top/bottom of screen to give a better illusion of looking up and down
					float deviser = 1;

					if (skew) 
						deviser =  (1 - ((MathF.Abs(updown) / 80) * 0.03f)) * Lerp((screenHeight - y) / ((float)screenHeight * 2f) + 0.5f, y / ((float)screenHeight * 2f) + 0.5f, (updown + 80) / 160) * 0.5f + 0.5f;

					int _x = (int)(stripe * deviser + screenWidth * (1 - deviser) / 2);

					if(_x < 0 || _x >= screenWidth) continue;

					if(zbuffer[_x,y] < transformY) continue;

					float d = (y) * 1 - screenHeight * 0.5f - (int)((cameraOffset + devZ - z) * 2 * spriteHeight / 2 + updown) + spriteHeight*0.5f; //256 and 128 factors to avoid floats

					int texY = (int)((d * bobj.sprite.height) / spriteHeight);

					Color col = bobj.sprite.colors[Mths.Clamp(texX,0,bobj.sprite.width-1),Mths.Clamp(texY,0,bobj.sprite.height-1)];

					if(col.a < 150) continue;
					col.a = 255;

					float addZ = MathF.Abs((t.GetPosition().Z)-cameraOffset)*2;

					zbuffer[_x,y] = transformY+addZ;
					pixels[_x,y] = col;
				}
			}
		}
	}

	struct queuedpixel
	{
		public int x,y,sx,sy;
		public Color color;
		public queuedpixel(int x, int y, int sx, int sy, Color color)
		{
			this.x = x;
			this.y = y;
			this.sx = sx;
			this.sy = sy;
			this.color = color;
		}
	}

	static void GameTick(object _p)
	{
		Program p = (Program)_p;
		p.currentFrameTime = p.sw.ElapsedMilliseconds;

		p.deltaTime = MathF.Abs((float)(p.currentFrameTime - p.lastFrameTime))/1000f;

		foreach(Thing t in p.activeThings)
		{
			Vector3 oldPos = t.GetPosition();
			t.GetThinker().T_Think(p.deltaTime);
			BlockmapManager.UpdateInBlockmap(t,oldPos);
		}

		p.lastFrameTime = p.currentFrameTime;
	}

	bool IsPointSolid(float x, float y, float z)
	{
		if (x < 0 || y < 0 || z < 0 || x >= mapWidth || y >= mapHeight || z >= mapLayers) return false;

		return worldMap[(int)z,(int)x,(int)y] == 0;
	}

	bool PointVsRect(Vector3 p, rect r)
	{
		return (p.X >= r.pos.X - r.size.X && p.Y >= r.pos.Y - r.size.Y && p.Z >= r.pos.Z - r.size.Z
				&& p.X <= r.pos.X + r.size.X && p.Y <= r.pos.Y + r.size.Y && p.Z <= r.pos.Z + r.size.Z);
	}

	hitInfo RectVsRect(rect r1, rect r2)
	{
		hitInfo hit = new hitInfo();

		Vector3 minimum1 = (r1.pos-r1.size*2);
		Vector3 maximum1 = (r1.pos+r1.size*2);
		Vector3 minimum2 = (r2.pos-r2.size*2);
		Vector3 maximum2 = (r2.pos+r2.size*2);

		hit.hit = 
		(
			minimum1.X <= maximum2.X &&
			maximum1.X >= minimum2.X &&
			minimum1.Y <= maximum2.Y &&
			maximum1.Y >= minimum2.Y &&
			minimum1.Z <= maximum2.Z &&
			maximum1.Z >= minimum2.Z
		);

		if(hit.hit)
		{	
			float distx = (r1.pos.X-r2.pos.X);
			float disty = (r1.pos.Y-r2.pos.Y);
			float distz = (r1.pos.Z-r2.pos.Z);
			hit.overlapX = (distx)-(r1.size.X-r2.size.X)*MathF.Sign(distx);
			hit.overlapY = (disty)-(r1.size.Y-r2.size.Y)*MathF.Sign(disty);
			hit.overlapZ = (distz)-(r1.size.Z-r2.size.Z)*MathF.Sign(distz);
		}
		else
		{
			hit.overlapX = 0;
			hit.overlapY = 0;
			hit.overlapZ = 0;
		}
		return hit;
	}


	hitInfo RectVsWorld(rect r)
	{
		float c1x = (r.pos.X+r.velocity.X - r.size.X), c1y = (r.pos.Y+r.velocity.Y - r.size.Y);
		float c2x = (r.pos.X+r.velocity.X + r.size.X), c2y = (r.pos.Y+r.velocity.Y + r.size.Y);
		float c3x = (r.pos.X+r.velocity.X - r.size.X), c3y = (r.pos.Y+r.velocity.Y + r.size.Y);
		float c4x = (r.pos.X+r.velocity.X + r.size.X), c4y = (r.pos.Y+r.velocity.Y - r.size.Y);

		if(c1x<0||c1y<0||c1x>=mapWidth||c1y>=mapHeight) return new hitInfo();
		if(c2x<0||c2y<0||c2x>=mapWidth||c2y>=mapHeight) return new hitInfo();
		if(c3x<0||c3y<0||c3x>=mapWidth||c3y>=mapHeight) return new hitInfo();
		if(c4x<0||c4y<0||c4x>=mapWidth||c4y>=mapHeight) return new hitInfo();

		hitInfo ret = new hitInfo();

		ret.overlapZ = (r.pos.Z+r.velocity.Z-r.size.Z)-((int)(r.pos.Z+r.velocity.Z-r.size.Z))-1;

		float z = r.pos.Z+r.velocity.Z-r.size.Z;
		if(z < 0) z = 0;
		if(z >= mapLayers) z = mapLayers-1;

		if(worldMap[(int)(z),(int)c1x,(int)c1y] != 0) {ret.hit = true; ret.overlapX = ((int)MathF.Round(c1x))-c1x; 
																		ret.overlapY = ((int)MathF.Round(c1y))-c1y; return ret;}
		if(worldMap[(int)(z),(int)c2x,(int)c2y] != 0) {ret.hit = true; ret.overlapX = ((int)MathF.Round(c2x))-c2x; 
																		ret.overlapY = ((int)MathF.Round(c2y))-c2y; return ret;}
		if(worldMap[(int)(z),(int)c3x,(int)c3y] != 0) {ret.hit = true; ret.overlapX = ((int)MathF.Round(c3x))-c3x; 
																		ret.overlapY = ((int)MathF.Round(c3y))-c3y; return ret;}
		if(worldMap[(int)(z),(int)c4x,(int)c4y] != 0) {ret.hit = true; ret.overlapX = ((int)MathF.Round(c4x))-c4x; 
																		ret.overlapY = ((int)MathF.Round(c4y))-c4y; return ret;}
		
		ret.overlapZ = (r.pos.Z+r.velocity.Z+r.size.Z)-((int)(r.pos.Z+r.velocity.Z+r.size.Z))-1;

		z = r.pos.Z+r.velocity.Z+r.size.Z;
		if(z < 0) z = 0;
		if(z >= mapLayers) z = mapLayers-1;

		if(worldMap[(int)(z),(int)c1x,(int)c1y] != 0) {ret.hit = true; ret.overlapX = ((int)MathF.Round(c1x))-c1x; 
																		ret.overlapY = ((int)MathF.Round(c1y))-c1y; return ret;}
		if(worldMap[(int)(z),(int)c2x,(int)c2y] != 0) {ret.hit = true; ret.overlapX = ((int)MathF.Round(c2x))-c2x; 
																		ret.overlapY = ((int)MathF.Round(c2y))-c2y; return ret;}
		if(worldMap[(int)(z),(int)c3x,(int)c3y] != 0) {ret.hit = true; ret.overlapX = ((int)MathF.Round(c3x))-c3x; 
																		ret.overlapY = ((int)MathF.Round(c3y))-c3y; return ret;}
		if(worldMap[(int)(z),(int)c4x,(int)c4y] != 0) {ret.hit = true; ret.overlapX = ((int)MathF.Round(c4x))-c4x; 
																		ret.overlapY = ((int)MathF.Round(c4y))-c4y; return ret;}

		return ret;
	}

	bool RayVsRect(Ray ray, rect target, Vector3 contactPoint, Vector2 normal, float t_hit_near)
	{
		Vector3 t_near, t_far;
		t_near = (target.pos-target.size - ray.position)/ray.direction;
		t_far = (target.pos+target.size - ray.position)/ray.direction;

		if(t_near.X > t_far.X) { float old = t_near.X; t_near.X = t_far.X; t_far.X = old; }
		if(t_near.Y > t_far.Y) { float old = t_near.Y; t_near.Y = t_far.Y; t_far.Y = old; }

		if (t_near.X > t_far.Y || t_near.Y > t_far.X) return false;

		t_hit_near = MathF.Max(t_near.X, t_near.Y);
		float t_hit_far = MathF.Max(t_far.X, t_far.Y);

		if (t_hit_far < 0) return false;

		contactPoint = ray.position + t_hit_near * ray.direction;

		if(t_near.X > t_near.Y)
		{
			normal = new Vector2(ray.direction.X > 0 ? 1 : -1, 0);
		}
		else
		{
			normal = new Vector2(0, ray.direction.Y > 0 ? 1 : -1);
		}

		return true;
	}

	public void CheckWorldCollision(rect rect, out rect nr, out hitInfo hitInf)
	{
		//first, lets find if we're colliding with any tiles.
		Vector3 minimum = (rect.pos-rect.size*2);
		Vector3 maximum = (rect.pos+rect.size*2);

		int tileMinX = (int)MathF.Floor(minimum.X), tileMaxX = (int)MathF.Ceiling(maximum.X);
		int tileMinY = (int)MathF.Floor(minimum.Y), tileMaxY = (int)MathF.Ceiling(maximum.Y);
		int tileMinZ = (int)MathF.Floor(minimum.Z), tileMaxZ = (int)MathF.Ceiling(maximum.Z);

		nr = rect;
		bool onGround = rect.pos.Z+rect.velocity.Z*deltaTime <= rect.size.Z;
		hitInf = new hitInfo();
		for(int x = tileMinX; x <= tileMaxX; x++)
		{
			for(int y = tileMinY; y <= tileMaxY; y++)
			{
				for(int z = tileMinZ; z <= tileMaxZ; z++)
				{
					if(x < 0 || y < 0 || z < 0 || x >= mapWidth || y >= mapHeight || z >= mapLayers) continue;

					hitInfo info = RectVsWorld(new rect{pos = rect.pos,size=rect.size,velocity=new Vector3(rect.velocity.X,0,0)});

					if(worldMap[z,x,y] != 0 && info.hit)
					{
						{nr.pos.X += info.overlapX*nr.velocity.X+(info.overlapX*0.1f); nr.velocity.X = 0;}
					}

					info = RectVsWorld(new rect{pos = rect.pos,size=rect.size,velocity=new Vector3(0,rect.velocity.Y,0)});

					if(worldMap[z,x,y] != 0 && info.hit)
					{
						{nr.pos.Y += info.overlapY*nr.velocity.Y+(info.overlapY*0.1f); nr.velocity.Y = 0;}
					}

					info = RectVsWorld(new rect{pos = rect.pos,size=rect.size+new Vector3(0,0,0.1f),velocity=new Vector3(0,0,rect.velocity.Z)});

					if(worldMap[z,x,y] != 0 && info.hit)
					{
						if(nr.velocity.Z-0.001f < 0) onGround = true;
						else                  info.ceilingHit = true;

						{nr.velocity.Z = 0;}
					}
					info.onGround = onGround;

					hitInf = info;
				}
			}
		}
	}
	public void CheckThingCollision(Thing self)
	{
		rect rect = self.GetBody();
		rect nr = rect;
		foreach(Thing thing in BlockmapManager.GetBlockContentsFromMapPoint((int)self.GetPosition().X,(int)self.GetPosition().Y))
		{
			rect nr2 = thing.GetBody();
			hitInfo info = RectVsRect(new rect{pos = rect.pos,size=rect.size,velocity=new Vector3(rect.velocity.X,0,0)},thing.GetBody());
			if(thing != self)
			{
				if(info.hit)
				{
					{nr.pos.X += (info.overlapX)*MathF.Abs(nr.velocity.X); nr.velocity.X = 0;}
				}

				info = RectVsRect(new rect{pos = rect.pos,size=rect.size,velocity=new Vector3(0,rect.velocity.Y,0)},thing.GetBody());

				if(info.hit)
				{
					{nr.pos.Y += (info.overlapY)*MathF.Abs(nr.velocity.Y); nr.velocity.Y = 0;}
				}

				info = RectVsRect(new rect{pos = rect.pos,size=rect.size+new Vector3(0,0,0.1f),velocity=new Vector3(0,0,rect.velocity.Z)},thing.GetBody());

				if(info.hit)
				{
					{nr.pos.Z += (info.overlapZ)*MathF.Abs(nr.velocity.Z); nr.velocity.Z = 0;}
				}

				self.SetBody(nr);
			}
		}
	}
	public static float MoveTowards(float current, float target, float maxDelta)
	{
		if (MathF.Abs(target - current) <= maxDelta)
		{
			return target;
		}
		return current + MathF.Sign(target - current) * maxDelta;
	}
	public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
	{
		Vector3 a = target - current;
		float magnitude = a.Length();
		if (magnitude <= maxDistanceDelta || magnitude == 0f)
		{
			return target;
		}
		return current + a / magnitude * maxDistanceDelta;
	}

	void Raycast(int x)
	{
		for (int z = 0; z < mapLayers; z++)
		{
			SRay(x,z);
		}

		waiting --;
	}

	void SRay(int x,int z)
	{
		//calculate ray position and direction
		float cameraX = (2 * x / (float)screenWidth - 1); //x-coordinate in camera space
		float rayDirX = dirX + planeX * cameraX;
		float rayDirY = dirY + planeY * cameraX;

		//which box of the map we're in
		int mapX = (int)(activeThings[0].GetPosition().X);
		int mapY = (int)(activeThings[0].GetPosition().Y);

		//length of ray from current position to next x or y-side
		float sideDistX;
		float sideDistY;

		float rayDist;

		//length of ray from one x or y-side to next x or y-side
		float deltaDistX = (rayDirX == 0) ? (float)0 : MathF.Abs(1 / rayDirX);
		float deltaDistY = (rayDirY == 0) ? (float)0 : MathF.Abs(1 / rayDirY);
		float perpWallDist;

		int steps = 0;

		//what direction to step in x or y-direction (either +1 or -1)
		int stepX;
		int stepY;

		int hit = 0; //was there a wall hit?
		int wasHit = 0;
		int side = 0; //was a NS or a EW wall hit?

		bool backSide;

		//calculate step and initial sideDist
		if (rayDirX < 0)
		{
			stepX = -1;
			sideDistX = (activeThings[0].GetPosition().X - mapX) * deltaDistX;
			rayDist = sideDistX;
		}
		else
		{
			stepX = 1;
			sideDistX = (mapX + 1.0f - activeThings[0].GetPosition().X) * deltaDistX;
			rayDist = sideDistX;
		}
		if (rayDirY < 0)
		{
			stepY = -1;
			sideDistY = (activeThings[0].GetPosition().Y - mapY) * deltaDistY;
			rayDist = sideDistY;
		}
		else
		{
			stepY = 1;
			sideDistY = (mapY + 1.0f - activeThings[0].GetPosition().Y) * deltaDistY;
			rayDist = sideDistY;
		}

		if(z < 0 || z >= mapLayers) return;

		int lastX = mapX,lastY = mapY;
		float lastRayDist= 0;

		wasHit = worldMap[z, Mths.Clamp(mapX,0,mapWidth-1), Mths.Clamp(mapY,0,mapHeight-1)];

		float lastLineHeight = (int)(cameraOffset) > z ? screenHeight:0;

		while (steps<140 && !(hit != 0 && (int)(cameraOffset) == z))
		{
			lastX = mapX;
			lastY = mapY;
			//jump to next map square, either in x-direction, or in y-direction
			if (sideDistX < sideDistY)
			{
				rayDist = sideDistX;
				sideDistX += deltaDistX;
				mapX += stepX;
				backSide = stepX < 0;
				side = 0;
			}
			else
			{
				rayDist = sideDistY;
				sideDistY += deltaDistY;
				mapY += stepY;
				backSide = stepY < 0;

				side = 1;
			}
			steps++;

			if (mapX < -1 || mapY < -1 || mapX > mapWidth || mapY > mapHeight) return;

			int checkX = mapX,checkY = mapY;
			bool renderingouter;

			if((mapX == mapWidth || mapY == mapHeight) || (mapX == -1 || mapY == -1))
			{
				hit = 0;
				checkX = Mths.Clamp(mapX,0,mapWidth-1);
				checkY = Mths.Clamp(mapY,0,mapHeight-1);
				renderingouter = true;
			}else
			{
				hit = worldMap[z,mapX, mapY];
				renderingouter = false;
			}

			if(hit != 0) lastRayDist = rayDist;

			if ((wasHit != hit))
			{
				if (side == 0) perpWallDist = -(sideDistX - deltaDistX);
				else perpWallDist = -(sideDistY - deltaDistY);

				//Calculate height of line to draw on screen
				float flineHeight = (screenHeight / perpWallDist);
				int lineHeight = (int)flineHeight;
				//calculate lowest and highest pixel to fill in current stripe
				int drawStart = -lineHeight / 2 + screenHeight / 2 + (int)((0.5f-cameraOffset + devZ + z) * 2 * flineHeight / 2 + updown);

				int drawEnd = lineHeight / 2 + screenHeight / 2 + (int)((0.5f-cameraOffset + devZ + z) * 2 * flineHeight / 2 + updown) - 1;

				//texturing calculations
				int texNum = worldMap[z,checkX, checkY] - 1; //1 subtracted from it so that texture 0 can be used!
				texNum = Mths.Clamp(texNum,0,textures.Length);

				//calculate value of wallX
				float wallX; //where exactly the wall was hit
				if (side == 0) wallX = activeThings[0].GetPosition().Y - perpWallDist * rayDirY;
				else wallX = activeThings[0].GetPosition().X - perpWallDist * rayDirX;
				wallX -= MathF.Floor(wallX);

				//x coordinate on the texture
				int texX = (int)(wallX * (float)(texWidth));
				if (side == 0 && rayDirX > 0) texX = texWidth - texX - 1;
				if (side == 1 && rayDirY < 0) texX = texWidth - texX - 1;

				//if (drawEnd >= screenHeight - 1) drawEnd = screenHeight - 2;

				float distance = rayDist / 10;

				if (distance <= 1) distance = 1f;

				//pixels[x, drawStart] = drawStart < drawEnd ? Color.MAGENTA:Color.BLACK;

				Vector2 intersection = new Vector2(activeThings[0].GetPosition().X + rayDirX * (rayDist - 0.1f), activeThings[0].GetPosition().Y + rayDirY * (rayDist - 0.1f));

				// How much to increase the texture coordinate per screen pixel
				double step = 1.0 * texWidth / lineHeight;
				// Starting texture coordinate
				double texPos = (drawStart - (int)((0.5f-cameraOffset + devZ + z) * 2 * flineHeight / 2 + updown) - screenHeight / 2 + lineHeight / 2) * step;

				float finalEnd = 0, finalStart = 0;
				float ceilz = 0;

				bool bottom = false,drawcaps = true;
				//choose wall color
				Color color;
				int maintex = worldMap[z, checkX, checkY];

				if((int)(cameraOffset) <= z)
				{
					bottom = true;
					finalEnd = drawStart+1;
					//drawcaps = (z <= 0 || worldMap[z-1,checkX,checkY] == 0) || renderingouter;
					//rendering the backmost part of a wall
					if(wasHit != 0) 
					{
						finalEnd = lastLineHeight;
						ceilz = 0f;
						//lastRayDist = rayDist;
					}
					else
					{
						lastLineHeight = drawEnd;
					}

					finalStart = drawStart;
				}
				else if((int)(cameraOffset) >= z)
				{
					bottom = false;
					//drawcaps = (z > mapLayers-2 || worldMap[z+1,checkX,checkY] == 0) || renderingouter;
					//rendering the backmost part of a wall
					if(wasHit != 0)
					{
						finalStart = lastLineHeight;
						ceilz = 1;
						//lastRayDist = rayDist;
					}
					else
					{
						lastLineHeight = drawEnd;
					}
					
					finalEnd = drawEnd;
				}

				void DrawY(int y,bool skipShade,float dist, bool wall = true,float distalongwall = 1)
				{
					//pinch top/bottom of screen to give a better illusion of looking up and down
					float deviser = 1;

					if (skew) 
						deviser =  (1 - ((MathF.Abs(updown) / 80) * 0.03f)) * Lerp((screenHeight - y) / ((float)screenHeight * 2f) + 0.5f, y / ((float)screenHeight * 2f) + 0.5f, (updown + 80) / 160) * 0.5f + 0.5f;

					int _x = (int)(((float)x * deviser + (float)screenWidth * (1f - deviser) / 2f));

					if (_x < 0 || _x >= screenWidth || (wall && (zbuffer[_x, y] < dist)))
					{
						texPos += step;
						return;
					}
					
					if(wall)
					{
						if(texNum < 0) texNum = 0;
						if(texNum >= textures.Length) texNum = textures.Length-1;

						// Cast the texture coordinate to integer, and mask with (texHeight - 1) in case of overflow
						int texY = (int)texPos & (texWidth - 1);
						texPos += step;

						if(texX < 0) texX = 0;
						if(texX >= texWidth) texX = texWidth;
						if(texY < 0) texY = 0;
						if(texY >= texWidth) texY = texWidth;
						
						color = textures[texNum].colors[texX,texY];
					}
					else
					{

						// rayDir for leftmost ray (x = 0) and rightmost ray (x = w)
						float rayDirX0 = dirX - planeX;
						float rayDirY0 = dirY - planeY;
						float rayDirX1 = dirX + planeX;
						float rayDirY1 = dirY + planeY;

						// Current y position compared to the center of the screen (the horizon)
						int p = (int)(y-updown) - screenHeight / 2;

						// Vertical position of the camera.
						float posZ = ((cameraOffset + devZ) - (z+(bottom?0:1)))*(screenHeight*0.5f);

						// Horizontal distance from the camera to the floor for the current row.
						float rowDistance = posZ / p;

						// calculate the real world step vector we have to add for each x (parallel to camera plane)
						// adding step by step avoids multiplications with a weight in the inner loop
						float floorStepX = rowDistance * (rayDirX1 - rayDirX0) / screenWidth;
						float floorStepY = rowDistance * (rayDirY1 - rayDirY0) / screenWidth;

						// real world coordinates of the leftmost column. This will be updated as we step to the right.
						float floorX = activeThings[0].GetPosition().X/2 + rowDistance * rayDirX0;
						float floorY = activeThings[0].GetPosition().Y/2 + rowDistance * rayDirY0;

						floorX += floorStepX*x;
						floorY += floorStepY*x;

						// the cell coord is simply got from the integer parts of floorX and floorY
						int cellX = (int)(floorX);
						int cellY = (int)(floorY);

						// get the texture coordinate from the fractional part
						int tx = (int)(texWidth*2 * (floorX - cellX)) & (texWidth - 1);
						int ty = (int)(texWidth*2 * (floorY - cellY)) & (texWidth - 1);
						
						tx = Mths.Clamp(tx,0,texWidth);
						ty = Mths.Clamp(ty,0,texWidth);

						color = textures[texNum].colors[tx,ty];
					}
					if(zbuffer[_x, y] < dist) return;

					if (y < 0) return;
					if (y >= screenHeight) return;

					float sideMulti = !skipShade && side == 1 ? .5f : (skipShade)?(bottom?.25f:1):.75f;
					sideMulti *= distalongwall;

					Color col = new Color((byte)(color.r *  sideMulti), (byte)(color.g * sideMulti), (byte)(color.b * sideMulti), color.a);
					//if(!wall)col = new Color((byte)(dist), (byte)(dist), (byte)(dist), color.a);

					float totalLightOnPixel = 1;

					if(screenScalar == 1)
					{
						Raylib.DrawPixel(_x,y, new Color((byte)(col.r * totalLightOnPixel), (byte)(col.g * totalLightOnPixel), (byte)(col.b * totalLightOnPixel), (byte)255));
					}
					else
						pixels[_x, y] = new Color((byte)(col.r * totalLightOnPixel), (byte)(col.g * totalLightOnPixel), (byte)(col.b * totalLightOnPixel), color.a);

					zbuffer[_x, y] = dist;
				}

				void DrawFloor(int y,int fromz,float dist)
				{
					zbuffer[x, y] = 1000;
					// rayDir for leftmost ray (x = 0) and rightmost ray (x = w)
					float rayDirX0 = dirX - planeX;
					float rayDirY0 = dirY - planeY;
					float rayDirX1 = dirX + planeX;
					float rayDirY1 = dirY + planeY;

					// Current y position compared to the center of the screen (the horizon)
					int p = (int)(y-updown) - screenHeight / 2;

					// Vertical position of the camera.
					float posZ = ((cameraOffset + devZ) - fromz)*(screenHeight*0.5f);

					// Horizontal distance from the camera to the floor for the current row.
					// 0.5 is the z position exactly in the middle between floor and ceiling.
					float rowDistance = posZ / p;

					// calculate the real world step vector we have to add for each x (parallel to camera plane)
					// adding step by step avoids multiplications with a weight in the inner loop
					float floorStepX = rowDistance * (rayDirX1 - rayDirX0) / screenWidth;
					float floorStepY = rowDistance * (rayDirY1 - rayDirY0) / screenWidth;

					// real world coordinates of the leftmost column. This will be updated as we step to the right.
					float floorX = activeThings[0].GetPosition().X/2 + rowDistance * rayDirX0;
					float floorY = activeThings[0].GetPosition().Y/2 + rowDistance * rayDirY0;

					floorX += floorStepX*x;
					floorY += floorStepY*x;

					// the cell coord is simply got from the integer parts of floorX and floorY
					int cellX = (int)(floorX);
					int cellY = (int)(floorY);

					// get the texture coordinate from the fractional part
					int tx = (int)(texWidth*2 * (floorX - cellX)) & (texWidth - 1);
					int ty = (int)(texWidth*2 * (floorY - cellY)) & (texWidth - 1);
					
					color = textures[texNum].colors[tx,ty];
					Color col = new Color((byte)(color.r / dist), (byte)(color.g / dist), (byte)(color.b / dist), color.a);
					//col = new Color((byte)(dist), (byte)(dist), (byte)(dist), color.a);

					float totalLightOnPixel = 1;

					if(screenScalar == 1)
					{
						Raylib.DrawPixel(x,y, new Color((byte)(col.r * totalLightOnPixel), (byte)(col.g * totalLightOnPixel), (byte)(col.b * totalLightOnPixel), (byte)255));
					}
					else
						Raylib.DrawRectangle(x*screenScalar,y*screenScalar,screenScalar*2,screenScalar,new Color((byte)(col.r * totalLightOnPixel), (byte)(col.g * totalLightOnPixel), (byte)(col.b * totalLightOnPixel), color.a));
				}

				if(2-drawEnd > 0)
				texPos += step*(2-drawEnd);
				Vector3 ppos = instance.activeThings[0].GetPosition();

				//draw the pixels of the stripe as a vertical line
				for (int y = 0; y < screenHeight-1; y++)
				{

					// if(y > screenHeight/2+updown)
					// {
					// 	texNum = 0;
					// 	DrawFloor(y,0,((screenHeight-(y-updown)))/((float)screenHeight/2f));
					// }

					if(y >= drawEnd && y <= drawStart && wasHit == 0)
					{
						texNum = maintex-1;
						float lerpalong = Mths.Clamp(MathF.Abs(((float)(drawStart-drawEnd)-(float)(y-drawEnd))/(drawStart-drawEnd)),0,1);

						float zval = rayDist+MathF.Abs((z)-cameraOffset)*2;

						DrawY(y,false,zval,true);
					}
					
					if(y >= finalEnd && y <= finalStart && drawcaps)
					{
						texNum = wasHit - 1;

						float zfromy = (MathF.Abs((screenHeight/2)-((y-screenHeight)-updown))/screenHeight)*lastRayDist;
						float zval = MathF.Abs((z+ceilz)-cameraOffset)*2 + rayDist;
						DrawY(y,true,zval,false);
					}
				}
			}
			wasHit = hit;
		}
	}
	RayHit TraceRay(Vector2 start, Vector2 end, float maxDist = -1)
	{
		float rayDirX = Vector2.Normalize(end-start).X;
		float rayDirY = Vector2.Normalize(end-start).Y;

		//which box of the map we're in
		int mapX = (int)(start.X);
		int mapY = (int)(start.Y);

		//length of ray from current position to next x or y-side
		float sideDistX;
		float sideDistY;

		float rayDist;

		//length of ray from one x or y-side to next x or y-side
		float deltaDistX = (rayDirX == 0) ? (float)0 : MathF.Abs(1 / rayDirX);
		float deltaDistY = (rayDirY == 0) ? (float)0 : MathF.Abs(1 / rayDirY);

		int steps = 0;

		//what direction to step in x or y-direction (either +1 or -1)
		int stepX;
		int stepY;

		int hit = 0; //was there a wall hit?

		//calculate step and initial sideDist
		if (rayDirX < 0)
		{
			stepX = -1;
			sideDistX = (start.X - mapX) * deltaDistX;
			rayDist = sideDistX;
		}
		else
		{
			stepX = 1;
			sideDistX = (mapX + 1.0f - start.X) * deltaDistX;
			rayDist = sideDistX;
		}
		if (rayDirY < 0)
		{
			stepY = -1;
			sideDistY = (start.Y - mapY) * deltaDistY;
			rayDist = sideDistY;
		}
		else
		{
			stepY = 1;
			sideDistY = (mapY + 1.0f - start.Y) * deltaDistY;
			rayDist = sideDistY;
		}

		float md = (maxDist < 0 ? Vector2.Distance(start, end) : maxDist);

		//perform DDA
		while (hit == 0 && rayDist < md)
		{
			//jump to next map square, either in x-direction, or in y-direction
			if (sideDistX < sideDistY)
			{
				rayDist = sideDistX;
				sideDistX += deltaDistX;
				mapX += stepX;
			}
			else
			{
				rayDist = sideDistY;
				sideDistY += deltaDistY;
				mapY += stepY;
			}
			steps++;

			if (mapX < 0 || mapY < 0 || mapX >= mapWidth || mapY >= mapWidth) continue;

			//Check if ray has hit a wall
			hit = worldMap[0,mapX, mapY];
		}
		return new RayHit {mapval=hit,mapx=mapX,mapy=mapY,posx=start.X+rayDirX*rayDist, posy = start.Y + rayDirY * rayDist };
	}

	struct RayHit
	{
		public int mapx, mapy;
		public float posx, posy;
		public int mapval;
	}

	float Lerp(float firstFloat, float secondFloat, float by)
	{
		return firstFloat * (1 - by) + secondFloat * by;
	}
}
public struct hitInfo
{
	public bool hit,onGround,ceilingHit;
	public float overlapX,overlapY,overlapZ;
}
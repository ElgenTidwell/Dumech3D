# Dumech3D

A 3D engine based around wolf-3d esc. raycasting tech. If this sounds kind of hacked together, it kinda is!

I'm just one person, and not the most clever at that, so if anyone is able to provide assistance with this dumb and weird project, please do!

# Features

Features a fully 3D based collision and map system, and I'm working on functionality for map objects and weapons and stuff.

Also features some very jank and unfinished dev tools.

# TO-DO

* Better depth calculation for ceilings and floors
* Finish up p_mobj AI
* Multithreaded rendering if possible (yes, this is all software!)
* Finish dev tools
	* Add Thing panel in level editor for placing map objects
	* State file (wtt) editor
* Improve culling & rendering
* Allow weapon & monster stats to be defined in the wtt
* Proper 3D Raycasting throughout the map for weapons & line of sight stuff.

# How do I use this?

Well, for making games with this I highly discourage you from that idea for the time being, this is nowhere near a completed state.
When it becomes more stable I'll make some Wiki pages.

For accessing the dev tools and whatnot, for now the key to enter these is '.'

For where all of the files and stuff are, most of the rendering and backend stuff is the 'Program.cs' file, most of the others are
self explanitory.

The 'Thinkers' folder contains all of the map object thinkers I've developed so far.

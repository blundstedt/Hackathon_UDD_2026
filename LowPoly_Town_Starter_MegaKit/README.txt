Low Poly Town Starter MegaKit v1.1.4 - Standard edition
======================================================

FREE (name your own price, from $0): 30 hand-picked assets from every family, GLB / FBX / OBJ, 4 color palettes. The Facades, Assembly, ground, sky dome and neon halo pieces are in Complete / Pro.

Low poly town kit for games, prototypes and renders. Standard GLB / FBX / OBJ files, tested with Godot 4.7 and
Blender 5.2 (other engines: not tested).
4 x 4 m road tiles (straights, corners, T, crossroads, crosswalks, roundabout, dead end, bus stop, parking,
transition to a country road), raised sidewalks with curbs, railway tiles and a station, a MODULAR building kit
(brick / plaster / siding / concrete walls, shopfronts, roofs, awnings, balconies, fire escapes, rooftop props),
ready-made houses, apartments, offices and shops (barber, cafe, bakery, pharmacy, laundromat, convenience store,
diner, gas station, garage, fire station, police station, bus and train stations), vehicles with separate wheels,
street furniture (lamps, traffic lights, signs without text, benches, bins, bus shelter...), urban nature and, since
v1.1.1, an upgraded facade kit (Complete / Pro) (arched brick facades, big storefront, classical column and portico, wide
entrance steps, stone base, large cornice, brick pillar, brick-and-iron fence, stone curbs) and walk-in assembly
pieces (Complete / Pro) (brick walls with a finished inside face, outer corners, floor with a stair opening, stair
railings, partition posts) to build buildings you can walk into.
New in v1.1.4 ("what you see is what you get"): the ground of the images (160 m grass ground), 2 sky domes (day / night),
3 low poly clouds, 23 neon halos and a round display base, 22 ready-made buildings (a red-brick town hall with a white
4-column portico, pediment and stone steps; 4 brick tenements with black iron fire escapes, stone sills and lintels,
bracketed cornices and shopfronts; an Art Deco setback tower with a spire; 8 colorful blocks with arched windows,
wrought-iron balconies, cornices and shopfronts with awnings; a red fire station with a hose tower; 3 glass towers; a grey
office block; 2 balcony apartment blocks; a brick hall with a clock tower), 6 fire escape pieces (landing, stair flight,
drop ladder, 4 / 5 / 6-storey stacks), a park gate with brick piers and lanterns, a striped lawn tile and 7 playground
pieces (swings, slide, seesaw, climbing frame, merry-go-round, sandbox, spring rider)
(Complete / Pro).
Objects only: no characters, no brands, no readable text.
Part of the "Amano Props" series: same scale and same 2 m grid in every pack (4 m tiles = Terrain Kit tiles).

CONTENTS
  Models/GLB/   glTF binary (recommended: Godot 4)
  Models/FBX/   FBX files for Blender and other 3D tools - the palette texture sits next to the models and loads automatically
  Models/OBJ/   OBJ + MTL
  Textures/     props_palette.png + props_emission.png (glow map)
                + 3 alternative palettes with their glow maps: _night / _pastel / _winter
  Docs/         STYLE_GUIDE.txt + Palette_Chart.png + RENDER_SETTINGS.txt (sky, lights, fog, glow values)

NOT INCLUDED
  - The ground, sky domes, neon halos and the display base (new in v1.1.4) are in the Complete and Pro editions;
    the Godot demo scenes, the Environment files and the .blend file are in the Pro edition ONLY.
  - The engine's bloom post-process and the 2D layout of the contact sheets are not models. No characters, no drivers.

CHANGELOG
  1.1.4  No model changed: the 30 models are identical (1.1.2 and 1.1.3 were never released). New in Complete / Pro:
         ground, sky domes, clouds, neon halos, 22 buildings, fire escapes, park gate, striped lawn and playground;
         Godot scenes (11, with the new full city) and .blend: Pro only. Palette textures: 6 colors added in free cells only.
  1.1.1  No model changed in this edition. New in Complete / Pro: facade kit and walk-in Assembly pieces.
  1.0.1  initial release

SPECS
  - Real-world scale in meters, Y-up on export (Z-up inside Blender), origin on the ground at the center of
    the object, front of every object = -Y in Blender.
  - Road tiles: 4 x 4 m, road surface at height 0 (Road_Straight runs along X). Sidewalk tiles: 4 x 4 m, top at
    0.15 m, the curb is on the -Y side (Sidewalk_Corner: -Y and +X). A street = sidewalk | road | sidewalk (12 m).
    Road_Roundabout is 12 x 12 m, Rail_Curve is 8 x 8 m (radius 6 m). Rail gauge 1.435 m, platforms 0.9 m high.
  - Modular buildings: walls 2 m wide x 3 m high x 0.2 m thick (centered on the grid line, street side = -Y),
    floors 2 x 2 m with the top at height 0, one roof slope covers a 2 m cell and rises 1.5 m. 2 m grid.
  - Ready-made buildings: footprints aligned on the 2 m grid, front = -Y. Put them on a sidewalk tile
    (height 0.15) or directly on the ground.
  - Wall items (signs, awnings, AC units, gutters, window boxes, shutters, cornices) have their back on the wall
    plane (local y = 0).
  - Facade kit (v1.1.1, folder Facades/, Complete / Pro): Facade_Brick_Window_Arched / Facade_Brick_Door_Arched are 2 x 3 m walls like
    the others; Facade_Storefront_Big is 4.1 m wide; Facade_Stone_Base and Cornice_Large use the same origin as a wall
    (Cornice_Large goes on top of a wall, e.g. z = 3); Column_Classic is 4 m high and carries Portico_Pediment (6.46 m
    wide) at z = 4; Steps_Wide (4.06 m) has its back on the wall plane, landing 0.48 m high. Fence_Brick_Pillar has ONE
    pillar (left end): place sections every 2 m with the same rotation, then close a run or frame an entrance with
    Wall_Brick_Pillar (taller gate pier, 3.4 m; Fence_Brick_Pillar piers are 2.06 m). Curb_Straight (4 m) and Curb_Corner (4 x 4 m tile, radius 1.5 m) sit on ground at z = 0 with
    the gutter on the road side (-Y; Curb_Corner: -Y and +X).
  - Assembly pieces (v1.1.1, folder Assembly/, Complete / Pro): Wall_Brick_Interior / _Window_Interior / _Door_Interior
    are 2 x 3 m brick walls CENTERED on the grid line (0.31 m thick, finished plaster inside face, door leaf = child
    node with the pivot on the hinge); Wall_Corner_Outer (+ _Plaster) goes on every outer corner point;
    Floor_Stair_Opening is 2 x 4 m (top at 0, underside -0.108) with a 1.34 x 3.93 m opening; Railing_Stair_Opening
    (2 m) and Railing_Stair_Opening_Corner close it; Partition_T / Partition_End_Cap finish interior partitions.
    Furniture, stairs and interior walls are NOT included: see the assembly guide (included in Complete / Pro) (Interiors MegaKit 2 is a
    separate pack, sold separately).
  - Vehicles: separate wheel objects <Name>_Wheel_FL / FR / RL / RR (pivot on the hub, axle = local X).
    Rail_Crossing_Barrier, Boom_Barrier and Gate_Picket have a separate moving part with the pivot on the hinge.
  - Low poly, flat shaded: 12 to 440 faces per asset in the full pack (design limit: 2,500 faces per asset).
  - One shared material / one palette texture for the whole pack. Use nearest / point filtering.
  - Lamps, traffic lights, neon signs and headlights use the emission map (Textures/props_emission.png, also
    embedded in every GLB). In the Night palette the windows light up too (props_emission_night.png).
  - 4 free color palettes with the same UVs: Classic (props_palette.png), Night, Pastel and Winter (snow on grass,
    trees and roofs) (props_palette_<name>.png + props_emission_<name>.png): swap both images to recolor.
  - See Docs/STYLE_GUIDE.txt for the palette table and naming rules.

ASSETS (30)
  [Roads] Road_Straight  (30 faces)
  [Roads] Road_Corner  (172 faces)
  [Roads] Road_T  (78 faces)
  [Roads] Road_Cross  (36 faces)
  [Roads] Road_Crosswalk  (72 faces)
  [Sidewalks] Sidewalk_Straight  (48 faces)
  [Sidewalks] Sidewalk_Corner  (54 faces)
  [Sidewalks] Sidewalk_Plain  (42 faces)
  [Parking] Parking_Stalls  (30 faces)
  [Rail] Rail_Straight  (72 faces)
  [Building_Modular] Wall_Brick  (78 faces)
  [Building_Modular] Wall_Brick_Window  (292 faces)
  [Building_Modular] Wall_Brick_Door  (318 faces)
  [Building_Modular] Shopfront_Window  (78 faces)
  [Building_Modular] Roof_Flat  (12 faces)
  [Building_Modular] Awning_Red  (116 faces)
  [Signs] Neon_Cup  (116 faces)
  [Buildings] House_Small  (317 faces)
  [Buildings] Shop_Cafe  (382 faces)
  [Shops] Gas_Pump  (226 faces)
  [Vehicles] Car_Sedan  (308 faces)
  [Vehicles] Car_Taxi  (436 faces)
  [Vehicles] Bus_City  (440 faces)
  [Street_Furniture] Street_Lamp_Classic  (138 faces)
  [Street_Furniture] Traffic_Light  (158 faces)
  [Street_Furniture] Sign_Stop_Blank  (36 faces)
  [Street_Furniture] Bench_Wood  (246 faces)
  [Street_Furniture] Trash_Can  (104 faces)
  [Street_Furniture] Fire_Hydrant  (124 faces)
  [Urban_Nature] Tree_Round_Medium  (133 faces)

LICENSE
  See LICENSE.txt. You may use these assets in personal and commercial projects.
  You may not resell or redistribute the assets themselves, even modified.

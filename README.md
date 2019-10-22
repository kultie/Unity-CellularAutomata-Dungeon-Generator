# Unity-CellularAutomata-Dungeon-Generator

## Features:
- Auto tiling when finish randomizing map. Using auto tile format of RPG maker VXAce > RPG maker MV.
- Rendering using Mesh Renderer

## How to play with this:
- In the Sample Scene. Main Camera object has a Controller component you can change dungeon width, height and textures of wall and path of dungeon.
- Hit play.

## Technical hurdle
- Right now the mesh creation step is very unoptimized that it created a lot of verticies and triangles. If any one khow how to fix it feel free to make a pull request

## Resource:
- Simple implementation for generating random dungeon using [Cellular Automata](https://gamedevelopment.tutsplus.com/tutorials/generate-random-cave-levels-using-cellular-automata--gamedev-9664)
- [Auto tile algorithm](http://thepowertobringlight.blogspot.com/2016/11/autotiles-formats-and-algorithms.html)
- Google ¯\_(ツ)_/¯

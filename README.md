# NEStefan
Steve's NES Emulator

I've always wanted to make an emulator. It hits so many of my interests: music, programming, video games, following rules and solving puzzles. I didn't feel programming something like that was quite in reach until I saw javidx's YouTube video series on making a NES emulator. He did great at breaking it down enough to feel like it was something I could tackle.

My goal here is to niavely follow javidx's videos and make what he made. Then I'd like to go back and make it my own in some way - even if it's just cleaning up the code and writing it how I would like to see it along with comments in my own words to seal in the learning.

This project started as SteveNES and now I've refactored a bit and called it NEStefan.

I'm on a Mac using Visual Studio - writing in C#. For graphics, I'm using the SDL library with the C# bindings provided.

I have a "Display Engine" project that is sort of javidx's Pixel Game Engine, or it does the task. It will handle game engine tasks, graphics and sound, and provide a way for the NES to do those things. I would like to separate that out to its own project in the future.

## Updates
* CPU Done and tested (legal codes only)
* PPU Done
* Mapper 000 games are now playable, sprites and sprite zero detection added
* Tested with: Super Mario Bros, Donkey Kong, 1942, Excitebike, Kung Fu
* Mapper 002 and 003 added!
* * Tested 002 with: Castlevania 1, DuckTales, Guardian Legend
* Tested 003 with: Gradius, Mickey Mousecapade
* Mapper 001 added
* Tested Legend of Zelda, Tetris, Zelda II, Castlevania II, Mega Man 2, Final Fantasy, Metroid, Ninja Gaiden, Adventures in the Magic Kingdom, Chip 'n Dale Rescue Rangers, Monster Party
* Fixed pallete issues with Zelda II
* * Made fixes to Mappers 001 and 004, more playable games
* Added saving for games with battery - saving to romname.sav
* Joystick/Gamepad working, specifcally set up for mine, but working with XBox One controller :)
* 4 channels of sound working
* GUI update, separate windows being managed now

## Next items
* Regain debugging ability - work on a debugging window perhaps to see RAM and watch code
* Illegal Op Codes
* Games/Mappers I want to target
* Metalstorm (mapper 4)
* Ninja Gaiden 1-3 (mappers 1 and 4)
* Star Tropics (mapper 4)
* Mapper 005 - Casvan III -- Including new sound
* Mapper 007 - Battletoads, Wizards and Warriors 1-3, Marble Madness, Solar Jetman, RC ProAm,
* Mapper 009 - Mike Tyson!
* 5th Channel Sound
* Smooth sound by paying attention to phase

## Issues
* Noticing edge of screen artifacts that probably should not be there.
* Mapper 001 REWITE not working, still want to rewrite so I fully understand
* Not passing all tests
* Mapper 004
* Metalstorm no background when playing
* Star tropics sort of freezes when starting
* Mapper 002
* Guardian Legend - GUI item on left side of screen is wonky
* Need to look at speed, but speed is really good when running in release mode, totally playable at 60FPS.

## Screenshots
![Screenshot of Zelda](/Screenshots/Zelda1_1.png)
![Screenshot of Zelda](/Screenshots/Zelda1_2.png)
![Screenshot of Zelda](/Screenshots/Zelda1_3.png)
![Screenshot of Zelda](/Screenshots/Zelda1_4.png)
![Screenshot of Zelda 2](/Screenshots/Zelda2_1.png)
![Screenshot of Donkey Kong](/Screenshots/DK.png)
![Screenshot of Donkey Kong](/Screenshots/DK_1.png)
![Screenshot of Double Dragon](/Screenshots/DoubleDragon.png)
![Screenshot of Double Dragon](/Screenshots/DoubleDragon_1.png)
![Screenshot of Double Dragon III](/Screenshots/DoubleDragon3.png)
![Screenshot of Dragon Warrior](/Screenshots/DragonWarrior.png)
![Screenshot of Dragon Warrior II](/Screenshots/DragonWarrior2.png)
![Screenshot of Dr Mario](/Screenshots/DrMario.png)
![Screenshot of Duck Tails](/Screenshots/DuckTails.png)
![Screenshot of Dynowarz](/Screenshots/Dynowars.png)
![Screenshot of Dynowarz](/Screenshots/Dynowars_1.png)
![Screenshot of Final Fantasy](/Screenshots/FinalFantasy_1.png)
![Screenshot of Final Fantasy](/Screenshots/FinalFantasy_2.png)
![Screenshot of Journey To Silus](/Screenshots/JourneyToSilus.png)
![Screenshot of Kid Icarus](/Screenshots/KidIcarus.png)
![Screenshot of Mega Man II](/Screenshots/MegaMan2_1.png)
![Screenshot of Rescue Rangers](/Screenshots/RescueRangers.png)
![Screenshot of Rescue Rangers](/Screenshots/RescueRangers_1.png)
![Screenshot of Rescue Rangers](/Screenshots/RescueRangers_2.png)
![Screenshot of Rescue Rangers](/Screenshots/RescueRangers_3.png)

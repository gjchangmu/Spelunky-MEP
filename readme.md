### Introduction

Spelunky Mod: Money Equals Power is a mod for Spelunky HD on steam. This mod makes your mobility and use of items cost gold. If you don't have enough gold your mobility will be very limited and you may get stuck, so try to keep your pocket from being empty. This mod makes the game feel slightly different and harder. Gold management is a thing, while in vanilla game gold is in most times just the score.

This is in beta status and any kind of comments are welcome. 

### Default Prices

Jump from ground: 300 each jump (jump from grabbing ledge is free)  
Whip: 1500 each whip (meant to encorage you to find items)  
Jetpack: 3000 for a full jar of gas (proportional to jetting time)  
Cape: 1000 each use  
Mattock: 500 each use  
Boomerang: 500 each use  
Machete: 500 each use  
Webgun: 100 each use  
Shotgun: 1000 each use  
Freezegun: 500 each use  
Cannon: 500 each use  
Camera: 100 each use  
Teleporter: 500 each use  

Prices are adjustable in price.ini file.

### What happens if gold is used up?

You can't jump high (while you can still climb onto 1-block high blocks). You cant whip. You cant use items.

### Is it a cheat?

Probably not, as this mod only makes the game harder. So I haven't made effert to disable daily run, leaderboard etc.

### How it is made

1. I made a cheat table used in Cheat Engine that do the exactly same thing.
2. I used memreader to track which parts of the game code were modified by the cheat table and I stored the difference into diff.bin. 
3. I used memreader to read out the injected code in the newly allocated memory region and saved it into newmem.bin.
4. The main mod program reads the two .bin files and writes the data into game memory with some manual address rebase, so that Cheat Engine is not needed any more.

All the related codes (the cheat table, memreader and the main mod program) are included in the source code if anyone would want to check.

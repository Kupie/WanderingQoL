# WanderingQoL

##### Built for Wandering Village v0.1.32

## Installation
1. Requires BepInEx 5.4.x to be installed: https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21
2. Place WanderingQoL.dll into \<Game Install Location\>\The Wandering Village\Windows64\BepInEx\plugins\
3. ### Do *not* bother the Game Devs if there are any crashes while using mods

## Configuration

1. Upon first launch, the configuration file will be generated:
 -  \<Game Install Location\>\The Wandering Village\Windows64\BepInEx\config\org.Kupie.WanderingQoL.cfg
2. Change settings with Notepad/Notepad++. Most are "true" or "false" to enable/disable, others are for changing hotkeys and are are commented with what they do. 
   - For example, changing line 25 from:
     - "Game Mute Hotkey with CTRL = M"
   - To instead be:
     - "Game Mute Hotkey with CTRL = B"
   - Would make the Game Mute/Unmute hotkey be Ctrl + B instead of Ctrl + M.
3. If you would like a GUI to change settings easily, there is another BepInEx plugin that makes it so F1 opens a nice menu to do so: 
   - https://github.com/BepInEx/BepInEx.ConfigurationManager/releases/
   - Changes using the GUI still require a restart of the game (for now)
    
### Features

#### General
- Remove "Welcome" Splash Screen (Default on)
- Remove Social Media (Discord/Forum) links (Default on)
- Mute when Game loses focus (Default on)
- Hotkey for muting/unmuting the Game (Default Ctrl + M)
- Hotkeys for Adding/Removing Workers (Default Z/X)

#### Cheats
- Allow up to 13 workers per building (Only works with Hotkeys, for now)
  - This is 13 because if there are more, the game marks the building as "Destroyed"
  
##### Debug
- If enabled, will log to the BepInEx console. Useful for me when debugging/testing, end users shouldn't care about this.

##### Planned future Features
- Skip Credits Videos (It seems that BepInEx does not load Chainloader till *after* the videos, so this has proved more difficult than expected
- Use the GUI to add cheat-enabled extra workers (also showing how many are set, currently going over the default limit does not update the GUI)
- Adjust different game balance values of harvests/usage/etc.
- Also pause game when losing Focus (Game doesn't use Unity's timescale to pause itself. Weird, right?)
- Allow configuration in-game
- Better Hotkey control for muting, such as having it be "Shift" + a key instead of only a "Ctrl" + a key 
- Hotkey to toggle the focus/unfocus automute
- Reloading config file when config file changes
- *Maybe* figure out more than 13 workers per building

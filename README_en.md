# DetailedIcon

A lightweight mod that makes the icon of RainWorld more detailed

[中文](README.md) | [English](README_en.md)

## How to be compatible with your mod slugcat

Each cat can load two resources: To replace the "Kill_Slugcat" and "Multiplayer_Death" textures in the original version.

Compatibility method (Targeting SlugBase Mod Single Cat): The two image resources are named "Kill_Slugcat_{MOD ID}" and "Multiplayer_Death_{MOD ID}" respectively.

A more common general form: "Kill_Slugcat_{Your slugcat's ID}" 和 "Multiplayer_Death_{Your slugcat's ID}"

Use Futile.atlasManager.LoadAtlas can display resources normally after loading.

Note: If you don't know your slugcat's ID, You can view the ID output in the console (The content of this output is used to replace "your slugcat's ID"/"MOD_ID" in the previous text)

Note: Also, The two strings after the "Texture state" in the console output are the names of the two textures you need to add.

## Special Thanks

Owl ：Provides ideas and methods for finding Player instances corresponding to players in downstream classes

## Contact Us

Outlook：XuYangJerry@outlook.com
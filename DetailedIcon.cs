using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using RWCustom;
using BepInEx;
using Debug = UnityEngine.Debug;
using JollyCoop;
using System.IO;
using Menu;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace DetailedIcon;

[BepInPlugin("XuYangJerry.DetailedIcon", "Detailed Icon", "1.2.2")]
public partial class DetailedIcon : BaseUnityPlugin
{
    public static Dictionary<int, Player> players = new Dictionary<int, Player>();

    private void OnEnable()
    {
        On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
        On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.ctor += JollyCoopJollyHUDJollyMeterPlayerIcon_ctor;
        On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.Update += JollyCoopJollyHUDJollyMeterPlayerIcon_Update;
        On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyDeathBump.ctor += JollyCoopJollyHUDJollyPlayerSpecificHudJollyDeathBump_ctor;
        On.Menu.MultiplayerMenu.PopulateSafariSlugcatButtons += MenuMultiplayerMenu_PopulateSafariSlugcatButtons;
        On.Menu.FastTravelScreen.SpawnSlugcatButtons += MenuFastTravelScreen_SpawnSlugcatButtons;
        On.CreatureSymbol.SpriteNameOfCreature += CreatureSymbol_SpriteNameOfCreature;
        On.Player.ctor += Player_ctor;
    }

    private void LoadResources(RainWorld rainWorld)
    {
        Debug.Log("Loading DetailedIcon resources...");

        Futile.atlasManager.LoadAtlas("atlases/icons/Kill_Slugcats");
        Futile.atlasManager.LoadAtlas("atlases/icons/Multiplayer_Death_Slugcats");

        Futile.atlasManager.LoadAtlas("atlases/icons/Inv_icon");
    }

    private void MenuFastTravelScreen_SpawnSlugcatButtons(On.Menu.FastTravelScreen.orig_SpawnSlugcatButtons orig, FastTravelScreen self)
    {
        if (!ModManager.ModdedRegionsEnabled)
        {
            return;
        }
        self.DestroySlugcatButtons();
        foreach (string text in ExtEnum<SlugcatStats.Name>.values.entries)
        {
            SlugcatStats.Name name = new SlugcatStats.Name(text, false);
            if (!SlugcatStats.HiddenOrUnplayableSlugcat(name) && self.GetRegionsVisited(name).Count > 0)
            {
                SimpleButton simpleButton = new SimpleButton(self, self.pages[0], "", "SLUG" + text, new Vector2(self.manager.rainWorld.options.ScreenSize.x / 2f + (1366f - self.manager.rainWorld.options.ScreenSize.x) / 2f, 90f), new Vector2(48f, 48f));
                if (self.activeMenuSlugcat == name)
                {
                    simpleButton.toggled = true;
                }
                simpleButton.nextSelectable[3] = simpleButton;
                simpleButton.nextSelectable[1] = ((self.chooseButton == null) ? self.backButton : self.chooseButton);
                FSprite fsprite = new FSprite("Kill_Slugcat", true);
                fsprite.element = Futile.atlasManager._allElementsByName.Keys.ToList().Exists(x => x == $"Kill_Slugcat_{text}") ? Futile.atlasManager.GetElementWithName($"Kill_Slugcat_{text}") : Futile.atlasManager.GetElementWithName("Kill_Slugcat");
                fsprite.color = PlayerGraphics.DefaultSlugcatColor(name);
                self.slugcatLabels.Add(fsprite);
                self.slugcatButtons.Add(simpleButton);
                self.pages[0].Container.AddChild(fsprite);
                self.pages[0].subObjects.Add(simpleButton);
            }
        }
        float num = (float)self.slugcatButtons.Count * 56f;
        for (int i = 0; i < self.slugcatButtons.Count; i++)
        {
            SimpleButton simpleButton2 = self.slugcatButtons[i];
            simpleButton2.pos.x += ((float)i * 56f - num * 0.5f);
            self.slugcatLabels[i].x = simpleButton2.pos.x + simpleButton2.size.x * 0.5f - (1366f - self.manager.rainWorld.options.ScreenSize.x) / 2f;
            self.slugcatLabels[i].y = simpleButton2.pos.y + simpleButton2.size.y * 0.5f;
        }
    }

    private void MenuMultiplayerMenu_PopulateSafariSlugcatButtons(On.Menu.MultiplayerMenu.orig_PopulateSafariSlugcatButtons orig, Menu.MultiplayerMenu self, string regionName)
    {
        for (int i = 0; i < self.safariSlugcatButtons.Count; i++)
        {
            self.safariSlugcatButtons[i].RemoveSprites();
            self.pages[0].RemoveSubObject(self.safariSlugcatButtons[i]);
        }
        self.safariSlugcatButtons.Clear();
        for (int j = 0; j < self.safariSlugcatLabels.Count; j++)
        {
            self.safariSlugcatLabels[j].RemoveFromContainer();
        }
        self.safariSlugcatLabels.Clear();
        for (int k = 0; k < ExtEnum<SlugcatStats.Name>.values.Count; k++)
        {
            SlugcatStats.Name name = new SlugcatStats.Name(ExtEnum<SlugcatStats.Name>.values.GetEntry(k), false);
            List<string> list = SlugcatStats.SlugcatStoryRegions(name);
            list.AddRange(SlugcatStats.SlugcatOptionalRegions(name));
            for (int l = 0; l < list.Count; l++)
            {
                bool flag = false;
                if (self.manager.rainWorld.progression.miscProgressionData.regionsVisited.ContainsKey(regionName))
                {
                    flag = self.manager.rainWorld.progression.miscProgressionData.regionsVisited[regionName].Contains(name.value);
                }
                if (regionName == list[l] && (flag || (MultiplayerUnlocks.CheckUnlockSafari() && !SlugcatStats.HiddenOrUnplayableSlugcat(name))))
                {
                    SimpleButton item = new SimpleButton(self, self.pages[0], "", "SAFSLUG" + name.Index.ToString(), new Vector2(self.manager.rainWorld.options.ScreenSize.x / 2f + (1366f - self.manager.rainWorld.options.ScreenSize.x) / 2f, 110f), new Vector2(48f, 48f));
                    FSprite fsprite = new FSprite("Kill_Slugcat", true);
                    fsprite.element = Futile.atlasManager._allElementsByName.Keys.ToList().Exists(x => x == $"Kill_Slugcat_{name.value}") ? Futile.atlasManager.GetElementWithName($"Kill_Slugcat_{name.value}") : Futile.atlasManager.GetElementWithName("Kill_Slugcat");
                    fsprite.color = PlayerGraphics.DefaultSlugcatColor(name);
                    self.safariSlugcatLabels.Add(fsprite);
                    self.safariSlugcatButtons.Add(item);
                    self.pages[0].Container.AddChild(fsprite);
                    self.pages[0].subObjects.Add(item);
                    break;
                }
            }
        }
        if (self.GetGameTypeSetup.safariSlugcatID >= 0 && self.GetGameTypeSetup.safariSlugcatID < self.safariSlugcatButtons.Count && !self.firstSafariSlugcatsButtonPopulate)
        {
            for (int m = 0; m < self.safariSlugcatButtons.Count; m++)
            {
                self.safariSlugcatButtons[m].toggled = false;
            }
            self.safariSlugcatButtons[self.GetGameTypeSetup.safariSlugcatID].toggled = true;
        }
        else
        {
            for (int n = 0; n < self.safariSlugcatButtons.Count; n++)
            {
                self.safariSlugcatButtons[n].toggled = false;
            }
            self.safariSlugcatButtons[0].toggled = true;
            self.GetGameTypeSetup.safariSlugcatID = 0;
        }
        self.firstSafariSlugcatsButtonPopulate = true;
        float num = (float)self.safariSlugcatButtons.Count * 56f;
        for (int num2 = 0; num2 < self.safariSlugcatButtons.Count; num2++)
        {
            SimpleButton simpleButton = self.safariSlugcatButtons[num2];
            simpleButton.pos.x = simpleButton.pos.x + ((float)num2 * 56f - num * 0.5f);
            self.safariSlugcatLabels[num2].x = simpleButton.pos.x + simpleButton.size.x * 0.5f - (1366f - self.manager.rainWorld.options.ScreenSize.x) / 2f;
            self.safariSlugcatLabels[num2].y = simpleButton.pos.y + simpleButton.size.y * 0.5f;
        }
    }

    private void JollyCoopJollyHUDJollyPlayerSpecificHudJollyDeathBump_ctor(On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyDeathBump.orig_ctor orig, JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyDeathBump self, JollyCoop.JollyHUD.JollyPlayerSpecificHud jollyHud)
    {
        self.jollyHud = jollyHud;
        self.SetPosToPlayer();
        self.gradient = new FSprite("Futile_White", true);
        self.gradient.shader = jollyHud.hud.rainWorld.Shaders["FlatLight"];
        if ((jollyHud.abstractPlayer.state as PlayerState).slugcatCharacter != SlugcatStats.Name.Night)
        {
            self.gradient.color = new Color(0f, 0f, 0f);
        }
        jollyHud.hud.fContainers[0].AddChild(self.gradient);
        self.gradient.alpha = 0f;
        self.gradient.x = -1000f;
        self.symbolSprite = new FSprite("Multiplayer_Death", true);
        string playerType = players.TryGetValue(jollyHud.abstractPlayer.ID.number, out Player player) ? player.slugcatStats.name.value : "Unknown";
        self.symbolSprite.element = Futile.atlasManager._allElementsByName.Keys.ToList().Exists(x => x == $"Multiplayer_Death_{playerType}") ? Futile.atlasManager.GetElementWithName($"Multiplayer_Death_{playerType}") : Futile.atlasManager.GetElementWithName("Multiplayer_Death");
        self.symbolSprite.color = PlayerGraphics.DefaultSlugcatColor((jollyHud.abstractPlayer.state as PlayerState).slugcatCharacter);
        jollyHud.hud.fContainers[0].AddChild(self.symbolSprite);
        self.symbolSprite.alpha = 0f;
        self.symbolSprite.x = -1000f;
    }

    private void JollyCoopJollyHUDJollyMeterPlayerIcon_Update(On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.orig_Update orig, JollyCoop.JollyHUD.JollyMeter.PlayerIcon self)
    {
        self.blink = Mathf.Max(0f, self.blink - 0.05f);
        self.lastBlink = self.blink;
        self.lastPos = self.pos;
        self.color = PlayerGraphics.SlugcatColor(self.playerState.slugcatCharacter);
        self.rad = Custom.LerpAndTick(self.rad, Custom.LerpMap(self.meter.fade, 0f, 0.79f, 0.79f, 1f, 1.3f), 0.12f, 0.1f);
        if (self.blinkRed > 0)
        {
            self.blinkRed--;
            self.rad *= Mathf.SmoothStep(1.1f, 0.85f, (float)(self.meter.counter % 20) / 20f);
            self.color = Color.Lerp(self.color, JollyCustom.GenerateClippedInverseColor(self.color), self.rad / 4f);
        }
        self.iconSprite.scale = self.rad;
        self.gradient.scale = self.baseGradScale * self.rad;
        if (self.playerState.permaDead || self.playerState.dead)
        {
            self.color = Color.gray;
            if (!self.dead)
            {
                self.iconSprite.RemoveFromContainer();
                string playerType = players.TryGetValue(self.playerState.playerNumber, out Player player) ? player.slugcatStats.name.value : "Unknown";
                self.iconSprite = new FSprite("Multiplayer_Death", true);
                self.iconSprite.element = Futile.atlasManager.GetElementWithName($"Multiplayer_Death{ (Futile.atlasManager._allElementsByName.Keys.ToList().Exists(x => x == $"Multiplayer_Death_{playerType}") ? $"_{playerType}" : "") }");
                self.iconSprite.scale *= 0.8f;
                self.meter.fContainer.AddChild(self.iconSprite);
                self.dead = true;
                self.meter.customFade = 5f;
                self.blink = 3f;
                self.gradient.color = Color.Lerp(Color.red, Color.black, 0.5f);
            }
        }
    }

    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        players[self.abstractCreature.ID.number] = self;
        Debug.Log($" [DetailedIcon] Player {self.abstractCreature.ID.number} created: {self.slugcatStats.name.value} ;\n\t\t\t\t Texture state: Kill_Slugcat_{self.slugcatStats.name.value} = {Futile.atlasManager._allElementsByName.Keys.ToList().Exists(x => x == $"Kill_Slugcat_{self.slugcatStats.name.value}")}; Multiplayer_Death_{self.slugcatStats.name.value} = {Futile.atlasManager._allElementsByName.Keys.ToList().Exists(x => x == $"Multiplayer_Death_{self.slugcatStats.name.value}")}");
    }

    private string CreatureSymbol_SpriteNameOfCreature(On.CreatureSymbol.orig_SpriteNameOfCreature orig, IconSymbol.IconSymbolData iconData)
    {
        if (iconData.critType == CreatureTemplate.Type.Slugcat)
        {
            string playerType = players.TryGetValue(iconData.intData, out Player player) ? player.slugcatStats.name.value : "Unknown";
            return "Kill_Slugcat" + ( Futile.atlasManager._allElementsByName.Keys.ToList().Exists(x => x == $"Kill_Slugcat_{playerType}") ? $"_{playerType}" : "" );
        }
        return orig(iconData);
    }

    private void JollyCoopJollyHUDJollyMeterPlayerIcon_ctor(On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.orig_ctor orig, JollyCoop.JollyHUD.JollyMeter.PlayerIcon self, JollyCoop.JollyHUD.JollyMeter meter, AbstractCreature associatedPlayer, Color color)
    {
        orig(self, meter, associatedPlayer, color);
        string playerType = (associatedPlayer.realizedCreature as Player).slugcatStats.name.value;
        self.iconSprite.element = Futile.atlasManager._allElementsByName.Keys.ToList().Exists(x => x == $"Kill_Slugcat_{playerType}") ? Futile.atlasManager.GetElementWithName($"Kill_Slugcat_{playerType}") : Futile.atlasManager.GetElementWithName("Kill_Slugcat");
    }
}

//放弃对多层的支持，原因：不知道如何获取颜色、修改过多、不确定是否会影响性能
public class PlayerIconPlus //: JollyCoop.JollyHUD.JollyMeter.PlayerIcon
{    
    private JollyCoop.JollyHUD.JollyMeter meter;
    public int playerNumber;
    private FSprite gradient;
    public float baseGradScale;
    public float baseGradAlpha;
    public FSprite[] iconSprite;
    public Color color;
    public Vector2 pos;
    public Vector2 lastPos;
    private float blink;
    public int blinkRed;
    private bool dead;
    private float lastBlink;
    private AbstractCreature player;
    private float rad;

    public Vector2 DrawPos(float timeStacker)
    {
        return Vector2.Lerp(this.lastPos, this.pos, timeStacker);
    }

    private PlayerState playerState
    {
        get
        {
            return this.player.state as PlayerState;
        }
    }

    private string playerType
    {
        get
        {
            return (this.player.realizedCreature as Player).slugcatStats.name.value;
        }
    }

    public void ClearSprites()
    {
        this.gradient.RemoveFromContainer();
        foreach (FSprite sprite in this.iconSprite)
        {
            sprite.RemoveFromContainer();
        }
    }

    public PlayerIconPlus(JollyCoop.JollyHUD.JollyMeter meter, AbstractCreature associatedPlayer, Color color) //: base(meter, associatedPlayer, color)
    {
        this.player = associatedPlayer;
        this.meter = meter;
        this.lastPos = this.pos;
        this.AddGradient(JollyCustom.ColorClamp(color, -1f, 360f, 60f, 360f, -1f, 360f));
        this.iconSprite = new FSprite[1] { new("Kill_Slugcat", true) };
        try
        {
            this.iconSprite[0].element = Futile.atlasManager.GetElementWithName($"Kill_Slugcat_{playerType}");
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            this.iconSprite[0].element = Futile.atlasManager.GetElementWithName($"Kill_Slugcat");
        }
        this.color = color;
        this.meter.fContainer.AddChild(this.iconSprite[0]);
        PlayerState playerState = this.playerState;
        this.playerNumber = ((playerState != null) ? playerState.playerNumber : 0);
        this.baseGradScale = 3.75f;
        this.baseGradAlpha = 0.45f;
    }

    public void AddGradient(Color color)
    {
        this.gradient = new FSprite("Futile_White", true);
        this.gradient.shader = this.meter.hud.rainWorld.Shaders["FlatLight"];
        this.gradient.color = color;
        this.gradient.scale = this.baseGradScale;
        this.gradient.alpha = this.baseGradAlpha;
        this.meter.fContainer.AddChild(this.gradient);
    }

    public void Draw(float timeStacker)
    {
        float num = Mathf.Lerp(this.meter.lastFade, this.meter.fade, timeStacker);
        this.iconSprite[0].alpha = num;
        this.gradient.alpha = Mathf.SmoothStep(0f, 1f, num) * this.baseGradAlpha;
        this.iconSprite[0].x = this.DrawPos(timeStacker).x;
        this.iconSprite[0].y = this.DrawPos(timeStacker).y + (float)(this.dead ? 7 : 0);
        this.gradient.x = this.iconSprite[0].x;
        this.gradient.y = this.iconSprite[0].y;
        if (this.meter.counter % 6 < 2 && this.lastBlink > 0f)
        {
            this.color = Color.Lerp(this.color, Custom.HSL2RGB(Custom.RGB2HSL(this.color).x, Custom.RGB2HSL(this.color).y, Custom.RGB2HSL(this.color).z + 0.2f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(this.lastBlink, this.blink, timeStacker)));
        }
        this.iconSprite[0].color = this.color;
    }

    public void Update()
    {
        this.blink = Mathf.Max(0f, this.blink - 0.05f);
        this.lastBlink = this.blink;
        this.lastPos = this.pos;
        this.color = PlayerGraphics.SlugcatColor(this.playerState.slugcatCharacter);
        this.rad = Custom.LerpAndTick(this.rad, Custom.LerpMap(this.meter.fade, 0f, 0.79f, 0.79f, 1f, 1.3f), 0.12f, 0.1f);
        if (this.blinkRed > 0)
        {
            this.blinkRed--;
            this.rad *= Mathf.SmoothStep(1.1f, 0.85f, (float)(this.meter.counter % 20) / 20f);
            this.color = Color.Lerp(this.color, JollyCustom.GenerateClippedInverseColor(this.color), this.rad / 4f);
        }
        this.iconSprite[0].scale = this.rad;
        this.gradient.scale = this.baseGradScale * this.rad;
        if (this.playerState.permaDead || this.playerState.dead)
        {
            this.color = Color.gray;
            if (!this.dead)
            {
                this.iconSprite[0].RemoveFromContainer();
                this.iconSprite = new FSprite[1] { new FSprite("Multiplayer_Death", true) };
                this.iconSprite[0].scale *= 0.8f;
                this.meter.fContainer.AddChild(this.iconSprite[0]);
                this.dead = true;
                this.meter.customFade = 5f;
                this.blink = 3f;
                this.gradient.color = Color.Lerp(Color.red, Color.black, 0.5f);
            }
        }
    }
}

public class MoreIconsLayers
{
    public static string path = Path.Combine(Directory.GetCurrentDirectory(), "MoreIconsLayers.txt");

    public static bool HasLayers(string slugType)
    {
        if (!File.Exists(MoreIconsLayers.path))
        {
            File.Create(MoreIconsLayers.path).Close();
        }
        string[] lines = File.ReadAllLines(MoreIconsLayers.path);
        foreach (string line in lines)
        {
            if (line.StartsWith(slugType))
            {
                return true;
            }
        }
        return false;
    }

    public static string[] GetLayers(string slugType)
    {
        if (!MoreIconsLayers.HasLayers(slugType)) { return new string[0]; }
        string[] lines = File.ReadAllLines(MoreIconsLayers.path);
        if (lines.Length == 0 || lines == null) { return new string[0]; }
        foreach (string line in lines)
        {
            if (line.StartsWith(slugType))
            {
                return line.Split(':')[1].Split(',');
            }
        }
        return new string[0];
    }
}
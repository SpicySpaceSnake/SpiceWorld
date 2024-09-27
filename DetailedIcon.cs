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

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace DetailedIcon;

[BepInPlugin("XuYangJerry.DetailedIcon", "DetailedIcon", "1.0.0")]
public partial class DetailedIcon : BaseUnityPlugin
{
    public static Dictionary<int, Player> players = new Dictionary<int, Player>();

    private void OnEnable()
    {
        On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
        On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.ctor += JollyCoopJollyHUDJollyMeterPlayerIcon_ctor;
        On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.Update += JollyCoopJollyHUDJollyMeterPlayerIcon_Update;
        On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyDeathBump.ctor += JollyCoopJollyHUDJollyPlayerSpecificHudJollyDeathBump_ctor;
        On.CreatureSymbol.SpriteNameOfCreature += CreatureSymbol_SpriteNameOfCreature;
        On.Player.ctor += Player_ctor;
    }

    private void LoadResources(RainWorld rainWorld)
    {
        Debug.Log("Loading DetailedIcon resources...");

        Futile.atlasManager.LoadAtlas("atlases/icons/Kill_Slugcats");
        Futile.atlasManager.LoadAtlas("atlases/icons/Multiplayer_Death_Slugcats");
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
        self.symbolSprite.element = Futile.atlasManager._allElementsByName.Keys.ToList().Exists(x => x.StartsWith($"Multiplayer_Death_{playerType}")) ? Futile.atlasManager.GetElementWithName($"Multiplayer_Death_{playerType}") : Futile.atlasManager.GetElementWithName("Multiplayer_Death");
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
                string playerType = self.iconSprite.element.name.Split('_').Length == 3 ? ("_" + self.iconSprite.element.name.Split('_')[2] ) : "";
                self.iconSprite = new FSprite("Multiplayer_Death", true);
                self.iconSprite.element = Futile.atlasManager.GetElementWithName($"Multiplayer_Death{playerType}");
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
        Debug.Log("Player " + self.abstractCreature.ID.number + " created.");
    }

    private string CreatureSymbol_SpriteNameOfCreature(On.CreatureSymbol.orig_SpriteNameOfCreature orig, IconSymbol.IconSymbolData iconData)
    {
        if (iconData.critType == CreatureTemplate.Type.Slugcat)
        {
            string playerType = players.TryGetValue(iconData.intData, out Player player) ? player.slugcatStats.name.value : "Unknown";
            return "Kill_Slugcat" + ( Futile.atlasManager._allElementsByName.Keys.ToList().Exists(x => x.StartsWith($"Kill_Slugcat_{playerType}")) ? $"_{playerType}" : "" );
        }
        return orig(iconData);
    }

    private void JollyCoopJollyHUDJollyMeterPlayerIcon_ctor(On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.orig_ctor orig, JollyCoop.JollyHUD.JollyMeter.PlayerIcon self, JollyCoop.JollyHUD.JollyMeter meter, AbstractCreature associatedPlayer, Color color)
    {
        orig(self, meter, associatedPlayer, color);
        string playerType = (associatedPlayer.realizedCreature as Player).slugcatStats.name.value;
        self.iconSprite.element = Futile.atlasManager._allElementsByName.Keys.ToList().Exists(x => x.StartsWith($"Kill_Slugcat_{playerType}")) ? Futile.atlasManager.GetElementWithName($"Kill_Slugcat_{playerType}") : Futile.atlasManager.GetElementWithName("Kill_Slugcat");
    }
}

//放弃对多层的支持，原因：不知道如何获取颜色
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
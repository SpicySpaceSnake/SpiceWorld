using System;
using System.Collections.Generic;
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
using MoreSlugcats;
using JollyCoop.JollyHUD;
using System.Runtime.CompilerServices;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace DetailedIcon;

[BepInPlugin("XuYangJerry.DetailedIcon", "Detailed Icon", "1.3.1")]
public partial class DetailedIcon : BaseUnityPlugin
{
    public static Dictionary<int, Player> players = new Dictionary<int, Player>();
    public static ConditionalWeakTable<JollyMeter.PlayerIcon, DetailedPlayerIcon> DetailedPlayerIconsData = new();
    public static Dictionary<int, Color> playerColors = new();

    public void OnEnable()
    {
        On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
        On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.ctor += JollyCoopJollyHUDJollyMeterPlayerIcon_ctor;
        On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.Draw += JollyCoopJollyHUDJollyMeterPlayerIcon_Draw;
        On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.Update += JollyCoopJollyHUDJollyMeterPlayerIcon_Update;
        On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.JollyDeathBump.ctor += JollyCoopJollyHUDJollyPlayerSpecificHudJollyDeathBump_ctor;
        On.Menu.MultiplayerMenu.PopulateSafariSlugcatButtons += MenuMultiplayerMenu_PopulateSafariSlugcatButtons;
        On.Menu.FastTravelScreen.SpawnSlugcatButtons += MenuFastTravelScreen_SpawnSlugcatButtons;
        On.CreatureSymbol.SpriteNameOfCreature += CreatureSymbol_SpriteNameOfCreature;
        On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
        On.Player.ctor += Player_ctor;
    }

    private void LoadResources(RainWorld rainWorld)
    {
        Debug.Log("Loading DetailedIcon resources...");

        Futile.atlasManager.LoadAtlas("atlases/icons/Kill_Slugcats");
        Futile.atlasManager.LoadAtlas("atlases/icons/Multiplayer_Death_Slugcats");

        Futile.atlasManager.LoadAtlas("atlases/icons/Inv_icon");

        Futile.atlasManager.LoadAtlas("atlases/icons/Kill_HunterDaddy");

        Futile.atlasManager.LoadAtlas("atlases/icons/AdditionalIcon");

        Futile.atlasManager.LoadAtlas("atlases/icons/Kill_Slugcats_Eyes");
        Futile.atlasManager.LoadAtlas("atlases/icons/Multiplayer_Death_Slugcats_Eyes");
        Futile.atlasManager.LoadAtlas("atlases/icons/Inv_icon_Eyes");
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
                simpleButton.nextSelectable[1] = self.chooseButton ?? self.backButton;
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
        if (DetailedPlayerIconsData.TryGetValue(self, out var icon))
        {
            icon.Update();
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
        if (iconData.critType == MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy)
        {
            return "Kill_HunterDaddy";
        }
        if (iconData.critType == CreatureTemplate.Type.Fly)
        {
            return "Kill_Fly";
        }
        if (iconData.critType == CreatureTemplate.Type.TempleGuard)
        {
            return "Kill_TempleGuard";
        }
        return orig(iconData);
    }

    private string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
    {
        if (itemType == AbstractPhysicalObject.AbstractObjectType.BlinkingFlower)
        {
            return "Kill_BlinkingFlower";
        }
        if (itemType == AbstractPhysicalObject.AbstractObjectType.DartMaggot)
        {
            return "Kill_DartMaggot";
        }
        if (itemType == AbstractPhysicalObject.AbstractObjectType.KarmaFlower)
        {
            return "Kill_KarmaFlower";
        }
        if (itemType == AbstractPhysicalObject.AbstractObjectType.SeedCob)
        {
            return "Kill_SeedCob";
        }
        if (itemType == AbstractPhysicalObject.AbstractObjectType.VoidSpawn)
        {
            return "Kill_VoidSpawn";
        }
        if (itemType == AbstractPhysicalObject.AbstractObjectType.Oracle)
        {
            return "Kill_Oracle";
        }
        if (itemType == MoreSlugcatsEnums.AbstractObjectType.HRGuard)
        {
            return "Kill_TempleGuard";
        }
        return orig(itemType, intData);
    }

    private void JollyCoopJollyHUDJollyMeterPlayerIcon_ctor(On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.orig_ctor orig, JollyCoop.JollyHUD.JollyMeter.PlayerIcon self, JollyCoop.JollyHUD.JollyMeter meter, AbstractCreature associatedPlayer, Color color)
    {
        orig(self, meter, associatedPlayer, color);
        string playerType = (associatedPlayer.realizedCreature as Player).slugcatStats.name.value;
        self.iconSprite.element = Futile.atlasManager._allElementsByName.Keys.ToList().Exists(x => x == $"Kill_Slugcat_{playerType}") ? Futile.atlasManager.GetElementWithName($"Kill_Slugcat_{playerType}") : Futile.atlasManager.GetElementWithName("Kill_Slugcat");
        DetailedPlayerIconsData.Add(self, new DetailedPlayerIcon(self, associatedPlayer, playerType));
    }

    private void JollyCoopJollyHUDJollyMeterPlayerIcon_Draw(On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.orig_Draw orig, JollyMeter.PlayerIcon self, float timeStacker)
    {
        orig(self, timeStacker);
        if (DetailedPlayerIconsData.TryGetValue(self, out var icon))
        {
            icon.Draw(timeStacker);
        }
    }
}

public class DetailedPlayerIcon
{
    public static WeakReference<JollyMeter.PlayerIcon> iconRef;
    public FSprite newIconSprite;
    public AbstractCreature player;
    public Color iconColor;
    public bool setColor;
    public bool dead = false;
    public string slugcatType;

    private PlayerState playerState
    {
        get
        {
            return this.player.state as PlayerState;
        }
    }

    public DetailedPlayerIcon(JollyMeter.PlayerIcon icon, AbstractCreature associatedPlayer, string slugcatType)
    {
        iconRef = new WeakReference<JollyMeter.PlayerIcon>(icon);
        this.slugcatType = slugcatType;
        this.newIconSprite = new FSprite(Futile.atlasManager.DoesContainElementWithName($"Kill_Slugcat_{slugcatType}_Eyes") ? $"Kill_Slugcat_{slugcatType}_Eyes" : "Kill_Slugcat_White_Eyes", true);
        this.player = associatedPlayer;
        this.iconColor = icon.color;
        this.setColor = false;
        icon.meter.fContainer.AddChild(this.newIconSprite);
    }

    public void Draw(float timeStacker)
    {
        if (!iconRef.TryGetTarget(out var icon))
            return;
        if (!icon.iconSprite.element.name.Contains(this.slugcatType))
        {
            if (this.playerState.permaDead || this.playerState.dead)
                icon.iconSprite.element = Futile.atlasManager.GetElementWithName(Futile.atlasManager.DoesContainElementWithName($"Multiplayer_Death_{slugcatType}_Eyes") ? $"Multiplayer_Death_{slugcatType}_Eyes" : "Multiplayer_Death_White_Eyes");
            else
                icon.iconSprite.element = Futile.atlasManager.GetElementWithName(Futile.atlasManager.DoesContainElementWithName($"Kill_Slugcat_{slugcatType}_Eyes") ? $"Kill_Slugcat_{slugcatType}_Eyes" : "Kill_Slugcat_White_Eyes");
        }

        this.newIconSprite.alpha = icon.iconSprite.alpha;
        this.newIconSprite.x = icon.iconSprite.x;
        this.newIconSprite.y = icon.iconSprite.y;
        this.newIconSprite.color = this.iconColor;
        this.newIconSprite.MoveBehindOtherNode(icon.iconSprite);
    }

    public void Update()
    {
        if (!iconRef.TryGetTarget(out var icon))
            return;
        if (!setColor && player.realizedCreature != null && (player.realizedCreature.graphicsModule as PlayerGraphics) != null)
        {
            setColor = true;
            this.iconColor = PlayerGraphics.JollyColor((player.realizedCreature as Player).playerState.playerNumber, 1);
        }
        if (this.playerState.permaDead || this.playerState.dead)
        {
            if (!this.dead)
            {
                this.newIconSprite.scale *= 0.8f;
                this.dead = true;
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
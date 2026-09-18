using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace SignLockSMAPI;

public class ModEntry : Mod
{
    internal static ModConfig Config = new();

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null)
            return;

        configMenu.Register(
            mod: ModManifest,
            reset: () => Config = new ModConfig(),
            save: () => Helper.WriteConfig(Config)
        );

        configMenu.AddKeybindList(
            mod: ModManifest,
            name: () => Helper.Translation.Get("config.BypassKey.name"),
            tooltip: () => Helper.Translation.Get("config.BypassKey.description"),
            getValue: () => Config.BypassKey,
            setValue: value => Config.BypassKey = value
        );
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Config.BypassKey.IsDown())
            return;
        if (!e.Button.IsActionButton() && !e.Button.IsUseToolButton())
            return;
        if (e.Button.IsUseToolButton() && Game1.player.CurrentTool != null)
            return;

        GameLocation location = Game1.currentLocation;
        if (location == null)
            return;

        Vector2 tile;
        if (e.Button == SButton.MouseRight || e.Button == SButton.MouseLeft)
            tile = e.Cursor.GrabTile;
        else
            tile = new Vector2(
                (int)(Game1.player.GetToolLocation().X / Game1.tileSize),
                (int)(Game1.player.GetToolLocation().Y / Game1.tileSize)
            );

        if (!location.objects.TryGetValue(tile, out var obj))
            location.objects.TryGetValue(tile + new Vector2(0, 1), out obj);

        if (obj is not Sign sign || sign.displayItem.Value == null)
            return;

        Helper.Input.Suppress(e.Button);
    }
}

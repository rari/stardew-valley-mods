using System.Collections.Generic;
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
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Input.CursorMoved += OnCursorMoved;
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
        if (!ShouldHandleButton(e.Button))
            return;
        if (!IsLockedSignUnderCursor(e.Cursor))
            return;

        Helper.Input.Suppress(e.Button);
    }

    // keyboard
    private void OnCursorMoved(object? sender, CursorMovedEventArgs e)
    {
        SuppressHeldButtonsOverLockedSign(e.NewPosition);
    }

    // controller
    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        SuppressHeldButtonsOverLockedSign(Helper.Input.GetCursorPosition());
    }

    private void SuppressHeldButtonsOverLockedSign(ICursorPosition cursor)
    {
        if (!Context.IsPlayerFree || Config.BypassKey.IsDown())
            return;
        if (!IsLockedSignUnderCursor(cursor))
            return;

        foreach (SButton button in GetHeldInteractionButtons())
        {
            if (!ShouldHandleButton(button))
                continue;

            Helper.Input.Suppress(button);
        }
    }

    private bool ShouldHandleButton(SButton button)
    {
        if (!Context.IsPlayerFree || Config.BypassKey.IsDown())
            return false;
        if (!button.IsActionButton() && !button.IsUseToolButton())
            return false;
        if (button.IsUseToolButton() && Game1.player.CurrentTool != null)
            return false;
        return true;
    }

    private static bool IsLockedSignUnderCursor(ICursorPosition cursor)
    {
        GameLocation location = Game1.currentLocation;
        if (location == null)
            return false;

        Vector2 down = new(0, 1);
        Vector2 grab = cursor.GrabTile;
        Vector2 tool = new(
            (int)(Game1.player.GetToolLocation().X / Game1.tileSize),
            (int)(Game1.player.GetToolLocation().Y / Game1.tileSize)
        );
        Vector2 playerGrab = Game1.player.GetGrabTile();

        foreach (Vector2 tile in new[] { grab, grab + down, tool, tool + down, playerGrab, playerGrab + down })
        {
            if (!location.objects.TryGetValue(tile, out var obj))
                continue;
            if (obj is Sign sign && sign.displayItem.Value != null)
                return true;
        }

        return false;
    }

    private IEnumerable<SButton> GetHeldInteractionButtons()
    {
        // keyboard
        if (Helper.Input.IsDown(SButton.MouseLeft) || Helper.Input.IsSuppressed(SButton.MouseLeft))
            yield return SButton.MouseLeft;
        if (Helper.Input.IsDown(SButton.MouseRight) || Helper.Input.IsSuppressed(SButton.MouseRight))
            yield return SButton.MouseRight;
        // controller
        if (Helper.Input.IsDown(SButton.ControllerA) || Helper.Input.IsSuppressed(SButton.ControllerA))
            yield return SButton.ControllerA;
        if (Helper.Input.IsDown(SButton.ControllerX) || Helper.Input.IsSuppressed(SButton.ControllerX))
            yield return SButton.ControllerX;

        foreach (var input in Game1.options.actionButton)
        {
            SButton button = input.ToSButton();
            if (Helper.Input.IsDown(button) || Helper.Input.IsSuppressed(button))
                yield return button;
        }
        foreach (var input in Game1.options.useToolButton)
        {
            SButton button = input.ToSButton();
            if (Helper.Input.IsDown(button) || Helper.Input.IsSuppressed(button))
                yield return button;
        }
    }
}

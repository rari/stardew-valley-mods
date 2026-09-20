using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace SignLockSMAPI;

internal sealed class ModEntry : Mod
{
    internal static ModConfig Config = new();

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.UpdateTicking += OnUpdateTicking;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Input.ButtonReleased += OnButtonReleased;
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
        if (!ShouldHandleButton(e.Button, e.Cursor))
            return;
        if (!IsLockedSignUnderCursor(e.Button, e.Cursor))
            return;

        Helper.Input.Suppress(e.Button);
    }

    private void OnButtonReleased(object? sender, ButtonReleasedEventArgs e)
    {
        ICursorPosition cursor = Helper.Input.GetCursorPosition();
        if (!ShouldHandleButton(e.Button, cursor))
            return;
        if (!IsLockedSignUnderCursor(e.Button, cursor))
            return;

        Helper.Input.Suppress(e.Button);
    }

    private void OnCursorMoved(object? sender, CursorMovedEventArgs e)
    {
        SuppressHeldButtonsOverLockedSign(e.NewPosition);
    }

    private void OnUpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        if (Config.BypassKey.IsDown())
            return;
        if (!HasHeldInteractionButton())
            return;

        SuppressHeldButtonsOverLockedSign(Helper.Input.GetCursorPosition());
    }

    private void SuppressHeldButtonsOverLockedSign(ICursorPosition cursor)
    {
        if (Config.BypassKey.IsDown())
            return;

        foreach (SButton button in GetHeldInteractionButtons())
        {
            if (!ShouldHandleButton(button, cursor))
                continue;
            if (!IsLockedSignUnderCursor(button, cursor))
                continue;

            Helper.Input.Suppress(button);
        }
    }

    private bool ShouldHandleButton(SButton button, ICursorPosition? cursor = null)
    {
        if (Config.BypassKey.IsDown())
            return false;
        if (!button.IsActionButton() && !button.IsUseToolButton())
            return false;
        if (button.IsUseToolButton() && Game1.player?.CurrentTool != null)
            return false;
        if (cursor != null && IsLockedSignUnderCursor(button, cursor))
            return true;
        if (!Context.IsPlayerFree)
            return false;
        if (IsCursorOverHud())
            return false;
        return true;
    }

    private static bool IsCursorOverHud()
    {
        if (Game1.activeClickableMenu != null)
            return true;

        int x = Game1.getMouseX();
        int y = Game1.getMouseY();
        foreach (IClickableMenu menu in Game1.onScreenMenus)
        {
            if (menu.isWithinBounds(x, y))
                return true;
        }

        return false;
    }

    private static bool IsLockedSignUnderCursor(SButton button, ICursorPosition cursor)
    {
        GameLocation location = Game1.currentLocation;
        if (location == null)
            return false;

        if (button is SButton.MouseLeft or SButton.MouseRight)
        {
            if (IsLockedSignTargetAt(location, cursor.GrabTile))
                return true;
            return IsLockedSignTargetAt(location, GetToolTile(cursor));
        }

        return IsLockedSignTargetAt(location, GetPrimaryTargetTile(button, cursor));
    }

    private static Vector2 GetPrimaryTargetTile(SButton button, ICursorPosition cursor)
    {
        if (button is SButton.MouseLeft or SButton.MouseRight || button.TryGetKeyboard(out _))
        {
            if (button.IsUseToolButton())
                return GetToolTile(cursor);
            return cursor.GrabTile;
        }

        if (button.IsUseToolButton())
            return GetToolTile();

        return Game1.player.GetGrabTile();
    }

    private static Vector2 GetToolTile(ICursorPosition? cursor = null)
    {
        Vector2 pos = cursor != null
            ? Game1.player.GetToolLocation(cursor.AbsolutePixels)
            : Game1.player.GetToolLocation();
        return new Vector2((int)(pos.X / Game1.tileSize), (int)(pos.Y / Game1.tileSize));
    }

    private static bool TileHasOtherOccupant(GameLocation location, Vector2 tile)
    {
        if (location.isCharacterAtTile(tile) != null)
            return true;

        if (!location.objects.TryGetValue(tile, out var obj))
            return false;

        return !IsLockedSign(obj);
    }

    private static bool IsLockedSignTargetAt(GameLocation location, Vector2 tile)
    {
        if (TileHasOtherOccupant(location, tile))
            return false;

        if (location.objects.TryGetValue(tile, out var obj))
            return IsLockedSign(obj);

        if (location.objects.TryGetValue(tile + new Vector2(0, 1), out var below) && IsLockedSign(below))
            return true;

        return location.objects.TryGetValue(tile + new Vector2(0, -1), out var above) && IsLockedSign(above);
    }

    private static bool IsLockedSign(StardewValley.Object obj)
    {
        return obj is Sign sign && sign.displayItem.Value != null;
    }

    private bool IsDownOrSuppressed(SButton button)
    {
        return Helper.Input.IsDown(button) || Helper.Input.IsSuppressed(button);
    }

    private static IEnumerable<SButton> InteractionButtons()
    {
        yield return SButton.MouseLeft;
        yield return SButton.MouseRight;
        yield return SButton.ControllerA;
        yield return SButton.ControllerX;

        foreach (var input in Game1.options.actionButton)
            yield return input.ToSButton();
        foreach (var input in Game1.options.useToolButton)
            yield return input.ToSButton();
    }

    private IEnumerable<SButton> GetHeldInteractionButtons()
    {
        foreach (SButton button in InteractionButtons())
        {
            if (IsDownOrSuppressed(button))
                yield return button;
        }
    }

    private bool HasHeldInteractionButton()
    {
        foreach (SButton _ in GetHeldInteractionButtons())
            return true;
        return false;
    }
}

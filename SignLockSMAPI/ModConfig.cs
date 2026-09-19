using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace SignLockSMAPI;

public class ModConfig
{
    public KeybindList BypassKey { get; set; } = KeybindList.Parse("LeftControl");
}

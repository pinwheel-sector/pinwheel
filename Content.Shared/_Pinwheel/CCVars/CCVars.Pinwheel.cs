using Robust.Shared.Configuration;

namespace Content.Shared._Pinwheel.CCVars;

[CVarDefs]
public sealed class CCVars_Pinwheel
{
    /// <summary>
    /// Should the content warning get displayed?
    /// </summary>
    public static readonly CVarDef<bool> ContentWarningDisplay =
        CVarDef.Create("contentwarning.display", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Should rejecting the content warning quit the game?
    /// </summary>
    public static readonly CVarDef<bool> ContentWarningQuitOnReject =
        CVarDef.Create("contentwarning.quit", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Has the content warning been acknowledged?
    /// </summary>
    public static readonly CVarDef<bool> ContentWarningAcknowledged =
        CVarDef.Create("contentwarning.acknowledged", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}

using Robust.Shared.Configuration;

namespace Content.Shared._Pinwheel.CCVars;

[CVarDefs]
public sealed class CCVars_Pinwheel
{
    /// <summary>
    /// Should the content warning get displayed?
    /// </summary>
    public static readonly CVarDef<bool> ContentWarningDisplay =
        CVarDef.Create("cw.display", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Should ignoring the content warning kick you from the server?
    /// </summary>
    public static readonly CVarDef<bool> ContentWarningKickOnIgnore =
        CVarDef.Create("cw.kick", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Has the content warning been acknowledged?
    /// </summary>
    public static readonly CVarDef<bool> ContentWarningAcknowledged =
        CVarDef.Create("cw.acknowledged", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}

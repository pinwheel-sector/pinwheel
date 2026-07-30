using Content.Shared.Popups;
using Content.Shared.Actions.Events;
using Content.Shared.Alert;
using Content.Shared.IdentityManagement;
using Content.Shared.Paper;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Abilities.Mime;

public sealed partial class MimePowersSystem : EntitySystem
{
    public static readonly EntProtoId MutedEffect = "StatusEffectMimeMuted";

    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private AlertsSystem _alertsSystem = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimePowersComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MimePowersComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<MimePowersComponent, BreakVowAlertEvent>(OnBreakVowAlert);
        SubscribeLocalEvent<MimePowersComponent, RetakeVowAlertEvent>(OnRetakeVowAlert);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        // Queue to track whether mimes can retake vows yet

        var query = EntityQueryEnumerator<MimePowersComponent>();
        while (query.MoveNext(out var uid, out var mime))
        {
            if (!mime.VowBroken || mime.ReadyToRepent)
                continue;

            if (_timing.CurTime < mime.VowRepentTime)
                continue;

            mime.ReadyToRepent = true;
            Dirty(uid, mime);
            _popupSystem.PopupEntity(Loc.GetString("mime-ready-to-repent"), uid, uid);
        }
    }

    private void OnMapInit(Entity<MimePowersComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.VowBroken)
            _statusEffects.TrySetStatusEffectDuration(ent, MutedEffect);

        if (ent.Comp.PreventWriting)
        {
            EnsureComp<BlockWritingComponent>(ent, out var illiterateComponent);
            illiterateComponent.FailWriteMessage = ent.Comp.FailWriteMessage;
            Dirty(ent, illiterateComponent);
        }
    }

    private void OnComponentShutdown(Entity<MimePowersComponent> ent, ref ComponentShutdown args)
    {
        _statusEffects.TryRemoveStatusEffect(ent, MutedEffect);
    }

    private void OnBreakVowAlert(Entity<MimePowersComponent> ent, ref BreakVowAlertEvent args)
    {
        if (args.Handled)
            return;

        BreakVow(ent, ent);
        args.Handled = true;
    }

    private void OnRetakeVowAlert(Entity<MimePowersComponent> ent, ref RetakeVowAlertEvent args)
    {
        if (args.Handled)
            return;

        RetakeVow(ent, ent);
        args.Handled = true;
    }

    /// <summary>
    /// Break this mime's vow to not speak.
    /// </summary>
    public void BreakVow(EntityUid uid, MimePowersComponent? mimePowers = null)
    {
        if (!Resolve(uid, ref mimePowers))
            return;

        if (mimePowers.VowBroken)
            return;

        mimePowers.Enabled = false;
        mimePowers.VowBroken = true;
        mimePowers.VowRepentTime = _timing.CurTime + mimePowers.VowCooldown;
        Dirty(uid, mimePowers);
        _statusEffects.TryRemoveStatusEffect(uid, MutedEffect);
        if (mimePowers.PreventWriting)
            RemComp<BlockWritingComponent>(uid);

        _alertsSystem.ShowAlert(uid, mimePowers.VowBrokenAlert);
    }

    /// <summary>
    /// Retake this mime's vow to not speak.
    /// </summary>
    public void RetakeVow(EntityUid uid, MimePowersComponent? mimePowers = null)
    {
        if (!Resolve(uid, ref mimePowers))
            return;

        if (!mimePowers.ReadyToRepent)
        {
            _popupSystem.PopupEntity(Loc.GetString("mime-not-ready-repent"), uid, uid);
            return;
        }

        mimePowers.Enabled = true;
        mimePowers.ReadyToRepent = false;
        mimePowers.VowBroken = false;
        Dirty(uid, mimePowers);
        _statusEffects.TrySetStatusEffectDuration(uid, MutedEffect);
        if (mimePowers.PreventWriting)
        {
            EnsureComp<BlockWritingComponent>(uid, out var illiterateComponent);
            illiterateComponent.FailWriteMessage = mimePowers.FailWriteMessage;
            Dirty(uid, illiterateComponent);
        }

        _alertsSystem.ClearAlert(uid, mimePowers.VowBrokenAlert);
    }
}

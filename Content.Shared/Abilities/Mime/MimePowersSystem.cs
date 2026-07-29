using Content.Shared.Popups;
using Content.Shared.Alert;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.IdentityManagement;
using Content.Shared.Paper;
using Content.Shared.Physics;
using Content.Shared.Speech.Muting;
using Robust.Shared.Timing;

namespace Content.Shared.Abilities.Mime;

public sealed partial class MimePowersSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private AlertsSystem _alertsSystem = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimePowersComponent, ComponentInit>(OnComponentInit);
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

    private void OnComponentInit(Entity<MimePowersComponent> ent, ref ComponentInit args)
    {
        EnsureComp<MutedComponent>(ent);

        if (ent.Comp.PreventWriting)
        {
            EnsureComp<BlockWritingComponent>(ent, out var illiterateComponent);
            illiterateComponent.FailWriteMessage = ent.Comp.FailWriteMessage;
            Dirty(ent, illiterateComponent);
        }

        _alertsSystem.ShowAlert(ent.Owner, ent.Comp.VowAlert);
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
        RemComp<MutedComponent>(uid);
        if (mimePowers.PreventWriting)
            RemComp<BlockWritingComponent>(uid);

        _alertsSystem.ClearAlert(uid, mimePowers.VowAlert);
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
        AddComp<MutedComponent>(uid);
        if (mimePowers.PreventWriting)
        {
            EnsureComp<BlockWritingComponent>(uid, out var illiterateComponent);
            illiterateComponent.FailWriteMessage = mimePowers.FailWriteMessage;
            Dirty(uid, illiterateComponent);
        }

        _alertsSystem.ClearAlert(uid, mimePowers.VowBrokenAlert);
        _alertsSystem.ShowAlert(uid, mimePowers.VowAlert);
    }
}

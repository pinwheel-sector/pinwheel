using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Shared._Pinwheel.CCVars;
using Robust.Client;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;

namespace Content.Client._Pinwheel.ContentWarning;

public sealed partial class ContentWarningUIController
    : UIController, IOnStateEntered<LobbyState>, IOnStateEntered<GameplayState>
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameController _gameController = default!;

    private ContentWarningPopup? _window;

    private void AttemptOpenContentWarningPopup()
    {
        if (!_cfg.GetCVar(CCVars_Pinwheel.ContentWarningDisplay)
            || _cfg.GetCVar(CCVars_Pinwheel.ContentWarningAcknowledged))
            return;

        OpenContentWarningPopup();
    }

    public void OnStateEntered(LobbyState _)
    {
        AttemptOpenContentWarningPopup();
    }

    public void OnStateEntered(GameplayState _)
    {
        AttemptOpenContentWarningPopup();
    }

    private void OpenContentWarningPopup()
    {
        if (_window != null)
            return;

        _window = new ContentWarningPopup();
        _window.OpenCentered();
        _window.OnContentWarningReject += () =>
        {
            _window.Close();
            _window = null;

            if (_cfg.GetCVar(CCVars_Pinwheel.ContentWarningQuitOnReject))
                _gameController.Shutdown("content warning rejected");
        };
        _window.OnContentWarningAccept += () =>
        {
            _window.Close();
            _window = null;
            _cfg.SetCVar(CCVars_Pinwheel.ContentWarningAcknowledged, true);
            _cfg.SaveToFile();
        };
    }
}

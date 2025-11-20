using MudBlazor;

namespace EventForge.Client.Services.UI;

/// <summary>
/// Service implementation for managing authentication dialogs
/// </summary>
public class AuthenticationDialogService : IAuthenticationDialogService
{
    private readonly IDialogService _dialogService;

    public AuthenticationDialogService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task<bool> ShowLoginDialogAsync()
    {
        var options = new DialogOptions
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            BackdropClick = false,
            NoHeader = false
        };

        var dialog = await _dialogService.ShowAsync<Shared.Components.UI.Dialogs.Common.LoginDialog>("", options);
        var result = await dialog.Result;

        return !result.Canceled;
    }
}

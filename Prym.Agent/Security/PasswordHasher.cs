// This file is kept for source-compatibility only.
// The canonical implementation now lives in Prym.UpdateShared.Security.PasswordHasher.
// Both Agent and Hub reference that shared copy — no behaviour change.

namespace Prym.Agent.Security;

/// <summary>
/// Forwards all calls to <see cref="Prym.UpdateShared.Security.PasswordHasher"/>.
/// Kept so existing callers within <c>Prym.Agent</c> compile without changes.
/// </summary>
public static class PasswordHasher
{
    /// <inheritdoc cref="Prym.UpdateShared.Security.PasswordHasher.IsHashed"/>
    public static bool IsHashed(string stored) =>
        Prym.UpdateShared.Security.PasswordHasher.IsHashed(stored);

    /// <inheritdoc cref="Prym.UpdateShared.Security.PasswordHasher.Hash"/>
    public static string Hash(string password) =>
        Prym.UpdateShared.Security.PasswordHasher.Hash(password);

    /// <inheritdoc cref="Prym.UpdateShared.Security.PasswordHasher.Verify"/>
    public static bool Verify(string password, string stored) =>
        Prym.UpdateShared.Security.PasswordHasher.Verify(password, stored);
}

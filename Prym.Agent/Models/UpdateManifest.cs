// This file is kept for source-compatibility only.
// The canonical implementations now live in Prym.UpdateShared.Models.
// Both Agent and Hub reference that shared copy — no behaviour change.

namespace Prym.Agent.Models;

/// <summary>
/// Forwards to <see cref="Prym.UpdateShared.Models.UpdateManifest"/>.
/// Kept so existing callers within <c>Prym.Agent</c> compile without changes.
/// </summary>
public class UpdateManifest : Prym.UpdateShared.Models.UpdateManifest { }

/// <summary>
/// Forwards to <see cref="Prym.UpdateShared.Models.UpdatePhase"/>.
/// Kept so existing callers within <c>Prym.Agent</c> compile without changes.
/// </summary>
public enum UpdatePhase
{
    Downloading                = Prym.UpdateShared.Models.UpdatePhase.Downloading,
    VerifyingChecksum          = Prym.UpdateShared.Models.UpdatePhase.VerifyingChecksum,
    AwaitingMaintenanceWindow  = Prym.UpdateShared.Models.UpdatePhase.AwaitingMaintenanceWindow,
    BackingUp                  = Prym.UpdateShared.Models.UpdatePhase.BackingUp,
    RunningPreMigrations       = Prym.UpdateShared.Models.UpdatePhase.RunningPreMigrations,
    StoppingService            = Prym.UpdateShared.Models.UpdatePhase.StoppingService,
    DeployingBinaries          = Prym.UpdateShared.Models.UpdatePhase.DeployingBinaries,
    StartingService            = Prym.UpdateShared.Models.UpdatePhase.StartingService,
    RunningPostMigrations      = Prym.UpdateShared.Models.UpdatePhase.RunningPostMigrations,
    VerifyingHealth            = Prym.UpdateShared.Models.UpdatePhase.VerifyingHealth,
    VerifyingDeploy            = Prym.UpdateShared.Models.UpdatePhase.VerifyingDeploy,
    Rollback                   = Prym.UpdateShared.Models.UpdatePhase.Rollback,
    Completed                  = Prym.UpdateShared.Models.UpdatePhase.Completed
}

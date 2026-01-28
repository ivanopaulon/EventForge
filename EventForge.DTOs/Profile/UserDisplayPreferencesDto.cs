using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Profile;

/// <summary>
/// DTO per le preferenze di visualizzazione dell'utente
/// </summary>
public class UserDisplayPreferencesDto
{
    /// <summary>
    /// Famiglia font principale
    /// </summary>
    [MaxLength(50)]
    public string PrimaryFontFamily { get; set; } = "Noto Sans";
    
    /// <summary>
    /// Famiglia font monospace
    /// </summary>
    [MaxLength(50)]
    public string MonospaceFontFamily { get; set; } = "Noto Sans Mono";
    
    /// <summary>
    /// Dimensione base del font (in px)
    /// </summary>
    [Range(12, 24)]
    public int BaseFontSize { get; set; } = 16;
    
    /// <summary>
    /// Tema preferito (già esistente nel sistema)
    /// </summary>
    [MaxLength(50)]
    public string PreferredTheme { get; set; } = "carbon-neon-light";
    
    /// <summary>
    /// Abilita varianti font con supporto multilingua esteso
    /// </summary>
    public bool EnableExtendedFonts { get; set; } = true;
    
    /// <summary>
    /// Usa font di sistema per performance (fallback)
    /// </summary>
    public bool UseSystemFonts { get; set; } = false;
}

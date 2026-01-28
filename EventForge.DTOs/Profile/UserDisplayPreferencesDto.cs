using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Profile;

/// <summary>
/// DTO per le preferenze di visualizzazione dell'utente - Sistema Font Noto Multi-Contesto
/// </summary>
public class UserDisplayPreferencesDto
{
    /// <summary>
    /// Font Noto per il testo principale del corpo
    /// Usato in: paragrafi, descrizioni, labels, form fields, card content
    /// </summary>
    [MaxLength(50)]
    public string BodyFont { get; set; } = "Noto Sans";
    
    /// <summary>
    /// Font Noto per i titoli (h1, h2, h3, etc.)
    /// Usato in: page titles, card headers, dialog titles, section headers
    /// </summary>
    [MaxLength(50)]
    public string HeadingsFont { get; set; } = "Noto Sans Display";
    
    /// <summary>
    /// Font Noto per codice e testo monospace (sempre Noto Sans Mono)
    /// Usato in: code blocks, JSON viewer, logs, technical data
    /// </summary>
    [MaxLength(50)]
    public string MonospaceFont { get; set; } = "Noto Sans Mono";
    
    /// <summary>
    /// Font Noto per contenuti lunghi/articoli
    /// Usato in: help pages, documentation, long-form content
    /// </summary>
    [MaxLength(50)]
    public string ContentFont { get; set; } = "Noto Serif";
    
    /// <summary>
    /// Dimensione base del font (in px)
    /// Range: 12-24px per accessibilità WCAG
    /// </summary>
    [Range(12, 24, ErrorMessage = "Font size must be between 12 and 24 pixels")]
    public int BaseFontSize { get; set; } = 16;
    
    /// <summary>
    /// Usa font di sistema invece di Google Fonts (per performance)
    /// </summary>
    public bool UseSystemFonts { get; set; } = false;
    
    /// <summary>
    /// Abilita subset per lingue specifiche (arabo, ebraico, giapponese, etc.)
    /// </summary>
    public bool EnableExtendedScripts { get; set; } = false;
    
    /// <summary>
    /// Liste di subset linguistici da caricare
    /// Valori possibili: "arabic", "hebrew", "japanese", "korean", "thai", "devanagari"
    /// </summary>
    public List<string> EnabledScripts { get; set; } = new();
    
    // Legacy properties for backward compatibility - will be removed in future versions
    [MaxLength(50)]
    [Obsolete("Use BodyFont instead")]
    public string? PrimaryFontFamily { get; set; }
    
    [MaxLength(50)]
    [Obsolete("Use MonospaceFont instead")]
    public string? MonospaceFontFamily { get; set; }
    
    [MaxLength(50)]
    public string PreferredTheme { get; set; } = "carbon-neon-light";
    
    [Obsolete("Use EnableExtendedScripts instead")]
    public bool EnableExtendedFonts { get; set; } = true;
}

# Frontend Development Documentation

Documentazione completa per lo sviluppo frontend di EventForge con Blazor Server e MudBlazor.

## 📋 Indice

### 🎨 UI/UX Design
- [UI Guidelines](./ui-guidelines.md) - Linee guida UI/UX e layout
- [Theming System](./theming.md) - Sistema di temi e personalizzazione
- [Component Standards](./component-standards.md) - Standard componenti MudBlazor
- [Responsive Design](./responsive-design.md) - Design responsivo

### 🌍 Internazionalizzazione
- [Translation System](./translation.md) - Sistema di traduzione e localizzazione
- [Language Support](./language-support.md) - Supporto multilingua
- [Translation Best Practices](./translation-best-practices.md) - Best practice traduzioni

### 🧩 Componenti
- [MudBlazor Components](./mudblazor.md) - Guida componenti MudBlazor
- [Custom Components](./custom-components.md) - Componenti personalizzati
- [Forms & Validation](./forms-validation.md) - Form e validazione
- [Navigation System](./navigation.md) - Sistema di navigazione

### 📱 User Experience
- [Performance Optimization](./performance.md) - Ottimizzazione performance
- [Accessibility Guidelines](./accessibility.md) - Linee guida accessibilità
- [User Interaction Patterns](./interaction-patterns.md) - Pattern di interazione

## 🚀 Quick Start per Frontend Developers

1. **Setup**: Leggi [Project Structure](../core/project-structure.md)
2. **UI Guidelines**: Inizia con [UI Guidelines](./ui-guidelines.md)
3. **Components**: Consulta [MudBlazor Components](./mudblazor.md)
4. **Theming**: Implementa con [Theming System](./theming.md)
5. **Translations**: Aggiungi traduzioni con [Translation System](./translation.md)

## 🎨 Design System

### Color Themes
EventForge supporta 6 temi colore:
- **Light Theme** - Tema chiaro moderno
- **Dark Theme** - Tema scuro classico
- **Warm Theme** - Tema caldo con tonalità terrose
- **Cool Theme** - Tema fresco con colori naturali
- **High Contrast** - Tema ad alto contrasto (WCAG AAA)
- **Fun Theme** - Tema divertente con colori vivaci

### Component Guidelines
- **MudCard**: Layout vertical stack con spacing uniforme
- **MudIconButton**: Icone semantiche con contrasto appropriato
- **MudTooltip**: Tooltip tradotte per accessibilità
- **MudTable**: Colonne ordinabili con DataLabel per mobile

### Layout Principles
1. **Mobile-First**: Design ottimizzato per mobile
2. **Progressive Enhancement**: Funzionalità aggiuntive per desktop
3. **Consistent Spacing**: Padding e margin uniformi
4. **Semantic Structure**: Markup semantico per accessibilità

## 📊 Performance Best Practices

### Blazor Server Optimization
- Uso minimale di JavaScript interop
- Componenti stateless quando possibile
- Lazy loading per dati pesanti
- Debouncing per input utente

### MudBlazor Optimization
- Riutilizzo componenti
- Binding ottimizzato
- Virtualizzazione per liste lunghe
- Caching locale appropriato

## 🔧 Development Tools

### Debugging
- Blazor Developer Tools
- Browser DevTools integration
- SignalR connection monitoring
- Performance profiling

### Testing
- Component unit testing
- UI integration testing
- Accessibility testing
- Cross-browser testing

## 🔗 Collegamenti Utili

- [Backend Documentation](../backend/) - Documentazione backend
- [Testing Guidelines](../testing/) - Linee guida testing
- [Core Documentation](../core/) - Documentazione core
- [Feature Guides](../features/) - Guide funzionalità specifiche
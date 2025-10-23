// Runtime helper to apply EventForge (ef-*) classes to MudBlazor components
// Adds classes to existing and dynamically inserted MudBlazor elements to avoid editing many Razor files.
// Runs on DOMContentLoaded and observes DOM mutations to apply classes to new nodes.
(function () {
 if (window.__efMudClassesApplied) return;
 window.__efMudClassesApplied = true;

 const applyToExisting = () => {
 try {
 // Buttons
 document.querySelectorAll('button.mud-button, button.mud-icon-button').forEach(btn => {
 if (!btn.classList.contains('ef-btn') && btn.matches('.mud-button')) {
 btn.classList.add('ef-btn');
 }
 if (btn.classList.contains('mud-icon-button')) {
 btn.classList.add('ef-icon-btn');
 }
 });

 // Inputs (text fields, selects, textareas)
 document.querySelectorAll('.mud-input-root, .mud-input-slot').forEach(el => {
 // Add ef-input to nearest input wrapper
 const root = el.closest('.mud-input-root') || el.closest('.mud-input');
 if (root && !root.classList.contains('ef-input')) {
 root.classList.add('ef-input');
 }
 });

 // Selects
 document.querySelectorAll('.mud-select-root, .mud-select').forEach(el => {
 if (!el.classList.contains('ef-select')) el.classList.add('ef-select');
 });

 // Textareas
 document.querySelectorAll('textarea.mud-input-control').forEach(el => {
 const root = el.closest('.mud-input-root') || el.closest('.mud-input');
 if (root && !root.classList.contains('ef-textarea')) root.classList.add('ef-textarea');
 });

 // Tables
 document.querySelectorAll('.mud-table').forEach(tbl => {
 if (!tbl.classList.contains('ef-table')) tbl.classList.add('ef-table');
 });

 // Chips
 document.querySelectorAll('.mud-chip').forEach(chip => {
 if (!chip.classList.contains('ef-chip-medium')) chip.classList.add('ef-chip-medium');
 });

 // Avatars
 document.querySelectorAll('.mud-avatar').forEach(av => {
 if (!av.classList.contains('ef-avatar-md')) {
 // size detection fallback
 av.classList.add('ef-avatar-md');
 }
 });

 // Drawers
 document.querySelectorAll('.mud-drawer').forEach(d => {
 if (!d.classList.contains('ef-drawer')) d.classList.add('ef-drawer');
 });

 // Action groups / toolbars
 document.querySelectorAll('.mud-toolbar-content, .mud-toolbar, .mud-tool-bar').forEach(tb => {
 if (!tb.classList.contains('ef-action-group')) tb.classList.add('ef-action-group', 'toolbar');
 });

 // Dialog action buttons
 document.querySelectorAll('.mud-dialog .mud-button, .mud-dialog .mud-icon-button').forEach(btn => {
 if (!btn.classList.contains('ef-btn')) btn.classList.add('ef-btn');
 });

 // Tables: toolbar areas
 document.querySelectorAll('.mud-table .mud-table-toolbar, .mud-table-toolbar').forEach(el => {
 if (!el.classList.contains('ef-table-toolbar')) el.classList.add('ef-table-toolbar');
 });
 }
 catch (e) {
 console.error('apply-mud-classes error', e);
 }
 };

 // Run on initial load
 document.addEventListener('DOMContentLoaded', () => {
 applyToExisting();

 // Observe mutations to apply to dynamic content
 const observer = new MutationObserver((mutations) => {
 let needs = false;
 for (const m of mutations) {
 if (m.addedNodes && m.addedNodes.length) { needs = true; break; }
 }
 if (needs) applyToExisting();
 });
 observer.observe(document.body, { childList: true, subtree: true });
 });
})();

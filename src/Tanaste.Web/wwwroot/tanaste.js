// tanaste.js — Global JavaScript helpers for Tanaste Dashboard

/**
 * Toggles theme classes on <body> to drive CSS custom properties and logo
 * colour adaptation.  Called from MainLayout whenever the theme is toggled
 * or the page first renders.
 *
 * @param {boolean} isDark - true when the Dashboard is in dark mode.
 */
window.setThemeClass = function (isDark) {
    document.body.classList.toggle('tanaste-dark', isDark);
    document.body.classList.toggle('tanaste-light', !isDark);
};

/**
 * Registers a global Ctrl+K (or Cmd+K on Mac) keydown listener that invokes
 * the .NET OpenPalette() method on the provided DotNetObjectReference.
 *
 * Called once from MainLayout.OnAfterRenderAsync.
 *
 * @param {DotNetObjectReference} dotNetRef - Reference to the MainLayout component.
 */
window.registerCtrlK = function (dotNetRef) {
    // Guard: avoid double-registering on hot-reload.
    if (window._tanasteCtrlKRegistered) return;
    window._tanasteCtrlKRegistered = true;

    document.addEventListener('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OpenPalette');
        }
    });
};

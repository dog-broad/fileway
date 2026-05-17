// Loaded before blazor.webassembly.js to prevent flash of unstyled content
window.ThemeInterop = {
    initialize: function () {
        var theme = localStorage.getItem('theme') || 'dark';
        document.documentElement.setAttribute('data-theme', theme);
        return theme;
    },
    getTheme: function () {
        return localStorage.getItem('theme') || 'dark';
    },
    setTheme: function (theme) {
        localStorage.setItem('theme', theme);
        document.documentElement.setAttribute('data-theme', theme);
    }
};

ThemeInterop.initialize();

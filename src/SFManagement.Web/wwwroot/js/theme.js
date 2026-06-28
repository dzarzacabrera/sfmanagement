(function () {
    var saved = localStorage.getItem('sfm-theme');
    if (saved === 'dark' || (!saved && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
        document.documentElement.classList.add('dark');
    }

    window.toggleTheme = function () {
        var html = document.documentElement;
        var isDark = html.classList.toggle('dark');
        localStorage.setItem('sfm-theme', isDark ? 'dark' : 'light');
    };

    window.toggleSidebar = function () {
        document.body.classList.toggle('sidebar-open');
    };

    document.addEventListener('click', function (e) {
        if (e.target.id === 'sidebar-backdrop') {
            document.body.classList.remove('sidebar-open');
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            document.body.classList.remove('sidebar-open');
        }
    });
})();

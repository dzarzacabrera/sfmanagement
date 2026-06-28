(function () {
    var container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'fixed bottom-4 right-4 z-[60] flex flex-col gap-2 pointer-events-none';
        document.body.appendChild(container);
    }

    window.showToast = function (message, type) {
        type = type || 'info';
        var colors = {
            success: 'bg-green-600 text-white',
            error: 'bg-red-600 text-white',
            info: 'bg-indigo-600 text-white',
            warning: 'bg-amber-500 text-white'
        };
        var icons = {
            success: '<svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>',
            error: '<svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>',
            info: '<svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>',
            warning: '<svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z"/></svg>'
        };

        var toast = document.createElement('div');
        toast.className = 'pointer-events-auto flex items-center gap-2 px-4 py-3 rounded-lg shadow-lg text-sm font-medium translate-x-full opacity-0 transition-all duration-300 ' + (colors[type] || colors.info);
        toast.innerHTML = (icons[type] || '') + '<span>' + message + '</span>';
        container.appendChild(toast);

        requestAnimationFrame(function () {
            toast.classList.remove('translate-x-full', 'opacity-0');
        });

        setTimeout(function () {
            toast.classList.add('translate-x-full', 'opacity-0');
            setTimeout(function () { toast.remove(); }, 300);
        }, 3000);
    };
})();

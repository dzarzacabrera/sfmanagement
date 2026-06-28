(function () {
    document.addEventListener('mouseover', function (e) {
        var target = e.target.closest('[data-tooltip]');
        if (!target) return;
        var text = target.dataset.tooltip;
        if (!text) return;

        var tip = document.createElement('div');
        tip.className = 'fixed z-50 px-2 py-1 rounded text-xs font-medium shadow-lg pointer-events-none';
        tip.style.background = '#1f2937';
        tip.style.color = '#f9fafb';
        tip.style.whiteSpace = 'nowrap';
        tip.textContent = text;

        document.body.appendChild(tip);

        var rect = target.getBoundingClientRect();
        var top = rect.top - tip.offsetHeight - 6;
        var left = rect.left + (rect.width - tip.offsetWidth) / 2;

        if (top < 0) top = rect.bottom + 6;

        tip.style.top = top + 'px';
        tip.style.left = Math.max(4, Math.min(left, window.innerWidth - tip.offsetWidth - 4)) + 'px';

        target._tooltipEl = tip;
    });

    document.addEventListener('mouseout', function (e) {
        var target = e.target.closest('[data-tooltip]');
        if (!target || !target._tooltipEl) return;
        target._tooltipEl.remove();
        target._tooltipEl = null;
    });
})();

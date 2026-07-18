(function () {
    var typeColors = {
        success: { bg: '#f0fdf4', border: '#86efac', text: '#15803d', cls: 'bg-green-50 border-green-300 text-green-700' },
        error: { bg: '#fef2f2', border: '#fca5a5', text: '#b91c1c', cls: 'bg-red-50 border-red-300 text-red-700' },
        info: { bg: '#eff6ff', border: '#93c5fd', text: '#1d4ed8', cls: 'bg-blue-50 border-blue-300 text-blue-700' },
        warning: { bg: '#fffbeb', border: '#fcd34d', text: '#b45309', cls: 'bg-amber-50 border-amber-300 text-amber-700' }
    };

    var icons = {
        success: '<svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>',
        error: '<svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>',
        info: '<svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>',
        warning: '<svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z"/></svg>'
    };

    window.showToast = function (message, type, triggerEl, refEl) {
        type = type || 'info';
        var c = typeColors[type] || typeColors.info;
        var isMobile = window.innerWidth < 640;

        var msg = document.createElement('div');
        var maxW = isMobile ? Math.round(window.innerWidth * 0.85) : 360;
        msg.style.cssText = 'position:fixed;z-index:9999;border-radius:8px;box-shadow:0 10px 15px -3px rgba(0,0,0,.1);font-size:14px;font-weight:500;border:1px solid ' + c.border + ';background:' + c.bg + ';color:' + c.text + ';opacity:0;transition:opacity .2s ease;max-width:' + maxW + 'px';

        var closeBtn = document.createElement('button');
        closeBtn.textContent = '\u00D7';
        closeBtn.style.cssText = 'position:absolute;top:6px;right:6px;width:28px;height:28px;padding:0;display:flex;align-items:center;justify-content:center;border:none;border-radius:50%;background:transparent;color:' + c.text + ';font-size:22px;line-height:1;cursor:pointer;opacity:.6';
        closeBtn.addEventListener('mouseenter', function () { closeBtn.style.opacity = '1'; });
        closeBtn.addEventListener('mouseleave', function () { closeBtn.style.opacity = '.6'; });
        closeBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            msg.style.opacity = '0';
            setTimeout(function () { msg.remove(); }, 200);
        });

        var body = document.createElement('div');
        body.style.cssText = 'display:flex;align-items:flex-start;gap:8px;padding:14px 32px 10px 14px';
        body.innerHTML = '<span style="flex:1;min-width:0;line-height:1.4">' + message + '</span>';

        msg.appendChild(closeBtn);
        msg.appendChild(body);

        var arrow = document.createElement('div');
        arrow.style.cssText = 'position:absolute;left:50%;bottom:-6px;width:12px;height:12px;background:' + c.bg + ';border-color:' + c.border + ';border-width:0 1px 1px 0;border-style:solid;transform:translateX(-50%) rotate(45deg);';
        msg.appendChild(arrow);

        if (isMobile) {
            msg.style.top = '50%';
            msg.style.left = '50%';
            msg.style.transform = 'translate(-50%, -50%)';
            arrow.style.display = 'none';
        } else if (refEl) {
            var rect = refEl.getBoundingClientRect();
            msg.style.bottom = (window.innerHeight - rect.top + 8) + 'px';
            var msgW = Math.min(maxW, window.innerWidth - 16);
            var left = rect.left + rect.width / 2 - msgW / 2;
            if (left < 8) left = 8;
            if (left + msgW > window.innerWidth - 8) left = window.innerWidth - 8 - msgW;
            msg.style.left = left + 'px';
        } else if (triggerEl) {
            var rect = triggerEl.getBoundingClientRect();
            msg.style.bottom = (window.innerHeight - rect.top + 12) + 'px';
            var msgW = Math.min(360, window.innerWidth - 16);
            var left = rect.left + rect.width / 2 - msgW / 2;
            if (left < 8) left = 8;
            if (left + msgW > window.innerWidth - 8) left = window.innerWidth - 8 - msgW;
            msg.style.left = left + 'px';
        } else {
            msg.style.top = '16px';
            msg.style.left = '50%';
            msg.style.transform = 'translateX(-50%)';
            arrow.style.display = 'none';
        }

        document.body.appendChild(msg);
        requestAnimationFrame(function () { msg.style.opacity = '1'; });

        setTimeout(function () {
            msg.style.opacity = '0';
            setTimeout(function () { msg.remove(); }, 200);
        }, 3000);
    };

    window.showUndoToast = function (message, onUndo, refEl) {
        var msg = document.createElement('div');
        var isMobile = window.innerWidth < 640;
        msg.style.cssText = 'position:fixed;z-index:9999;display:flex;align-items:center;gap:8px;padding:10px 16px;border-radius:8px;box-shadow:0 10px 15px -3px rgba(0,0,0,.1);font-size:14px;font-weight:500;background:#fff;color:#111827;border:1px solid #e5e7eb;opacity:0;transition:opacity .2s ease';
        msg.innerHTML = '<svg class="w-4 h-4 shrink-0 text-brand" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg><span>' + message + '</span><button class="ml-2 px-2 py-1 rounded text-sm font-semibold text-white" style="background:#21668f;border:none;cursor:pointer">Undo</button>';

        if (isMobile) {
            msg.style.top = '50%';
            msg.style.left = '50%';
            msg.style.transform = 'translate(-50%, -50%)';
        } else if (refEl) {
            var rect = refEl.getBoundingClientRect();
            msg.style.top = (rect.bottom + 8) + 'px';
            var msgW = Math.min(280, window.innerWidth - 16);
            var left = rect.right + 8;
            if (left + msgW > window.innerWidth - 8) left = rect.left - msgW - 8;
            if (left < 8) left = 8;
            msg.style.left = left + 'px';
        } else {
            msg.style.top = '16px';
            msg.style.left = '50%';
            msg.style.transform = 'translateX(-50%)';
        }

        document.body.appendChild(msg);
        requestAnimationFrame(function () { msg.style.opacity = '1'; });

        msg.querySelector('button').addEventListener('click', function () {
            msg.style.opacity = '0';
            setTimeout(function () { msg.remove(); }, 200);
            if (onUndo) onUndo();
        });

        setTimeout(function () {
            msg.style.opacity = '0';
            setTimeout(function () { msg.remove(); }, 200);
        }, 2500);
    };
})();

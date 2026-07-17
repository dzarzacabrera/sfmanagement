var lastFocusedElement = null;
var isDragging = false;
var dragStartY = 0;
var dragStartScrollTop = 0;
var isMobileModal = false;
var sheetDragY = 0;
var sheetDragging = false;

function applyMobileSheet() {
    var root = document.getElementById('modal-root');
    var panel = document.getElementById('modal-panel');
    var content = document.getElementById('modal-content');
    var dragHeader = document.getElementById('modal-drag-header');
    var closeDesktop = root.querySelector('button[aria-label="Close"].absolute');

    root.classList.remove('items-center', 'p-4');
    root.classList.add('items-end');
    root.style.backgroundColor = 'rgba(0,0,0,0.3)';

    panel.style.borderRadius = '1rem 1rem 0 0';
    panel.style.maxWidth = '100%';
    panel.style.maxHeight = '90dvh';
    panel.style.height = 'auto';
    panel.style.transform = '';
    panel.style.transition = '';

    content.style.height = 'auto';
    content.style.maxHeight = 'calc(90dvh - 48px)';

    var titleDiv = content.querySelector('.shrink-0');
    if (titleDiv) {
        titleDiv.style.paddingTop = '0.25rem';
        titleDiv.style.paddingBottom = '0.75rem';
    }

    dragHeader.classList.remove('hidden');
    dragHeader.classList.add('flex');
    if (closeDesktop) closeDesktop.style.display = 'none';

    setTimeout(function () {
        content.scrollTop = 0;
    }, 0);
}

function removeMobileSheet() {
    var root = document.getElementById('modal-root');
    var panel = document.getElementById('modal-panel');
    var content = document.getElementById('modal-content');
    var dragHeader = document.getElementById('modal-drag-header');
    var closeDesktop = root.querySelector('button[aria-label="Close"].absolute');

    root.classList.add('items-center', 'p-4');
    root.classList.remove('items-end');
    root.style.backgroundColor = '';

    panel.style.borderRadius = '';
    panel.style.maxWidth = '';
    panel.style.maxHeight = '';
    panel.style.height = '';
    panel.style.transform = '';
    panel.style.transition = '';

    content.style.height = '';
    content.style.maxHeight = '';

    var titleDiv = content.querySelector('.shrink-0');
    if (titleDiv) {
        titleDiv.style.paddingTop = '';
        titleDiv.style.paddingBottom = '';
    }

    dragHeader.classList.add('hidden');
    dragHeader.classList.remove('flex');
    if (closeDesktop) closeDesktop.style.display = '';
}

function openModal(html, small) {
    var root = document.getElementById('modal-root');
    var panel = document.getElementById('modal-panel');
    var content = document.getElementById('modal-content');
    lastFocusedElement = document.activeElement;

    document.querySelectorAll('._tooltip-el, [class*="pointer-events-none"][style*="background"]').forEach(function (el) {
        if (el.parentNode) el.remove();
    });

    content.innerHTML = html;

    if (small) {
        panel.classList.add('modal-sm');
    } else {
        panel.classList.remove('modal-sm');
    }

    isMobileModal = window.innerWidth < 640;
    if (isMobileModal) {
        applyMobileSheet();
    } else {
        removeMobileSheet();
    }

    requestAnimationFrame(function () {
        if (content.scrollHeight > content.clientHeight) {
            var forms = content.querySelectorAll('form');
            forms.forEach(function (f) { f.style.paddingRight = '8px'; });
        }
    });
    root.classList.remove('hidden', 'modal-exit-active', 'modal-sheet-exit-active');
    var enterClass = isMobileModal ? 'modal-sheet-enter' : 'modal-enter';
    root.classList.add('flex', enterClass);

    var scrollY = window.scrollY;
    document.body.style.position = 'fixed';
    document.body.style.top = '-' + scrollY + 'px';
    document.body.style.left = '0';
    document.body.style.right = '0';
    document.body.dataset.scrollY = scrollY;

    requestAnimationFrame(function () {
        root.classList.remove(enterClass);
        var activeClass = isMobileModal ? 'modal-sheet-enter-active' : 'modal-enter-active';
        root.classList.add(activeClass);
    });

    setTimeout(function () {
        var firstInput = content.querySelector('input, select, textarea, button');
        if (firstInput) firstInput.focus();
    }, 100);
}

function closeModal() {
    var root = document.getElementById('modal-root');
    var panel = document.getElementById('modal-panel');
    var exitClass = isMobileModal ? 'modal-sheet-exit' : 'modal-exit';
    var exitActiveClass = isMobileModal ? 'modal-sheet-exit-active' : 'modal-exit-active';
    root.classList.remove('modal-enter-active', 'modal-sheet-enter-active');
    root.classList.add(exitClass, exitActiveClass);

    setTimeout(function () {
        root.classList.add('hidden');
        root.classList.remove('flex', exitClass, exitActiveClass);
        panel.classList.remove('modal-sm');
        removeMobileSheet();

        var scrollY = parseInt(document.body.dataset.scrollY || '0');
        document.body.style.position = '';
        document.body.style.top = '';
        document.body.style.left = '';
        document.body.style.right = '';
        document.body.dataset.scrollY = '';
        window.scrollTo(0, scrollY);

        if (lastFocusedElement) {
            lastFocusedElement.focus();
            lastFocusedElement = null;
        }
    }, 150);
}

// --- Drag-to-dismiss on handle header (mobile + desktop) ---
(function () {
    var header = document.getElementById('modal-drag-header');
    if (!header) return;
    var isSheetOpen = false;

    function getSheetBg() {
        return window.innerWidth < 640 ? 0.3 : 0.4;
    }

    header.addEventListener('touchstart', function (e) {
        if (e.target.closest('button')) return;
        isSheetOpen = document.getElementById('modal-root').classList.contains('modal-sheet-enter-active') ||
                       document.getElementById('modal-root').classList.contains('modal-enter-active');
        if (!isSheetOpen) return;
        sheetDragY = e.touches[0].clientY;
        sheetDragging = true;
        var panel = document.getElementById('modal-panel');
        panel.style.transition = 'none';
    }, { passive: true });

    document.addEventListener('touchmove', function (e) {
        if (!sheetDragging) return;
        var delta = e.touches[0].clientY - sheetDragY;
        if (delta < 0) delta = 0;
        var panel = document.getElementById('modal-panel');
        var root = document.getElementById('modal-root');
        panel.style.transform = 'translateY(' + delta + 'px)';
        var opacity = Math.max(0, 1 - delta / 300);
        root.style.backgroundColor = 'rgba(0,0,0,' + (getSheetBg() * opacity) + ')';
    }, { passive: true });

    document.addEventListener('touchend', function () {
        if (!sheetDragging) return;
        sheetDragging = false;
        var panel = document.getElementById('modal-panel');
        var lastDelta = parseFloat(panel.style.transform.replace(/[^0-9.-]/g, '')) || 0;
        panel.style.transition = 'transform 0.2s ease-out';
        if (lastDelta > 80) {
            closeModal();
        } else {
            panel.style.transform = 'translateY(0)';
            var root = document.getElementById('modal-root');
            root.style.backgroundColor = 'rgba(0,0,0,' + getSheetBg() + ')';
        }
    });

    header.addEventListener('mousedown', function (e) {
        if (e.target.closest('button')) return;
        isSheetOpen = document.getElementById('modal-root').classList.contains('modal-sheet-enter-active') ||
                       document.getElementById('modal-root').classList.contains('modal-enter-active');
        if (!isSheetOpen) return;
        sheetDragY = e.clientY;
        sheetDragging = true;
        var panel = document.getElementById('modal-panel');
        panel.style.transition = 'none';
        e.preventDefault();
    });

    document.addEventListener('mousemove', function (e) {
        if (!sheetDragging) return;
        var delta = e.clientY - sheetDragY;
        if (delta < 0) delta = 0;
        var panel = document.getElementById('modal-panel');
        var root = document.getElementById('modal-root');
        panel.style.transform = 'translateY(' + delta + 'px)';
        var opacity = Math.max(0, 1 - delta / 300);
        root.style.backgroundColor = 'rgba(0,0,0,' + (getSheetBg() * opacity) + ')';
    });

    document.addEventListener('mouseup', function () {
        if (!sheetDragging) return;
        sheetDragging = false;
        var panel = document.getElementById('modal-panel');
        var lastDelta = parseFloat(panel.style.transform.replace(/[^0-9.-]/g, '')) || 0;
        panel.style.transition = 'transform 0.2s ease-out';
        if (lastDelta > 80) {
            closeModal();
        } else {
            panel.style.transform = 'translateY(0)';
            var root = document.getElementById('modal-root');
            root.style.backgroundColor = 'rgba(0,0,0,' + getSheetBg() + ')';
        }
    });
})();

// --- Drag to scroll on modal content (desktop) ---
document.addEventListener('mousedown', function (e) {
    var content = document.getElementById('modal-content');
    if (!content || !content.contains(e.target)) return;
    if (e.target.closest('input, select, textarea, button, a, [role="button"], .skill-remove, .skill-pill')) return;

    isDragging = true;
    dragStartY = e.clientY;
    dragStartScrollTop = content.scrollTop;
    content.style.cursor = 'grabbing';
    content.style.userSelect = 'none';
    e.preventDefault();
});

document.addEventListener('mousemove', function (e) {
    if (!isDragging) return;
    var content = document.getElementById('modal-content');
    if (!content) return;
    var deltaY = dragStartY - e.clientY;
    content.scrollTop = dragStartScrollTop + deltaY;
});

document.addEventListener('mouseup', function () {
    if (!isDragging) return;
    isDragging = false;
    var content = document.getElementById('modal-content');
    if (content) {
        content.style.cursor = '';
        content.style.userSelect = '';
    }
});

// --- Close on backdrop click ---
document.addEventListener('click', function (e) {
    if (isDragging) return;
    var root = document.getElementById('modal-root');
    if (e.target === root) closeModal();
});

document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeModal();

    if (e.key === 'Tab') {
        var root = document.getElementById('modal-root');
        if (root.classList.contains('hidden')) return;

        var focusable = root.querySelectorAll('input, select, textarea, button, [tabindex]:not([tabindex="-1"])');
        if (focusable.length === 0) return;

        var first = focusable[0];
        var last = focusable[focusable.length - 1];

        if (e.shiftKey) {
            if (document.activeElement === first) {
                e.preventDefault();
                last.focus();
            }
        } else {
            if (document.activeElement === last) {
                e.preventDefault();
                first.focus();
            }
        }
    }
});

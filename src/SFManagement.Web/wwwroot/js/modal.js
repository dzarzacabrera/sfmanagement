var lastFocusedElement = null;
var isDragging = false;
var dragStartY = 0;
var dragStartScrollTop = 0;

function openModal(html) {
    var root = document.getElementById('modal-root');
    var content = document.getElementById('modal-content');
    lastFocusedElement = document.activeElement;

    document.querySelectorAll('._tooltip-el, [class*="pointer-events-none"][style*="background"]').forEach(function (el) {
        if (el.parentNode) el.remove();
    });

    content.innerHTML = html;
    // Add right padding to form content when scrollable to avoid scrollbar overlap
    requestAnimationFrame(function () {
        if (content.scrollHeight > content.clientHeight) {
            var forms = content.querySelectorAll('form');
            forms.forEach(function (f) { f.style.paddingRight = '8px'; });
        }
    });
    root.classList.remove('hidden', 'modal-exit-active');
    root.classList.add('flex', 'modal-enter');

    // Lock background: prevent scroll on body with position fixed
    var scrollY = window.scrollY;
    document.body.style.position = 'fixed';
    document.body.style.top = '-' + scrollY + 'px';
    document.body.style.left = '0';
    document.body.style.right = '0';
    document.body.dataset.scrollY = scrollY;

    requestAnimationFrame(function () {
        root.classList.remove('modal-enter');
        root.classList.add('modal-enter-active');
    });

    setTimeout(function () {
        var firstInput = content.querySelector('input, select, textarea, button');
        if (firstInput) firstInput.focus();
    }, 100);
}

function closeModal() {
    var root = document.getElementById('modal-root');
    root.classList.remove('modal-enter-active');
    root.classList.add('modal-exit', 'modal-exit-active');

    setTimeout(function () {
        root.classList.add('hidden');
        root.classList.remove('flex', 'modal-exit', 'modal-exit-active');

        // Restore background scroll position
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

// --- Drag to scroll on modal content (desktop) ---
document.addEventListener('mousedown', function (e) {
    var content = document.getElementById('modal-content');
    if (!content || !content.contains(e.target)) return;
    // Ignore clicks on interactive elements
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

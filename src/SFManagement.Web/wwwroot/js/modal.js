var lastFocusedElement = null;

function openModal(html) {
    var root = document.getElementById('modal-root');
    var content = document.getElementById('modal-content');
    lastFocusedElement = document.activeElement;

    content.innerHTML = html;
    root.classList.remove('hidden', 'modal-exit-active');
    root.classList.add('flex', 'modal-enter');
    document.body.style.overflow = 'hidden';

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
        document.body.style.overflow = '';
        if (lastFocusedElement) {
            lastFocusedElement.focus();
            lastFocusedElement = null;
        }
    }, 150);
}

document.addEventListener('click', function (e) {
    var root = document.getElementById('modal-root');
    if (e.target === root) closeModal();
});

document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeModal();

    // Focus trap
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

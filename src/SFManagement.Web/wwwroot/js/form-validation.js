(function () {
    'use strict';

    var errorClass = 'field-error-msg';

    function showError(input, message) {
        clearError(input);
        input.classList.add('border-red-400', 'dark:border-red-500');
        input.classList.remove('border-gray-300', 'dark:border-gray-600');
        var p = document.createElement('p');
        p.className = errorClass + ' text-xs text-red-600 dark:text-red-400 mt-1';
        p.textContent = message;
        input.parentElement.appendChild(p);
    }

    function clearError(input) {
        input.classList.remove('border-red-400', 'dark:border-red-500');
        input.classList.add('border-gray-300', 'dark:border-gray-600');
        var existing = input.parentElement.querySelector('.' + errorClass);
        if (existing) existing.remove();
    }

    function validateForm(form) {
        var valid = true;
        var fields = form.querySelectorAll('input[required]:not([type="hidden"]), textarea[required], select[required]');
        fields.forEach(function (field) {
            if (!field.value.trim()) {
                var label = field.closest('div')?.querySelector('label');
                var name = label ? label.textContent.replace(/\s*\(.*\)\s*$/, '').trim() : 'This field';
                showError(field, name + ' is required.');
                valid = false;
            }
        });
        return valid;
    }

    function clearAll(form) {
        form.querySelectorAll('input[required]:not([type="hidden"]), textarea[required], select[required]').forEach(clearError);
    }

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (form.tagName !== 'FORM') return;
        clearAll(form);
        if (!validateForm(form)) {
            e.preventDefault();
            e.stopImmediatePropagation();
        }
    }, true);

    document.addEventListener('input', function (e) {
        if (e.target.matches('input[required]:not([type="hidden"]), textarea[required], select[required]')) {
            clearError(e.target);
        }
    });
})();

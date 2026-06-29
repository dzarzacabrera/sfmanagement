document.querySelectorAll('.skill-selector').forEach(function (container) {
    const filterInput = container.querySelector('.skill-filter');
    const unselectedBox = container.querySelector('.skill-unselected');
    const selectedBox = container.querySelector('.skill-selected');
    const fieldPrefix = container.dataset.fieldPrefix || '';

    var skills = [];
    try {
        skills = JSON.parse(container.dataset.skills || '[]');
    } catch (e) {
        return;
    }

    var selected = new Map();

    function render() {
        var filter = filterInput.value.toLowerCase();

        // Clean up any lingering tooltips before re-rendering
        document.querySelectorAll('._tooltipEl, [data-tooltip]').forEach(function (el) {
            if (el._tooltipEl) { el._tooltipEl.remove(); el._tooltipEl = null; }
        });

        var unselectedHtml = '';
        skills.forEach(function (s) {
            if (selected.has(s.vectorPosition)) return;
            if (filter && !s.name.toLowerCase().includes(filter)) return;
            unselectedHtml += '<span class="skill-pill cursor-pointer px-3 py-1 rounded-full text-xs font-medium bg-white border border-gray-300 hover:border-brand hover:text-brand transition-colors" data-pos="' + s.vectorPosition + '" data-name="' + s.name.replace(/'/g, '&apos;') + '" data-tooltip="' + s.name + ': click to add">' + s.name + '</span>';
        });
        unselectedBox.innerHTML = unselectedHtml;

        var selectedHtml = '';
        selected.forEach(function (level, pos) {
            var skill = skills.find(function (s) { return s.vectorPosition === pos; });
            var name = skill ? skill.name : 'Skill #' + pos;
            selectedHtml += '<div class="skill-selected-item flex items-center gap-3 p-2 rounded-lg border border-brand-dark/30 bg-brand-light/20" data-pos="' + pos + '">';
            selectedHtml += '<span class="skill-selected-pill px-3 py-1 rounded-full text-xs font-medium bg-brand-light/40 text-brand">' + name + '</span>';
            selectedHtml += '<span class="text-xs text-gray-500">Level:</span>';
            selectedHtml += '<input type="number" class="skill-level w-20 border border-gray-300 rounded px-2 py-1 text-xs" min="0" max="10" step="0.5" value="' + level + '">';
            selectedHtml += '<button type="button" class="skill-remove ml-auto text-gray-400 hover:text-red-500 text-sm font-bold">&times;</button>';
            selectedHtml += '</div>';
        });
        selectedBox.innerHTML = selectedHtml;

        // Hidden inputs for form submission
        var existingHidden = container.querySelectorAll('input[type="hidden"][name$="skillPositions"], input[type="hidden"][name$="skillLevels"]');
        existingHidden.forEach(function (h) { h.remove(); });

        selected.forEach(function (level, pos) {
            var hidPos = document.createElement('input');
            hidPos.type = 'hidden';
            hidPos.name = fieldPrefix + 'skillPositions';
            hidPos.value = pos;
            container.appendChild(hidPos);

            var hidLvl = document.createElement('input');
            hidLvl.type = 'hidden';
            hidLvl.name = fieldPrefix + 'skillLevels';
            hidLvl.value = level;
            container.appendChild(hidLvl);
        });
    }

    // Click unselected pill to add
    unselectedBox.addEventListener('click', function (e) {
        var pill = e.target.closest('.skill-pill');
        if (!pill) return;
        var pos = parseInt(pill.dataset.pos);
        selected.set(pos, 6);
        render();
    });

    // Click remove on selected item
    selectedBox.addEventListener('click', function (e) {
        if (e.target.classList.contains('skill-remove')) {
            var item = e.target.closest('.skill-selected-item');
            var pos = parseInt(item.dataset.pos);
            selected.delete(pos);
            render();
        }
    });

    // Update level on input change
    selectedBox.addEventListener('change', function (e) {
        var input = e.target.closest('.skill-level');
        if (!input) return;
        var item = input.closest('.skill-selected-item');
        var pos = parseInt(item.dataset.pos);
        var val = parseFloat(input.value);
        if (isNaN(val)) val = 0;
        selected.set(pos, Math.min(10, Math.max(0, val)));
        render();
    });

    filterInput.addEventListener('input', render);

    render();
});

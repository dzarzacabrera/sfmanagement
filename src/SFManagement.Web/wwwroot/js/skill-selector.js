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

    // Pre-populate selected from initial-skills (used when editing a task)
    var initialSkills = [];
    try {
        var raw = container.dataset.initialSkills;
        if (raw && raw !== '[]') initialSkills = JSON.parse(raw);
    } catch (e) { }
    initialSkills.forEach(function (s) {
        selected.set(s.pos, s.level);
    });

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
            unselectedHtml += '<span class="skill-pill cursor-pointer inline-flex items-center px-3 rounded-md text-xs font-medium bg-white border border-gray-300 hover:border-brand hover:text-brand transition-colors h-[40px]" data-pos="' + s.vectorPosition + '" data-name="' + s.name.replace(/'/g, '&apos;') + '">' + s.name + '</span>';
        });
        unselectedBox.innerHTML = unselectedHtml;

        var selectedHtml = '';
        selected.forEach(function (level, pos) {
            var skill = skills.find(function (s) { return s.vectorPosition === pos; });
            var name = skill ? skill.name : 'Skill #' + pos;
            selectedHtml += '<div class="skill-selected-item flex items-center gap-1 px-2 rounded-lg border border-brand-dark/30 bg-brand-light/20 h-[40px] min-w-0" data-pos="' + pos + '">';
            selectedHtml += '<span class="skill-selected-pill text-xs font-medium text-brand flex-1 min-w-0 truncate">' + name + '</span>';
            selectedHtml += '<input type="number" class="skill-level w-16 border border-gray-300 rounded px-1 py-1 text-xs text-center" min="0" max="10" step="any" value="' + level + '">';
            selectedHtml += '<button type="button" class="skill-remove text-gray-400 hover:text-red-500 text-xl font-bold px-1 flex items-center' + (selected.size <= 1 ? ' hidden' : '') + '">&times;</button>';
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
        var newVal = Math.min(10, Math.max(0, Math.round(val * 100) / 100));
        if (Math.abs(newVal - selected.get(pos)) < 0.001) return;
        selected.set(pos, newVal);
        render();
    });

    // Arrow keys step by 0.5 on skill-level inputs
    selectedBox.addEventListener('keydown', function (e) {
        if (e.key !== 'ArrowUp' && e.key !== 'ArrowDown') return;
        var input = e.target.closest('.skill-level');
        if (!input) return;
        e.preventDefault();
        var item = input.closest('.skill-selected-item');
        var pos = parseInt(item.dataset.pos);
        var val = parseFloat(input.value);
        if (isNaN(val)) val = 0;
        var step = e.key === 'ArrowUp' ? 0.5 : -0.5;
        var newVal = Math.min(10, Math.max(0, Math.round((val + step) * 100) / 100));
        if (Math.abs(newVal - selected.get(pos)) < 0.001) return;
        selected.set(pos, newVal);
        render();
    });

    filterInput.addEventListener('input', render);

    render();
});

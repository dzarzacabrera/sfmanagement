(function () {
    'use strict';

    var state = {};

    function getRoots() {
        return document.querySelectorAll('[data-pg-root]');
    }

    function getItems(root) {
        var gridId = root.dataset.pgGrid;
        if (!gridId) return [];
        var container = document.getElementById(gridId);
        if (!container) {
            // Try data-pg-container instead
            container = document.querySelector(root.dataset.pgGrid);
        }
        if (!container) return [];
        var items = container.querySelectorAll('[data-pg-item]');
        return Array.from(items);
    }

    function filteredItems(items) {
        return items.filter(function (el) {
            if (el.dataset.pgFiltered === 'true') return false;
            // Skip items inside a hidden container (e.g., #projectList when card view is active)
            var parent = el.parentElement;
            while (parent && parent !== document.body) {
                if (parent.classList && parent.classList.contains('hidden')) return false;
                parent = parent.parentElement;
            }
            return true;
        });
    }

    function render(id) {
        var root = document.querySelector('[data-pg-root="' + id + '"]');
        if (!root) return;
        var s = state[id];
        if (!s) return;

        var items = getItems(root);
        var visible = filteredItems(items);
        var totalFiltered = visible.length;
        var totalAll = items.length;
        var pageSize = s.pageSize;
        var totalPages = Math.max(1, Math.ceil(totalFiltered / pageSize));
        if (s.page > totalPages) s.page = totalPages;
        var page = s.page;

        var infoEl = document.querySelector('[data-pg-info="' + id + '"]');
        var navEl = document.querySelector('[data-pg-nav="' + id + '"]');
        var pagesEl = document.querySelector('[data-pg-pages="' + id + '"]');
        var prevBtn = document.querySelector('[data-pg-prev="' + id + '"]');
        var nextBtn = document.querySelector('[data-pg-next="' + id + '"]');

        // Hide items
        var start = (page - 1) * pageSize;
        var end = Math.min(start + pageSize, totalFiltered);

        items.forEach(function (el) {
            var idx = visible.indexOf(el);
            if (idx === -1) {
                el.style.display = 'none';
            } else if (idx >= start && idx < end) {
                el.style.display = '';
            } else {
                el.style.display = 'none';
            }
        });

        // Update info
        if (infoEl) {
            if (totalFiltered === 0) {
                infoEl.textContent = '0 of 0 ' + root.dataset.pgLabel;
            } else {
                infoEl.textContent = (start + 1) + '-' + end + ' of ' + totalFiltered + ' ' + root.dataset.pgLabel;
            }
        }

        // Update page numbers
        if (pagesEl) {
            pagesEl.textContent = page + ' / ' + totalPages;
        }

        // Update nav buttons
        if (prevBtn) {
            prevBtn.disabled = page <= 1;
        }
        if (nextBtn) {
            nextBtn.disabled = page >= totalPages;
        }

        // Hide nav entirely if single page
        if (navEl) {
            if (totalPages <= 1) {
                navEl.classList.add('opacity-0', 'pointer-events-none');
            } else {
                navEl.classList.remove('opacity-0', 'pointer-events-none');
            }
        }
    }

    function updatePage(id, page) {
        var s = state[id];
        if (!s) return;
        s.page = page;
        render(id);
        saveState(id);
    }

    function changePageSize(id, size) {
        var s = state[id];
        if (!s) return;
        s.pageSize = parseInt(size, 10);
        s.page = 1;
        render(id);
        saveState(id);
    }

    function saveState(id) {
        var s = state[id];
        if (!s) return;
        try {
            var key = 'pg-state-' + id;
            localStorage.setItem(key, JSON.stringify({ pageSize: s.pageSize, page: s.page }));
        } catch (e) { /* ignore */ }
    }

    function loadState(id, defaults) {
        try {
            var key = 'pg-state-' + id;
            var saved = localStorage.getItem(key);
            if (saved) {
                var parsed = JSON.parse(saved);
                return { pageSize: parsed.pageSize || defaults.pageSize, page: parsed.page || 1 };
            }
        } catch (e) { /* ignore */ }
        return { pageSize: defaults.pageSize, page: 1 };
    }

    function init() {
        getRoots().forEach(function (root) {
            var id = root.dataset.pgRoot;
            if (!id) return;
            var defaults = {
                pageSize: 20,
                page: 1
            };

            var saved = loadState(id, defaults);
            state[id] = {
                pageSize: saved.pageSize,
                page: saved.page
            };

            // Set select to saved value
            var sizeSelect = document.querySelector('[data-pg-size="' + id + '"]');
            if (sizeSelect) {
                sizeSelect.value = saved.pageSize;
            }

            render(id);

            // Page size change
            if (sizeSelect) {
                sizeSelect.addEventListener('change', function () {
                    changePageSize(id, this.value);
                });
            }

            // Prev button
            var prevBtn = document.querySelector('[data-pg-prev="' + id + '"]');
            if (prevBtn) {
                prevBtn.addEventListener('click', function () {
                    updatePage(id, state[id].page - 1);
                });
            }

            // Next button
            var nextBtn = document.querySelector('[data-pg-next="' + id + '"]');
            if (nextBtn) {
                nextBtn.addEventListener('click', function () {
                    updatePage(id, state[id].page + 1);
                });
            }
        });
    }

    // Expose refresh function for search handlers to call
    window.paginationRefresh = function (gridId) {
        var roots = getRoots();
        // If gridId given, refresh that specific root
        if (gridId) {
            var root = document.querySelector('[data-pg-grid="' + gridId + '"]');
            if (root) {
                var id = root.dataset.pgRoot;
                if (id && state[id]) {
                    state[id].page = 1;
                    render(id);
                    saveState(id);
                }
            }
            return;
        }
        // Otherwise refresh all
        roots.forEach(function (root) {
            var id = root.dataset.pgRoot;
            if (id && state[id]) {
                state[id].page = 1;
                render(id);
                saveState(id);
            }
        });
    };

    // Re-run pagination on filter change without resetting page
    window.paginationReRender = function (gridId) {
        var roots = getRoots();
        if (gridId) {
            var root = document.querySelector('[data-pg-grid="' + gridId + '"]');
            if (root) {
                var id = root.dataset.pgRoot;
                if (id && state[id]) {
                    // If current page exceeds new total pages, adjust
                    var items = getItems(root);
                    var visible = filteredItems(items);
                    var totalPages = Math.max(1, Math.ceil(visible.length / state[id].pageSize));
                    if (state[id].page > totalPages) {
                        state[id].page = totalPages;
                    }
                    render(id);
                    saveState(id);
                }
            }
            return;
        }
        roots.forEach(function (root) {
            var id = root.dataset.pgRoot;
            if (id && state[id]) {
                render(id);
            }
        });
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Re-init for dynamically loaded content
    document.addEventListener('pg-init', init);
})();

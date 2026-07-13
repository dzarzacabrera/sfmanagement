function showSkeleton(container) {
    var skeleton = document.createElement('div');
    skeleton.className = 'skeleton skeleton-card w-full mb-3';
    skeleton.id = 'skeleton-' + Date.now();
    container.appendChild(skeleton);
    return skeleton;
}

function findCard(idOrEnc) {
    return document.querySelector('.task-card[data-task-id-encrypted="' + idOrEnc + '"]');
}

function openAssignModal(taskIdEnc, projectIdEnc) {
    closeStatusSheet();
    var col = document.querySelector('.kanban-column[data-status="InProgress"]');
    var skel = col ? showSkeleton(col) : null;

    fetch('/Dashboard/AssignPopup?taskId=' + taskIdEnc + '&projectId=' + projectIdEnc)
        .then(function (r) { return r.text(); })
        .then(function (html) {
            if (skel) skel.remove();
            openModal(html);
            document.getElementById('assignForm').addEventListener('submit', function (e) {
                e.preventDefault();
                var btn = e.submitter || this.querySelector('button[type="submit"]');
                if (btn) { btn.disabled = true; btn.innerHTML = '<span class="spinner mr-1"></span> Assigning...'; }
                var formData = new FormData(this);
                fetch('/Dashboard/AssignWorker', { method: 'POST', body: formData })
                    .then(function (r) {
                        if (r.ok) {
                            closeModal();
                            refreshTaskCard(taskIdEnc);
                            showToast('Worker assigned successfully', 'success');
                        } else {
                            showToast('Assign failed: ' + r.status, 'error');
                            if (btn) { btn.disabled = false; btn.textContent = 'Assign'; }
                        }
                    })
                    .catch(function (err) {
                        showToast('Assign error: ' + err.message, 'error');
                        if (btn) { btn.disabled = false; btn.textContent = 'Assign'; }
                    });
            });
        })
        .catch(function (err) {
            if (skel) skel.remove();
            showToast('Failed to load assign popup: ' + err.message, 'error');
        });
}

function removeWorker(taskIdEnc, workerIdEnc, btn) {
    var formData = new FormData();
    formData.append('taskIdEncrypted', taskIdEnc);
    formData.append('workerIdEncrypted', workerIdEnc);

    var card = btn.closest('.task-card');
    if (card) card.style.opacity = '0.5';

    fetch('/Dashboard/RemoveWorker', { method: 'POST', body: formData })
        .then(function (r) {
            if (r.ok) {
                refreshTaskCard(taskIdEnc);
                showToast('Worker removed', 'success');
            } else {
                showToast('Remove failed: ' + r.status, 'error');
                if (card) card.style.opacity = '';
            }
        })
        .catch(function (err) {
            showToast('Remove error: ' + err.message, 'error');
            if (card) card.style.opacity = '';
        });
}

function openEvaluationModal(taskIdEnc, projectIdEnc) {
    closeStatusSheet();
    var col = document.querySelector('.kanban-column[data-status="Finish"]');
    var skel = col ? showSkeleton(col) : null;

    fetch('/Dashboard/EvaluationPopup?taskId=' + taskIdEnc + '&projectId=' + (projectIdEnc || window.__dashboardProjectIdEncrypted || ''))
        .then(function (r) { if (!r.ok) throw new Error('HTTP ' + r.status); return r.text(); })
        .then(function (html) {
            if (skel) skel.remove();
            openModal(html);
        })
        .catch(function (err) {
            if (skel) skel.remove();
            showToast('Failed to load evaluation: ' + err.message, 'error');
        });
}

// Event delegation for evaluation form submit (survives innerHTML replacement)
document.getElementById('modal-root').addEventListener('submit', function (e) {
    var form = e.target.closest('#evaluationForm');
    if (!form) return;
    e.preventDefault();

    var taskIdEncInput = form.querySelector('[name="taskIdEncrypted"]');
    if (!taskIdEncInput) return;
    var taskIdEnc = taskIdEncInput.value;
    var workerSelect = form.querySelector('[name="workerIdEncrypted"]');
    var workerIdEnc = workerSelect ? workerSelect.value : '';
    var sliders = form.querySelectorAll('#skillSliders input[type="range"]');
    var skillPositions = [];
    var basePoints = [];

    sliders.forEach(function (s) {
        var pos = s.getAttribute('data-position');
        var val = parseFloat(s.value);
        skillPositions.push(pos);
        basePoints.push(Math.round((val / 10.0 - 0.5) * 10000));
    });

    var btn = form.querySelector('button[type="submit"]');
    if (btn) { btn.disabled = true; btn.innerHTML = '<span class="spinner mr-1"></span> Submitting...'; }

    var formData = new FormData();
    formData.append('taskIdEncrypted', taskIdEnc);
    formData.append('workerIdEncrypted', workerIdEnc);
    for (var i = 0; i < skillPositions.length; i++) {
        formData.append('skillPositions', skillPositions[i]);
        formData.append('basePoints', basePoints[i]);
    }

    fetch('/Dashboard/SubmitEvaluation', { method: 'POST', body: formData })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (data.hasMore) {
                showToast('Evaluation submitted. Select next worker.', 'success');
                openEvaluationModal(taskIdEnc, window.__dashboardProjectIdEncrypted || '');
            } else {
                closeModal();
                showToast('All evaluations submitted successfully', 'success');
                setTimeout(function () { location.reload(); }, 300);
            }
        })
        .catch(function (err) {
            showToast('Evaluation error: ' + err.message, 'error');
            if (btn) { btn.disabled = false; btn.textContent = 'Submit Evaluation'; }
        });
});

function reloadEvaluationPopup(taskIdEnc, workerIdEnc) {
    var projectIdEnc = window.__dashboardProjectIdEncrypted || '';
    fetch('/Dashboard/EvaluationPopup?taskId=' + taskIdEnc + '&projectId=' + projectIdEnc + '&workerId=' + workerIdEnc)
        .then(function (r) { if (!r.ok) throw new Error('HTTP ' + r.status); return r.text(); })
        .then(function (html) {
            var content = document.getElementById('modal-content');
            if (content) content.innerHTML = html;
        })
        .catch(function (err) {
            showToast('Failed to reload: ' + err.message, 'error');
        });
}

function setAllSliders(value) {
    var sliders = document.querySelectorAll('#skillSliders input[type="range"]');
    sliders.forEach(function (s) {
        s.value = value;
        updateSliderPreview(s);
    });
}

function roundToFive(v) {
    return Math.sign(v) * Math.round(Math.abs(v) / 0.05) * 0.05;
}

function updateSliderPreview(input) {
    var pos = input.getAttribute('data-position');
    var val = parseFloat(input.value);
    var valEl = document.getElementById('val-' + pos);
    if (valEl) valEl.textContent = val.toFixed(1);

    // Fill left portion of slider with brand color
    var pct = (val / 10) * 100;
    input.style.background = 'linear-gradient(to right, #21668f 0%, #21668f ' + pct + '%, #dbeafe ' + pct + '%, #dbeafe 100%)';

    var basePoints = val / 10.0 - 0.5;
    var bpHidden = document.getElementById('bp-' + pos);
    if (bpHidden) bpHidden.value = Math.round(basePoints * 10000);

    var form = document.getElementById('evaluationForm');
    var multiplier = form ? parseFloat(form.getAttribute('data-crit-multiplier')) || 1.0 : 1.0;

    var currentEl = document.getElementById('current-' + pos);
    var newEl = document.getElementById('new-' + pos);
    var diffEl = document.getElementById('diff-' + pos);
    if (!currentEl || !newEl || !diffEl) return;

    var current = parseFloat(input.getAttribute('data-current')) || 0;
    var impact = basePoints * multiplier;
    var newLevel = Math.max(0, Math.min(10, roundToFive(current + impact)));

    newEl.textContent = newLevel.toFixed(2);

    var diff = newLevel - current;
    var diffText = (diff >= 0 ? '+' : '') + diff.toFixed(2);
    diffEl.textContent = diffText;

    diffEl.className = 'inline-block px-1.5 py-0.5 rounded-full text-sm sm:text-xs font-semibold';
    if (diff > 0.001) diffEl.classList.add('bg-green-100', 'text-green-700');
    else if (diff < -0.001) diffEl.classList.add('bg-red-100', 'text-red-700');
    else diffEl.classList.add('bg-gray-100', 'text-gray-500');
}

function refreshTaskCard(taskIdEnc, newStatus) {
    var card = findCard(taskIdEnc);
    if (!card) return;

    // Clean up orphaned tooltips before removing old card
    card.querySelectorAll('[data-tooltip]').forEach(function (el) {
        if (el._tooltipEl) {
            el._tooltipEl.remove();
            el._tooltipEl = null;
        }
    });

    var projectIdEnc = window.__dashboardProjectIdEncrypted || '';

    fetch('/Dashboard/GetTaskCardHtml?taskId=' + taskIdEnc + '&projectId=' + projectIdEnc)
        .then(function (r) { if (!r.ok) return null; return r.text(); })
        .then(function (html) {
            if (!html) return;
            var temp = document.createElement('div');
            temp.innerHTML = html;
            var newCard = temp.querySelector('.task-card');
            if (!newCard) return;

            if (newStatus) {
                var targetCol = document.querySelector('.kanban-column[data-status="' + newStatus + '"]');
                if (targetCol) {
                    var targetContainer = targetCol.querySelector('.space-y-4') || targetCol;
                    var placeholder = targetCol.querySelector('.empty-placeholder');
                    if (placeholder) placeholder.remove();
                    targetContainer.insertBefore(newCard, targetContainer.firstChild);
                    card.remove();
                }
            } else {
                card.parentNode.replaceChild(newCard, card);
            }
            updateColumnCounts();
        })
        .catch(function () { /* silently fail */ });
}

function updateTaskCard(taskIdEnc, newStatus) {
    var card = findCard(taskIdEnc);
    if (!card) return;

    if (newStatus) {
        var targetCol = document.querySelector('.kanban-column[data-status="' + newStatus + '"]');
        if (targetCol) {
            card.style.transform = 'scale(0.95)';
            card.style.opacity = '0.5';
            setTimeout(function () {
                card.style.transform = '';
                card.style.opacity = '';
                var placeholder = targetCol.querySelector('.empty-placeholder');
                if (placeholder) placeholder.remove();
                targetCol.appendChild(card);
                card.setAttribute('data-status', newStatus);
            }, 150);
        }
    }

    updateColumnCounts();
}

function updateColumnCounts() {
    document.querySelectorAll('.kanban-column').forEach(function (col) {
        var count = col.querySelectorAll('.task-card').length;
        var counter = col.querySelector('.task-count');
        if (counter) counter.textContent = count;
    });
}

// Event delegation for drag events (works with dynamically refreshed cards)
var kanbanGrid = document.getElementById('kanban-grid');
if (kanbanGrid) {
    kanbanGrid.addEventListener('dragstart', function (e) {
        var card = e.target.closest('.task-card');
        if (!card) return;
        e.dataTransfer.setData('text/plain', card.dataset.taskIdEncrypted);
        card.style.opacity = '0.5';
    });

    kanbanGrid.addEventListener('dragend', function (e) {
        var card = e.target.closest('.task-card');
        if (!card) return;
        card.style.opacity = '';
    });

    kanbanGrid.addEventListener('dragover', function (e) {
        var col = e.target.closest('.kanban-column');
        if (col) e.preventDefault();
    });

    kanbanGrid.addEventListener('drop', function (e) {
        var col = e.target.closest('.kanban-column');
        if (!col) return;
        e.preventDefault();
        var taskId = e.dataTransfer.getData('text/plain');

        // Look up encrypted ID from the card
        var card = document.querySelector('.task-card[data-task-id-encrypted="' + taskId + '"]');
        var taskIdEnc = card ? card.dataset.taskIdEncrypted : taskId;

        var newStatus = col.dataset.status;

        if (!taskId || !newStatus) return;

        if (card && card.getAttribute('data-status') === newStatus) return;

        var formData = new FormData();
        formData.append('taskIdEncrypted', taskIdEnc);
        formData.append('newStatus', newStatus);

        fetch('/Dashboard/ChangeStatus', { method: 'POST', body: formData })
            .then(function (r) {
                if (r.ok) {
                    refreshTaskCard(taskIdEnc, newStatus);
                    showToast('Task moved to ' + newStatus, 'success');
                } else {
                    showToast('Status change failed: ' + r.status, 'error');
                }
            })
            .catch(function (err) { showToast('Status change error: ' + err.message, 'error'); });
    });
}

function archiveTask(taskIdEnc, btn, keepVisible) {
    var card = btn.closest('.task-card') || btn.closest('.task-row');
    if (!card) return;

    var undoKey = 'archive-undo-' + taskIdEnc;

    if (!keepVisible) {
        // Collapse animation for dashboard kanban cards
        var h = card.scrollHeight;
        card.style.overflow = 'hidden';
        card.style.maxHeight = h + 'px';
        void card.offsetWidth;
        card.style.transition = 'transform 0.3s ease, opacity 0.3s ease, max-height 0.3s ease 0.3s, margin 0.3s ease 0.3s, padding 0.3s ease 0.3s';
        card.style.transform = 'scale(0.95)';
        card.style.opacity = '0';
        card.style.maxHeight = '0';
        card.style.margin = '0';
        card.style.padding = '0';
    }

    function restoreCard() {
        card.style.transition = 'transform 0.3s ease, opacity 0.3s ease, max-height 0.3s ease, margin 0.3s ease, padding 0.3s ease';
        card.style.transform = '';
        card.style.opacity = '';
        card.style.maxHeight = '';
        card.style.margin = '';
        card.style.padding = '';
        card.style.overflow = '';
    }

    showUndoToast('Task archived.', function () {
        restoreCard();
        clearTimeout(window[undoKey]);
        showToast('Archive cancelled', 'undo');
    });

    window[undoKey] = setTimeout(function () {
        var formData = new FormData();
        formData.append('taskIdEncrypted', taskIdEnc);
        fetch('/Dashboard/ArchiveTask', { method: 'POST', body: formData })
            .then(function (r) {
                if (r.ok) {
                    if (keepVisible) {
                        var statusBadge = card.querySelector('[data-status-badge]');
                        if (statusBadge) {
                            statusBadge.textContent = 'Archived';
                            statusBadge.className = 'px-2 py-0.5 rounded-full text-sm sm:text-xs font-medium bg-gray-100 text-gray-500';
                        }
                        btn.style.display = 'none';
                        var actionsBar = btn.parentElement;
                        if (actionsBar) {
                            var restoreBtn = document.createElement('button');
                            restoreBtn.className = 'bg-white border border-gray-300 text-gray-600 px-2 py-1.5 rounded-lg text-xs font-medium hover:bg-gray-50 dark:border-gray-600 dark:text-gray-200 dark:hover:bg-gray-700 transition-colors whitespace-nowrap';
                            restoreBtn.title = 'Restore to Finish';
                            restoreBtn.textContent = 'Restore';
                            restoreBtn.dataset.taskId = taskIdEnc;
                            restoreBtn.addEventListener('click', function (e) {
                                e.stopPropagation();
                                var rid = this.dataset.taskId;
                                var fd = new FormData();
                                fd.append('taskIdEncrypted', rid);
                                fd.append('newStatus', 'Finish');
                                fetch('/Dashboard/ChangeStatus', { method: 'POST', body: fd })
                                    .then(function (r) {
                                        if (r.ok) { location.reload(); }
                                        else { showToast('Restore failed', 'error'); }
                                    })
                                    .catch(function () { showToast('Restore error', 'error'); });
                            });
                            actionsBar.appendChild(restoreBtn);
                        }
                    } else {
                        card.remove();
                    }
                    if (typeof updateColumnCounts === 'function') updateColumnCounts();
                    showToast('Task archived permanently', 'success');
                } else {
                    showToast('Archive failed: ' + r.status, 'error');
                    if (!keepVisible) restoreCard();
                }
            })
            .catch(function (err) {
                showToast('Archive error: ' + err.message, 'error');
                if (!keepVisible) restoreCard();
            });
    }, 2000);
}

function openAddWorkerPopup(projectIdEnc) {
    var col = document.querySelector('.kanban-column');
    var skel = col ? showSkeleton(col) : null;

    fetch('/Dashboard/AddWorkerPopup?projectId=' + projectIdEnc)
        .then(function (r) { return r.text(); })
        .then(function (html) {
            if (skel) skel.remove();
            openModal(html);
            document.getElementById('addWorkerToProjectForm').addEventListener('submit', function (e) {
                e.preventDefault();
                var btn = this.querySelector('button[type="submit"]');
                if (btn) { btn.disabled = true; btn.innerHTML = '<span class="spinner mr-1"></span> Adding...'; }
                var formData = new FormData(this);
                fetch('/Dashboard/AddWorkersToProject', { method: 'POST', body: formData })
                    .then(function (r) {
                        if (r.ok) {
                            closeModal();
                            showToast('Worker added to project', 'success');
                            location.reload();
                        } else {
                            showToast('Failed: ' + r.status, 'error');
                            if (btn) { btn.disabled = false; btn.textContent = 'Add Worker'; }
                        }
                    })
                    .catch(function (err) {
                        showToast('Error: ' + err.message, 'error');
                        if (btn) { btn.disabled = false; btn.textContent = 'Add Worker'; }
                    });
            });
        })
        .catch(function (err) {
            if (skel) skel.remove();
            showToast('Failed to load popup: ' + err.message, 'error');
        });
}

// Status flow: Queued (0) -> InProgress (1) -> Finish (2); Blocked is separate
var STATUS_FLOW = ['Queued', 'InProgress', 'Finish'];
var STATUS_LABELS = { Queued: 'Queued', InProgress: 'In Progress', Finish: 'Finished', Blocked: 'Blocked' };

function moveTaskStatus(taskIdEnc, direction) {
    var card = findCard(taskIdEnc);
    if (!card) return;
    var current = card.getAttribute('data-status');
    if (current === 'Blocked') {
        if (direction === -1) changeStatus(taskIdEnc, 'InProgress');
        return;
    }
    var idx = STATUS_FLOW.indexOf(current);
    if (idx === -1) return;
    var nextIdx = idx + direction;
    if (nextIdx < 0 || nextIdx >= STATUS_FLOW.length) return;
    changeStatus(taskIdEnc, STATUS_FLOW[nextIdx]);
}

function openStatusSheet(taskIdEnc, currentStatus, btn) {
    var sheet = document.getElementById('status-bottom-sheet');
    var body = document.getElementById('sheet-body');
    var handle = document.getElementById('sheet-handle');
    var opts = document.getElementById('status-sheet-options');
    if (!sheet || !opts) return;

    // Reset
    sheet.className = 'z-[70]';
    sheet.style.position = '';
    sheet.style.top = '';
    sheet.style.left = '';
    sheet.style.width = '';
    sheet.style.maxHeight = '';
    sheet.style.backgroundColor = '';
    sheet.onclick = null;
    body.className = 'bg-white dark:bg-gray-800 shadow-2xl overflow-y-auto';
    body.style.borderRadius = '';
    body.style.padding = '';
    body.style.width = '';
    body.style.maxHeight = '';
    handle.classList.remove('hidden');

    if (window.innerWidth >= 640 && btn) {
        // Desktop: dropdown near button — no backdrop
        handle.classList.add('hidden');
        body.style.borderRadius = '0.75rem';
        body.style.padding = '0.5rem';
        body.style.width = '10rem';
        body.style.maxHeight = '20rem';
        body.className += ' border border-gray-200 dark:border-gray-700';

        var rect = btn.getBoundingClientRect();
        var container = btn.closest('.task-card, .task-row');
        if (container) {
            container.style.position = 'relative';
            container.appendChild(sheet);
            sheet.style.position = 'absolute';
            sheet.style.top = (btn.offsetTop + btn.offsetHeight) + 'px';
            var dropLeft = btn.offsetLeft;
            if (dropLeft + 160 > container.clientWidth) {
                dropLeft = Math.max(0, btn.offsetLeft + btn.offsetWidth - 160);
            }
            sheet.style.left = dropLeft + 'px';
            // Prevent clicks inside dropdown from bubbling to card
            sheet._stopProp = sheet._stopProp || function (e) { e.stopPropagation(); };
            sheet.removeEventListener('click', sheet._stopProp);
            sheet.addEventListener('click', sheet._stopProp);
        } else {
            // Fallback: fixed positioning
            sheet.style.position = 'fixed';
            sheet.style.top = rect.bottom + 'px';
            var dropLeft = rect.left;
            if (dropLeft + 160 > window.innerWidth - 8) {
                dropLeft = Math.max(8, rect.right - 160);
            }
            sheet.style.left = dropLeft + 'px';
        }
    } else {
        // Mobile: bottom sheet — parent gets the backdrop background
        sheet.className += ' fixed inset-0 items-end justify-center flex';
        sheet.style.backgroundColor = 'rgba(0,0,0,0.3)';
        body.style.borderRadius = '1rem 1rem 0 0';
        body.style.padding = '1.5rem 1.5rem 2rem';
        body.style.width = '100%';
        body.style.maxHeight = '60vh';
        // Close when tapping outside the sheet body
        sheet.onclick = function (e) {
            if (e.target === sheet) closeStatusSheet();
        };
    }

    opts.innerHTML = '';
    var statuses = ['Queued', 'InProgress', 'Finish', 'Blocked'];
    statuses.forEach(function (s) {
        var label = STATUS_LABELS[s] || s;
        var isCurrent = s === currentStatus;
        var el = document.createElement('button');
        var isDesktop = window.innerWidth >= 640;
        el.className = 'w-full text-left px-4 py-3 rounded-lg text-sm font-medium transition-colors ' +
            (isCurrent ? 'bg-brand text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600');
        if (isDesktop && isCurrent) {
            el.className += ' cursor-default';
        }
        el.textContent = label;
        if (!isCurrent) {
            el.onclick = function (e) {
                e.stopPropagation();
                closeStatusSheet();
                changeStatus(taskIdEnc, s);
            };
        }
        opts.appendChild(el);
    });

    sheet.classList.remove('hidden');
}

function closeStatusSheet() {
    var sheet = document.getElementById('status-bottom-sheet');
    if (!sheet) return;
    document.body.appendChild(sheet);
    sheet.classList.add('hidden');
}

// Close status sheet on click outside (desktop dropdown)
document.addEventListener('click', function (e) {
    var sheet = document.getElementById('status-bottom-sheet');
    if (!sheet || sheet.classList.contains('hidden')) return;
    if (window.innerWidth < 640) return;
    if (e.target.closest('#status-bottom-sheet') || e.target.closest('[onclick*="openStatusSheet"]')) return;
    closeStatusSheet();
});

function changeStatus(taskIdEnc, newStatus) {
    var formData = new FormData();
    formData.append('taskIdEncrypted', taskIdEnc);
    formData.append('newStatus', newStatus);

    fetch('/Dashboard/ChangeStatus', { method: 'POST', body: formData })
        .then(function (r) {
            if (r.ok) {
                refreshTaskCard(taskIdEnc, newStatus);
                showToast('Task moved to ' + (STATUS_LABELS[newStatus] || newStatus), 'success');
            } else {
                return r.text().then(function (txt) {
                    showToast('Status change failed: ' + txt, 'error');
                });
            }
        })
        .catch(function (err) { showToast('Status change error: ' + err.message, 'error'); });
}

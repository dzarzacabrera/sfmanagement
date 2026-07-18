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
    var modalRoot = document.getElementById('modal-root');
    if (modalRoot && !modalRoot.classList.contains('hidden')) closeModal();
    // Store the card's encrypted task ID (used by refreshTaskCard to find the correct card)
    // The modal's TaskIdEncrypted has a different AES-GCM nonce, so we can't rely on it for DOM lookups
    if (modalRoot) modalRoot.dataset.cardTaskId = taskIdEnc;
    var col = document.querySelector('.kanban-column[data-status="InProgress"]');
    var skel = col ? showSkeleton(col) : null;

    fetch('/Dashboard/AssignPopup?taskId=' + taskIdEnc + '&projectId=' + projectIdEnc)
        .then(function (r) { return r.text(); })
        .then(function (html) {
            if (skel) skel.remove();
            openModal(html);
            // Reset scroll to the top of the modal (header/summary), not the bottom,
            // especially on mobile where the worker list scrolls internally.
            requestAnimationFrame(function () {
                var c = document.getElementById('modal-content');
                if (c) c.scrollTop = 0;
                var list = document.getElementById('assignWorkerList');
                if (list) list.scrollTop = 0;
            });
            // Update button count for pre-selected (already assigned) workers
            updateAssignButton();
            // Store the set of initially selected worker IDs so submitAssignWorkers can detect changes
            if (modalRoot) {
                var initial = [];
                document.querySelectorAll('.assign-card.selected').forEach(function (c) {
                    initial.push(c.dataset.workerId);
                });
                modalRoot.dataset.initialWorkerIds = initial.join(',');
            }
        })
        .catch(function (err) {
            if (skel) skel.remove();
            showToast('Failed to load assign popup: ' + err.message, 'error');
        });
}

function updateAssignButton() {
    var count = document.querySelectorAll('.assign-card.selected').length;
    var assignBtns = document.querySelectorAll('#assignBtn, #assignProjectBtn');
    assignBtns.forEach(function (btn) {
        btn.disabled = count === 0;
        btn.textContent = count > 0 ? 'Assign (' + count + ')' : 'Assign';
    });
}

function toggleWorkerSelect(card) {
    card.classList.toggle('selected');
    var rankNum = card.querySelector('.rank-num');
    var rankCheck = card.querySelector('.rank-check');

    var hasCustomLeft = card.style.borderLeftColor && card.style.borderLeftColor !== '';
    if (hasCustomLeft && !card.dataset.origBorderLeft) {
        card.dataset.origBorderLeft = card.style.borderLeftColor;
    }

    if (card.classList.contains('selected')) {
        card.style.borderColor = '#21668f';
        card.style.backgroundColor = 'rgba(33,102,143,0.06)';
        if (rankNum) rankNum.classList.add('hidden');
        if (rankCheck) rankCheck.classList.remove('hidden');
    } else {
        card.style.borderColor = '';
        card.style.borderTopColor = '';
        card.style.borderRightColor = '';
        card.style.borderBottomColor = '';
        if (card.dataset.origBorderLeft) {
            card.style.borderLeftColor = card.dataset.origBorderLeft;
        }
        card.style.backgroundColor = '';
        if (rankNum) rankNum.classList.remove('hidden');
        if (rankCheck) rankCheck.classList.add('hidden');
    }
    updateAssignButton();
}

function submitAssignWorkers(taskIdEnc) {
    var cards = document.querySelectorAll('.assign-card.selected');
    var root = document.getElementById('modal-root');
    // Skip if no changes were made to the selection
    var initialIds = root ? root.dataset.initialWorkerIds || '' : '';
    var currentIds = [];
    cards.forEach(function (c) { currentIds.push(c.dataset.workerId); });
    if (currentIds.sort().join(',') === initialIds.split(',').sort().join(',')) {
        closeModal();
        return;
    }

    var btn = document.getElementById('assignBtn');
    if (btn) { btn.disabled = true; btn.innerHTML = '<span class="spinner mr-1"></span> Assigning...'; }
    var formData = new FormData();
    formData.append('taskIdEncrypted', taskIdEnc);
    var workerNames = [];
    cards.forEach(function (card) {
        formData.append('workerIdsEncrypted', card.dataset.workerId);
        var n = card.querySelector('.min-w-0 .text-sm.font-semibold');
        if (n) workerNames.push(n.textContent.trim());
    });
    // The modal's taskIdEnc has a different AES-GCM nonce than the card's data-task-id-encrypted.
    // Use the stored card task ID to find and update the correct DOM card.
    var cardTaskId = root ? root.dataset.cardTaskId : null;
    fetch('/Dashboard/AssignWorkers', { method: 'POST', body: formData })
        .then(function (r) {
            if (r.ok) {
                closeModal();
                showToast('Workers assigned successfully', 'success');
                var refreshId = cardTaskId || taskIdEnc;
                // Update Task/Index card workers directly (also uses card's encrypted ID, not modal's)
                updateTaskIndexWorkers(refreshId, workerNames);
                refreshTaskCard(refreshId);
            } else {
                showToast('Assign failed: ' + r.status, 'error', btn);
                if (btn) { btn.disabled = false; btn.textContent = 'Assign'; }
            }
        })
        .catch(function (err) {
            showToast('Assign error: ' + err.message, 'error', btn);
            if (btn) { btn.disabled = false; btn.textContent = 'Assign'; }
        });
}

function updateTaskIndexWorkers(taskIdEnc, workerNames) {
    var joined = workerNames.join(', ');
    // Card view in Task/Index — replace the full list (workerNames already includes pre-assigned)
    document.querySelectorAll('.task-card[data-task-id="' + taskIdEnc + '"]').forEach(function (card) {
        var existing = card.querySelector('.mt-3.text-sm.text-gray-500');
        if (existing) {
            existing.innerHTML = '<span class="font-medium text-gray-700 dark:text-gray-300">Team:</span> ' + (joined || '—');
        } else {
            card.insertAdjacentHTML('beforeend', '<div class="mt-3 text-sm text-gray-500 dark:text-gray-400"><span class="font-medium text-gray-700 dark:text-gray-300">Team:</span> ' + (joined || '—') + '</div>');
        }
    });
    // List view row in Task/Index — replace the full list
    document.querySelectorAll('.task-row[data-task-id="' + taskIdEnc + '"]').forEach(function (row) {
        var cells = row.querySelectorAll('.col-span-2');
        if (cells.length >= 2) {
            var assignedCell = cells[1];
            assignedCell.textContent = joined || '—';
        }
    });
}

function submitAddWorkersToProject(projectIdEnc) {
    var cards = document.querySelectorAll('.assign-card.selected');
    if (cards.length === 0) return;
    var btn = document.getElementById('assignProjectBtn');
    if (btn) { btn.disabled = true; btn.innerHTML = '<span class="spinner mr-1"></span> Assigning...'; }
    var formData = new FormData();
    formData.append('projectIdEncrypted', projectIdEnc);
    cards.forEach(function (card) {
        formData.append('workerIdEncrypted', card.dataset.workerId);
    });
    fetch('/Dashboard/AddWorkersToProject', { method: 'POST', body: formData })
        .then(function (r) {
            if (r.ok) {
                closeModal();
                showToast('Workers added to project', 'success');
                location.reload();
            } else {
                showToast('Failed: ' + r.status, 'error', btn);
                if (btn) { btn.disabled = false; btn.textContent = 'Assign'; }
            }
        })
        .catch(function (err) {
            showToast('Error: ' + err.message, 'error', btn);
            if (btn) { btn.disabled = false; btn.textContent = 'Assign'; }
        });
}

function removeWorker(taskIdEnc, workerIdEnc, btn) {
    var formData = new FormData();
    formData.append('taskIdEncrypted', taskIdEnc);
    formData.append('workerIdEncrypted', workerIdEnc);

    var card = btn.closest('.task-card');
    if (card) card.style.opacity = '0.5';

    // Remove any lingering tooltip before card is refreshed
    if (btn._tooltipEl) { btn._tooltipEl.remove(); btn._tooltipEl = null; }

    fetch('/Dashboard/RemoveWorker', { method: 'POST', body: formData })
        .then(function (r) {
            if (card) card.style.opacity = '';
            if (r.ok) {
                refreshTaskCard(taskIdEnc);
                showToast('Worker removed', 'success', btn);
            } else {
                showToast('Remove failed: ' + r.status, 'error', btn);
            }
        })
        .catch(function (err) {
            showToast('Remove error: ' + err.message, 'error', btn);
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
                showToast('Evaluation submitted. Select next worker.', 'success', btn);
                openEvaluationModal(taskIdEnc, window.__dashboardProjectIdEncrypted || '');
            } else {
                closeModal();
                showToast('All evaluations submitted successfully', 'success');
                setTimeout(function () { location.reload(); }, 300);
            }
        })
        .catch(function (err) {
            showToast('Evaluation error: ' + err.message, 'error', btn);
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

function refreshTaskCard(taskIdEnc, newStatus, insertBefore) {
    var projectIdEnc = window.__dashboardProjectIdEncrypted;
    if (!projectIdEnc) return;

    fetch('/Dashboard/GetTaskCardHtml?taskId=' + taskIdEnc + '&projectId=' + projectIdEnc)
        .then(function (r) { if (!r.ok) throw new Error('HTTP ' + r.status); return r.text(); })
        .then(function (html) {
            if (!html) throw new Error('Empty response');
            var newCard = htmlToCard(html);
            if (!newCard) throw new Error('No .task-card in response');

            var card = findCard(taskIdEnc);
            if (!card) {
                var status = newStatus || newCard.getAttribute('data-status') || 'Queued';
                var col = document.querySelector('.kanban-column[data-status="' + status + '"]');
                if (col) {
                    var container = col.querySelector('.space-y-4') || col;
                    var placeholder = col.querySelector('.empty-placeholder');
                    if (placeholder) placeholder.remove();
                    if (insertBefore && container.contains(insertBefore)) {
                        container.insertBefore(newCard, insertBefore);
                    } else {
                        container.appendChild(newCard);
                    }
                }
                updateColumnCounts();
                return;
            }

            var attrs = newCard.attributes;
            for (var i = 0; i < attrs.length; i++) {
                card.setAttribute(attrs[i].name, attrs[i].value);
            }
            card.innerHTML = newCard.innerHTML;

            if (newStatus) {
                var targetCol = document.querySelector('.kanban-column[data-status="' + newStatus + '"]');
                if (targetCol) {
                    var container = targetCol.querySelector('.space-y-4') || targetCol;
                    var placeholder = targetCol.querySelector('.empty-placeholder');
                    if (placeholder) placeholder.remove();
                    if (insertBefore && container.contains(insertBefore)) {
                        container.insertBefore(card, insertBefore);
                    } else {
                        container.appendChild(card);
                    }
                }
            }

            updateColumnCounts();
        })
        .catch(function (err) {
            showToast('Card refresh failed: ' + err.message, 'error');
        });
}

function htmlToCard(html) {
    var temp = document.createElement('div');
    temp.innerHTML = html;
    return temp.querySelector('.task-card');
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
    var cols = document.querySelectorAll('.kanban-column');
    cols.forEach(function (col) {
        var count = col.querySelectorAll('.task-card').length;
        var counter = col.querySelector('.task-count');
        if (counter) counter.textContent = count;
    });

    // On desktop: shrink empty columns to half width
    var grid = document.getElementById('kanban-grid');
    if (!grid) return;
    if (window.innerWidth < 1024) {
        grid.style.gridTemplateColumns = '';
        return;
    }
    var template = Array.from(cols).map(function (col) {
        return col.querySelectorAll('.task-card').length === 0 ? '0.5fr' : '1fr';
    }).join(' ');
    grid.style.gridTemplateColumns = template;
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

        var card = document.querySelector('.task-card[data-task-id-encrypted="' + taskId + '"]');
        var taskIdEnc = card ? card.dataset.taskIdEncrypted : taskId;

        var newStatus = col.dataset.status;

        if (!taskId || !newStatus) return;

        if (card && card.getAttribute('data-status') === newStatus) return;

        var container = col.querySelector('.space-y-4') || col;
        var dropY = e.clientY;
        var insertBefore = null;
        var cards = container.querySelectorAll('.task-card');
        for (var i = 0; i < cards.length; i++) {
            var rect = cards[i].getBoundingClientRect();
            if (dropY < rect.top + rect.height / 2) {
                insertBefore = cards[i];
                break;
            }
        }

        var formData = new FormData();
        formData.append('taskIdEncrypted', taskIdEnc);
        formData.append('newStatus', newStatus);

        fetch('/Dashboard/ChangeStatus', { method: 'POST', body: formData })
            .then(function (r) {
                if (r.ok) {
                    refreshTaskCard(taskIdEnc, newStatus, insertBefore);
                    showToast('Task moved to ' + newStatus, 'success', null, col);
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
    }, btn);

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

function finalizeProject(projectIdEnc, btn) {
    var formData = new FormData();
    formData.append('projectIdEncrypted', projectIdEnc);

    // Determine which endpoint: DashboardController or ProjectController
    var url = btn && btn.closest('.project-card, .project-row') ? '/Project/FinalizeProject' : '/Dashboard/FinalizeProject';

    if (btn) { btn.disabled = true; btn.textContent = '...'; }

    fetch(url, { method: 'POST', body: formData })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (data.success) {
                showToast('Project closed successfully', 'success', btn);
                // Reload the page to reflect changes (tasks hidden, banner shown)
                setTimeout(function () { location.reload(); }, 500);
            } else {
                showToast(data.message || 'Failed to close project', 'error', btn);
                if (btn) { btn.disabled = false; btn.textContent = 'Close'; }
            }
        })
        .catch(function (err) {
            showToast('Close error: ' + err.message, 'error', btn);
            if (btn) { btn.disabled = false; btn.textContent = 'Close'; }
        });
}

function initCreateTaskForm(container) {
    var form = container.querySelector('#createTaskForm');
    if (!form) return;
    if (window.initSkillSelectors) initSkillSelectors(container);
    form.addEventListener('submit', function (e) {
        if (!window.validateSkillSelection(this)) {
            e.preventDefault();
            return;
        }
        e.preventDefault();
        var btn = this.querySelector('button[type="submit"]');
        btn.disabled = true;
        btn.textContent = 'Creating...';
        var formData = new FormData(this);
        fetch('/Task/Create', { method: 'POST', body: formData })
            .then(function (r) {
                if (r.redirected) {
                    closeModal();
                    window.showToast('Task created successfully.', 'success');
                    location.reload();
                } else {
                    return r.text().then(function (html) {
                        window.showToast('Failed to create task.', 'error');
                        btn.disabled = false;
                        btn.textContent = 'Create Task';
                    });
                }
            })
            .catch(function (err) {
                window.showToast('Error: ' + err.message, 'error');
                btn.disabled = false;
                btn.textContent = 'Create Task';
            });
    });
}

function openCreateTaskPopup(projectIdEnc) {
    fetch('/Dashboard/CreateTaskPopup?projectId=' + projectIdEnc)
        .then(function (r) { return r.text(); })
        .then(function (html) {
            openModal(html);
            initCreateTaskForm(document.getElementById('modal-content'));
        })
        .catch(function (err) {
            showToast('Failed to load form: ' + err.message, 'error');
        });
}

function initCreateWorkerForm(container) {
    var form = container.querySelector('#createWorkerForm');
    if (!form) return;
    if (window.initSkillSelectors) initSkillSelectors(container);
    form.addEventListener('submit', function (e) {
        if (!window.validateSkillSelection(this)) {
            e.preventDefault();
            return;
        }
        e.preventDefault();
        var btn = this.querySelector('button[type="submit"]');
        btn.disabled = true;
        btn.textContent = 'Creating...';
        var formData = new FormData(this);
        fetch('/Worker/Create', { method: 'POST', body: formData })
            .then(function (r) {
                if (r.redirected) {
                    closeModal();
                    window.showToast('Worker created successfully.', 'success');
                    location.reload();
                } else {
                    return r.text().then(function (html) {
                        window.showToast('Failed to create worker.', 'error');
                        btn.disabled = false;
                        btn.textContent = 'Create Worker';
                    });
                }
            })
            .catch(function (err) {
                window.showToast('Error: ' + err.message, 'error');
                btn.disabled = false;
                btn.textContent = 'Create Worker';
            });
    });
}

function openCreateWorkerPopup(projectIdEnc) {
    var url = '/Dashboard/CreateWorkerPopup';
    if (projectIdEnc) url += '?projectId=' + projectIdEnc;
    fetch(url)
        .then(function (r) { return r.text(); })
        .then(function (html) {
            openModal(html);
            initCreateWorkerForm(document.getElementById('modal-content'));
        })
        .catch(function (err) {
            showToast('Failed to load form: ' + err.message, 'error');
        });
}

function openAddWorkerPopup(projectIdEnc) {
    var col = document.querySelector('.kanban-column');
    var skel = col ? showSkeleton(col) : null;

    fetch('/Dashboard/AddWorkerPopup?projectId=' + projectIdEnc)
        .then(function (r) { return r.text(); })
        .then(function (html) {
            if (skel) skel.remove();
            openModal(html);
            initWorkerFilter();
        })
        .catch(function (err) {
            if (skel) skel.remove();
            showToast('Failed to load popup: ' + err.message, 'error');
        });
}

function initWorkerFilter() {
    var input = document.getElementById('workerFilter');
    var clearBtn = document.getElementById('clearWorkerFilter');
    if (!input || !clearBtn) return;
    function toggleClear() {
        clearBtn.classList.toggle('hidden', !input.value);
    }
    input.addEventListener('input', function () {
        var q = this.value.toLowerCase().trim();
        document.querySelectorAll('#addWorkerList .worker-card').forEach(function (el) {
            var match = !q || el.dataset.name.includes(q);
            el.style.display = match ? '' : 'none';
        });
        toggleClear();
    });
    clearBtn.addEventListener('click', function () {
        input.value = '';
        input.dispatchEvent(new Event('input'));
        input.focus();
    });
}

// Status flow: Queued (0) -> InProgress (1) -> InReview (2) -> Finish (3)
var STATUS_FLOW = ['Queued', 'InProgress', 'InReview', 'Finish'];
var STATUS_LABELS = { Queued: 'Queued', InProgress: 'In Progress', InReview: 'In Review', Finish: 'Finished' };

function moveTaskStatus(taskIdEnc, direction) {
    var card = findCard(taskIdEnc);
    if (!card) return;
    var current = card.getAttribute('data-status');
    if (current === 'InReview') {
        if (direction === -1) changeStatus(taskIdEnc, 'InProgress');
        if (direction === 1) changeStatus(taskIdEnc, 'Finish');
        return;
    }
    var idx = STATUS_FLOW.indexOf(current);
    if (idx === -1) return;
    var nextIdx = idx + direction;
    if (nextIdx < 0 || nextIdx >= STATUS_FLOW.length) return;
    changeStatus(taskIdEnc, STATUS_FLOW[nextIdx]);
}

var _activeStatusBtn = null;

function openStatusSheet(taskIdEnc, currentStatus, btn) {
    if (window.innerWidth >= 640 && btn) {
        if (_activeStatusBtn === btn) {
            closeStatusSheet();
            _activeStatusBtn = null;
            return;
        }
        _activeStatusBtn = btn;
        openStatusDropdown(taskIdEnc, currentStatus, btn);
    } else {
        _activeStatusBtn = null;
        openStatusMobileSheet(taskIdEnc, currentStatus);
    }
}

function openStatusMobileSheet(taskIdEnc, currentStatus) {
    var statuses = ['Queued', 'InProgress', 'InReview', 'Finish'];
    var html = '<div class="px-6 pt-2 pb-6"><h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide mb-4">Change Status</h3><div class="flex flex-col gap-2">';
    statuses.forEach(function (s) {
        var label = STATUS_LABELS[s] || s;
        var isCurrent = s === currentStatus;
        var cls = 'w-full text-left px-4 py-3 rounded-lg text-sm font-medium transition-colors ' +
            (isCurrent ? 'bg-brand text-white cursor-default' : 'bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600');
        html += '<button class="' + cls + '" data-status="' + s + '"' + (isCurrent ? ' disabled' : '') + '>' + label + '</button>';
    });
    html += '</div></div>';

    openModal(html);

    var content = document.getElementById('modal-content');
    content.style.height = 'auto';
    content.style.maxHeight = 'none';
    var panel = document.getElementById('modal-panel');
    if (panel) panel.style.maxHeight = 'none';
    content.querySelectorAll('button[data-status]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var s = this.getAttribute('data-status');
            if (s && s !== currentStatus) {
                closeModal();
                changeStatus(taskIdEnc, s);
            }
        });
    });
}

function openStatusDropdown(taskIdEnc, currentStatus, btn) {
    var sheet = document.getElementById('status-bottom-sheet');
    var body = document.getElementById('sheet-body');
    var handle = document.getElementById('sheet-handle');
    var opts = document.getElementById('status-sheet-options');
    if (!sheet || !opts) return;

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
        sheet._stopProp = sheet._stopProp || function (e) { e.stopPropagation(); };
        sheet.removeEventListener('click', sheet._stopProp);
        sheet.addEventListener('click', sheet._stopProp);
    } else {
        sheet.style.position = 'fixed';
        sheet.style.top = rect.bottom + 'px';
        var dropLeft = rect.left;
        if (dropLeft + 160 > window.innerWidth - 8) {
            dropLeft = Math.max(8, rect.right - 160);
        }
        sheet.style.left = dropLeft + 'px';
    }

    opts.innerHTML = '';
    var statuses = ['Queued', 'InProgress', 'InReview', 'Finish'];
    statuses.forEach(function (s) {
        var label = STATUS_LABELS[s] || s;
        var isCurrent = s === currentStatus;
        var el = document.createElement('button');
        el.className = 'w-full text-left px-4 py-3 rounded-lg text-sm font-medium transition-colors ' +
            (isCurrent ? 'bg-brand text-white cursor-default' : 'bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600');
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
    _activeStatusBtn = null;
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

    // Apply empty-column width on load and on resize
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', updateColumnCounts);
    } else {
        updateColumnCounts();
    }
    window.addEventListener('resize', updateColumnCounts);


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

function showSkeleton(container) {
    var skeleton = document.createElement('div');
    skeleton.className = 'skeleton skeleton-card w-full mb-3';
    skeleton.id = 'skeleton-' + Date.now();
    container.appendChild(skeleton);
    return skeleton;
}

function openAssignModal(taskId, projectId) {
    var col = document.querySelector('.kanban-column[data-status="InProgress"]');
    var skel = col ? showSkeleton(col) : null;

    fetch('/Dashboard/AssignPopup?taskId=' + taskId + '&projectId=' + projectId)
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
                            refreshTaskCard(taskId);
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

function removeWorker(taskId, workerId, btn) {
    var formData = new FormData();
    formData.append('taskId', taskId);
    formData.append('workerId', workerId);

    var card = btn.closest('.task-card');
    if (card) card.style.opacity = '0.5';

    fetch('/Dashboard/RemoveWorker', { method: 'POST', body: formData })
        .then(function (r) {
            if (r.ok) {
                refreshTaskCard(taskId);
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

function openEvaluationModal(taskId, projectId) {
    var col = document.querySelector('.kanban-column[data-status="Finish"]');
    var skel = col ? showSkeleton(col) : null;

    fetch('/Dashboard/EvaluationPopup?taskId=' + taskId + '&projectId=' + (projectId || 1))
        .then(function (r) { return r.text(); })
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

    var taskId = form.querySelector('[name="taskId"]').value;
    var workerSelect = form.querySelector('[name="workerId"]');
    var workerId = workerSelect ? workerSelect.value : 0;
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
    formData.append('taskId', taskId);
    formData.append('workerId', workerId);
    for (var i = 0; i < skillPositions.length; i++) {
        formData.append('skillPositions', skillPositions[i]);
        formData.append('basePoints', basePoints[i]);
    }

    fetch('/Dashboard/SubmitEvaluation', { method: 'POST', body: formData })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (data.hasMore) {
                showToast('Evaluation submitted. Select next worker.', 'success');
                openEvaluationModal(parseInt(taskId), window.__dashboardProjectId || 1);
            } else {
                closeModal();
                refreshTaskCard(parseInt(taskId));
                showToast('All evaluations submitted successfully', 'success');
            }
        })
        .catch(function (err) {
            showToast('Evaluation error: ' + err.message, 'error');
            if (btn) { btn.disabled = false; btn.textContent = 'Submit Evaluation'; }
        });
});

function reloadEvaluationPopup(taskId, workerId) {
    var projectId = window.__dashboardProjectId || 1;
    fetch('/Dashboard/EvaluationPopup?taskId=' + taskId + '&projectId=' + projectId + '&workerId=' + workerId)
        .then(function (r) { return r.text(); })
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

function updateSliderPreview(input) {
    var pos = input.getAttribute('data-position');
    var val = parseFloat(input.value);
    var valEl = document.getElementById('val-' + pos);
    if (valEl) valEl.textContent = val.toFixed(1);

    var basePoints = val / 10.0 - 0.5;
    var bpHidden = document.getElementById('bp-' + pos);
    if (bpHidden) bpHidden.value = Math.round(basePoints * 10000);

    var form = document.getElementById('evaluationForm');
    var multiplier = form ? parseFloat(form.getAttribute('data-crit-multiplier')) || 1.0 : 1.0;

    // Update preview table
    var currentEl = document.getElementById('current-' + pos);
    var newEl = document.getElementById('new-' + pos);
    var diffEl = document.getElementById('diff-' + pos);
    if (!currentEl || !newEl || !diffEl) return;

    var current = parseFloat(input.getAttribute('data-current')) || 0;
    var impact = basePoints * multiplier;
    var newLevel = Math.max(0, Math.min(10, Math.round((current + impact) / 0.05) * 0.05));

    newEl.textContent = newLevel.toFixed(2);

    var diff = newLevel - current;
    var diffText = (diff >= 0 ? '+' : '') + diff.toFixed(2);
    diffEl.textContent = diffText;

    diffEl.className = 'inline-block px-1.5 py-0.5 rounded-full text-[10px] font-semibold';
    if (diff > 0.001) diffEl.classList.add('bg-green-100', 'text-green-700');
    else if (diff < -0.001) diffEl.classList.add('bg-red-100', 'text-red-700');
    else diffEl.classList.add('bg-gray-100', 'text-gray-500');
}

function refreshTaskCard(taskId, newStatus) {
    var card = document.querySelector('.task-card[data-task-id="' + taskId + '"]');
    if (!card) return;

    var projectId = window.__dashboardProjectId || 1;

    fetch('/Dashboard/GetTaskCardHtml?taskId=' + taskId + '&projectId=' + projectId)
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

function updateTaskCard(taskId, newStatus) {
    var card = document.querySelector('.task-card[data-task-id="' + taskId + '"]');
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
document.getElementById('kanban-grid').addEventListener('dragstart', function (e) {
    var card = e.target.closest('.task-card');
    if (!card) return;
    e.dataTransfer.setData('text/plain', card.dataset.taskId);
    card.style.opacity = '0.5';
});

document.getElementById('kanban-grid').addEventListener('dragend', function (e) {
    var card = e.target.closest('.task-card');
    if (!card) return;
    card.style.opacity = '';
});

document.getElementById('kanban-grid').addEventListener('dragover', function (e) {
    var col = e.target.closest('.kanban-column');
    if (col) e.preventDefault();
});

document.getElementById('kanban-grid').addEventListener('drop', function (e) {
    var col = e.target.closest('.kanban-column');
    if (!col) return;
    e.preventDefault();
    var taskId = e.dataTransfer.getData('text/plain');
    var newStatus = col.dataset.status;

    if (!taskId || !newStatus) return;

    var formData = new FormData();
    formData.append('taskId', taskId);
    formData.append('newStatus', newStatus);

    fetch('/Dashboard/ChangeStatus', { method: 'POST', body: formData })
        .then(function (r) {
            if (r.ok) {
                refreshTaskCard(taskId, newStatus);
                showToast('Task moved to ' + newStatus, 'success');
            } else {
                showToast('Status change failed: ' + r.status, 'error');
            }
        })
        .catch(function (err) { showToast('Status change error: ' + err.message, 'error'); });
});

function archiveTask(taskId, projectId, btn) {
    var card = btn.closest('.task-card');
    if (!card) return;

    // Animate card out
    card.style.transition = 'transform 0.3s ease, opacity 0.3s ease';
    card.style.transform = 'scale(0.95)';
    card.style.opacity = '0';

    var undoKey = 'archive-undo-' + taskId;

    showUndoToast('Task archived.', function () {
        // Undo: restore the card
        card.style.transform = '';
        card.style.opacity = '';
        clearTimeout(window[undoKey]);
        showToast('Archive cancelled', 'info');
    });

    // After 2 seconds, actually archive
    window[undoKey] = setTimeout(function () {
        var formData = new FormData();
        formData.append('taskId', taskId);
        fetch('/Dashboard/ArchiveTask', { method: 'POST', body: formData })
            .then(function (r) {
                if (r.ok) {
                    card.remove();
                    updateColumnCounts();
                    showToast('Task archived permanently', 'info');
                } else {
                    showToast('Archive failed: ' + r.status, 'error');
                    card.style.transform = '';
                    card.style.opacity = '';
                }
            })
            .catch(function (err) {
                showToast('Archive error: ' + err.message, 'error');
                card.style.transform = '';
                card.style.opacity = '';
            });
    }, 2000);
}

function openAddWorkerPopup(projectId) {
    var col = document.querySelector('.kanban-column');
    var skel = col ? showSkeleton(col) : null;

    fetch('/Dashboard/AddWorkerPopup?projectId=' + projectId)
        .then(function (r) { return r.text(); })
        .then(function (html) {
            if (skel) skel.remove();
            openModal(html);
            document.getElementById('addWorkerToProjectForm').addEventListener('submit', function (e) {
                e.preventDefault();
                var btn = this.querySelector('button[type="submit"]');
                if (btn) { btn.disabled = true; btn.innerHTML = '<span class="spinner mr-1"></span> Adding...'; }
                var formData = new FormData(this);
                fetch('/Dashboard/AddWorkerToProject', { method: 'POST', body: formData })
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

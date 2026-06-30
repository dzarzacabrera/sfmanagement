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
                var btn = this.querySelector('button[type="submit"]');
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
            document.getElementById('evaluationForm').addEventListener('submit', function (e) {
                e.preventDefault();
                var btn = this.querySelector('button[type="submit"]');
                if (btn) { btn.disabled = true; btn.innerHTML = '<span class="spinner mr-1"></span> Submitting...'; }
                var formData = new FormData(this);
                fetch('/Dashboard/SubmitEvaluation', { method: 'POST', body: formData })
                    .then(function (r) {
                        if (r.ok) {
                            closeModal();
                            refreshTaskCard(taskId);
                            showToast('Evaluation submitted successfully', 'success');
                        } else {
                            showToast('Evaluation failed: ' + r.status, 'error');
                            if (btn) { btn.disabled = false; btn.textContent = 'Submit Evaluation'; }
                        }
                    })
                    .catch(function (err) {
                        showToast('Evaluation error: ' + err.message, 'error');
                        if (btn) { btn.disabled = false; btn.textContent = 'Submit Evaluation'; }
                    });
            });
        })
        .catch(function (err) {
            if (skel) skel.remove();
            showToast('Failed to load evaluation: ' + err.message, 'error');
        });
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
                    var placeholder = targetCol.querySelector('.empty-placeholder');
                    if (placeholder) placeholder.remove();
                    targetCol.appendChild(newCard);
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

document.querySelectorAll('.kanban-column').forEach(function (col) {
    col.addEventListener('dragover', function (e) { e.preventDefault(); });
    col.addEventListener('drop', function (e) {
        e.preventDefault();
        var taskId = e.dataTransfer.getData('text/plain');
        var newStatus = this.dataset.status;

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
});

document.querySelectorAll('.task-card').forEach(function (card) {
    card.setAttribute('draggable', true);
    card.addEventListener('dragstart', function (e) {
        e.dataTransfer.setData('text/plain', this.dataset.taskId);
        this.style.opacity = '0.5';
    });
    card.addEventListener('dragend', function (e) {
        this.style.opacity = '';
    });
});

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

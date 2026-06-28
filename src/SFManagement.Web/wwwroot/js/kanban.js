function openAssignModal(taskId, projectId) {
    fetch('/Dashboard/AssignPopup?taskId=' + taskId + '&projectId=' + projectId)
        .then(function (r) { return r.text(); })
        .then(function (html) {
            openModal(html);
            document.getElementById('assignForm').addEventListener('submit', function (e) {
                e.preventDefault();
                var formData = new FormData(this);
                fetch('/Dashboard/AssignWorker', { method: 'POST', body: formData })
                    .then(function (r) {
                        if (r.ok) {
                            closeModal();
                            updateTaskCard(taskId, null);
                        } else {
                            console.error('AssignWorker failed:', r.status);
                        }
                    })
                    .catch(function (err) { console.error('AssignWorker error:', err); });
            });
        })
        .catch(function (err) { console.error('AssignPopup error:', err); });
}

function openEvaluationModal(taskId, projectId) {
    fetch('/Dashboard/EvaluationPopup?taskId=' + taskId + '&projectId=' + (projectId || 1))
        .then(function (r) { return r.text(); })
        .then(function (html) {
            openModal(html);
            document.getElementById('evaluationForm').addEventListener('submit', function (e) {
                e.preventDefault();
                var formData = new FormData(this);
                fetch('/Dashboard/SubmitEvaluation', { method: 'POST', body: formData })
                    .then(function (r) {
                        if (r.ok) {
                            closeModal();
                            var card = document.querySelector('.task-card[data-task-id="' + taskId + '"]');
                            if (card) card.remove();
                        } else {
                            console.error('SubmitEvaluation failed:', r.status);
                        }
                    })
                    .catch(function (err) { console.error('SubmitEvaluation error:', err); });
            });
        })
        .catch(function (err) { console.error('EvaluationPopup error:', err); });
}

function updateTaskCard(taskId, newStatus) {
    var card = document.querySelector('.task-card[data-task-id="' + taskId + '"]');
    if (!card) return;

    if (newStatus) {
        var targetCol = document.querySelector('.kanban-column[data-status="' + newStatus + '"]');
        if (targetCol) {
            var placeholder = targetCol.querySelector('.empty-placeholder');
            if (placeholder) placeholder.remove();
            targetCol.appendChild(card);
            card.setAttribute('data-status', newStatus);

            var statusBadge = card.querySelector('.status-badge');
            if (statusBadge) statusBadge.textContent = newStatus;
        }
    } else {
        // Assigned worker changed — update badge on card
        card.classList.add('border-l-4', 'border-l-green-500');
    }

    // Update column counters
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
                    updateTaskCard(taskId, newStatus);
                } else {
                    console.error('ChangeStatus failed:', r.status);
                }
            })
            .catch(function (err) { console.error('ChangeStatus error:', err); });
    });
});

document.querySelectorAll('.task-card').forEach(function (card) {
    card.setAttribute('draggable', true);
    card.addEventListener('dragstart', function (e) {
        e.dataTransfer.setData('text/plain', this.dataset.taskId);
    });
});

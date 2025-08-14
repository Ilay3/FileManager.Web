// Files Manager JavaScript functionality

class FilesManager {
    constructor() {
        this.currentFolderId = null;
        this.selectedFiles = new Set();
        this.contextItem = null;
        this.init();
    }

    init() {
        this.bindEvents();
        this.bindSelectionEvents();
        this.loadInitialData();
    }

    navigateTo(url) {
        window.location.href = url;
    }

    bindEvents() {
        document.addEventListener('dblclick', (e) => {
            const item = e.target.closest('.explorer-item');
            if (!item) return;
            const id = item.dataset.id;
            const type = item.dataset.type;
            if (type === 'folder') {
                const url = `${window.location.pathname}?folderId=${id}`;
                this.navigateTo(url);
            } else {
                this.previewFile(id);
            }
        });
    }

    bindSelectionEvents() {
        document.addEventListener('change', (e) => {
            if (e.target.id === 'selectAll') {
                const checked = e.target.checked;
                document.querySelectorAll('.file-select').forEach(cb => {
                    cb.checked = checked;
                    const id = cb.dataset.fileId;
                    const row = cb.closest('.explorer-item');
                    if (checked) {
                        this.selectedFiles.add(id);
                        row?.classList.add('selected');
                    } else {
                        this.selectedFiles.delete(id);
                        row?.classList.remove('selected');
                    }
                });
                this.updateSelectionButtons();
            } else if (e.target.classList.contains('file-select')) {
                const id = e.target.dataset.fileId;
                const row = e.target.closest('.explorer-item');
                if (e.target.checked) {
                    this.selectedFiles.add(id);
                    row?.classList.add('selected');
                } else {
                    this.selectedFiles.delete(id);
                    row?.classList.remove('selected');
                }
                this.updateSelectionButtons();
            }
        });
    }

    updateSelectionButtons() {
        const downloadBtn = document.getElementById('downloadSelected');
        if (downloadBtn) downloadBtn.disabled = this.selectedFiles.size === 0;
        const deleteBtn = document.getElementById('deleteSelected');
        if (deleteBtn) deleteBtn.disabled = this.selectedFiles.size === 0;
    }

    async downloadSelected() {
        if (this.selectedFiles.size === 0) return;
        const response = await fetchWithProgress('/api/files/download-zip', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ ids: Array.from(this.selectedFiles) })
        });
        if (response.ok) {
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = 'files.zip';
            a.click();
            window.URL.revokeObjectURL(url);
        }
    }

    async deleteSelected() {
        if (this.selectedFiles.size === 0) return;
        const response = await fetchWithProgress('/api/files/delete', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ ids: Array.from(this.selectedFiles) })
        });
        if (response.ok) {
            this.showNotification('Файлы удалены', 'success');
            setTimeout(() => location.reload(), 500);
        } else {
            const text = await response.text();
            this.showNotification('Ошибка удаления файлов: ' + text, 'error');
        }
    }

    loadInitialData() {
        const params = new URLSearchParams(window.location.search);
        this.currentFolderId = params.get('folderId') || null;
    }



    // Tree view functions
    async toggleTreeNode(nodeId) {
        const node = document.querySelector(`[data-node-id="${nodeId}"]`);
        if (!node) return;

        const children = node.querySelector('.tree-children');
        const toggle = node.querySelector('.tree-toggle');

        if (!children || !toggle) return;

        if (children.style.display === 'none' || !children.style.display) {
            // Expand
            children.style.display = 'block';
            toggle.textContent = '▼';

            // Load children if not loaded
            if (children.children.length === 0) {
                await this.loadFolderContents(nodeId, children);
            }
        } else {
            // Collapse
            children.style.display = 'none';
            toggle.textContent = '▶';
        }
    }

    async loadFolderContents(folderId, container) {
        try {
            const response = await fetchWithProgress(`/api/folders/${folderId}/contents`);
            if (response.ok) {
                const data = await response.json();
                this.renderTreeChildren(data.children, container);
            }
        } catch (error) {
            console.error('Error loading folder contents:', error);
        }
    }

    renderTreeChildren(children, container) {
        container.innerHTML = '';

        children.forEach(child => {
            const childElement = this.createTreeNodeElement(child);
            container.appendChild(childElement);
        });
    }

    createTreeNodeElement(nodeData) {
        const div = document.createElement('div');
        div.className = 'tree-node';
        div.setAttribute('data-node-id', nodeData.id);
        div.setAttribute('data-level', nodeData.level);

        const content = document.createElement('div');
        content.className = 'tree-node-content';
        content.style.paddingLeft = `${nodeData.level * 20}px`;

        if (nodeData.type === 'folder') {
            const safeName = nodeData.name.replace(/'/g, "\\'");
            content.innerHTML = `
                ${nodeData.hasChildren ? '<span class="tree-toggle" onclick="filesManager.toggleTreeNode(\'' + nodeData.id + '\')">▶</span>' : '<span class="tree-spacer"></span>'}
                <span class="tree-icon">${nodeData.icon}</span>
                <a href="?folderId=${nodeData.id}" class="tree-link folder-link">${nodeData.name}</a>
                ${nodeData.itemsCount ? '<span class="tree-count">(' + nodeData.itemsCount + ')</span>' : ''}
                <div class="tree-file-actions">
                    <button class="btn btn-tiny" onclick="openRenameFolderModal('${nodeData.id}', '${safeName}')" title="Переименовать">✏️</button>
                    <button class="btn btn-tiny" onclick="moveFolder('${nodeData.id}')" title="Переместить">📁</button>
                    <button class="btn btn-tiny" onclick="deleteFolder('${nodeData.id}', '${safeName}')" title="Удалить">🗑️</button>
                    <button class="btn btn-tiny" onclick="shareAccess('folder','${nodeData.id}')" title="Права">🔑</button>
                </div>
                <span class="tree-date">${this.formatDate(nodeData.updatedAt || nodeData.createdAt)}</span>
            `;

            if (nodeData.hasChildren) {
                const childrenDiv = document.createElement('div');
                childrenDiv.className = 'tree-children';
                childrenDiv.style.display = 'none';
                div.appendChild(childrenDiv);
            }
        } else {
            content.innerHTML = `
                <span class="tree-spacer"></span>
                <span class="tree-icon">${nodeData.icon}</span>
                <span class="tree-link file-link" onclick="filesManager.previewFile('${nodeData.id}')">${nodeData.name}</span>
                ${nodeData.sizeBytes ? '<span class="tree-size">' + this.formatFileSize(nodeData.sizeBytes) + '</span>' : ''}
                <div class="tree-file-actions">
                    <button class="btn btn-tiny" onclick="filesManager.downloadFile('${nodeData.id}')" title="Скачать">⬇️</button>
                    <button class="btn btn-tiny" onclick="filesManager.deleteFile('${nodeData.id}', '${nodeData.name}')" title="Удалить">🗑️</button>
                    <button class="btn btn-tiny" onclick="shareAccess('file','${nodeData.id}')" title="Права">🔑</button>
                </div>
                <span class="tree-date">${this.formatDate(nodeData.updatedAt || nodeData.createdAt)}</span>
            `;
        }

        div.appendChild(content);
        return div;
    }

    // File actions
    async previewFile(fileId) {
        try {
            // Переходим на страницу предпросмотра
            this.navigateTo(`/Files/Preview/${fileId}`);
        } catch (error) {
            console.error('Error opening file preview:', error);
            this.showNotification('Ошибка при открытии предпросмотра файла', 'error');
        }
    }

    async editFile(fileId, fileName) {
        try {
            const response = await fetchWithProgress(`/api/files/${fileId}/edit`);
            const data = await response.json();

            if (data.hasActiveEditors && !data.canProceed) {
                this.showNotification('Файл сейчас редактируется другим пользователем. Попробуйте позже.', 'error');
                return;
            }

            if (data.hasActiveEditors && data.warnings) {
                const warningMessage = 'Внимание! ' + data.warnings.join('\n') + '\n\nПродолжить редактирование?';
                if (!confirm(warningMessage)) {
                    return;
                }
            }

            if (data.editUrl) {
                // Открываем в новой вкладке
                window.open(data.editUrl, '_blank');

                // Показываем уведомление
                this.showNotification('Файл открыт для редактирования в новой вкладке', 'success');
            }
        } catch (error) {
            console.error('Error opening file for edit:', error);
            this.showNotification('Ошибка при открытии файла для редактирования', 'error');
        }
    }

    async viewFile(fileId) {
        // Переадресация на новую функцию предпросмотра
        return this.previewFile(fileId);
    }

    async downloadFile(fileId) {
        try {
            // Прямое скачивание файла
            window.location.href = `/api/files/${fileId}/content`;
        } catch (error) {
            console.error('Error downloading file:', error);
            this.showNotification('Ошибка при скачивании файла', 'error');
        }
    }

    async addFavorite(itemId, itemType) {
        try {
            const url = itemType === 'file'
                ? `/api/favorites/files/${itemId}`
                : `/api/favorites/folders/${itemId}`;
            const response = await fetchWithProgress(url, { method: 'POST' });
            if (response.ok) {
                this.showNotification('Добавлено в избранное', 'success');
            } else {
                const text = await response.text();
                this.showNotification('Не удалось добавить в избранное: ' + text, 'error');
            }
        } catch (error) {
            console.error('Error adding favorite:', error);
            this.showNotification('Ошибка при добавлении в избранное', 'error');
        }
    }

    async removeFavorite(itemId, itemType) {
        try {
            const el = document.querySelector(`[data-id="${itemId}"]`);
            if (!el) {
                return;
            }
            const url = itemType === 'file'
                ? `/api/favorites/files/${itemId}`
                : `/api/favorites/folders/${itemId}`;
            const response = await fetchWithProgress(url, { method: 'DELETE' });
            if (response.ok) {
                this.showNotification('Удалено из избранного', 'success');
                el.remove();
            } else if (response.status === 404) {
                el.remove();
            } else {
                const text = await response.text();
                this.showNotification('Не удалось удалить из избранного: ' + text, 'error');
            }
        } catch (error) {
            console.error('Error removing favorite:', error);
            this.showNotification('Ошибка при удалении из избранного', 'error');
        }
    }

    async deleteFile(fileId, fileName) {
        if (!confirm(`Вы уверены, что хотите удалить файл "${fileName}"?`)) {
            return;
        }
        try {
            const response = await fetchWithProgress(`/api/files/${fileId}`, { method: 'DELETE', credentials: 'include' });
            if (response.ok) {
                this.showNotification('Файл удалён', 'success');
                const listItem = document.querySelector(`.explorer-item[data-id="${fileId}"]`);
                if (listItem) {
                    listItem.remove();
                }
                const treeNode = document.querySelector(`.tree-node[data-node-id="${fileId}"]`);
                if (treeNode) {
                    treeNode.remove();
                }
                this.selectedFiles.delete(fileId);
                this.updateDownloadButton();
            } else if (response.status === 401 || response.status === 403) {
                this.showNotification('Недостаточно прав для удаления файла', 'error');
            } else {
                const text = await response.text();
                this.showNotification('Ошибка удаления файла: ' + text, 'error');
            }
        } catch (error) {
            console.error('Error deleting file:', error);
            this.showNotification('Ошибка при удалении файла', 'error');
        }
    }

    async showProperties(itemId, itemName, itemType) {
        const crumbs = Array.from(document.querySelectorAll('.breadcrumb-item'))
            .map(el => el.textContent.trim().replace(/\s*\/\s*/g, ''))
            .filter(t => t.length > 0);
        const path = (crumbs.length ? crumbs.join('/') + '/' : '') + itemName;

        let size = '-';
        let creator = '';
        let created = '';
        let updated = '';
        let accessRules = [];

        try {
            if (itemType === 'file') {
                const res = await fetchWithProgress(`/api/files/${itemId}`, { credentials: 'include' });
                if (res.ok) {
                    const data = await res.json();
                    size = data.formattedSize;
                    creator = data.uploadedByName;
                    created = new Date(data.createdAt).toLocaleString();
                    updated = data.updatedAt ? new Date(data.updatedAt).toLocaleString() : created;
                }
            } else {
                const res = await fetchWithProgress(`/api/folders/${itemId}`, { credentials: 'include' });
                if (res.ok) {
                    const data = await res.json();
                    creator = data.createdByName;
                    created = new Date(data.createdAt).toLocaleString();
                    updated = data.updatedAt ? new Date(data.updatedAt).toLocaleString() : created;
                }
            }
            const accessRes = await fetchWithProgress(
                itemType === 'file'
                    ? `/api/access/file/${itemId}`
                    : `/api/access/folder/${itemId}`,
                { credentials: 'include' }
            );
            if (accessRes.ok) {
                accessRules = await accessRes.json();
            }
        } catch (error) {
            console.error('Error loading properties:', error);
        }

        document.getElementById('propPath').textContent = path;
        document.getElementById('propSize').textContent = size;
        document.getElementById('propCreator').textContent = creator;
        document.getElementById('propCreated').textContent = created;
        document.getElementById('propUpdated').textContent = updated;

        const accessBody = document.getElementById('propAccessBody');
        if (accessBody) {
            accessBody.innerHTML = '';
            accessRules.forEach(r => {
                const tr = document.createElement('tr');
                const nameTd = document.createElement('td');
                nameTd.textContent = r.userName || r.groupName || '';
                const typeTd = document.createElement('td');
                typeTd.textContent = (r.accessType & 2) ? 'Изменение' : 'Чтение';
                tr.appendChild(nameTd);
                tr.appendChild(typeTd);
                accessBody.appendChild(tr);
            });
        }

        const modalEl = document.getElementById('propertiesModal');
        if (modalEl) {
            modalEl.style.display = 'flex';
            setTimeout(() => {
                modalEl.classList.remove('modal-exit');
                modalEl.classList.add('modal-enter');
            }, 10);
        }
    }

    // Folder actions
    async createFolder(name, parentId) {
        try {
            const response = await fetchWithProgress('/api/folders', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name, parentId })
            });
            if (response.ok) {
                this.showNotification('Папка создана', 'success');
                setTimeout(() => location.reload(), 500);
            } else {
                const text = await response.text();
                this.showNotification('Ошибка создания папки: ' + text, 'error');
            }
        } catch (error) {
            console.error('Error creating folder:', error);
        }
    }

    async renameFolder(folderId, newName) {
        try {
            const response = await fetchWithProgress(`/api/folders/${folderId}/rename`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name: newName })
            });
            if (response.ok) {
                this.showNotification('Папка переименована', 'success');
                setTimeout(() => location.reload(), 500);
            } else {
                const text = await response.text();
                this.showNotification('Ошибка переименования: ' + text, 'error');
            }
        } catch (error) {
            console.error('Error renaming folder:', error);
        }
    }

    async deleteFolder(folderId, folderName) {
        if (!confirm(`Удалить папку "${folderName}"?`)) return;
        try {
            const response = await fetchWithProgress(`/api/folders/${folderId}`, { method: 'DELETE' });
            if (response.ok) {
                this.showNotification('Папка удалена', 'success');
                setTimeout(() => location.reload(), 500);
            } else {
                const text = await response.text();
                this.showNotification('Ошибка удаления: ' + text, 'error');
            }
        } catch (error) {
            console.error('Error deleting folder:', error);
        }
    }

    async moveFolder(folderId) {
        const newParentId = prompt('ID новой папки (оставьте пустым для корня)');
        if (newParentId === null) return;
        try {
            const response = await fetchWithProgress(`/api/folders/${folderId}/move`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ newParentId: newParentId || null })
            });
            if (response.ok) {
                this.showNotification('Папка перемещена', 'success');
                setTimeout(() => location.reload(), 500);
            } else {
                const text = await response.text();
                this.showNotification('Ошибка перемещения: ' + text, 'error');
            }
        } catch (error) {
            console.error('Error moving folder:', error);
        }
    }

    async shareAccess(itemType, itemId) {
        const principalId = prompt('ID пользователя или группы');
        if (!principalId) return;
        const access = prompt('Права (Read,Write,Delete)', 'Read');
        if (!access) return;
        const body = {
            fileId: itemType === 'file' ? itemId : null,
            folderId: itemType === 'folder' ? itemId : null,
            userId: principalId,
            groupId: null,
            accessType: access
        };
        try {
            const response = await fetchWithProgress('/api/access/grant', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });
            if (response.ok) {
                this.showNotification('Права назначены', 'success');
            } else {
                this.showNotification('Ошибка назначения прав', 'error');
            }
        } catch (error) {
            console.error('Error granting access:', error);
        }
    }

    // Sorting
    sortBy(field) {
        const params = new URLSearchParams(window.location.search);
        const currentSort = params.get('sortBy');
        const currentDirection = params.get('sortDirection') || 'asc';

        const newDirection = (currentSort === field && currentDirection === 'asc') ? 'desc' : 'asc';

        params.set('sortBy', field);
        params.set('sortDirection', newDirection);
        params.set('page', '1');

        window.location.search = params.toString();
    }

    // Utility method for notifications
    showNotification(message, type = 'info') {
        if (typeof window.showNotification === 'function') {
            window.showNotification(message, type);
        }
    }

    // Utility functions
    formatFileSize(bytes) {
        if (bytes < 1024) return `${bytes} Б`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} КБ`;
        if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} МБ`;
        return `${(bytes / (1024 * 1024 * 1024)).toFixed(1)} ГБ`;
    }

    formatDate(dateString) {
        if (!dateString) return '';
        const date = new Date(dateString);
        return date.toLocaleDateString('ru-RU', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    }
}

// Global functions for onclick handlers
let filesManager;

function toggleTreeNode(nodeId) {
    if (filesManager) {
        filesManager.toggleTreeNode(nodeId);
    }
}

function viewFile(fileId) {
    if (filesManager) {
        filesManager.viewFile(fileId);
    }
}

function previewFile(fileId) {
    if (filesManager) {
        filesManager.previewFile(fileId);
    }
}

function editFile(fileId, fileName) {
    if (filesManager) {
        filesManager.editFile(fileId, fileName);
    }
}

function downloadFile(fileId) {
    if (filesManager) {
        filesManager.downloadFile(fileId);
    }
}

function downloadSelected() {
    if (filesManager) {
        filesManager.downloadSelected();
    }
}

function deleteSelected() {
    if (filesManager) {
        filesManager.deleteSelected();
    }
}

function deleteFile(fileId, fileName) {
    if (filesManager) {
        filesManager.deleteFile(fileId, fileName);
    }
}

let createFolderParentId = null;
let renameFolderId = null;

function openCreateFolderModal(parentId) {
    createFolderParentId = parentId;
    const input = document.getElementById('createFolderName');
    if (input) input.value = '';
    const modal = document.getElementById('createFolderModal');
    if (modal) {
        modal.style.display = 'flex';
        setTimeout(() => {
            modal.classList.remove('modal-exit');
            modal.classList.add('modal-enter');
        }, 10);
    }
}

function closeCreateFolderModal() {
    const modal = document.getElementById('createFolderModal');
    if (modal) {
        modal.classList.remove('modal-enter');
        modal.classList.add('modal-exit');
        setTimeout(() => {
            modal.style.display = 'none';
        }, 300);
    }
}

function submitCreateFolder() {
    const input = document.getElementById('createFolderName');
    if (!input || !filesManager) return;
    const name = input.value.trim();
    if (!name) return;
    filesManager.createFolder(name, createFolderParentId);
    closeCreateFolderModal();
}

function openRenameFolderModal(folderId, currentName) {
    renameFolderId = folderId;
    const input = document.getElementById('renameFolderName');
    if (input) input.value = currentName;
    const modal = document.getElementById('renameFolderModal');
    if (modal) {
        modal.style.display = 'flex';
        setTimeout(() => {
            modal.classList.remove('modal-exit');
            modal.classList.add('modal-enter');
        }, 10);
    }
}

function closeRenameFolderModal() {
    const modal = document.getElementById('renameFolderModal');
    if (modal) {
        modal.classList.remove('modal-enter');
        modal.classList.add('modal-exit');
        setTimeout(() => {
            modal.style.display = 'none';
        }, 300);
    }
}

function submitRenameFolder() {
    const input = document.getElementById('renameFolderName');
    if (!input || !filesManager) return;
    const newName = input.value.trim();
    if (!newName) return;
    filesManager.renameFolder(renameFolderId, newName);
    closeRenameFolderModal();
}

function deleteFolder(folderId, folderName) {
    if (filesManager) {
        filesManager.deleteFolder(folderId, folderName);
    }
}

function moveFolder(folderId) {
    if (filesManager) {
        filesManager.moveFolder(folderId);
    }
}

function shareAccess(type, id) {
    if (filesManager) {
        filesManager.shareAccess(type, id);
    }
}

function sortBy(field) {
    if (filesManager) {
        filesManager.sortBy(field);
    }
}


function closePropertiesModal() {
    const modal = document.getElementById('propertiesModal');
    if (modal) {
        modal.classList.remove('modal-enter');
        modal.classList.add('modal-exit');
        setTimeout(() => {
            modal.style.display = 'none';
        }, 300);
    }
}

// Initialize when DOM is loaded
// Open upload page
window.openUploadModal = function (folderId) {
    const url = folderId ? `/Files/Upload?folderId=${folderId}` : '/Files/Upload';
    window.location.href = url;
};

// Drag and drop for the main page
function initDragAndDrop() {
    const mainContent = document.querySelector('.content-area');
    if (mainContent) {
        mainContent.addEventListener('dragover', function (e) {
            e.preventDefault();
            e.stopPropagation();
            mainContent.classList.add('drag-over');
        });

        mainContent.addEventListener('dragleave', function (e) {
            e.preventDefault();
            e.stopPropagation();
            mainContent.classList.remove('drag-over');
        });

        mainContent.addEventListener('drop', function (e) {
            e.preventDefault();
            e.stopPropagation();
            mainContent.classList.remove('drag-over');

            // Open upload modal with files
            if (typeof openUploadModal === 'function') {
                openUploadModal();
                setTimeout(() => {
                    if (typeof handleFiles === 'function') {
                        handleFiles(Array.from(e.dataTransfer.files));
                    }
                }, 100);
            }
        });
    }
}

// Version management functions
window.viewVersions = function (fileId) {
    if (window.navigateWithTransition) {
        window.navigateWithTransition(`/Files/${fileId}/Versions`);
    } else {
        window.location.href = `/Files/${fileId}/Versions`;
    }
};

function initVersionHistoryLinks() {
    const fileItems = document.querySelectorAll('.file-card, .files-table tr');
    fileItems.forEach(item => {
        const fileId = item.getAttribute('data-file-id');
        if (fileId) {
            // Placeholder for future version history integrations
        }
    });
}

function initializeFilesManager() {
    if (typeof window.filesManager === 'undefined') {
        window.filesManager = new FilesManager();
        filesManager = window.filesManager;
        initDragAndDrop();
        initVersionHistoryLinks();
    }
}

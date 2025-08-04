// Files Manager JavaScript functionality

class FilesManager {
    constructor() {
        this.currentFolderId = null;
        this.currentView = 'list';
        this.selectedFiles = new Set();
        this.init();
    }

    init() {
        this.bindEvents();
        this.bindSelectionEvents();
        this.loadInitialData();
    }

    bindEvents() {
        // Search functionality
        const searchInput = document.getElementById('searchInput');
        const searchBtn = document.getElementById('searchBtn');

        if (searchInput) {
            searchInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    this.performSearch(searchInput.value);
                }
            });
        }

        if (searchBtn && searchInput) {
            searchBtn.addEventListener('click', () => {
                this.performSearch(searchInput.value);
            });
        }

        // Filter changes
        const filterType = document.getElementById('filterType');
        if (filterType) {
            filterType.addEventListener('change', () => this.applyFilters());
        }

        const onlyMyFiles = document.getElementById('onlyMyFiles');
        if (onlyMyFiles) {
            onlyMyFiles.addEventListener('change', () => this.applyFilters());
        }
    }

    bindSelectionEvents() {
        document.addEventListener('change', (e) => {
            if (e.target.id === 'selectAll') {
                const checked = e.target.checked;
                document.querySelectorAll('.file-select').forEach(cb => {
                    cb.checked = checked;
                    const id = cb.dataset.fileId;
                    if (checked) this.selectedFiles.add(id); else this.selectedFiles.delete(id);
                });
                this.updateDownloadButton();
            } else if (e.target.classList.contains('file-select')) {
                const id = e.target.dataset.fileId;
                if (e.target.checked) this.selectedFiles.add(id); else this.selectedFiles.delete(id);
                this.updateDownloadButton();
            }
        });
    }

    updateDownloadButton() {
        const btn = document.getElementById('downloadSelected');
        if (btn) btn.disabled = this.selectedFiles.size === 0;
    }

    async downloadSelected() {
        if (this.selectedFiles.size === 0) return;
        const response = await fetch('/api/files/download-zip', {
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

    loadInitialData() {
        // Load based on current URL parameters
        const params = new URLSearchParams(window.location.search);
        this.currentFolderId = params.get('folderId') || null;
        this.currentView = params.get('view') || 'list';
    }

    performSearch(searchTerm) {
        const params = this.buildSearchParams();
        if (searchTerm) {
            params['SearchRequest.SearchTerm'] = searchTerm;
        } else {
            delete params['SearchRequest.SearchTerm'];
        }
        const newUrl = `${window.location.pathname}?${new URLSearchParams(params)}`;
        window.location.href = newUrl;
    }

    applyFilters() {
        const params = this.buildSearchParams();

        const filterType = document.getElementById('filterType');
        if (filterType && filterType.value) {
            params['SearchRequest.FileType'] = filterType.value;
        }

        const onlyMyFiles = document.getElementById('onlyMyFiles');
        if (onlyMyFiles && onlyMyFiles.checked) {
            params['SearchRequest.OnlyMyFiles'] = true;
        }

        const dateFrom = document.getElementById('dateFrom');
        if (dateFrom && dateFrom.value) params['SearchRequest.DateFrom'] = dateFrom.value;
        const dateTo = document.getElementById('dateTo');
        if (dateTo && dateTo.value) params['SearchRequest.DateTo'] = dateTo.value;
        const updatedFrom = document.getElementById('updatedFrom');
        if (updatedFrom && updatedFrom.value) params['SearchRequest.UpdatedFrom'] = updatedFrom.value;
        const updatedTo = document.getElementById('updatedTo');
        if (updatedTo && updatedTo.value) params['SearchRequest.UpdatedTo'] = updatedTo.value;
        const extension = document.getElementById('extension');
        if (extension && extension.value) params['SearchRequest.Extension'] = extension.value;
        const minSize = document.getElementById('minSize');
        if (minSize && minSize.value) params['SearchRequest.MinSizeBytes'] = minSize.value;
        const maxSize = document.getElementById('maxSize');
        if (maxSize && maxSize.value) params['SearchRequest.MaxSizeBytes'] = maxSize.value;
        const tags = document.getElementById('tags');
        if (tags && tags.value) params['SearchRequest.Tags'] = tags.value;
        const ownerId = document.getElementById('ownerId');
        if (ownerId && ownerId.value) params['SearchRequest.OwnerId'] = ownerId.value;

        // Update URL and reload
        const newUrl = `${window.location.pathname}?${new URLSearchParams(params)}`;
        window.location.href = newUrl;
    }

    buildSearchParams() {
        const params = new URLSearchParams(window.location.search);
        return {
            'SearchRequest.SearchTerm': params.get('SearchRequest.SearchTerm') || '',
            'SearchRequest.FolderId': this.currentFolderId,
            'SearchRequest.FileType': params.get('SearchRequest.FileType') || '',
            'SearchRequest.OnlyMyFiles': params.get('SearchRequest.OnlyMyFiles') === 'true',
            'SearchRequest.SortBy': params.get('SearchRequest.SortBy') || 'name',
            'SearchRequest.SortDirection': params.get('SearchRequest.SortDirection') || 'asc',
            'SearchRequest.DateFrom': params.get('SearchRequest.DateFrom') || '',
            'SearchRequest.DateTo': params.get('SearchRequest.DateTo') || '',
            'SearchRequest.UpdatedFrom': params.get('SearchRequest.UpdatedFrom') || '',
            'SearchRequest.UpdatedTo': params.get('SearchRequest.UpdatedTo') || '',
            'SearchRequest.Extension': params.get('SearchRequest.Extension') || '',
            'SearchRequest.MinSizeBytes': params.get('SearchRequest.MinSizeBytes') || '',
            'SearchRequest.MaxSizeBytes': params.get('SearchRequest.MaxSizeBytes') || '',
            'SearchRequest.Tags': params.get('SearchRequest.Tags') || '',
            'SearchRequest.OwnerId': params.get('SearchRequest.OwnerId') || '',
            'SearchRequest.Page': 1,
            folderId: this.currentFolderId
        };
    }

    toggleAdvanced() {
        const block = document.getElementById('advancedFilters');
        if (!block) return;
        block.style.display = block.style.display === 'none' ? 'block' : 'none';
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
            const response = await fetch(`/api/folders/${folderId}/contents`);
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
                <a href="?folderId=${nodeData.id}&view=tree" class="tree-link folder-link">${nodeData.name}</a>
                ${nodeData.itemsCount ? '<span class="tree-count">(' + nodeData.itemsCount + ')</span>' : ''}
                <div class="tree-file-actions">
                    <button class="btn btn-tiny" onclick="renameFolder('${nodeData.id}', '${safeName}')" title="Переименовать">✏️</button>
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
            window.location.href = `/Files/Preview/${fileId}`;
        } catch (error) {
            console.error('Error opening file preview:', error);
            alert('Ошибка при открытии предпросмотра файла');
        }
    }

    async editFile(fileId) {
        try {
            const response = await fetch(`/api/files/${fileId}/edit`);
            const data = await response.json();

            if (data.hasActiveEditors && !data.canProceed) {
                alert('Файл сейчас редактируется другим пользователем. Попробуйте позже.');
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
            alert('Ошибка при открытии файла для редактирования');
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
            alert('Ошибка при скачивании файла');
        }
    }

    async deleteFile(fileId, fileName) {
        if (!confirm(`Вы уверены, что хотите удалить файл "${fileName}"?`)) {
            return;
        }

        // TODO: Implement file deletion
        console.log('Deleting file:', fileId);
        alert(`Удаление файла ${fileId} будет добавлено в следующем этапе`);
    }

    // Folder actions
    async createFolder(parentId) {
        const name = prompt('Введите название папки');
        if (!name) return;
        try {
            const response = await fetch('/api/folders', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name, parentId })
            });
            if (response.ok) {
                location.reload();
            } else {
                const text = await response.text();
                alert('Ошибка создания папки: ' + text);
            }
        } catch (error) {
            console.error('Error creating folder:', error);
        }
    }

    async renameFolder(folderId, currentName) {
        const newName = prompt('Новое имя папки', currentName);
        if (!newName) return;
        try {
            const response = await fetch(`/api/folders/${folderId}/rename`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name: newName })
            });
            if (response.ok) {
                location.reload();
            } else {
                const text = await response.text();
                alert('Ошибка переименования: ' + text);
            }
        } catch (error) {
            console.error('Error renaming folder:', error);
        }
    }

    async deleteFolder(folderId, folderName) {
        if (!confirm(`Удалить папку "${folderName}"?`)) return;
        try {
            const response = await fetch(`/api/folders/${folderId}`, { method: 'DELETE' });
            if (response.ok) {
                location.reload();
            } else {
                const text = await response.text();
                alert('Ошибка удаления: ' + text);
            }
        } catch (error) {
            console.error('Error deleting folder:', error);
        }
    }

    async moveFolder(folderId) {
        const newParentId = prompt('ID новой папки (оставьте пустым для корня)');
        if (newParentId === null) return;
        try {
            const response = await fetch(`/api/folders/${folderId}/move`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ newParentId: newParentId || null })
            });
            if (response.ok) {
                location.reload();
            } else {
                const text = await response.text();
                alert('Ошибка перемещения: ' + text);
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
            const response = await fetch('/api/access/grant', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });
            if (response.ok) {
                alert('Права назначены');
            } else {
                alert('Ошибка назначения прав');
            }
        } catch (error) {
            console.error('Error granting access:', error);
        }
    }

    // View switching
    changeView(viewMode) {
        const params = new URLSearchParams(window.location.search);
        params.set('view', viewMode);
        window.location.search = params.toString();
    }

    // Sorting
    sortBy(field) {
        const params = new URLSearchParams(window.location.search);
        const currentSort = params.get('SearchRequest.SortBy');
        const currentDirection = params.get('SearchRequest.SortDirection') || 'asc';

        // Toggle direction if same field
        const newDirection = (currentSort === field && currentDirection === 'asc') ? 'desc' : 'asc';

        params.set('SearchRequest.SortBy', field);
        params.set('SearchRequest.SortDirection', newDirection);
        params.set('SearchRequest.Page', '1'); // Reset to first page

        window.location.search = params.toString();
    }

    // Utility method for notifications
    showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;

        Object.assign(notification.style, {
            position: 'fixed',
            top: '20px',
            right: '20px',
            padding: '12px 16px',
            borderRadius: '4px',
            color: 'white',
            backgroundColor: type === 'success' ? '#28a745' : type === 'error' ? '#dc3545' : '#17a2b8',
            zIndex: '9999'
        });

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.remove();
        }, 5000);
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

function editFile(fileId) {
    if (filesManager) {
        filesManager.editFile(fileId);
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

function deleteFile(fileId, fileName) {
    if (filesManager) {
        filesManager.deleteFile(fileId, fileName);
    }
}

function createFolder(parentId) {
    if (filesManager) {
        filesManager.createFolder(parentId);
    }
}

function renameFolder(folderId, currentName) {
    if (filesManager) {
        filesManager.renameFolder(folderId, currentName);
    }
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

function changeView(viewMode) {
    if (filesManager) {
        filesManager.changeView(viewMode);
    }
}

function sortBy(field) {
    if (filesManager) {
        filesManager.sortBy(field);
    }
}

function toggleAdvanced() {
    if (filesManager) {
        filesManager.toggleAdvanced();
    }
}

function applyFilters() {
    if (filesManager) {
        filesManager.applyFilters();
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    filesManager = new FilesManager();
});


// Upload modal functions (will be loaded from _UploadModal.cshtml)
// These are just declarations to avoid errors
window.openUploadModal = window.openUploadModal || function (folderId) {
    console.log('Upload modal not loaded yet');
};

// Drag and drop for the main page
document.addEventListener('DOMContentLoaded', function () {
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
});


// Version management functions
window.viewVersions = function (fileId) {
    window.location.href = `/Files/${fileId}/Versions`;
};

// Add version button to file actions where appropriate
document.addEventListener('DOMContentLoaded', function () {
    // Add version history links to file context menus if they exist
    const fileItems = document.querySelectorAll('.file-card, .files-table tr');
    fileItems.forEach(item => {
        const fileId = item.getAttribute('data-file-id');
        if (fileId) {
            // Add context menu or additional actions as needed
            // This can be expanded based on UI requirements
        }
    });
});
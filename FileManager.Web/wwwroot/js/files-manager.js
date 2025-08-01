// Files Manager JavaScript functionality

class FilesManager {
    constructor() {
        this.currentFolderId = null;
        this.currentView = 'list';
        this.searchTimeout = null;
        this.init();
    }

    init() {
        this.bindEvents();
        this.loadInitialData();
    }

    bindEvents() {
        // Search functionality
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                clearTimeout(this.searchTimeout);
                this.searchTimeout = setTimeout(() => {
                    this.performSearch(e.target.value);
                }, 300);
            });

            searchInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    this.performSearch(e.target.value);
                }
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

    loadInitialData() {
        // Load based on current URL parameters
        const params = new URLSearchParams(window.location.search);
        this.currentFolderId = params.get('folderId') || null;
        this.currentView = params.get('view') || 'list';
    }

    async performSearch(searchTerm) {
        const params = this.buildSearchParams();
        params.SearchTerm = searchTerm;

        try {
            const response = await fetch(`/api/files/search?${new URLSearchParams(params)}`);
            if (response.ok) {
                const data = await response.json();
                this.updateFilesList(data);
            }
        } catch (error) {
            console.error('Search error:', error);
        }
    }

    applyFilters() {
        const params = this.buildSearchParams();

        const filterType = document.getElementById('filterType');
        if (filterType && filterType.value) {
            params.FileType = filterType.value;
        }

        const onlyMyFiles = document.getElementById('onlyMyFiles');
        if (onlyMyFiles && onlyMyFiles.checked) {
            params.OnlyMyFiles = true;
        }

        // Update URL and reload
        const newUrl = `${window.location.pathname}?${new URLSearchParams(params)}`;
        window.location.href = newUrl;
    }

    buildSearchParams() {
        const params = new URLSearchParams(window.location.search);
        return {
            SearchTerm: params.get('SearchRequest.SearchTerm') || '',
            FolderId: this.currentFolderId,
            FileType: params.get('SearchRequest.FileType') || '',
            OnlyMyFiles: params.get('SearchRequest.OnlyMyFiles') === 'true',
            SortBy: params.get('SearchRequest.SortBy') || 'name',
            SortDirection: params.get('SearchRequest.SortDirection') || 'asc',
            Page: 1
        };
    }

    updateFilesList(data) {
        // This would update the files list via AJAX
        // For now, we'll just reload the page
        console.log('Files data received:', data);
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
            content.innerHTML = `
                ${nodeData.hasChildren ? '<span class="tree-toggle" onclick="filesManager.toggleTreeNode(\'' + nodeData.id + '\')">▶</span>' : '<span class="tree-spacer"></span>'}
                <span class="tree-icon">${nodeData.icon}</span>
                <a href="?folderId=${nodeData.id}&view=tree" class="tree-link folder-link">${nodeData.name}</a>
                ${nodeData.itemsCount ? '<span class="tree-count">(' + nodeData.itemsCount + ')</span>' : ''}
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
                <span class="tree-link file-link" onclick="filesManager.viewFile('${nodeData.id}')">${nodeData.name}</span>
                ${nodeData.sizeBytes ? '<span class="tree-size">' + this.formatFileSize(nodeData.sizeBytes) + '</span>' : ''}
                <div class="tree-file-actions">
                    <button class="btn btn-tiny" onclick="filesManager.downloadFile('${nodeData.id}')" title="Скачать">⬇️</button>
                    <button class="btn btn-tiny" onclick="filesManager.deleteFile('${nodeData.id}', '${nodeData.name}')" title="Удалить">🗑️</button>
                </div>
                <span class="tree-date">${this.formatDate(nodeData.updatedAt || nodeData.createdAt)}</span>
            `;
        }

        div.appendChild(content);
        return div;
    }

    // File actions
    async viewFile(fileId) {
        // TODO: Implement file viewing
        console.log('Viewing file:', fileId);
        alert(`Просмотр файла ${fileId} будет добавлен в следующем этапе`);
    }

    async downloadFile(fileId) {
        // TODO: Implement file download
        console.log('Downloading file:', fileId);
        alert(`Скачивание файла ${fileId} будет добавлено в следующем этапе`);
    }

    async deleteFile(fileId, fileName) {
        if (!confirm(`Вы уверены, что хотите удалить файл "${fileName}"?`)) {
            return;
        }

        // TODO: Implement file deletion
        console.log('Deleting file:', fileId);
        alert(`Удаление файла ${fileId} будет добавлено в следующем этапе`);
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

function downloadFile(fileId) {
    if (filesManager) {
        filesManager.downloadFile(fileId);
    }
}

function deleteFile(fileId, fileName) {
    if (filesManager) {
        filesManager.deleteFile(fileId, fileName);
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

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    filesManager = new FilesManager();
});
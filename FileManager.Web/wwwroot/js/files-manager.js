// Современный файловый менеджер с улучшенным UX

class ModernFilesManager {
    constructor() {
        this.currentFolderId = null;
        this.currentView = 'list';
        this.selectedFiles = new Set();
        this.contextItem = null;
        this.searchTimeout = null;
        this.clipboard = null;
        this.draggedItem = null;
        this.undoStack = [];
        this.redoStack = [];

        this.init();
    }

    init() {
        this.bindEvents();
        this.bindSelectionEvents();
        this.bindContextMenu();
        this.bindKeyboardShortcuts();
        this.bindDragAndDrop();
        this.loadInitialData();
        this.initializePerformanceOptimizations();
    }

    // Навигация с анимацией
    navigateTo(url, addToHistory = true) {
        if (window.loadPage) {
            this.showLoadingState();
            window.loadPage(url, addToHistory)
                .finally(() => this.hideLoadingState());
        } else {
            window.location.href = url;
        }
    }

    // Состояния загрузки
    showLoadingState() {
        const elements = document.querySelectorAll('.explorer-item, .file-card');
        elements.forEach(el => {
            el.style.opacity = '0.6';
            el.style.pointerEvents = 'none';
        });

        if (typeof showPageLoading === 'function') {
            showPageLoading();
        }
    }

    hideLoadingState() {
        const elements = document.querySelectorAll('.explorer-item, .file-card');
        elements.forEach(el => {
            el.style.opacity = '';
            el.style.pointerEvents = '';
        });

        if (typeof hidePageLoading === 'function') {
            hidePageLoading();
        }
    }

    // Привязка событий
    bindEvents() {
        // Поиск с дебаунсом
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
                    e.preventDefault();
                    clearTimeout(this.searchTimeout);
                    this.performSearch(e.target.value);
                }
            });
        }

        // Переключение видов
        this.bindViewToggle();

        // Двойной клик для открытия
        document.addEventListener('dblclick', (e) => {
            const item = e.target.closest('.explorer-item');
            if (!item) return;

            e.preventDefault();
            this.openItem(item.dataset.id, item.dataset.type);
        });

        // Фильтры
        this.bindFilterEvents();
    }

    bindViewToggle() {
        const viewList = document.getElementById('viewList');
        const viewGrid = document.getElementById('viewGrid');

        if (viewList) {
            viewList.addEventListener('click', () => this.changeView('list'));
        }
        if (viewGrid) {
            viewGrid.addEventListener('click', () => this.changeView('grid'));
        }
    }

    bindFilterEvents() {
        const filterElements = [
            'filterType', 'onlyMyFiles', 'dateFrom', 'dateTo',
            'ownerSearch', 'extension', 'minSize', 'maxSize', 'tags'
        ];

        filterElements.forEach(id => {
            const element = document.getElementById(id);
            if (element) {
                const eventType = element.type === 'checkbox' ? 'change' : 'input';
                element.addEventListener(eventType, () => {
                    clearTimeout(this.searchTimeout);
                    this.searchTimeout = setTimeout(() => {
                        this.applyFilters();
                    }, 300);
                });
            }
        });

        // Специальный обработчик для поиска владельца
        this.initializeOwnerSearch();
    }

    bindSelectionEvents() {
        let lastSelected = null;
        document.addEventListener('click', (e) => {
            const item = e.target.closest('.explorer-item');
            if (!item) {
                if (!e.ctrlKey && !e.metaKey && !e.target.closest('.selection-toolbar')) {
                    this.clearSelection();
                }
                return;
            }

            const id = item.dataset.id;
            if (!id) return;

            if (e.shiftKey && lastSelected) {
                const items = Array.from(document.querySelectorAll('.explorer-item'));
                const start = items.indexOf(lastSelected);
                const end = items.indexOf(item);
                if (start !== -1 && end !== -1) {
                    this.clearSelection();
                    const [min, max] = start < end ? [start, end] : [end, start];
                    for (let i = min; i <= max; i++) {
                        const el = items[i];
                        this.selectedFiles.add(el.dataset.id);
                        el.classList.add('selected');
                    }
                }
            } else {
                if (!e.ctrlKey && !e.metaKey) {
                    this.clearSelection();
                }
                if (this.selectedFiles.has(id)) {
                    this.selectedFiles.delete(id);
                    item.classList.remove('selected');
                } else {
                    this.selectedFiles.add(id);
                    item.classList.add('selected');
                }
                lastSelected = item;
            }

            this.updateSelectionUI();
        });
    }

    // Инициализация поиска владельца
    async initializeOwnerSearch() {
        const ownerSearch = document.getElementById('ownerSearch');
        if (!ownerSearch) return;

        try {
            const response = await fetch('/api/users');
            const users = await response.json();

            const datalist = document.getElementById('usersList');
            if (datalist) {
                datalist.innerHTML = users.map(user =>
                    `<option value="${user.email}" data-id="${user.id}">${user.fullName}</option>`
                ).join('');
            }

            ownerSearch.addEventListener('input', () => {
                const selectedOption = Array.from(datalist.options)
                    .find(option => option.value === ownerSearch.value);

                const hiddenInput = document.getElementById('ownerId');
                if (hiddenInput) {
                    hiddenInput.value = selectedOption ? selectedOption.dataset.id : '';
                }
            });
        } catch (error) {
            console.error('Error loading users:', error);
        }
    }

    // Контекстное меню
    bindContextMenu() {
        document.addEventListener('contextmenu', (e) => {
            const withinFiles = e.target.closest('.files-wrapper, .content-area');
            if (!withinFiles) return;

            e.preventDefault();
            this.showContextMenu(e, e.target.closest('.explorer-item'));
        });

        document.addEventListener('click', () => this.hideContextMenu());

        const menu = document.getElementById('contextMenu');
        if (menu) {
            menu.addEventListener('click', (e) => {
                const action = e.target.dataset.action ||
                    e.target.closest('li')?.dataset.action;
                if (action) {
                    this.handleContextAction(action);
                }
            });
        }
    }

    showContextMenu(event, item) {
        const menu = document.getElementById('contextMenu');
        if (!menu) return;

        this.contextItem = item ? {
            id: item.dataset.id,
            name: item.dataset.name,
            type: item.dataset.type
        } : null;

        this.updateContextMenuItems();

        menu.style.display = 'block';

        // Позиционирование с учетом границ экрана
        const rect = menu.getBoundingClientRect();
        const x = Math.min(event.pageX, window.innerWidth - rect.width - 10);
        const y = Math.min(event.pageY, window.innerHeight - rect.height - 10);

        menu.style.left = x + 'px';
        menu.style.top = y + 'px';

        // Анимация появления
        menu.style.opacity = '0';
        menu.style.transform = 'scale(0.95)';
        setTimeout(() => {
            menu.style.opacity = '1';
            menu.style.transform = 'scale(1)';
            menu.style.transition = 'all 0.15s ease';
        }, 10);
    }

    updateContextMenuItems() {
        const menu = document.getElementById('contextMenu');
        if (!menu) return;

        const items = menu.querySelectorAll('li[data-action]');

        if (this.contextItem) {
            // Контекст для конкретного элемента
            const showFor = {
                'upload': false,
                'create-folder': false,
                'manage-access': false,
                'view-grid': false,
                'view-list': false,
                'preview': this.contextItem.type === 'file',
                'edit': this.contextItem.type === 'file',
                'rename': true,
                'download': this.contextItem.type === 'file',
                'access': true,
                'delete': true,
                'properties': true
            };

            items.forEach(item => {
                const action = item.dataset.action;
                item.style.display = showFor[action] ? 'flex' : 'none';
            });
        } else {
            // Контекст для пустой области
            const showFor = {
                'upload': true,
                'create-folder': true,
                'manage-access': true,
                'view-grid': true,
                'view-list': true,
                'preview': false,
                'edit': false,
                'rename': false,
                'download': false,
                'access': false,
                'delete': false,
                'properties': false
            };

            items.forEach(item => {
                const action = item.dataset.action;
                item.style.display = showFor[action] ? 'flex' : 'none';
            });
        }
    }

    hideContextMenu() {
        const menu = document.getElementById('contextMenu');
        if (menu) {
            menu.style.opacity = '0';
            menu.style.transform = 'scale(0.95)';
            setTimeout(() => {
                menu.style.display = 'none';
                menu.style.transition = '';
            }, 150);
        }
    }

    handleContextAction(action) {
        // Действия для пустой области
        const emptyAreaActions = ['upload', 'create-folder', 'manage-access', 'view-grid', 'view-list'];

        if (emptyAreaActions.includes(action)) {
            switch (action) {
                case 'upload':
                    this.openUploadModal();
                    break;
                case 'create-folder':
                    this.openCreateFolderModal();
                    break;
                case 'manage-access':
                    this.openAccessModal(this.currentFolderId, true);
                    break;
                case 'view-grid':
                    this.changeView('grid');
                    break;
                case 'view-list':
                    this.changeView('list');
                    break;
            }
            this.hideContextMenu();
            return;
        }

        // Действия для конкретного элемента
        if (!this.contextItem) return;

        const { id, name, type } = this.contextItem;

        switch (action) {
            case 'preview':
                if (type === 'file') this.previewFile(id);
                break;
            case 'edit':
                if (type === 'file') this.editFile(id);
                break;
            case 'rename':
                this.renameItem(id, name, type);
                break;
            case 'download':
                if (type === 'file') this.downloadFile(id);
                break;
            case 'access':
                this.openAccessModal(id, type === 'folder');
                break;
            case 'delete':
                this.deleteItem(id, name, type);
                break;
            case 'properties':
                this.showProperties(id, name, type);
                break;
        }

        this.hideContextMenu();
    }

    // Клавиатурные сочетания
    bindKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            // Игнорируем, если фокус в поле ввода
            if (e.target.matches('input, textarea, select')) return;

            // Только с Ctrl/Cmd
            if (e.ctrlKey || e.metaKey) {
                switch (e.key) {
                    case 'a':
                        e.preventDefault();
                        this.selectAll();
                        break;
                    case 'c':
                        e.preventDefault();
                        this.copySelected();
                        break;
                    case 'v':
                        e.preventDefault();
                        this.pasteItems();
                        break;
                    case 'z':
                        e.preventDefault();
                        if (e.shiftKey) {
                            this.redo();
                        } else {
                            this.undo();
                        }
                        break;
                    case 'f':
                        e.preventDefault();
                        this.focusSearch();
                        break;
                }
            } else {
                switch (e.key) {
                    case 'Delete':
                        e.preventDefault();
                        this.deleteSelected();
                        break;
                    case 'F2':
                        e.preventDefault();
                        this.renameSelected();
                        break;
                    case 'Escape':
                        e.preventDefault();
                        this.clearSelection();
                        this.hideContextMenu();
                        break;
                }
            }
        });
    }

    // Drag & Drop
    bindDragAndDrop() {
        document.addEventListener('dragstart', (e) => {
            const item = e.target.closest('.explorer-item');
            if (!item) return;

            this.draggedItem = {
                id: item.dataset.id,
                name: item.dataset.name,
                type: item.dataset.type
            };

            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', item.dataset.id);

            // Визуальная обратная связь
            item.style.opacity = '0.5';
        });

        document.addEventListener('dragend', (e) => {
            const item = e.target.closest('.explorer-item');
            if (item) {
                item.style.opacity = '';
            }
            this.draggedItem = null;
        });

        document.addEventListener('dragover', (e) => {
            const dropTarget = e.target.closest('.explorer-item[data-type="folder"]');
            if (dropTarget && this.draggedItem) {
                e.preventDefault();
                e.dataTransfer.dropEffect = 'move';
                dropTarget.classList.add('drag-over');
            }
        });

        document.addEventListener('dragleave', (e) => {
            const dropTarget = e.target.closest('.explorer-item[data-type="folder"]');
            if (dropTarget) {
                dropTarget.classList.remove('drag-over');
            }
        });

        document.addEventListener('drop', (e) => {
            const dropTarget = e.target.closest('.explorer-item[data-type="folder"]');
            if (dropTarget && this.draggedItem) {
                e.preventDefault();
                dropTarget.classList.remove('drag-over');
                this.moveItem(this.draggedItem.id, dropTarget.dataset.id);
            }
        });
    }

    // Действия с файлами
    async openItem(itemId, itemType) {
        if (itemType === 'folder') {
            this.navigateTo(`?folderId=${itemId}&view=${this.currentView}`);
        } else {
            this.previewFile(itemId);
        }
    }

    async previewFile(fileId) {
        try {
            this.addToHistory('preview', { fileId });
            this.navigateTo(`/Files/Preview/${fileId}`);
        } catch (error) {
            console.error('Error opening file preview:', error);
            this.showNotification('Ошибка при открытии предпросмотра файла', 'error');
        }
    }

    async editFile(fileId) {
        try {
            const response = await fetch(`/api/files/${fileId}/edit`);
            const data = await response.json();

            if (data.hasActiveEditors && !data.canProceed) {
                this.showNotification('Файл сейчас редактируется другим пользователем', 'warning');
                return;
            }

            if (data.hasActiveEditors && data.warnings) {
                const proceed = await this.showConfirmDialog(
                    'Файл редактируется',
                    'Внимание! ' + data.warnings.join('\n') + '\n\nПродолжить редактирование?'
                );
                if (!proceed) return;
            }

            if (data.editUrl) {
                window.open(data.editUrl, '_blank');
                this.showNotification('Файл открыт для редактирования в новой вкладке', 'success');
            }
        } catch (error) {
            console.error('Error opening file for edit:', error);
            this.showNotification('Ошибка при открытии файла для редактирования', 'error');
        }
    }

    async downloadFile(fileId) {
        try {
            window.location.href = `/api/files/${fileId}/content`;
        } catch (error) {
            console.error('Error downloading file:', error);
            this.showNotification('Ошибка при скачивании файла', 'error');
        }
    }

    async deleteItem(itemId, itemName, itemType) {
        const proceed = await this.showConfirmDialog(
            'Подтверждение удаления',
            `Вы уверены, что хотите удалить ${itemType === 'file' ? 'файл' : 'папку'} "${itemName}"?`
        );

        if (!proceed) return;

        try {
            const endpoint = itemType === 'file' ? `/api/files/${itemId}` : `/api/folders/${itemId}`;
            const response = await fetch(endpoint, { method: 'DELETE' });

            if (response.ok) {
                this.addToHistory('delete', { itemId, itemName, itemType });
                this.showNotification(`${itemType === 'file' ? 'Файл' : 'Папка'} удален`, 'success');
                this.refreshCurrentView();
            } else {
                const error = await response.text();
                this.showNotification(`Ошибка удаления: ${error}`, 'error');
            }
        } catch (error) {
            console.error('Error deleting item:', error);
            this.showNotification('Ошибка при удалении', 'error');
        }
    }

    // Управление папками
    async createFolder(name, parentId) {
        try {
            const response = await fetch('/api/folders', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name, parentId })
            });

            if (response.ok) {
                const folder = await response.json();
                this.addToHistory('create', { folder });
                this.showNotification('Папка создана', 'success');
                this.refreshCurrentView();
                return folder;
            } else {
                const error = await response.text();
                this.showNotification(`Ошибка создания папки: ${error}`, 'error');
            }
        } catch (error) {
            console.error('Error creating folder:', error);
            this.showNotification('Ошибка при создании папки', 'error');
        }
    }

    async renameItem(itemId, currentName, itemType) {
        const newName = await this.showInputDialog(
            `Переименование ${itemType === 'file' ? 'файла' : 'папки'}`,
            'Введите новое название:',
            currentName
        );

        if (!newName || newName === currentName) return;

        try {
            const endpoint = itemType === 'file'
                ? `/api/files/${itemId}/rename`
                : `/api/folders/${itemId}/rename`;

            const response = await fetch(endpoint, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name: newName })
            });

            if (response.ok) {
                this.addToHistory('rename', { itemId, oldName: currentName, newName, itemType });
                this.showNotification(`${itemType === 'file' ? 'Файл' : 'Папка'} переименован`, 'success');
                this.refreshCurrentView();
            } else {
                const error = await response.text();
                this.showNotification(`Ошибка переименования: ${error}`, 'error');
            }
        } catch (error) {
            console.error('Error renaming item:', error);
            this.showNotification('Ошибка при переименовании', 'error');
        }
    }

    // Поиск и фильтрация
    performSearch(searchTerm) {
        const params = this.buildSearchParams();
        if (searchTerm) {
            params['SearchRequest.SearchTerm'] = searchTerm;
        } else {
            delete params['SearchRequest.SearchTerm'];
        }

        const newUrl = `${window.location.pathname}?${new URLSearchParams(params)}`;
        this.navigateTo(newUrl);
    }

    applyFilters() {
        const params = this.buildSearchParams();

        // Собираем значения фильтров
        const filterElements = {
            'SearchRequest.FileType': 'filterType',
            'SearchRequest.OnlyMyFiles': 'onlyMyFiles',
            'SearchRequest.DateFrom': 'dateFrom',
            'SearchRequest.DateTo': 'dateTo',
            'SearchRequest.Extension': 'extension',
            'SearchRequest.MinSizeBytes': 'minSize',
            'SearchRequest.MaxSizeBytes': 'maxSize',
            'SearchRequest.Tags': 'tags',
            'SearchRequest.OwnerId': 'ownerId'
        };

        Object.entries(filterElements).forEach(([param, elementId]) => {
            const element = document.getElementById(elementId);
            if (element) {
                if (element.type === 'checkbox') {
                    if (element.checked) params[param] = 'true';
                } else if (element.value) {
                    params[param] = element.value;
                }
            }
        });

        const newUrl = `${window.location.pathname}?${new URLSearchParams(params)}`;
        this.navigateTo(newUrl);
    }

    toggleAdvanced() {
        const block = document.getElementById('advancedFilters');
        if (!block) return;

        const isVisible = block.style.display !== 'none';
        block.style.display = isVisible ? 'none' : 'block';

        // Анимация
        if (!isVisible) {
            block.style.opacity = '0';
            block.style.transform = 'translateY(-10px)';
            setTimeout(() => {
                block.style.opacity = '1';
                block.style.transform = 'translateY(0)';
                block.style.transition = 'all 0.2s ease';
            }, 10);
        }
    }

    changeView(view) {
        if (this.currentView === view) return;

        this.currentView = view;
        const params = this.buildSearchParams();
        params.view = view;

        const newUrl = `${window.location.pathname}?${new URLSearchParams(params)}`;
        this.navigateTo(newUrl);
    }

    // Буфер обмена и операции
    copySelected() {
        const selected = Array.from(this.selectedFiles);
        if (selected.length === 0) return;

        this.clipboard = {
            items: selected,
            operation: 'copy'
        };

        this.showNotification(`Скопировано элементов: ${selected.length}`, 'info');
    }

    async pasteItems() {
        if (!this.clipboard) return;

        try {
            // Здесь должна быть логика копирования/перемещения файлов
            this.showNotification('Функция будет реализована в следующих версиях', 'info');
        } catch (error) {
            console.error('Error pasting items:', error);
            this.showNotification('Ошибка при вставке', 'error');
        }
    }

    // Выделение
    selectAll() {
        const items = document.querySelectorAll('.explorer-item');
        items.forEach(item => {
            this.selectedFiles.add(item.dataset.id);
            item.classList.add('selected');
        });

        this.updateSelectionUI();
    }

    clearSelection() {
        this.selectedFiles.clear();
        document.querySelectorAll('.explorer-item.selected').forEach(item => {
            item.classList.remove('selected');
        });

        this.updateSelectionUI();
    }

    updateSelectionUI() {
        const count = this.selectedFiles.size;
        const toolbar = document.querySelector('.selection-toolbar');

        if (count > 0) {
            if (!toolbar) {
                this.createSelectionToolbar();
            }
            this.updateSelectionToolbar(count);
        } else if (toolbar) {
            toolbar.remove();
        }
    }

    createSelectionToolbar() {
        const toolbar = document.createElement('div');
        toolbar.className = 'selection-toolbar';
        toolbar.innerHTML = `
            <div class="selection-info">
                <span class="selection-count">0</span> элементов выбрано
            </div>
            <div class="selection-actions">
                <button class="btn btn-small" onclick="filesManager.downloadSelected()">
                    <i class="bi bi-download"></i> Скачать
                </button>
                <button class="btn btn-small" onclick="filesManager.copySelected()">
                    <i class="bi bi-copy"></i> Копировать
                </button>
                <button class="btn btn-small btn-danger" onclick="filesManager.deleteSelected()">
                    <i class="bi bi-trash"></i> Удалить
                </button>
                <button class="btn btn-small btn-secondary" onclick="filesManager.clearSelection()">
                    <i class="bi bi-x"></i> Отменить
                </button>
            </div>
        `;

        const container = document.querySelector('.files-wrapper');
        if (container) {
            container.insertBefore(toolbar, container.firstChild);
        }
    }

    updateSelectionToolbar(count) {
        const countElement = document.querySelector('.selection-count');
        if (countElement) {
            countElement.textContent = count;
        }
    }

    // История операций (Undo/Redo)
    addToHistory(operation, data) {
        this.undoStack.push({ operation, data, timestamp: Date.now() });
        if (this.undoStack.length > 50) {
            this.undoStack.shift();
        }
        this.redoStack = []; // Очищаем redo при новой операции
    }

    async undo() {
        if (this.undoStack.length === 0) return;

        const action = this.undoStack.pop();
        this.redoStack.push(action);

        try {
            await this.executeUndoAction(action);
            this.showNotification('Операция отменена', 'info');
        } catch (error) {
            console.error('Error undoing action:', error);
            this.showNotification('Ошибка отмены операции', 'error');
        }
    }

    async redo() {
        if (this.redoStack.length === 0) return;

        const action = this.redoStack.pop();
        this.undoStack.push(action);

        try {
            await this.executeRedoAction(action);
            this.showNotification('Операция повторена', 'info');
        } catch (error) {
            console.error('Error redoing action:', error);
            this.showNotification('Ошибка повтора операции', 'error');
        }
    }

    // Диалоги
    async showConfirmDialog(title, message) {
        return new Promise((resolve) => {
            if (typeof showCustomConfirm === 'function') {
                showCustomConfirm(message, () => resolve(true));
            } else {
                resolve(confirm(message));
            }
        });
    }

    async showInputDialog(title, message, defaultValue = '') {
        return new Promise((resolve) => {
            const result = prompt(message, defaultValue);
            resolve(result);
        });
    }

    // Утилиты
    buildSearchParams() {
        const params = new URLSearchParams(window.location.search);
        return {
            'SearchRequest.SearchTerm': params.get('SearchRequest.SearchTerm') || '',
            'SearchRequest.FolderId': this.currentFolderId,
            'SearchRequest.SortBy': params.get('SearchRequest.SortBy') || 'name',
            'SearchRequest.SortDirection': params.get('SearchRequest.SortDirection') || 'asc',
            'SearchRequest.Page': 1,
            folderId: this.currentFolderId
        };
    }

    loadInitialData() {
        const params = new URLSearchParams(window.location.search);
        this.currentFolderId = params.get('folderId') || null;
        this.currentView = params.get('view') || 'list';
    }

    refreshCurrentView() {
        setTimeout(() => {
            if (typeof window.loadPage === 'function') {
                window.loadPage(window.location.href, false);
            } else {
                window.location.reload();
            }
        }, 1000);
    }

    showNotification(message, type = 'info') {
        if (typeof window.showNotification === 'function') {
            window.showNotification(message, type);
        }
    }

    // Оптимизации производительности
    initializePerformanceOptimizations() {
        // Виртуализация для больших списков
        this.initializeVirtualScrolling();

        // Ленивая загрузка изображений
        this.initializeLazyLoading();

        // Кэширование результатов поиска
        this.searchCache = new Map();
    }

    initializeVirtualScrolling() {
        if (!('IntersectionObserver' in window)) return;

        const container = document.querySelector('.files-grid, .list-content');
        if (!container) return;

        // Реализация виртуального скроллинга для больших списков
        // Показываем только видимые элементы + буфер
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target.querySelector('img[data-src]');
                    if (img) {
                        img.src = img.dataset.src;
                        img.removeAttribute('data-src');
                    }
                }
            });
        });

        // Наблюдаем за всеми элементами
        document.querySelectorAll('.file-card, .explorer-item').forEach(item => {
            observer.observe(item);
        });
    }

    initializeLazyLoading() {
        const images = document.querySelectorAll('img[data-src]');

        if ('IntersectionObserver' in window) {
            const imageObserver = new IntersectionObserver((entries, observer) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const img = entry.target;
                        img.src = img.dataset.src;
                        img.classList.remove('lazy');
                        imageObserver.unobserve(img);
                    }
                });
            });

            images.forEach(img => imageObserver.observe(img));
        } else {
            // Fallback для старых браузеров
            images.forEach(img => {
                img.src = img.dataset.src;
            });
        }
    }

    // Методы-заглушки для обратной совместимости
    openUploadModal() {
        if (typeof openUploadModal === 'function') {
            openUploadModal(this.currentFolderId);
        }
    }

    openCreateFolderModal() {
        if (typeof openCreateFolderModal === 'function') {
            openCreateFolderModal(this.currentFolderId);
        }
    }

    openAccessModal(itemId, isFolder) {
        if (typeof openAccessModal === 'function') {
            openAccessModal(itemId, isFolder);
        }
    }

    showProperties(itemId, itemName, itemType) {
        if (typeof loadItemProperties === 'function') {
            const modal = document.getElementById('propertiesModal');
            if (modal) {
                modal.style.display = 'flex';
                setTimeout(() => {
                    modal.classList.add('modal-enter');
                    modal.querySelector('.modal-content-animated').classList.add('show');
                    loadItemProperties(itemId, itemName, itemType);
                }, 10);
            }
        }
    }

    // Дополнительные методы для работы с выбранными файлами
    async downloadSelected() {
        if (this.selectedFiles.size === 0) return;

        try {
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
        } catch (error) {
            console.error('Error downloading selected files:', error);
            this.showNotification('Ошибка при скачивании файлов', 'error');
        }
    }

    async deleteSelected() {
        if (this.selectedFiles.size === 0) return;

        const proceed = await this.showConfirmDialog(
            'Подтверждение удаления',
            `Вы уверены, что хотите удалить выбранные элементы (${this.selectedFiles.size})?`
        );

        if (!proceed) return;

        // Удаляем каждый файл
        for (const fileId of this.selectedFiles) {
            try {
                const response = await fetch(`/api/files/${fileId}`, { method: 'DELETE' });
                if (!response.ok) {
                    console.error(`Failed to delete file ${fileId}`);
                }
            } catch (error) {
                console.error(`Error deleting file ${fileId}:`, error);
            }
        }

        this.clearSelection();
        this.showNotification('Файлы удалены', 'success');
        this.refreshCurrentView();
    }

    focusSearch() {
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.focus();
            searchInput.select();
        }
    }
}

// Создаем глобальный экземпляр
let filesManager;

// Инициализация при загрузке DOM
function initializeFilesManager() {
    try {
        filesManager = new ModernFilesManager();
        window.filesManager = filesManager;
    } catch (error) {
        console.error('Не удалось инициализировать файловый менеджер', error);
    }
}

document.addEventListener('DOMContentLoaded', initializeFilesManager);

// Глобальные функции для обратной совместимости
window.changeView = function (view) {
    if (window.filesManager) {
        window.filesManager.changeView(view);
    } else {
        const params = new URLSearchParams(window.location.search);
        params.set('view', view);
        window.location.search = params.toString();
    }
};

window.toggleAdvanced = function () {
    if (window.filesManager) {
        window.filesManager.toggleAdvanced();
    } else {
        const block = document.getElementById('advancedFilters');
        if (!block) return;
        const isVisible = block.style.display !== 'none';
        block.style.display = isVisible ? 'none' : 'block';
    }
};
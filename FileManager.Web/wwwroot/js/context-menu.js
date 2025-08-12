window.contextMenu = (function () {
    function init(filesManager) {
        document.addEventListener('contextmenu', (e) => {
            const within = e.target.closest('.files-wrapper');
            if (!within) return;
            e.preventDefault();
            const menu = document.getElementById('contextMenu');
            if (!menu) return;
            const item = e.target.closest('.explorer-item');
            if (item) {
                filesManager.contextItem = {
                    id: item.dataset.id,
                    name: item.dataset.name,
                    type: item.dataset.type
                };
                menu.querySelector('[data-action="upload"]').style.display = 'none';
                menu.querySelector('[data-action="create-folder"]').style.display = 'none';
                menu.querySelector('[data-action="manage-access"]').style.display = 'none';
                menu.querySelector('[data-action="rename"]').style.display = filesManager.contextItem.type === 'folder' ? 'flex' : 'none';
                menu.querySelector('[data-action="download"]').style.display = filesManager.contextItem.type === 'file' ? 'flex' : 'none';
                menu.querySelector('[data-action="preview"]').style.display = filesManager.contextItem.type === 'file' ? 'flex' : 'none';
                menu.querySelector('[data-action="edit"]').style.display = filesManager.contextItem.type === 'file' ? 'flex' : 'none';
                menu.querySelector('[data-action="access"]').style.display = 'flex';
                menu.querySelector('[data-action="add-favorite"]').style.display = 'flex';
                menu.querySelector('[data-action="delete"]').style.display = 'flex';
                menu.querySelector('[data-action="properties"]').style.display = 'flex';
            } else {
                filesManager.contextItem = null;
                menu.querySelector('[data-action="upload"]').style.display = 'flex';
                menu.querySelector('[data-action="create-folder"]').style.display = 'flex';
                menu.querySelector('[data-action="manage-access"]').style.display = 'flex';
                menu.querySelector('[data-action="rename"]').style.display = 'none';
                menu.querySelector('[data-action="download"]').style.display = 'none';
                menu.querySelector('[data-action="preview"]').style.display = 'none';
                menu.querySelector('[data-action="edit"]').style.display = 'none';
                menu.querySelector('[data-action="access"]').style.display = 'none';
                menu.querySelector('[data-action="add-favorite"]').style.display = 'none';
                menu.querySelector('[data-action="delete"]').style.display = 'none';
                menu.querySelector('[data-action="properties"]').style.display = 'none';
            }
            menu.style.display = 'block';
            menu.style.left = e.pageX + 'px';
            menu.style.top = e.pageY + 'px';
        });

        document.addEventListener('click', hideContextMenu);

        const menu = document.getElementById('contextMenu');
        if (menu) {
            menu.addEventListener('click', (e) => {
                const action = e.target.dataset.action || e.target.closest('li')?.dataset.action;
                if (action) {
                    handleContextAction(action, filesManager);
                }
            });
        }
    }

    function hideContextMenu() {
        const menu = document.getElementById('contextMenu');
        if (menu) menu.style.display = 'none';
    }

    function handleContextAction(action, filesManager) {
        if (['upload', 'create-folder', 'manage-access'].includes(action)) {
            switch (action) {
                case 'upload':
                    openUploadModal(filesManager.currentFolderId);
                    break;
                case 'create-folder':
                    openCreateFolderModal(filesManager.currentFolderId);
                    break;
                case 'manage-access':
                    openAccessModal(filesManager.currentFolderId, true);
                    break;
            }
            hideContextMenu();
            return;
        }
        if (!filesManager.contextItem) return;
        const { id, name, type } = filesManager.contextItem;
        switch (action) {
            case 'preview':
                if (type === 'file') {
                    filesManager.previewFile(id);
                }
                break;
            case 'edit':
                if (type === 'file') {
                    filesManager.editFile(id, name);
                }
                break;
            case 'rename':
                if (type === 'folder') {
                    openRenameFolderModal(id, name);
                }
                break;
            case 'download':
                if (type === 'file') {
                    filesManager.downloadFile(id);
                }
                break;
            case 'access':
                openAccessModal(id, type === 'folder');
                break;
            case 'add-favorite':
                filesManager.addFavorite(id, type);
                break;
            case 'delete':
                if (type === 'file') {
                    filesManager.deleteFile(id, name);
                } else {
                    deleteFolder(id, name);
                }
                break;
            case 'properties':
                filesManager.showProperties(id, name, type);
                break;
        }
        hideContextMenu();
    }

    return { init };
})();

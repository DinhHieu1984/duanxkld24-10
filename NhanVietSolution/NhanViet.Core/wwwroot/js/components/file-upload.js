/**
 * Advanced File Upload Component
 * Supports drag & drop, multiple files, progress tracking, and validation
 */
class FileUploadComponent {
    constructor(element, options = {}) {
        this.element = element;
        this.options = {
            maxFileSize: 10 * 1024 * 1024, // 10MB
            allowedTypes: ['application/pdf', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'],
            maxFiles: 5,
            uploadUrl: '/api/upload',
            multiple: true,
            dragAndDrop: true,
            showPreview: true,
            autoUpload: false,
            ...options
        };

        this.files = [];
        this.uploadedFiles = [];
        this.isUploading = false;

        this.init();
    }

    init() {
        this.createUploadArea();
        this.bindEvents();
    }

    createUploadArea() {
        const uploadHtml = `
            <div class="file-upload-container">
                <div class="file-upload-area ${this.options.dragAndDrop ? 'drag-drop-enabled' : ''}" id="upload-area-${this.generateId()}">
                    <div class="upload-icon">📁</div>
                    <div class="upload-text">
                        <p class="upload-main-text">
                            ${this.options.dragAndDrop ? 'Drag & drop files here or' : ''} 
                            <button type="button" class="btn btn-link upload-browse-btn">browse files</button>
                        </p>
                        <p class="upload-sub-text">
                            Supported formats: ${this.getAllowedTypesText()}
                            <br>Maximum file size: ${this.formatFileSize(this.options.maxFileSize)}
                            ${this.options.maxFiles > 1 ? `<br>Maximum ${this.options.maxFiles} files` : ''}
                        </p>
                    </div>
                    <input type="file" class="file-input" 
                           ${this.options.multiple ? 'multiple' : ''} 
                           accept="${this.options.allowedTypes.join(',')}" 
                           style="display: none;">
                </div>
                <div class="file-list" style="display: none;"></div>
                <div class="upload-progress" style="display: none;">
                    <div class="progress">
                        <div class="progress-bar" role="progressbar" style="width: 0%"></div>
                    </div>
                    <div class="progress-text">Uploading...</div>
                </div>
                <div class="upload-actions" style="display: none;">
                    <button type="button" class="btn btn-primary upload-btn">Upload Files</button>
                    <button type="button" class="btn btn-secondary clear-btn">Clear All</button>
                </div>
            </div>
        `;

        this.element.innerHTML = uploadHtml;
        this.uploadArea = this.element.querySelector('.file-upload-area');
        this.fileInput = this.element.querySelector('.file-input');
        this.fileList = this.element.querySelector('.file-list');
        this.progressContainer = this.element.querySelector('.upload-progress');
        this.progressBar = this.element.querySelector('.progress-bar');
        this.progressText = this.element.querySelector('.progress-text');
        this.actionsContainer = this.element.querySelector('.upload-actions');
    }

    bindEvents() {
        // File input change
        this.fileInput.addEventListener('change', (e) => {
            this.handleFiles(e.target.files);
        });

        // Browse button click
        this.element.querySelector('.upload-browse-btn').addEventListener('click', () => {
            this.fileInput.click();
        });

        // Drag and drop events
        if (this.options.dragAndDrop) {
            this.uploadArea.addEventListener('dragover', (e) => {
                e.preventDefault();
                this.uploadArea.classList.add('drag-over');
            });

            this.uploadArea.addEventListener('dragleave', (e) => {
                e.preventDefault();
                this.uploadArea.classList.remove('drag-over');
            });

            this.uploadArea.addEventListener('drop', (e) => {
                e.preventDefault();
                this.uploadArea.classList.remove('drag-over');
                this.handleFiles(e.dataTransfer.files);
            });
        }

        // Upload button
        this.element.addEventListener('click', (e) => {
            if (e.target.matches('.upload-btn')) {
                this.uploadFiles();
            }
        });

        // Clear button
        this.element.addEventListener('click', (e) => {
            if (e.target.matches('.clear-btn')) {
                this.clearFiles();
            }
        });

        // Remove file buttons
        this.element.addEventListener('click', (e) => {
            if (e.target.matches('.remove-file-btn')) {
                const fileId = e.target.dataset.fileId;
                this.removeFile(fileId);
            }
        });
    }

    handleFiles(fileList) {
        const files = Array.from(fileList);
        
        // Check file count limit
        if (this.files.length + files.length > this.options.maxFiles) {
            this.showError(`Maximum ${this.options.maxFiles} files allowed`);
            return;
        }

        files.forEach(file => {
            if (this.validateFile(file)) {
                const fileObj = {
                    id: this.generateId(),
                    file: file,
                    name: file.name,
                    size: file.size,
                    type: file.type,
                    status: 'pending',
                    progress: 0
                };
                this.files.push(fileObj);
            }
        });

        this.updateFileList();
        this.updateActions();

        if (this.options.autoUpload && this.files.length > 0) {
            this.uploadFiles();
        }
    }

    validateFile(file) {
        // Check file size
        if (file.size > this.options.maxFileSize) {
            this.showError(`File "${file.name}" is too large. Maximum size is ${this.formatFileSize(this.options.maxFileSize)}`);
            return false;
        }

        // Check file type
        if (this.options.allowedTypes.length > 0 && !this.options.allowedTypes.includes(file.type)) {
            this.showError(`File "${file.name}" has unsupported format`);
            return false;
        }

        return true;
    }

    updateFileList() {
        if (this.files.length === 0) {
            this.fileList.style.display = 'none';
            return;
        }

        this.fileList.style.display = 'block';
        
        const filesHtml = this.files.map(fileObj => `
            <div class="file-item" data-file-id="${fileObj.id}">
                <div class="file-info">
                    <div class="file-icon">${this.getFileIcon(fileObj.type)}</div>
                    <div class="file-details">
                        <div class="file-name">${fileObj.name}</div>
                        <div class="file-size">${this.formatFileSize(fileObj.size)}</div>
                    </div>
                </div>
                <div class="file-status">
                    ${this.getFileStatusHtml(fileObj)}
                </div>
                <div class="file-actions">
                    <button type="button" class="btn btn-sm btn-outline-danger remove-file-btn" data-file-id="${fileObj.id}">
                        ✕
                    </button>
                </div>
            </div>
        `).join('');

        this.fileList.innerHTML = filesHtml;
    }

    getFileStatusHtml(fileObj) {
        switch (fileObj.status) {
            case 'pending':
                return '<span class="badge bg-secondary">Pending</span>';
            case 'uploading':
                return `
                    <div class="file-progress">
                        <div class="progress progress-sm">
                            <div class="progress-bar" style="width: ${fileObj.progress}%"></div>
                        </div>
                        <small>${fileObj.progress}%</small>
                    </div>
                `;
            case 'completed':
                return '<span class="badge bg-success">✓ Uploaded</span>';
            case 'error':
                return '<span class="badge bg-danger">✗ Error</span>';
            default:
                return '';
        }
    }

    updateActions() {
        if (this.files.length > 0 && !this.options.autoUpload) {
            this.actionsContainer.style.display = 'block';
        } else {
            this.actionsContainer.style.display = 'none';
        }
    }

    async uploadFiles() {
        if (this.isUploading || this.files.length === 0) return;

        this.isUploading = true;
        this.showProgress();

        const pendingFiles = this.files.filter(f => f.status === 'pending');
        let completedCount = 0;

        for (const fileObj of pendingFiles) {
            try {
                fileObj.status = 'uploading';
                this.updateFileList();

                const result = await this.uploadSingleFile(fileObj);
                
                fileObj.status = 'completed';
                fileObj.url = result.url;
                this.uploadedFiles.push(fileObj);
                
                completedCount++;
                this.updateOverallProgress(completedCount, pendingFiles.length);
                
            } catch (error) {
                fileObj.status = 'error';
                fileObj.error = error.message;
                this.showError(`Failed to upload ${fileObj.name}: ${error.message}`);
            }
            
            this.updateFileList();
        }

        this.isUploading = false;
        this.hideProgress();
        
        // Trigger upload complete event
        this.triggerEvent('uploadComplete', {
            files: this.uploadedFiles,
            total: pendingFiles.length,
            successful: completedCount
        });
    }

    async uploadSingleFile(fileObj) {
        return new Promise((resolve, reject) => {
            const formData = new FormData();
            formData.append('file', fileObj.file);

            const xhr = new XMLHttpRequest();

            xhr.upload.addEventListener('progress', (e) => {
                if (e.lengthComputable) {
                    fileObj.progress = Math.round((e.loaded / e.total) * 100);
                    this.updateFileList();
                }
            });

            xhr.addEventListener('load', () => {
                if (xhr.status === 200) {
                    try {
                        const response = JSON.parse(xhr.responseText);
                        resolve(response);
                    } catch (e) {
                        reject(new Error('Invalid response format'));
                    }
                } else {
                    reject(new Error(`Upload failed with status ${xhr.status}`));
                }
            });

            xhr.addEventListener('error', () => {
                reject(new Error('Network error occurred'));
            });

            xhr.open('POST', this.options.uploadUrl);
            xhr.send(formData);
        });
    }

    removeFile(fileId) {
        this.files = this.files.filter(f => f.id !== fileId);
        this.updateFileList();
        this.updateActions();
        
        this.triggerEvent('fileRemoved', { fileId });
    }

    clearFiles() {
        this.files = [];
        this.uploadedFiles = [];
        this.updateFileList();
        this.updateActions();
        this.fileInput.value = '';
        
        this.triggerEvent('filesCleared');
    }

    showProgress() {
        this.progressContainer.style.display = 'block';
        this.progressBar.style.width = '0%';
        this.progressText.textContent = 'Uploading...';
    }

    hideProgress() {
        this.progressContainer.style.display = 'none';
    }

    updateOverallProgress(completed, total) {
        const percentage = Math.round((completed / total) * 100);
        this.progressBar.style.width = `${percentage}%`;
        this.progressText.textContent = `Uploading... ${completed}/${total} files`;
    }

    showError(message) {
        // Create or update error message
        let errorDiv = this.element.querySelector('.upload-error');
        if (!errorDiv) {
            errorDiv = document.createElement('div');
            errorDiv.className = 'upload-error alert alert-danger';
            this.element.appendChild(errorDiv);
        }
        
        errorDiv.textContent = message;
        errorDiv.style.display = 'block';
        
        // Auto-hide after 5 seconds
        setTimeout(() => {
            errorDiv.style.display = 'none';
        }, 5000);
    }

    getFileIcon(mimeType) {
        if (mimeType.includes('pdf')) return '📄';
        if (mimeType.includes('word')) return '📝';
        if (mimeType.includes('image')) return '🖼️';
        return '📁';
    }

    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    getAllowedTypesText() {
        const typeMap = {
            'application/pdf': 'PDF',
            'application/msword': 'DOC',
            'application/vnd.openxmlformats-officedocument.wordprocessingml.document': 'DOCX',
            'image/jpeg': 'JPEG',
            'image/png': 'PNG'
        };
        
        return this.options.allowedTypes
            .map(type => typeMap[type] || type.split('/')[1].toUpperCase())
            .join(', ');
    }

    generateId() {
        return Math.random().toString(36).substr(2, 9);
    }

    triggerEvent(eventName, data = {}) {
        const event = new CustomEvent(eventName, {
            detail: { ...data, component: this }
        });
        this.element.dispatchEvent(event);
    }

    // Public API methods
    getFiles() {
        return this.files;
    }

    getUploadedFiles() {
        return this.uploadedFiles;
    }

    reset() {
        this.clearFiles();
    }

    destroy() {
        this.element.innerHTML = '';
    }
}

// Auto-initialize file upload components
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.file-upload').forEach(element => {
        const options = JSON.parse(element.dataset.options || '{}');
        new FileUploadComponent(element, options);
    });
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FileUploadComponent;
}
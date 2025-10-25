/**
 * Advanced Search & Filter Component
 * Provides dynamic search and filtering functionality
 */
class SearchFilterComponent {
    constructor(options = {}) {
        this.options = {
            searchInputSelector: '#search-input',
            filterContainerSelector: '#filter-container',
            resultsContainerSelector: '#results-container',
            paginationSelector: '#pagination',
            apiEndpoint: '/api/v1/search',
            debounceDelay: 300,
            pageSize: 20,
            ...options
        };

        this.currentPage = 1;
        this.totalPages = 1;
        this.currentFilters = {};
        this.searchTimeout = null;

        this.init();
    }

    init() {
        this.bindEvents();
        this.loadInitialData();
    }

    bindEvents() {
        // Search input with debouncing
        const searchInput = document.querySelector(this.options.searchInputSelector);
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                clearTimeout(this.searchTimeout);
                this.searchTimeout = setTimeout(() => {
                    this.performSearch(e.target.value);
                }, this.options.debounceDelay);
            });
        }

        // Filter checkboxes and selects
        const filterContainer = document.querySelector(this.options.filterContainerSelector);
        if (filterContainer) {
            filterContainer.addEventListener('change', (e) => {
                if (e.target.matches('input[type="checkbox"], select')) {
                    this.updateFilters();
                }
            });
        }

        // Clear filters button
        document.addEventListener('click', (e) => {
            if (e.target.matches('.clear-filters')) {
                this.clearFilters();
            }
        });

        // Pagination
        document.addEventListener('click', (e) => {
            if (e.target.matches('.pagination-link')) {
                e.preventDefault();
                const page = parseInt(e.target.dataset.page);
                if (page && page !== this.currentPage) {
                    this.goToPage(page);
                }
            }
        });
    }

    async performSearch(query = '') {
        this.showLoading();
        
        try {
            const params = new URLSearchParams({
                q: query,
                page: this.currentPage,
                pageSize: this.options.pageSize,
                ...this.currentFilters
            });

            const response = await fetch(`${this.options.apiEndpoint}?${params}`);
            const data = await response.json();

            this.displayResults(data);
            this.updatePagination(data.pagination || {});
            this.updateUrl(query, this.currentFilters);
        } catch (error) {
            console.error('Search error:', error);
            this.showError('An error occurred while searching. Please try again.');
        } finally {
            this.hideLoading();
        }
    }

    updateFilters() {
        const filterContainer = document.querySelector(this.options.filterContainerSelector);
        if (!filterContainer) return;

        this.currentFilters = {};

        // Collect checkbox filters
        filterContainer.querySelectorAll('input[type="checkbox"]:checked').forEach(checkbox => {
            const filterName = checkbox.name;
            if (!this.currentFilters[filterName]) {
                this.currentFilters[filterName] = [];
            }
            this.currentFilters[filterName].push(checkbox.value);
        });

        // Collect select filters
        filterContainer.querySelectorAll('select').forEach(select => {
            if (select.value) {
                this.currentFilters[select.name] = select.value;
            }
        });

        // Reset to first page when filters change
        this.currentPage = 1;
        this.performSearch(this.getCurrentSearchQuery());
    }

    clearFilters() {
        const filterContainer = document.querySelector(this.options.filterContainerSelector);
        if (!filterContainer) return;

        // Clear checkboxes
        filterContainer.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
            checkbox.checked = false;
        });

        // Reset selects
        filterContainer.querySelectorAll('select').forEach(select => {
            select.selectedIndex = 0;
        });

        this.currentFilters = {};
        this.currentPage = 1;
        this.performSearch(this.getCurrentSearchQuery());
    }

    goToPage(page) {
        this.currentPage = page;
        this.performSearch(this.getCurrentSearchQuery());
    }

    displayResults(data) {
        const resultsContainer = document.querySelector(this.options.resultsContainerSelector);
        if (!resultsContainer) return;

        if (!data.results || data.results.length === 0) {
            resultsContainer.innerHTML = this.getNoResultsHtml();
            return;
        }

        const resultsHtml = data.results.map(item => this.getResultItemHtml(item)).join('');
        resultsContainer.innerHTML = resultsHtml;

        // Update results count
        this.updateResultsCount(data.totalResults || 0);
    }

    getResultItemHtml(item) {
        // This should be customized based on the type of results
        return `
            <div class="result-item" data-id="${item.id}">
                <h3 class="result-title">
                    <a href="${item.url || '#'}">${item.title || 'Untitled'}</a>
                </h3>
                <p class="result-description">${item.description || ''}</p>
                <div class="result-meta">
                    <span class="result-type">${item.type || ''}</span>
                    <span class="result-date">${this.formatDate(item.date)}</span>
                </div>
            </div>
        `;
    }

    getNoResultsHtml() {
        return `
            <div class="no-results">
                <div class="no-results-icon">🔍</div>
                <h3>No results found</h3>
                <p>Try adjusting your search terms or filters.</p>
            </div>
        `;
    }

    updatePagination(pagination) {
        const paginationContainer = document.querySelector(this.options.paginationSelector);
        if (!paginationContainer) return;

        this.totalPages = pagination.totalPages || 1;
        
        if (this.totalPages <= 1) {
            paginationContainer.innerHTML = '';
            return;
        }

        const paginationHtml = this.getPaginationHtml();
        paginationContainer.innerHTML = paginationHtml;
    }

    getPaginationHtml() {
        let html = '<nav class="pagination-nav"><ul class="pagination">';

        // Previous button
        if (this.currentPage > 1) {
            html += `<li><a href="#" class="pagination-link" data-page="${this.currentPage - 1}">Previous</a></li>`;
        }

        // Page numbers
        const startPage = Math.max(1, this.currentPage - 2);
        const endPage = Math.min(this.totalPages, this.currentPage + 2);

        if (startPage > 1) {
            html += `<li><a href="#" class="pagination-link" data-page="1">1</a></li>`;
            if (startPage > 2) {
                html += '<li><span class="pagination-ellipsis">...</span></li>';
            }
        }

        for (let i = startPage; i <= endPage; i++) {
            const activeClass = i === this.currentPage ? 'active' : '';
            html += `<li><a href="#" class="pagination-link ${activeClass}" data-page="${i}">${i}</a></li>`;
        }

        if (endPage < this.totalPages) {
            if (endPage < this.totalPages - 1) {
                html += '<li><span class="pagination-ellipsis">...</span></li>';
            }
            html += `<li><a href="#" class="pagination-link" data-page="${this.totalPages}">${this.totalPages}</a></li>`;
        }

        // Next button
        if (this.currentPage < this.totalPages) {
            html += `<li><a href="#" class="pagination-link" data-page="${this.currentPage + 1}">Next</a></li>`;
        }

        html += '</ul></nav>';
        return html;
    }

    updateResultsCount(count) {
        const countElement = document.querySelector('.results-count');
        if (countElement) {
            countElement.textContent = `${count} results found`;
        }
    }

    updateUrl(query, filters) {
        const url = new URL(window.location);
        
        if (query) {
            url.searchParams.set('q', query);
        } else {
            url.searchParams.delete('q');
        }

        // Update filter parameters
        Object.keys(filters).forEach(key => {
            if (Array.isArray(filters[key])) {
                url.searchParams.set(key, filters[key].join(','));
            } else {
                url.searchParams.set(key, filters[key]);
            }
        });

        url.searchParams.set('page', this.currentPage);

        window.history.replaceState({}, '', url);
    }

    getCurrentSearchQuery() {
        const searchInput = document.querySelector(this.options.searchInputSelector);
        return searchInput ? searchInput.value : '';
    }

    showLoading() {
        const resultsContainer = document.querySelector(this.options.resultsContainerSelector);
        if (resultsContainer) {
            resultsContainer.innerHTML = '<div class="loading">Searching...</div>';
        }
    }

    hideLoading() {
        // Loading is hidden when results are displayed
    }

    showError(message) {
        const resultsContainer = document.querySelector(this.options.resultsContainerSelector);
        if (resultsContainer) {
            resultsContainer.innerHTML = `<div class="error">${message}</div>`;
        }
    }

    formatDate(dateString) {
        if (!dateString) return '';
        const date = new Date(dateString);
        return date.toLocaleDateString();
    }

    loadInitialData() {
        // Load data based on URL parameters
        const urlParams = new URLSearchParams(window.location.search);
        const query = urlParams.get('q') || '';
        const page = parseInt(urlParams.get('page')) || 1;

        this.currentPage = page;

        // Set search input value
        const searchInput = document.querySelector(this.options.searchInputSelector);
        if (searchInput) {
            searchInput.value = query;
        }

        // Load filters from URL
        this.loadFiltersFromUrl(urlParams);

        // Perform initial search
        this.performSearch(query);
    }

    loadFiltersFromUrl(urlParams) {
        const filterContainer = document.querySelector(this.options.filterContainerSelector);
        if (!filterContainer) return;

        urlParams.forEach((value, key) => {
            if (key === 'q' || key === 'page') return;

            // Handle checkbox filters
            const checkboxes = filterContainer.querySelectorAll(`input[name="${key}"]`);
            if (checkboxes.length > 0) {
                const values = value.split(',');
                checkboxes.forEach(checkbox => {
                    checkbox.checked = values.includes(checkbox.value);
                });
            }

            // Handle select filters
            const select = filterContainer.querySelector(`select[name="${key}"]`);
            if (select) {
                select.value = value;
            }
        });

        this.updateFilters();
    }
}

// Auto-initialize if elements exist
document.addEventListener('DOMContentLoaded', () => {
    if (document.querySelector('#search-input') || document.querySelector('#filter-container')) {
        window.searchFilter = new SearchFilterComponent();
    }
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SearchFilterComponent;
}
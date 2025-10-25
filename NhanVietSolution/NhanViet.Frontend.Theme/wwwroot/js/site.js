/* ========================================
   NhanViet Frontend Theme - Main JavaScript
   ======================================== */

(function() {
    'use strict';

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        initializeTheme();
    });

    function initializeTheme() {
        // Initialize all components
        initializeNavigation();
        initializeJobSearch();
        initializeConsultationForm();
        initializeScrollEffects();
        initializeTooltips();
    }

    // Navigation functionality
    function initializeNavigation() {
        const navbar = document.querySelector('.navbar');
        const navbarToggler = document.querySelector('.navbar-toggler');
        const navbarCollapse = document.querySelector('.navbar-collapse');

        // Mobile menu toggle
        if (navbarToggler && navbarCollapse) {
            navbarToggler.addEventListener('click', function() {
                navbarCollapse.classList.toggle('show');
            });
        }

        // Close mobile menu when clicking outside
        document.addEventListener('click', function(event) {
            if (navbarCollapse && navbarCollapse.classList.contains('show')) {
                if (!navbar.contains(event.target)) {
                    navbarCollapse.classList.remove('show');
                }
            }
        });

        // Smooth scrolling for anchor links
        const anchorLinks = document.querySelectorAll('a[href^="#"]');
        anchorLinks.forEach(function(link) {
            link.addEventListener('click', function(e) {
                const targetId = this.getAttribute('href');
                const targetElement = document.querySelector(targetId);
                
                if (targetElement) {
                    e.preventDefault();
                    targetElement.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });
    }

    // Job search functionality
    function initializeJobSearch() {
        const searchForm = document.querySelector('#job-search-form');
        const searchInput = document.querySelector('#job-search-input');
        const countryFilter = document.querySelector('#country-filter');
        const categoryFilter = document.querySelector('#category-filter');

        if (searchForm) {
            searchForm.addEventListener('submit', function(e) {
                e.preventDefault();
                performJobSearch();
            });
        }

        // Real-time search (debounced)
        if (searchInput) {
            let searchTimeout;
            searchInput.addEventListener('input', function() {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(performJobSearch, 500);
            });
        }

        // Filter change handlers
        if (countryFilter) {
            countryFilter.addEventListener('change', performJobSearch);
        }

        if (categoryFilter) {
            categoryFilter.addEventListener('change', performJobSearch);
        }
    }

    function performJobSearch() {
        const searchTerm = document.querySelector('#job-search-input')?.value || '';
        const country = document.querySelector('#country-filter')?.value || '';
        const category = document.querySelector('#category-filter')?.value || '';

        // Show loading state
        const resultsContainer = document.querySelector('#job-results');
        if (resultsContainer) {
            resultsContainer.innerHTML = '<div class="text-center"><div class="loading"></div> Đang tìm kiếm...</div>';
        }

        // Build search parameters
        const params = new URLSearchParams();
        if (searchTerm) params.append('search', searchTerm);
        if (country) params.append('country', country);
        if (category) params.append('category', category);

        // Perform AJAX search
        fetch(`/api/jobs/search?${params.toString()}`)
            .then(response => response.json())
            .then(data => {
                displayJobResults(data);
            })
            .catch(error => {
                console.error('Search error:', error);
                if (resultsContainer) {
                    resultsContainer.innerHTML = '<div class="alert alert-danger">Có lỗi xảy ra khi tìm kiếm. Vui lòng thử lại.</div>';
                }
            });
    }

    function displayJobResults(jobs) {
        const resultsContainer = document.querySelector('#job-results');
        if (!resultsContainer) return;

        if (jobs.length === 0) {
            resultsContainer.innerHTML = '<div class="alert alert-info">Không tìm thấy đơn hàng phù hợp.</div>';
            return;
        }

        let html = '<div class="row">';
        jobs.forEach(function(job) {
            html += `
                <div class="col-md-6 col-lg-4 mb-4">
                    <div class="card job-card h-100">
                        <div class="card-body">
                            <h5 class="card-title">${escapeHtml(job.title)}</h5>
                            <p class="card-text">${escapeHtml(job.country)} - ${escapeHtml(job.category)}</p>
                            <p class="job-salary">Lương: ${escapeHtml(job.salary)}</p>
                            <p class="card-text">${escapeHtml(job.summary)}</p>
                            <a href="/jobs/${job.id}" class="btn btn-primary">Xem chi tiết</a>
                        </div>
                    </div>
                </div>
            `;
        });
        html += '</div>';

        resultsContainer.innerHTML = html;
    }

    // Consultation form functionality
    function initializeConsultationForm() {
        const consultationForm = document.querySelector('#consultation-form');
        
        if (consultationForm) {
            consultationForm.addEventListener('submit', function(e) {
                e.preventDefault();
                submitConsultationForm(this);
            });
        }
    }

    function submitConsultationForm(form) {
        const submitButton = form.querySelector('button[type="submit"]');
        const originalText = submitButton.textContent;
        
        // Show loading state
        submitButton.disabled = true;
        submitButton.innerHTML = '<span class="loading"></span> Đang gửi...';

        // Get form data
        const formData = new FormData(form);

        // Submit form
        fetch('/api/consultation/submit', {
            method: 'POST',
            body: formData
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showAlert('success', 'Cảm ơn bạn! Chúng tôi sẽ liên hệ với bạn sớm nhất có thể.');
                form.reset();
            } else {
                showAlert('danger', data.message || 'Có lỗi xảy ra. Vui lòng thử lại.');
            }
        })
        .catch(error => {
            console.error('Form submission error:', error);
            showAlert('danger', 'Có lỗi xảy ra. Vui lòng thử lại sau.');
        })
        .finally(() => {
            // Restore button state
            submitButton.disabled = false;
            submitButton.textContent = originalText;
        });
    }

    // Scroll effects
    function initializeScrollEffects() {
        const navbar = document.querySelector('.navbar');
        
        if (navbar) {
            window.addEventListener('scroll', function() {
                if (window.scrollY > 100) {
                    navbar.classList.add('navbar-scrolled');
                } else {
                    navbar.classList.remove('navbar-scrolled');
                }
            });
        }

        // Animate elements on scroll
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver(function(entries) {
            entries.forEach(function(entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('animate-in');
                }
            });
        }, observerOptions);

        // Observe elements with animation class
        const animateElements = document.querySelectorAll('.animate-on-scroll');
        animateElements.forEach(function(element) {
            observer.observe(element);
        });
    }

    // Initialize tooltips
    function initializeTooltips() {
        const tooltipElements = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        tooltipElements.forEach(function(element) {
            new bootstrap.Tooltip(element);
        });
    }

    // Utility functions
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function showAlert(type, message) {
        const alertContainer = document.querySelector('#alert-container') || document.body;
        const alertElement = document.createElement('div');
        alertElement.className = `alert alert-${type} alert-dismissible fade show`;
        alertElement.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        alertContainer.insertBefore(alertElement, alertContainer.firstChild);

        // Auto-dismiss after 5 seconds
        setTimeout(function() {
            if (alertElement.parentNode) {
                alertElement.remove();
            }
        }, 5000);
    }

    // Expose global functions
    window.NhanVietTheme = {
        performJobSearch: performJobSearch,
        showAlert: showAlert
    };

})();
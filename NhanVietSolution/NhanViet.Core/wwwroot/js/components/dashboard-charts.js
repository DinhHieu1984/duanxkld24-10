/**
 * Dashboard Charts Component
 * Provides interactive charts for dashboard analytics
 * Uses Chart.js library
 */
class DashboardChartsComponent {
    constructor(options = {}) {
        this.options = {
            apiEndpoint: '/api/v1/dashboard',
            refreshInterval: 300000, // 5 minutes
            ...options
        };

        this.charts = {};
        this.refreshTimer = null;

        this.init();
    }

    async init() {
        try {
            await this.loadChartLibrary();
            await this.loadDashboardData();
            this.startAutoRefresh();
        } catch (error) {
            console.error('Failed to initialize dashboard charts:', error);
        }
    }

    async loadChartLibrary() {
        // Load Chart.js if not already loaded
        if (typeof Chart === 'undefined') {
            return new Promise((resolve, reject) => {
                const script = document.createElement('script');
                script.src = 'https://cdn.jsdelivr.net/npm/chart.js';
                script.onload = resolve;
                script.onerror = reject;
                document.head.appendChild(script);
            });
        }
    }

    async loadDashboardData() {
        try {
            const response = await fetch(this.options.apiEndpoint);
            const data = await response.json();

            this.createJobOrdersChart(data.charts.jobOrdersByCountry);
            this.createCompaniesChart(data.charts.companiesByIndustry);
            this.createCategoriesChart(data.charts.jobOrdersByCategory);
            this.createStatisticsCards(data.statistics);
        } catch (error) {
            console.error('Error loading dashboard data:', error);
            this.showError('Failed to load dashboard data');
        }
    }

    createJobOrdersChart(data) {
        const canvas = document.getElementById('jobOrdersChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.jobOrders) {
            this.charts.jobOrders.destroy();
        }

        this.charts.jobOrders = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: Object.keys(data),
                datasets: [{
                    label: 'Job Orders by Country',
                    data: Object.values(data),
                    backgroundColor: [
                        '#3498db', '#e74c3c', '#2ecc71', '#f39c12', '#9b59b6',
                        '#1abc9c', '#34495e', '#e67e22', '#95a5a6', '#f1c40f'
                    ],
                    borderColor: '#2c3e50',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: 'Job Orders by Country'
                    },
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                }
            }
        });
    }

    createCompaniesChart(data) {
        const canvas = document.getElementById('companiesChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.companies) {
            this.charts.companies.destroy();
        }

        this.charts.companies = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: Object.keys(data),
                datasets: [{
                    label: 'Companies by Industry',
                    data: Object.values(data),
                    backgroundColor: [
                        '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF',
                        '#FF9F40', '#FF6384', '#C9CBCF', '#4BC0C0', '#FF6384'
                    ],
                    hoverBackgroundColor: [
                        '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF',
                        '#FF9F40', '#FF6384', '#C9CBCF', '#4BC0C0', '#FF6384'
                    ]
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: 'Companies by Industry'
                    },
                    legend: {
                        position: 'right'
                    }
                }
            }
        });
    }

    createCategoriesChart(data) {
        const canvas = document.getElementById('categoriesChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        
        if (this.charts.categories) {
            this.charts.categories.destroy();
        }

        this.charts.categories = new Chart(ctx, {
            type: 'line',
            data: {
                labels: Object.keys(data),
                datasets: [{
                    label: 'Job Orders by Category',
                    data: Object.values(data),
                    borderColor: '#3498db',
                    backgroundColor: 'rgba(52, 152, 219, 0.1)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: 'Job Orders by Category'
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                }
            }
        });
    }

    createStatisticsCards(statistics) {
        this.updateStatCard('total-jobs', statistics.jobOrders.total);
        this.updateStatCard('active-jobs', statistics.jobOrders.active);
        this.updateStatCard('total-companies', statistics.companies.total);
        this.updateStatCard('verified-companies', statistics.companies.verified);
        this.updateStatCard('total-applications', statistics.jobOrders.applications);
        this.updateStatCard('average-rating', statistics.companies.averageRating?.toFixed(1) || '0.0');
    }

    updateStatCard(cardId, value) {
        const card = document.getElementById(cardId);
        if (card) {
            const valueElement = card.querySelector('.stat-value');
            if (valueElement) {
                // Animate number change
                this.animateNumber(valueElement, parseInt(valueElement.textContent) || 0, value);
            }
        }
    }

    animateNumber(element, start, end, duration = 1000) {
        const range = end - start;
        const increment = range / (duration / 16);
        let current = start;

        const timer = setInterval(() => {
            current += increment;
            if ((increment > 0 && current >= end) || (increment < 0 && current <= end)) {
                current = end;
                clearInterval(timer);
            }
            element.textContent = Math.floor(current);
        }, 16);
    }

    startAutoRefresh() {
        if (this.refreshTimer) {
            clearInterval(this.refreshTimer);
        }

        this.refreshTimer = setInterval(() => {
            this.loadDashboardData();
        }, this.options.refreshInterval);
    }

    stopAutoRefresh() {
        if (this.refreshTimer) {
            clearInterval(this.refreshTimer);
            this.refreshTimer = null;
        }
    }

    showError(message) {
        const errorContainer = document.getElementById('dashboard-error');
        if (errorContainer) {
            errorContainer.innerHTML = `
                <div class="alert alert-danger">
                    <strong>Error:</strong> ${message}
                    <button type="button" class="btn btn-sm btn-outline-danger ms-2" onclick="dashboardCharts.loadDashboardData()">
                        Retry
                    </button>
                </div>
            `;
            errorContainer.style.display = 'block';
        }
    }

    hideError() {
        const errorContainer = document.getElementById('dashboard-error');
        if (errorContainer) {
            errorContainer.style.display = 'none';
        }
    }

    destroy() {
        this.stopAutoRefresh();
        
        Object.values(this.charts).forEach(chart => {
            if (chart && typeof chart.destroy === 'function') {
                chart.destroy();
            }
        });
        
        this.charts = {};
    }

    refresh() {
        this.loadDashboardData();
    }

    exportChart(chartName, format = 'png') {
        const chart = this.charts[chartName];
        if (!chart) {
            console.error(`Chart '${chartName}' not found`);
            return;
        }

        const url = chart.toBase64Image();
        const link = document.createElement('a');
        link.download = `${chartName}-chart.${format}`;
        link.href = url;
        link.click();
    }

    // Utility method to resize charts when container size changes
    resizeCharts() {
        Object.values(this.charts).forEach(chart => {
            if (chart && typeof chart.resize === 'function') {
                chart.resize();
            }
        });
    }
}

// Auto-initialize dashboard charts
document.addEventListener('DOMContentLoaded', () => {
    if (document.querySelector('.dashboard-charts')) {
        window.dashboardCharts = new DashboardChartsComponent();
        
        // Handle window resize
        window.addEventListener('resize', () => {
            if (window.dashboardCharts) {
                window.dashboardCharts.resizeCharts();
            }
        });
        
        // Handle page visibility change to pause/resume auto-refresh
        document.addEventListener('visibilitychange', () => {
            if (window.dashboardCharts) {
                if (document.hidden) {
                    window.dashboardCharts.stopAutoRefresh();
                } else {
                    window.dashboardCharts.startAutoRefresh();
                }
            }
        });
    }
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = DashboardChartsComponent;
}
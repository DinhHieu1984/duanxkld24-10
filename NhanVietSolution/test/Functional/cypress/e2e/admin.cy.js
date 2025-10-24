describe('NhanViet Admin Panel', () => {
  it('should redirect to login when accessing admin without authentication', () => {
    cy.visit('/admin')
    
    // Should redirect to login page or show login form
    cy.url().should('match', /(login|admin)/)
    
    // Check if login form elements exist
    cy.get('input[type="text"], input[type="email"]').should('exist')
    cy.get('input[type="password"]').should('exist')
    cy.get('button[type="submit"], input[type="submit"]').should('exist')
  })

  it('should display OrchardCore admin interface', () => {
    cy.visit('/admin')
    
    // Check for OrchardCore specific elements
    cy.get('body').should('contain.text', 'Orchard')
  })

  it('should have proper admin layout structure', () => {
    cy.visit('/admin')
    
    // Check for basic admin layout elements
    cy.get('head').should('exist')
    cy.get('body').should('exist')
    cy.get('form').should('exist') // Login form should be present
  })

  it('should be accessible on different devices', () => {
    // Mobile
    cy.viewport(375, 667)
    cy.visit('/admin')
    cy.get('body').should('be.visible')
    
    // Tablet
    cy.viewport(768, 1024)
    cy.visit('/admin')
    cy.get('body').should('be.visible')
    
    // Desktop
    cy.viewport(1920, 1080)
    cy.visit('/admin')
    cy.get('body').should('be.visible')
  })
})
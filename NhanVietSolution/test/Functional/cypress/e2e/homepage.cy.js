describe('NhanViet Homepage', () => {
  beforeEach(() => {
    cy.visit('/')
  })

  it('should load the homepage successfully', () => {
    cy.get('body').should('be.visible')
    cy.title().should('not.be.empty')
  })

  it('should have proper meta tags', () => {
    cy.get('head meta[name="viewport"]').should('exist')
    cy.get('head meta[charset]').should('exist')
  })

  it('should display navigation menu', () => {
    // Check if navigation exists (adjust selector based on actual implementation)
    cy.get('nav, .navbar, [role="navigation"]').should('exist')
  })

  it('should be responsive', () => {
    // Test mobile viewport
    cy.viewport(375, 667)
    cy.get('body').should('be.visible')
    
    // Test tablet viewport
    cy.viewport(768, 1024)
    cy.get('body').should('be.visible')
    
    // Test desktop viewport
    cy.viewport(1920, 1080)
    cy.get('body').should('be.visible')
  })

  it('should have accessible content', () => {
    // Check for basic accessibility
    cy.get('main, [role="main"]').should('exist')
    cy.get('h1, h2, h3').should('exist')
  })
})
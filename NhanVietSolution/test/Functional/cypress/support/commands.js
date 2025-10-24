// ***********************************************
// This example commands.js shows you how to
// create various custom commands and overwrite
// existing commands.
//
// For more comprehensive examples of custom
// commands please read more here:
// https://on.cypress.io/custom-commands
// ***********************************************

// NhanViet specific commands for testing OrchardCore functionality

Cypress.Commands.add('checkContentType', (contentType) => {
  cy.visit('/admin/ContentTypes')
  cy.get('body').should('contain.text', contentType)
})

Cypress.Commands.add('verifyJobOrderFields', () => {
  cy.visit('/admin/ContentTypes/Edit/JobOrder')
  
  const expectedFields = [
    'JobTitle',
    'CompanyName', 
    'Location',
    'Salary',
    'JobType',
    'Experience',
    'Education',
    'Skills',
    'Description',
    'IsActive'
  ]
  
  expectedFields.forEach(field => {
    cy.get('body').should('contain.text', field)
  })
})

Cypress.Commands.add('verifyNewsArticleFields', () => {
  cy.visit('/admin/ContentTypes/Edit/NewsArticle')
  
  const expectedFields = [
    'Title',
    'Summary',
    'Content',
    'Author',
    'Category',
    'FeaturedImage',
    'IsPublished'
  ]
  
  expectedFields.forEach(field => {
    cy.get('body').should('contain.text', field)
  })
})

Cypress.Commands.add('checkModuleEnabled', (moduleName) => {
  cy.visit('/admin/Features')
  cy.get(`[data-module="${moduleName}"], .module-${moduleName}`)
    .should('exist')
    .and('contain.text', 'Enabled')
})

Cypress.Commands.add('testResponsiveness', (selector = 'body') => {
  const viewports = [
    { width: 375, height: 667, name: 'Mobile' },
    { width: 768, height: 1024, name: 'Tablet' },
    { width: 1920, height: 1080, name: 'Desktop' }
  ]
  
  viewports.forEach(viewport => {
    cy.viewport(viewport.width, viewport.height)
    cy.get(selector).should('be.visible')
    cy.log(`✓ ${viewport.name} viewport test passed`)
  })
})
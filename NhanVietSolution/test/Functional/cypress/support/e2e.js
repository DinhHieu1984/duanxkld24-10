// ***********************************************************
// This example support/e2e.js is processed and
// loaded automatically before your test files.
//
// This is a great place to put global configuration and
// behavior that modifies Cypress.
//
// You can change the location of this file or turn off
// automatically serving support files with the
// 'supportFile' configuration option.
//
// You can read more here:
// https://on.cypress.io/configuration
// ***********************************************************

// Import commands.js using ES2015 syntax:
import './commands'

// Import code coverage support
import '@cypress/code-coverage/support'

// Alternatively you can use CommonJS syntax:
// require('./commands')

// Global configuration
Cypress.on('uncaught:exception', (err, runnable) => {
  // Prevent Cypress from failing on uncaught exceptions
  // that might occur in OrchardCore during development
  if (err.message.includes('ResizeObserver loop limit exceeded')) {
    return false
  }
  if (err.message.includes('Non-Error promise rejection captured')) {
    return false
  }
  // Let other errors fail the test
  return true
})

// Custom commands for OrchardCore testing
Cypress.Commands.add('loginAsAdmin', (username = 'admin', password = 'Password123!') => {
  cy.visit('/admin')
  cy.get('input[name="UserName"], input[name="Email"]').type(username)
  cy.get('input[name="Password"]').type(password)
  cy.get('button[type="submit"], input[type="submit"]').click()
})

Cypress.Commands.add('createContent', (contentType, data) => {
  cy.visit(`/admin/Contents/ContentItems/${contentType}/Create`)
  
  // Fill form based on data object
  Object.keys(data).forEach(key => {
    cy.get(`[name*="${key}"]`).type(data[key])
  })
  
  cy.get('button[type="submit"]').click()
})

Cypress.Commands.add('waitForOrchardCore', () => {
  // Wait for OrchardCore to be fully loaded
  cy.get('body').should('be.visible')
  cy.wait(1000) // Give OrchardCore time to initialize
})
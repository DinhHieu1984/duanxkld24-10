# NhanViet API Documentation

## Overview
NhanViet Job Portal API provides comprehensive endpoints for job management, company profiles, and recruitment services.

**Base URL:** `https://api.nhanviet.com`  
**Version:** 1.0.0  
**Authentication:** Bearer Token required for protected endpoints

## Authentication

### Bearer Token
Include the authorization header in protected requests:
```
Authorization: Bearer <your-token>
```

## Core API Endpoints

### 1. Dashboard API
**GET** `/api/v1/dashboard`
- **Description:** Get dashboard analytics data
- **Authentication:** Required
- **Response:**
```json
{
  "statistics": {
    "jobOrders": {
      "total": 1250,
      "active": 890,
      "expired": 360,
      "applications": 5420
    },
    "companies": {
      "total": 340,
      "verified": 280,
      "featured": 45,
      "averageRating": 4.2
    }
  },
  "featuredCompanies": [...],
  "recentJobOrders": [...],
  "charts": {
    "jobOrdersByCountry": {...},
    "companiesByIndustry": {...},
    "jobOrdersByCategory": {...}
  }
}
```

### 2. Global Search API
**GET** `/api/v1/search`
- **Description:** Search across all content types
- **Parameters:**
  - `q` (string, required): Search query
  - `type` (string, optional): Content type filter (`all`, `jobs`, `companies`)
- **Response:**
```json
{
  "query": "software developer",
  "jobOrders": [...],
  "companies": [...]
}
```

### 3. Home Page API
**GET** `/api/v1/home`
- **Description:** Get home page data
- **Response:**
```json
{
  "featuredJobOrders": [...],
  "featuredCompanies": [...],
  "statistics": {...},
  "popularCategories": [...],
  "popularCountries": [...]
}
```

### 4. Job Application API
**POST** `/api/v1/apply`
- **Description:** Submit job application
- **Request Body:**
```json
{
  "jobOrderId": "string",
  "applicantName": "string",
  "applicantEmail": "string",
  "applicantPhone": "string",
  "coverLetter": "string",
  "resumeUrl": "string"
}
```
- **Response:**
```json
{
  "message": "Job application submitted successfully"
}
```

### 5. Health Check API
**GET** `/api/v1/health`
- **Description:** System health status
- **Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2024-10-25T10:30:00Z",
  "version": "1.0.0",
  "services": {
    "jobOrderService": "OK",
    "companyService": "OK",
    "notificationService": "OK"
  },
  "database": "Connected",
  "uptime": "2.15:30:45"
}
```

## Job Orders API

### Get Job Orders
**GET** `/api/joborder`
- **Parameters:**
  - `skip` (int): Number of records to skip
  - `take` (int): Number of records to take
  - `country` (string): Filter by country
  - `category` (string): Filter by category
  - `isActive` (bool): Filter by active status
- **Response:** Array of job order objects

### Get Job Order by ID
**GET** `/api/joborder/{id}`
- **Response:** Single job order object

### Create Job Order
**POST** `/api/joborder`
- **Authentication:** Required
- **Request Body:** Job order creation model
- **Response:** Created job order object

### Update Job Order
**PUT** `/api/joborder/{id}`
- **Authentication:** Required
- **Request Body:** Job order update model
- **Response:** Updated job order object

### Delete Job Order
**DELETE** `/api/joborder/{id}`
- **Authentication:** Required
- **Response:** 204 No Content

### Search Job Orders
**GET** `/api/joborder/search`
- **Parameters:**
  - `q` (string): Search query
  - Additional filter parameters
- **Response:** Array of matching job orders

### Get Active Job Orders
**GET** `/api/joborder/active`
- **Response:** Array of active job orders

### Get Job Order Statistics
**GET** `/api/joborder/statistics`
- **Authentication:** Required
- **Response:** Job order statistics object

## Companies API

### Get Companies
**GET** `/api/company`
- **Parameters:**
  - `skip` (int): Number of records to skip
  - `take` (int): Number of records to take
  - `industry` (string): Filter by industry
  - `size` (string): Filter by company size
  - `location` (string): Filter by location
- **Response:** Array of company objects

### Get Company by ID
**GET** `/api/company/{id}`
- **Response:** Single company object

### Create Company
**POST** `/api/company`
- **Authentication:** Required (ManageCompanies policy)
- **Request Body:** Company creation model
- **Response:** Created company object

### Update Company
**PUT** `/api/company/{id}`
- **Authentication:** Required (ManageCompanies policy)
- **Request Body:** Company update model
- **Response:** Updated company object

### Delete Company
**DELETE** `/api/company/{id}`
- **Authentication:** Required (ManageCompanies policy)
- **Response:** 204 No Content

### Search Companies
**GET** `/api/company/search`
- **Parameters:**
  - `q` (string): Search query
  - Additional filter parameters
- **Response:** Array of matching companies

### Get Companies by Industry
**GET** `/api/company/by-industry/{industry}`
- **Response:** Array of companies in specified industry

### Get Featured Companies
**GET** `/api/company/featured`
- **Parameters:**
  - `count` (int): Number of companies to return (default: 10)
- **Response:** Array of featured companies

### Get Company Statistics
**GET** `/api/company/statistics`
- **Authentication:** Required (ViewCompanyStatistics policy)
- **Response:** Company statistics object

### Verify Company
**POST** `/api/company/{id}/verify`
- **Authentication:** Required (ManageCompanies policy)
- **Response:** Success message

### Feature Company
**POST** `/api/company/{id}/feature`
- **Authentication:** Required (ManageCompanies policy)
- **Response:** Success message

## Data Models

### Job Order Model
```json
{
  "contentItemId": "string",
  "jobTitle": "string",
  "jobDescription": "string",
  "requirements": "string",
  "benefits": "string",
  "salaryRange": "string",
  "jobType": "string",
  "experienceLevel": "string",
  "category": "string",
  "country": "string",
  "city": "string",
  "contactEmail": "string",
  "contactPhone": "string",
  "applicationDeadline": "2024-12-31T23:59:59Z",
  "isActive": true,
  "createdUtc": "2024-10-25T10:30:00Z",
  "modifiedUtc": "2024-10-25T10:30:00Z",
  "published": true
}
```

### Company Model
```json
{
  "contentItemId": "string",
  "companyName": "string",
  "description": "string",
  "industry": "string",
  "companySize": "string",
  "website": "string",
  "email": "string",
  "phone": "string",
  "address": "string",
  "city": "string",
  "country": "string",
  "postalCode": "string",
  "foundedYear": 2020,
  "isVerified": true,
  "isFeatured": false,
  "rating": 4.5,
  "reviewCount": 25,
  "createdUtc": "2024-10-25T10:30:00Z",
  "modifiedUtc": "2024-10-25T10:30:00Z",
  "published": true
}
```

## Error Responses

### Standard Error Format
```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human readable error message",
    "details": "Additional error details"
  }
}
```

### Common HTTP Status Codes
- `200 OK` - Success
- `201 Created` - Resource created successfully
- `204 No Content` - Success with no response body
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

## Rate Limiting
- **Limit:** 1000 requests per hour per API key
- **Headers:**
  - `X-RateLimit-Limit`: Request limit per hour
  - `X-RateLimit-Remaining`: Remaining requests
  - `X-RateLimit-Reset`: Time when limit resets

## Pagination
Most list endpoints support pagination:
- `skip` (int): Number of records to skip (default: 0)
- `take` (int): Number of records to return (default: 20, max: 100)

Response includes pagination metadata:
```json
{
  "data": [...],
  "total": 1250,
  "skip": 0,
  "take": 20
}
```

## Filtering and Sorting
Many endpoints support filtering and sorting:
- **Filtering:** Use query parameters matching field names
- **Sorting:** Use `sortBy` parameter with field name and `sortDescending` boolean

Example:
```
GET /api/joborder?country=Vietnam&category=IT&sortBy=CreatedUtc&sortDescending=true
```

## WebHooks (Future)
Webhook endpoints for real-time notifications:
- Job application received
- Company verification status changed
- Job order expired

## SDK and Libraries
Official SDKs available for:
- JavaScript/TypeScript
- C#/.NET
- Python
- PHP

## Support
- **Documentation:** https://docs.nhanviet.com
- **Support Email:** api-support@nhanviet.com
- **Status Page:** https://status.nhanviet.com
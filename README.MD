# ClaimFlow - Insurance Claims Management Platform

A complete **event-driven microservices platform** for managing insurance claims and documents, built with **ASP.NET Core 10**, **RabbitMQ**, **PostgreSQL**, and **MinIO**.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Services](#services)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Quick Start](#quick-start)
- [API Documentation](#api-documentation)
- [Workflows](#workflows)
- [Event Flow](#event-flow)
---

## 🎯 Overview

ClaimFlow is a microservices-based insurance claims management system that demonstrates:
- ✅ **Microservices Architecture** - Independent, scalable services
- ✅ **Event-Driven Communication** - RabbitMQ for async messaging
- ✅ **Cloud-Native Storage** - MinIO for S3-compatible object storage
- ✅ **Full Observability** - Structured logging with Serilog
- ✅ **API-First Design** - OpenAPI/Scalar documentation

---

## 🏢 Services

### 1. Claims Service (Port 5116)
**Purpose:** Manage insurance claims lifecycle

**Responsibilities:**
- Create, read, update, delete claims
- Track claim status workflow (Submitted → UnderReview → Approved → Settled → Closed)
- Auto-generate unique claim numbers
- Manage claimant information
- Store document metadata (synced from Documents Service)

**Database:** PostgreSQL (`claimsdb`)
- `Claims` table - Claim records
- `ClaimDocuments` table - Document metadata (synced via events)

**API Endpoints:**
- `POST /api/claims` - Create claim (JSON)
- `POST /api/claims/with-files` - Create claim with file uploads (sends files via events)
- `GET /api/claims` - List claims (paginated, filterable)
- `GET /api/claims/{id}` - Get claim by ID
- `PUT /api/claims/{id}` - Update claim
- `DELETE /api/claims/{id}` - Delete claim
- `GET /health` - Health check

**Events Published:**
- `ClaimCreatedEvent` - When a new claim is created
- `ClaimStatusChangedEvent` - When claim status updates
- `StoreDocumentRequestEvent` - When files need to be stored

**Events Subscribed:**
- `DocumentUploadedEvent` - When Documents Service stores a file
- `DocumentDeletedEvent` - When a document is deleted

---

### 2. Documents Service (Port 5173)
**Purpose:** Handle document storage and retrieval

**Responsibilities:**
- Upload documents to MinIO (S3-compatible storage)
- Generate presigned download URLs
- Manage document metadata
- Handle document deletion
- Receive files via events from Claims Service

**Database:** PostgreSQL (`documentsdb`)
- `Documents` table - Document metadata and MinIO references

**Storage:** MinIO
- Bucket: `claimflow-documents`
- Files stored with GUID-based keys

**API Endpoints:**
- `POST /api/documents/upload` - Upload document (multipart/form-data)
- `GET /api/documents/{id}` - Get document metadata
- `GET /api/documents/claim/{claimId}` - List all documents for a claim
- `GET /api/documents/{id}/download-url` - Get presigned download URL
- `DELETE /api/documents/{id}` - Delete document
- `GET /health` - Health check

**Events Published:**
- `DocumentUploadedEvent` - When a document is successfully stored
- `DocumentDeletedEvent` - When a document is deleted

**Events Subscribed:**
- `ClaimCreatedEvent` - Prepare for document uploads
- `ClaimStatusChangedEvent` - React to claim lifecycle changes
- `StoreDocumentRequestEvent` - Store files received via events

---

### 3. Shared Messaging Library
**Purpose:** Common event models and RabbitMQ infrastructure

**Components:**
- Event models (DTOs for all events)
- `IEventBus` interface
- `RabbitMQEventBus` implementation
- Service registration extensions

**Events:**
- `ClaimCreatedEvent`
- `ClaimStatusChangedEvent`
- `StoreDocumentRequestEvent`
- `DocumentUploadedEvent`
- `DocumentDeletedEvent`

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         User Interface                           │
│                    (Postman / Scalar UI / CLI)                   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP
                              │
        ┌─────────────────────┴─────────────────────┐
        │                                           │
        ▼                                           ▼
┌───────────────────┐                    ┌───────────────────┐
│  Claims Service   │                    │ Documents Service │
│   Port: 5001      │◄──────Events──────►│   Port: 5002      │
│                   │                    │                   │
│ - Claims CRUD     │                    │ - File Upload     │
│ - Workflow Mgmt   │                    │ - MinIO Storage   │
│ - Doc Metadata    │                    │ - Download URLs   │
└─────────┬─────────┘                    └─────────┬─────────┘
          │                                        │
          │                                        │
          ▼                                        ▼
┌─────────────────┐    ┌──────────────────┐  ┌──────────────┐
│  PostgreSQL     │    │    RabbitMQ      │  │    MinIO     │
│   (claimsdb)    │    │   (CloudAMQP)    │  │ (S3 Storage) │
│                 │    │                  │  │              │
│ - Claims        │    │ - Event Queue    │  │ - Documents  │
│ - ClaimDocs     │    │ - Pub/Sub        │  │ - Bucket     │
└─────────────────┘    └──────────────────┘  └──────────────┘
```

### Communication Patterns

**Synchronous (HTTP REST):**
- User → Services (API calls)

**Asynchronous (RabbitMQ Events):**
- Claims Service → Documents Service (file storage requests)
- Documents Service → Claims Service (upload confirmations)
- Cross-service notifications

---

## 🛠️ Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| **Backend Framework** | ASP.NET Core | 10.0 |
| **Language** | C# | 12 |
| **Database** | PostgreSQL | 15 |
| **ORM** | Entity Framework Core | 10.0 |
| **Message Broker** | RabbitMQ (CloudAMQP) | 7.2.0 client |
| **Object Storage** | MinIO | 7.0.0 client |
| **API Documentation** | Scalar UI | 2.5.6+ |
| **OpenAPI** | Microsoft.AspNetCore.OpenApi | 10.0 |
| **Logging** | Serilog | 10.0 |

---

## 📁 Project Structure

```
claimflow-services/
│
├── claims-service/                          # Service 1
│   ├── src/
│   │   └── ClaimsService.API/
│   │       ├── Controllers/
│   │       │   └── ClaimsController.cs      # REST endpoints
│   │       ├── Data/
│   │       │   └── ClaimsDbContext.cs       # EF Core context
│   │       ├── EventHandlers/               # RabbitMQ handlers
│   │       │   ├── DocumentUploadedEventHandler.cs
│   │       │   └── DocumentDeletedEventHandler.cs
│   │       ├── Models/
│   │       │   ├── Claim.cs                 # Domain entity
│   │       │   ├── ClaimDocument.cs         # Document metadata
│   │       │   └── ClaimDtos.cs             # DTOs
│   │       ├── Services/
│   │       │   ├── IClaimsService.cs
│   │       │   └── ClaimsService.cs         # Business logic
│   │       ├── Program.cs                   # App startup
│   │       └── appsettings.json
│   └── README.md
│
├── documents-service/                       # Service 2
│   ├── src/
│   │   └── DocumentsService.API/
│   │       ├── Controllers/
│   │       │   └── DocumentsController.cs   # REST endpoints
│   │       ├── Data/
│   │       │   └── DocumentsDbContext.cs    # EF Core context
│   │       ├── EventHandlers/               # RabbitMQ handlers
│   │       │   ├── ClaimCreatedEventHandler.cs
│   │       │   ├── ClaimStatusChangedEventHandler.cs
│   │       │   └── StoreDocumentRequestEventHandler.cs
│   │       ├── Models/
│   │       │   ├── Document.cs              # Domain entity
│   │       │   └── DocumentDtos.cs          # DTOs
│   │       ├── Services/
│   │       │   ├── IDocumentsService.cs
│   │       │   ├── DocumentsService.cs
│   │       │   ├── IStorageService.cs
│   │       │   └── MinioStorageService.cs   # MinIO integration
│   │       ├── Program.cs
│   │       └── appsettings.json
│   └── README.md
│
├── shared-messaging/                        # Shared library
│   ├── IntegrationEvents.cs                 # Event models
│   ├── IEventBus.cs                         # Interface
│   ├── RabbitMQEventBus.cs                  # RabbitMQ implementation
│   ├── ServiceCollectionExtensions.cs       # DI registration
│   └── shared-messaging.csproj
│
└── README.md                                # This file
```

---

## 🚀 Quick Start

### Prerequisites

- **.NET 10 SDK**
- **PostgreSQL 15+**
- **MinIO** (or S3-compatible storage)
- **RabbitMQ** (CloudAMQP recommended)

### Local Development

```bash
# 1. Start dependencies (PostgreSQL + MinIO + RabbitMQ)
# Configure connection strings in appsettings.json

# 2. Run Claims Service
cd claims-service/src/ClaimsService.API
dotnet restore
dotnet run

# 3. Run Documents Service (in another terminal)
cd documents-service/src/DocumentsService.API
dotnet restore
dotnet run
```

**Services will be available at:**
- Claims API: http://localhost:5001/scalar/v1
- Documents API: http://localhost:5002/scalar/v1

---

## 📚 API Documentation

### Scalar UI (Interactive API Explorer)

Modern, interactive API documentation built into both services.

**Access:**
- **Claims Service**: http://localhost:5001/scalar/v1
- **Documents Service**: http://localhost:5002/scalar/v1

**Features:**
- ✅ Try API calls directly in browser
- ✅ Auto-generated from OpenAPI spec
- ✅ Request/response examples
- ✅ Schema documentation
- ✅ Code generation

**How to Use:**
1. Navigate to Scalar URL
2. Browse endpoints in left panel
3. Click **Test Request** on any endpoint
4. Fill in parameters
5. Click **Send** to execute

### OpenAPI Specification (JSON)

Machine-readable API specs available at:
- **Claims**: http://localhost:5001/openapi/v1.json
- **Documents**: http://localhost:5002/openapi/v1.json

Use these to:
- Generate client SDKs
- Import into Postman
- Create custom tooling

---

## 🔄 Workflows

### Workflow 1: Create Claim (JSON Only)

```
1. User submits claim
   POST /api/claims
   {
     "policyNumber": "POL-2024-001",
     "claimType": 1,
     "claimAmount": 5000,
     "incidentDate": "2024-02-15T10:00:00Z",
     "claimantName": "John Doe",
     "claimantEmail": "john@example.com",
     "description": "Vehicle collision"
   }

2. Claims Service
   ✅ Validates data
   ✅ Generates claim number (CLM-20240215-XXXX)
   ✅ Saves to database
   ✅ Publishes ClaimCreatedEvent

3. Response
   {
     "id": "guid...",
     "claimNumber": "CLM-20240215-1234",
     "status": "Submitted",
     ...
   }
```

### Workflow 2: Create Claim with Files (Event-Driven)

```
1. User submits claim with attachments
   POST /api/claims/with-files (multipart/form-data)
   - policyNumber: POL-2024-002
   - claimType: 1
   - claimAmount: 7500
   - attachments: [photo1.jpg, photo2.jpg]

2. Claims Service
   ✅ Creates claim
   ✅ Reads files into memory
   ✅ Publishes StoreDocumentRequestEvent (with file bytes)
   ✅ Returns immediately

3. RabbitMQ
   ✅ Queues events
   ✅ Routes to Documents Service

4. Documents Service
   ✅ Receives event
   ✅ Uploads files to MinIO
   ✅ Saves metadata to database
   ✅ Publishes DocumentUploadedEvent

5. Claims Service
   ✅ Receives confirmation
   ✅ Updates ClaimDocuments table
   ✅ Adds note to claim
```

### Workflow 3: Upload Document Directly

```
1. User uploads document
   POST /api/documents/upload
   - claimId: guid
   - file: invoice.pdf
   - documentType: 1 (Invoice)

2. Documents Service
   ✅ Validates file (<50MB)
   ✅ Uploads to MinIO
   ✅ Saves metadata
   ✅ Publishes DocumentUploadedEvent

3. Claims Service
   ✅ Receives event
   ✅ Stores metadata locally
   ✅ Updates claim notes
```

### Workflow 4: Download Document

```
1. User requests download URL
   GET /api/documents/{id}/download-url

2. Documents Service
   ✅ Finds document in database
   ✅ Generates presigned MinIO URL (1 hour expiry)
   ✅ Returns URL

3. User downloads
   ✅ Uses URL in browser or curl
   ✅ File downloaded directly from MinIO
   ✅ No service involvement
```

### Workflow 5: Update Claim Status

```
1. Adjuster updates claim
   PUT /api/claims/{id}
   {
     "status": 4,  // Approved
     "assignedTo": "adjuster@insurance.com"
   }

2. Claims Service
   ✅ Updates database
   ✅ Publishes ClaimStatusChangedEvent

3. Documents Service
   ✅ Receives event
   ✅ Logs status change
   ✅ Can trigger business logic (e.g., lock uploads if rejected)
```

---

## 🔔 Event Flow

### Events Overview

```
Claims Service          RabbitMQ          Documents Service
      │                    │                     │
      │  ClaimCreated     │                     │
      ├───────────────────►│────────────────────►│
      │                    │                     │
      │  StoreDocument    │                     │
      │  Request          │                     │
      ├───────────────────►│────────────────────►│
      │                    │                     │
      │                    │  DocumentUploaded  │
      │◄───────────────────│◄────────────────────┤
      │                    │                     │
      │  ClaimStatus      │                     │
      │  Changed          │                     │
      ├───────────────────►│────────────────────►│
      │                    │                     │
```

### Event Details

| Event | Publisher | Subscriber | Payload | Purpose |
|-------|-----------|------------|---------|---------|
| **ClaimCreatedEvent** | Claims | Documents | ClaimId, ClaimNumber, PolicyNumber, ClaimantInfo | Notify of new claim |
| **ClaimStatusChangedEvent** | Claims | Documents | ClaimId, OldStatus, NewStatus | Status transition |
| **StoreDocumentRequestEvent** | Claims | Documents | ClaimId, FileName, FileData (bytes) | Store file via events |
| **DocumentUploadedEvent** | Documents | Claims | DocumentId, ClaimId, FileName, Size | Confirm upload |
| **DocumentDeletedEvent** | Documents | Claims | DocumentId, ClaimId, FileName | Confirm deletion |



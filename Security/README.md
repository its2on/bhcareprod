# BHCARE Security Service

This directory contains the security services for the BHCARE application.

## Overview

The security service provides:

- JWT Authentication and Authorization
- User management with secure password storage
- Data encryption/decryption services
- Rate limiting and IP-based security
- Security logging and intrusion detection

## Files

- `security_server.py` - Main security service with all features
- `Security.py` - Simplified client that connects to the main service

## Setup and Configuration

### Prerequisites

```bash
pip install fastapi uvicorn pyjwt bcrypt python-multipart cryptography
```

### Environment Variables

For production, set these environment variables:

- `BHCARE_SECRET_KEY` - Secret key for JWT signing
- `BHCARE_SECURITY_URL` - URL of the security service (for client)
- `BHCARE_ADMIN_EMAIL` - Admin user email address
- `BHCARE_ADMIN_PASSWORD` - Admin user password

### Secure Admin Credentials

For added security, admin credentials are never hardcoded in the source code. They can be configured in several ways:

1. **Environment Variables** (recommended for production)
   ```
   set BHCARE_ADMIN_EMAIL=your-admin-email@example.com
   set BHCARE_ADMIN_PASSWORD=your-secure-password
   ```

2. **Credentials File** (for development)
   ```
   # Create from the provided template
   cp admin_credentials.env.template admin_credentials.env
   # Edit with your secure credentials
   notepad admin_credentials.env
   ```

3. **Generated at Runtime** (fallback)
   - If no configuration is found, a warning will be shown
   - A random password will be generated and displayed in the logs
   - This is only suitable for development

### Running the Security Service

```bash
# Start the main security service
uvicorn Security.security_server:app --reload --host 0.0.0.0 --port 8000

# To run the client (if needed separately)
uvicorn Security.Security:app --reload --host 0.0.0.0 --port 8001
```

## API Usage

### Authentication

```bash
# Request a token
curl -X POST "http://localhost:8000/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "healthcenterbaesa@gmail.com", "password": "Admin123!"}'

# Response:
# {"token": "eyJ0eXA...", "token_type": "bearer"}
```

### Verifying a Token

```bash
curl -X GET "http://localhost:8000/verify" \
  -H "Authorization: Bearer eyJ0eXA..."

# Response:
# {"user": "healthcenterbaesa@gmail.com", "role": "admin", "status": "valid"}
```

### Encrypting Data

```bash
curl -X POST "http://localhost:8000/encrypt" \
  -H "Authorization: Bearer eyJ0eXA..." \
  -H "Content-Type: application/json" \
  -d '{"text": "Sensitive patient data"}'

# Response:
# {"encrypted_text": "gAAAAABk..."}
```

### Decrypting Data

```bash
curl -X POST "http://localhost:8000/decrypt" \
  -H "Authorization: Bearer eyJ0eXA..." \
  -H "Content-Type: application/json" \
  -d '{"encrypted_text": "gAAAAABk..."}'

# Response:
# {"decrypted_text": "Sensitive patient data"}
```

### Accessing Security Logs (Admin Only)

```bash
curl -X GET "http://localhost:8000/logs" \
  -H "Authorization: Bearer eyJ0eXA..."

# Response:
# {"logs": ["2023-10-25 10:15:22 - INFO - Request: GET /health from IP: 127.0.0.1", ...]}
```

### Health Check

```bash
curl -X GET "http://localhost:8000/health"

# Response:
# {"status": "healthy", "timestamp": "2023-10-25T10:15:22.123456"}
```

## Security Features

1. **Secure Key Management**
   - Environment variable based
   - File-based fallback with auto-generation
   - Persistent encryption key storage

2. **Rate Limiting**
   - Configurable request limits
   - IP-based throttling

3. **Brute Force Protection**
   - Failed login tracking
   - IP blocking after multiple failures
   - Configurable lockout period

4. **User Management**
   - JSON file-based user storage (can be extended to database)
   - Password hashing with bcrypt
   - Role-based access control

5. **Comprehensive Logging**
   - Request tracking
   - Error monitoring
   - Security incident logging

6. **OAuth2 Implementation**
   - Bearer token authentication
   - Standard authorization header support
   - Client implementation available

## Production Recommendations

1. Use environment variables for all secrets
2. Deploy behind HTTPS-enabled reverse proxy
3. Restrict CORS and trusted hosts settings
4. Set up persistent database storage for users
5. Implement regular key rotation
6. Configure appropriate logging and monitoring

## Integration with Main Application

The security service is designed to be used as a standalone service or integrated directly.

### Direct Integration

```python
from fastapi import Depends
from Security.security_server import get_current_user

@app.get("/protected-endpoint")
def protected_endpoint(current_user: dict = Depends(get_current_user)):
    return {"message": f"Hello, {current_user['username']}!"}
```

### Client Integration

```python
from fastapi import Depends
from Security.Security import get_current_user

@app.get("/protected-endpoint")
def protected_endpoint(current_user: dict = Depends(get_current_user)):
    return {"message": f"Hello, {current_user['username']}!"}
```

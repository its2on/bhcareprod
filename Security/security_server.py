from fastapi import FastAPI, HTTPException, Request, Depends, status
import jwt
import datetime
import bcrypt
import logging
import os
import json
import time
import secrets
from typing import Dict, Optional
from cryptography.fernet import Fernet
from fastapi.security import OAuth2PasswordBearer
from pydantic import BaseModel, EmailStr, Field, validator
from fastapi.middleware.trustedhost import TrustedHostMiddleware
from fastapi.middleware.cors import CORSMiddleware

# Models for request validation
class UserCredentials(BaseModel):
    username: str = Field(..., min_length=3, max_length=50)
    password: str = Field(..., min_length=8)
    ip: Optional[str] = None

class EncryptRequest(BaseModel):
    text: str = Field(..., min_length=1)

class DecryptRequest(BaseModel):
    encrypted_text: str = Field(..., min_length=1)

# Create FastAPI app
app = FastAPI(title="BHCARE Security Service", 
              description="Security services for BHCARE application",
              version="1.0.0")

# Add security middleware
app.add_middleware(TrustedHostMiddleware, allowed_hosts=["localhost", "127.0.0.1", "*"])
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Replace with specific origins in production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Configure OAuth2
oauth2_scheme = OAuth2PasswordBearer(tokenUrl="authenticate")

# Setup secure key management
def get_secret_key():
    env_key = os.getenv("BHCARE_SECRET_KEY")
    if env_key:
        return env_key
    
    # Fallback to file-based key (for development)
    key_file = "secret_key.txt"
    if os.path.exists(key_file):
        with open(key_file, "r") as f:
            return f.read().strip()
    else:
        # Generate and save a key for development (not for production)
        import secrets
        new_key = secrets.token_hex(32)
        with open(key_file, "w") as f:
            f.write(new_key)
        return new_key

# Setup encryption key management
def get_encryption_key():
    key_file = "encryption_key.key"
    if os.path.exists(key_file):
        with open(key_file, "rb") as f:
            return f.read()
    else:
        # Generate and save a key
        key = Fernet.generate_key()
        with open(key_file, "wb") as f:
            f.write(key)
        return key

# Initialize keys
SECRET_KEY = get_secret_key()
ENCRYPTION_KEY = get_encryption_key()
cipher_suite = Fernet(ENCRYPTION_KEY)

# Setup enhanced logging
log_dir = "logs"
os.makedirs(log_dir, exist_ok=True)
logging.basicConfig(
    filename=os.path.join(log_dir, "security_logs.txt"),
    level=logging.INFO,
    format="%(asctime)s - %(levelname)s - %(message)s"
)

# User management
USER_DB_FILE = "user_credentials.json"

def load_users():
    if os.path.exists(USER_DB_FILE):
        try:
            with open(USER_DB_FILE, "r") as f:
                return json.load(f)
        except json.JSONDecodeError:
            logging.error(f"Failed to parse {USER_DB_FILE}")
            return {}
    return {}

def save_users(users_dict):
    with open(USER_DB_FILE, "w") as f:
        json.dump(users_dict, f)

# Get admin credentials from environment variables
def get_default_admin_credentials():
    # Get credentials from environment variables
    admin_email = os.environ.get("BHCARE_ADMIN_EMAIL")
    admin_password = os.environ.get("BHCARE_ADMIN_PASSWORD")
    
    # If not set, try to read from secure file
    if not admin_email or not admin_password:
        creds_file = "admin_credentials.env"
        if os.path.exists(creds_file):
            try:
                with open(creds_file, "r") as f:
                    for line in f:
                        if "=" in line:
                            key, value = line.strip().split("=", 1)
                            if key == "ADMIN_EMAIL":
                                admin_email = value
                            elif key == "ADMIN_PASSWORD":
                                admin_password = value
            except Exception as e:
                logging.error(f"Error reading admin credentials file: {str(e)}")
    
    # Use default values only if nothing else is available (development only)
    if not admin_email:
        admin_email = "admin@example.com"  # Placeholder - should be overridden
        logging.warning("Using default admin email. Set BHCARE_ADMIN_EMAIL environment variable for security.")
    
    if not admin_password:
        admin_password = "ChangeMe!" + secrets.token_hex(4)  # Generate random password
        logging.warning(f"Using generated admin password: {admin_password}")
        logging.warning("Set BHCARE_ADMIN_PASSWORD environment variable for security.")
    
    return admin_email, admin_password

# Initialize with admin if no users exist
users = load_users()
if not users:
    admin_email, admin_password = get_default_admin_credentials()
    users = {
        admin_email: {
            "password": bcrypt.hashpw(admin_password.encode(), bcrypt.gensalt()).decode(),
            "role": "admin",
            "created_at": datetime.datetime.now().isoformat()
        }
    }
    save_users(users)
    logging.info(f"Created initial admin user with email: {admin_email}")

# Rate limiting
class RateLimiter:
    def __init__(self, max_requests=5, time_window=60):
        self.max_requests = max_requests
        self.time_window = time_window  # seconds
        self.requests = {}

    def is_allowed(self, ip):
        current_time = time.time()
        if ip not in self.requests:
            self.requests[ip] = []
        
        # Clean old requests
        self.requests[ip] = [t for t in self.requests[ip] if current_time - t < self.time_window]
        
        # Check if under limit
        if len(self.requests[ip]) < self.max_requests:
            self.requests[ip].append(current_time)
            return True
        return False

rate_limiter = RateLimiter()

# Failed login tracking with IP blocking
class FailedLoginTracker:
    def __init__(self, max_attempts=5, block_time=300):  # 5 attempts, 5 min block
        self.max_attempts = max_attempts
        self.block_time = block_time
        self.failed_attempts = {}
        self.blocked_ips = {}
    
    def record_failure(self, ip):
        current_time = time.time()
        
        # Check if IP is blocked
        if ip in self.blocked_ips:
            block_until = self.blocked_ips[ip]
            if current_time < block_until:
                return False  # Still blocked
            else:
                # Unblock if time has passed
                del self.blocked_ips[ip]
                self.failed_attempts[ip] = []
        
        # Record failure
        if ip not in self.failed_attempts:
            self.failed_attempts[ip] = []
        
        self.failed_attempts[ip].append(current_time)
        
        # Clean old attempts
        self.failed_attempts[ip] = [t for t in self.failed_attempts[ip] 
                                  if current_time - t < 3600]  # 1 hour window
        
        # Block if too many attempts
        if len(self.failed_attempts[ip]) >= self.max_attempts:
            self.blocked_ips[ip] = current_time + self.block_time
            logging.warning(f"IP {ip} blocked for {self.block_time} seconds due to too many failed logins")
            return False
        
        return True
    
    def is_blocked(self, ip):
        current_time = time.time()
        if ip in self.blocked_ips:
            if current_time < self.blocked_ips[ip]:
                return True
            else:
                del self.blocked_ips[ip]
        return False

login_tracker = FailedLoginTracker()

# Middleware for request logging and IP checks
@app.middleware("http")
async def log_requests(request: Request, call_next):
    start_time = time.time()
    
    # Get client IP
    client_ip = request.client.host if request.client else "unknown"
    
    # Check if IP is blocked
    if login_tracker.is_blocked(client_ip):
        return HTTPException(
            status_code=status.HTTP_429_TOO_MANY_REQUESTS,
            detail="Too many failed attempts. Please try again later."
        )
    
    # Apply rate limiting
    if not rate_limiter.is_allowed(client_ip):
        logging.warning(f"Rate limit exceeded for IP: {client_ip}")
        return HTTPException(
            status_code=status.HTTP_429_TOO_MANY_REQUESTS,
            detail="Rate limit exceeded. Please try again later."
        )
    
    # Log the request
    path = request.url.path
    method = request.method
    logging.info(f"Request: {method} {path} from IP: {client_ip}")
    
    response = await call_next(request)
    
    # Calculate and log duration
    duration = time.time() - start_time
    status_code = response.status_code
    logging.info(f"Response: {status_code} for {method} {path} - took {duration:.3f}s")
    
    return response

# Authentication endpoint with improved security
@app.post("/authenticate")
async def authenticate(user: UserCredentials, request: Request):
    client_ip = request.client.host if request.client else "unknown"
    
    # Update request with IP if not provided
    if not user.ip:
        user.ip = client_ip
    
    # Check if IP is allowed
    if login_tracker.is_blocked(client_ip):
        raise HTTPException(
            status_code=status.HTTP_429_TOO_MANY_REQUESTS,
            detail="Too many failed login attempts. Please try again later."
        )
    
    # Load latest users
    current_users = load_users()
    
    # Check credentials
    if user.username in current_users and bcrypt.checkpw(
        user.password.encode(), 
        current_users[user.username]["password"].encode()
    ):
        # Generate secure token with more claims
        token_data = {
            "sub": user.username,
            "role": current_users[user.username].get("role", "user"),
            "iat": datetime.datetime.utcnow(),
            "exp": datetime.datetime.utcnow() + datetime.timedelta(hours=1)
        }
        token = jwt.encode(token_data, SECRET_KEY, algorithm="HS256")
        
        # Log successful login
        logging.info(f"Successful login for user: {user.username} from IP: {client_ip}")
        
        return {"token": token, "token_type": "bearer"}
    
    # Record failed attempt
    login_tracker.record_failure(client_ip)
    logging.warning(f"Failed login attempt for user: {user.username} from IP: {client_ip}")
    
    # Don't reveal if username exists or not
    raise HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Invalid username or password",
        headers={"WWW-Authenticate": "Bearer"},
    )

# Token verification with dependency
async def get_current_user(token: str = Depends(oauth2_scheme)):
    try:
        payload = jwt.decode(token, SECRET_KEY, algorithms=["HS256"])
        username = payload.get("sub")
        if username is None:
            raise HTTPException(status_code=401, detail="Invalid token")
        return {"username": username, "role": payload.get("role", "user")}
    except jwt.ExpiredSignatureError:
        raise HTTPException(status_code=401, detail="Token expired")
    except jwt.InvalidTokenError:
        raise HTTPException(status_code=401, detail="Invalid token")

# Protected verification endpoint
@app.get("/verify")
async def verify_token(current_user: dict = Depends(get_current_user)):
    return {"user": current_user["username"], "role": current_user["role"], "status": "valid"}

# Secure data encryption
@app.post("/encrypt")
async def encrypt_data(data: EncryptRequest, current_user: dict = Depends(get_current_user)):
    try:
        plaintext = data.text
    encrypted_text = cipher_suite.encrypt(plaintext.encode()).decode()
    return {"encrypted_text": encrypted_text}
    except Exception as e:
        logging.error(f"Encryption error: {str(e)}")
        raise HTTPException(status_code=500, detail="Encryption failed")

# Secure data decryption
@app.post("/decrypt")
async def decrypt_data(data: DecryptRequest, current_user: dict = Depends(get_current_user)):
    try:
        encrypted_text = data.encrypted_text
    decrypted_text = cipher_suite.decrypt(encrypted_text.encode()).decode()
    return {"decrypted_text": decrypted_text}
    except Exception as e:
        logging.error(f"Decryption error: {str(e)}")
        raise HTTPException(status_code=500, detail="Decryption failed. Invalid data or key.")

# Secure log access (admin only)
@app.get("/logs")
async def get_logs(current_user: dict = Depends(get_current_user)):
    # Check if user has admin role
    if current_user.get("role") != "admin":
        raise HTTPException(status_code=403, detail="Insufficient permissions")
    
    log_file = os.path.join(log_dir, "security_logs.txt")
    if os.path.exists(log_file):
        # Get the last 100 lines only (for security and performance)
        with open(log_file, "r") as f:
            lines = f.readlines()
            last_logs = lines[-100:] if len(lines) > 100 else lines
        return {"logs": last_logs}
    return {"logs": "No logs available"}

# Health check endpoint
@app.get("/health")
async def health_check():
    return {"status": "healthy", "timestamp": datetime.datetime.utcnow().isoformat()}

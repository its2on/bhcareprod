"""
Basic JWT authentication module for BHCARE
This is a simplified client for the main security_server.py service.
"""
from fastapi import FastAPI, HTTPException, Depends, status
from fastapi.security import OAuth2PasswordBearer
import jwt
import datetime
import os
import json
import requests
import logging
from pydantic import BaseModel

# Create models for validation
class UserLogin(BaseModel):
    username: str
    password: str

# Setup basic logging
logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")

# Initialize FastAPI app
app = FastAPI(title="BHCARE Authentication Client", 
              description="Simple authentication client for BHCARE application")

# Get security settings from environment or file
def get_security_settings():
    # Try to get from environment
    security_url = os.environ.get("BHCARE_SECURITY_URL")
    secret_key = os.environ.get("BHCARE_SECRET_KEY")
    
    # If not in environment, try to read from configuration files
    if not security_url or not secret_key:
        # Check for settings.json file
        settings_file = "security_settings.json"
        if os.path.exists(settings_file):
            try:
                with open(settings_file, "r") as f:
                    settings = json.load(f)
                    if not security_url and "security_url" in settings:
                        security_url = settings["security_url"]
                    if not secret_key and "secret_key" in settings:
                        secret_key = settings["secret_key"]
            except Exception as e:
                logging.error(f"Error reading settings file: {str(e)}")
        
        # Check for secret key file as fallback
        if not secret_key:
            try:
                key_file = "secret_key.txt"
                if os.path.exists(key_file):
                    with open(key_file, "r") as f:
                        secret_key = f.read().strip()
            except Exception as e:
                logging.error(f"Error loading secret key file: {str(e)}")
    
    # Default security URL if not specified
    if not security_url:
        security_url = "http://localhost:8000"
        logging.warning(f"Using default security URL: {security_url}")
        
    # Generate a temporary secret key if none found (development only)
    if not secret_key:
        import secrets
        secret_key = secrets.token_hex(32)
        logging.warning("Using a temporary secret key. This is NOT secure for production!")
        logging.warning("Set BHCARE_SECRET_KEY environment variable for production use.")
    
    return {
        "security_url": security_url,
        "secret_key": secret_key
    }

# Get security settings
settings = get_security_settings()
SECURITY_URL = settings["security_url"]
SECRET_KEY = settings["secret_key"]

# OAuth2 setup for token handling
oauth2_scheme = OAuth2PasswordBearer(tokenUrl="authenticate")

# Authentication endpoint that communicates with main security service
@app.post("/authenticate")
async def authenticate(user: UserLogin):
    try:
        # Try to authenticate against the main security service
        response = requests.post(
            f"{SECURITY_URL}/authenticate",
            json={"username": user.username, "password": user.password}
        )
        
        if response.status_code == 200:
            return response.json()
        else:
            # Just pass through the error from the security service
            error_detail = "Authentication failed"
            try:
                error_data = response.json()
                if "detail" in error_data:
                    error_detail = error_data["detail"]
            except:
                pass
            
            raise HTTPException(
                status_code=response.status_code,
                detail=error_detail
            )
    except requests.RequestException as e:
        # Handle connection errors to the security service
        logging.error(f"Security service connection error: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="Authentication service unavailable"
        )

# Token verification against local key
async def get_current_user(token: str = Depends(oauth2_scheme)):
    try:
        # First try local verification
        payload = jwt.decode(token, SECRET_KEY, algorithms=["HS256"])
        username = payload.get("sub")
        if username is None:
            raise HTTPException(status_code=401, detail="Invalid token")
        return {"username": username, "role": payload.get("role", "user")}
    except jwt.ExpiredSignatureError:
        raise HTTPException(status_code=401, detail="Token expired")
    except jwt.InvalidTokenError:
        # If local verification fails, try against the security service
        try:
            response = requests.get(
                f"{SECURITY_URL}/verify",
                headers={"Authorization": f"Bearer {token}"}
            )
            
            if response.status_code == 200:
                return response.json()
            else:
                raise HTTPException(status_code=401, detail="Invalid token")
        except requests.RequestException:
            raise HTTPException(status_code=401, detail="Token verification failed")

# Verification endpoint
@app.get("/verify")
async def verify_token(current_user: dict = Depends(get_current_user)):
    return {"user": current_user.get("username"), "role": current_user.get("role"), "status": "valid"}

# Health check endpoint
@app.get("/health")
async def health_check():
    return {"status": "healthy", "timestamp": datetime.datetime.utcnow().isoformat()}

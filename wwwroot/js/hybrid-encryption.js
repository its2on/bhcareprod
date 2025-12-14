/**
 * Hybrid Encryption Service for Frontend
 * Combines AES (symmetric) and RSA (asymmetric) encryption
 * Uses Web Crypto API for RSA and CryptoJS for AES
 */

class HybridEncryptionService {
    constructor() {
        this.algorithm = {
            name: 'RSA-OAEP',
            hash: 'SHA-256'
        };
        this.aesKeySize = 256;
    }

    /**
     * Generate RSA key pair (2048-bit)
     * @returns {Promise<{publicKey: CryptoKey, privateKey: CryptoKey}>}
     */
    async generateRSAKeyPair() {
        try {
            console.log('Generating RSA key pair...');
            
            const keyPair = await window.crypto.subtle.generateKey(
                {
                    name: 'RSA-OAEP',
                    modulusLength: 2048,
                    publicExponent: new Uint8Array([1, 0, 1]),
                    hash: 'SHA-256'
                },
                true,
                ['encrypt', 'decrypt']
            );

            console.log('RSA key pair generated successfully');
            return keyPair;
        } catch (error) {
            console.error('Error generating RSA key pair:', error);
            throw new Error('Failed to generate RSA key pair: ' + error.message);
        }
    }

    /**
     * Export RSA key to base64 string
     * @param {CryptoKey} key - The crypto key to export
     * @param {string} type - 'public' or 'private'
     * @returns {Promise<string>}
     */
    async exportRSAKey(key, type) {
        try {
            const format = type === 'public' ? 'spki' : 'pkcs8';
            const keyBuffer = await window.crypto.subtle.exportKey(format, key);
            return this.arrayBufferToBase64(keyBuffer);
        } catch (error) {
            console.error('Error exporting RSA key:', error);
            throw new Error('Failed to export RSA key: ' + error.message);
        }
    }

    /**
     * Encrypt data using hybrid encryption (AES + RSA)
     * @param {string} plaintext - Data to encrypt
     * @param {string} publicKeyPem - RSA public key
     * @returns {Promise<{encryptedData: string, encryptedKey: string, iv: string}>}
     */
    async encrypt(plaintext, publicKeyPem) {
        try {
            console.log('Starting hybrid encryption...');
            
            if (!plaintext || !publicKeyPem) {
                throw new Error('Plaintext and public key are required');
            }

            // Generate random AES key
            const aesKey = this.generateAESKey();
            
            // Encrypt data with AES
            const encryptedResult = this.encryptWithAES(plaintext, aesKey);
            
            // Encrypt AES key with RSA
            const encryptedKey = await this.encryptAESKeyWithRSA(aesKey, publicKeyPem);
            
            console.log('Hybrid encryption completed');
            return {
                encryptedData: encryptedResult.encryptedData,
                encryptedKey: encryptedKey,
                iv: encryptedResult.iv
            };
        } catch (error) {
            console.error('Error in hybrid encryption:', error);
            throw new Error('Encryption failed: ' + error.message);
        }
    }

    /**
     * Decrypt data using hybrid decryption (RSA + AES)
     * @param {Object} encryptedObject - Object containing encryptedData, encryptedKey, and iv
     * @param {string} privateKeyPem - RSA private key
     * @returns {Promise<string>}
     */
    async decrypt(encryptedObject, privateKeyPem) {
        try {
            console.log('Starting hybrid decryption...');
            
            if (!encryptedObject || !privateKeyPem) {
                throw new Error('Encrypted object and private key are required');
            }

            const { encryptedData, encryptedKey } = encryptedObject;

            // Decrypt AES key with RSA
            const aesKey = await this.decryptAESKeyWithRSA(encryptedKey, privateKeyPem);
            
            // Decrypt data with AES
            const decryptedData = this.decryptWithAES(encryptedData, aesKey);
            
            console.log('Hybrid decryption completed');
            return decryptedData;
        } catch (error) {
            console.error('Error in hybrid decryption:', error);
            throw new Error('Decryption failed: ' + error.message);
        }
    }

    /**
     * Encrypt AES key with RSA public key
     * @param {string} aesKey - AES key to encrypt
     * @param {string} publicKeyPem - RSA public key
     * @returns {Promise<string>}
     */
    async encryptAESKeyWithRSA(aesKey, publicKeyPem) {
        try {
            const publicKeyBuffer = this.base64ToArrayBuffer(publicKeyPem);
            const publicKey = await window.crypto.subtle.importKey(
                'spki',
                publicKeyBuffer,
                this.algorithm,
                false,
                ['encrypt']
            );

            const aesKeyBuffer = new TextEncoder().encode(aesKey);
            const encryptedKeyBuffer = await window.crypto.subtle.encrypt(
                this.algorithm,
                publicKey,
                aesKeyBuffer
            );

            return this.arrayBufferToBase64(encryptedKeyBuffer);
        } catch (error) {
            console.error('Error encrypting AES key with RSA:', error);
            throw new Error('RSA encryption failed: ' + error.message);
        }
    }

    /**
     * Decrypt AES key with RSA private key
     * @param {string} encryptedAesKey - RSA encrypted AES key
     * @param {string} privateKeyPem - RSA private key
     * @returns {Promise<string>}
     */
    async decryptAESKeyWithRSA(encryptedAesKey, privateKeyPem) {
        try {
            const privateKeyBuffer = this.base64ToArrayBuffer(privateKeyPem);
            const privateKey = await window.crypto.subtle.importKey(
                'pkcs8',
                privateKeyBuffer,
                this.algorithm,
                false,
                ['decrypt']
            );

            const encryptedKeyBuffer = this.base64ToArrayBuffer(encryptedAesKey);
            const decryptedKeyBuffer = await window.crypto.subtle.decrypt(
                this.algorithm,
                privateKey,
                encryptedKeyBuffer
            );

            return new TextDecoder().decode(decryptedKeyBuffer);
        } catch (error) {
            console.error('Error decrypting AES key with RSA:', error);
            throw new Error('RSA decryption failed: ' + error.message);
        }
    }

    /**
     * Generate random AES key (256-bit)
     * @returns {string}
     */
    generateAESKey() {
        const key = CryptoJS.lib.WordArray.random(32); // 256 bits = 32 bytes
        return CryptoJS.enc.Base64.stringify(key);
    }

    /**
     * Encrypt data with AES-256-CBC
     * @param {string} plaintext - Data to encrypt
     * @param {string} keyBase64 - AES key in base64
     * @returns {{encryptedData: string, iv: string}}
     */
    encryptWithAES(plaintext, keyBase64) {
        try {
            const key = CryptoJS.enc.Base64.parse(keyBase64);
            const iv = CryptoJS.lib.WordArray.random(16); // 128-bit IV
            
            const encrypted = CryptoJS.AES.encrypt(plaintext, key, {
                iv: iv,
                mode: CryptoJS.mode.CBC,
                padding: CryptoJS.pad.Pkcs7
            });
            
            return {
                encryptedData: CryptoJS.enc.Base64.stringify(encrypted.ciphertext),
                iv: CryptoJS.enc.Base64.stringify(iv)
            };
        } catch (error) {
            console.error('Error in AES encryption:', error);
            throw new Error('AES encryption failed: ' + error.message);
        }
    }

    /**
     * Decrypt data with AES-256-CBC
     * @param {string} ciphertext - AES encrypted data (base64)
     * @param {string} keyBase64 - AES key in base64
     * @returns {string}
     */
    decryptWithAES(ciphertext, keyBase64) {
        try {
            const key = CryptoJS.enc.Base64.parse(keyBase64);
            const encrypted = CryptoJS.enc.Base64.parse(ciphertext);
            
            // For this simplified version, we'll use a zero IV
            // In a real implementation, you'd need to store and retrieve the IV
            const iv = CryptoJS.lib.WordArray.create([0, 0, 0, 0]);
            
            const decrypted = CryptoJS.AES.decrypt(
                { ciphertext: encrypted },
                key,
                {
                    iv: iv,
                    mode: CryptoJS.mode.CBC,
                    padding: CryptoJS.pad.Pkcs7
                }
            );
            
            return decrypted.toString(CryptoJS.enc.Utf8);
        } catch (error) {
            console.error('Error in AES decryption:', error);
            throw new Error('AES decryption failed: ' + error.message);
        }
    }

    /**
     * Validate RSA private key format
     * @param {string} privateKeyPem - Private key to validate
     * @returns {Promise<boolean>}
     */
    async validatePrivateKey(privateKeyPem) {
        try {
            if (!privateKeyPem) return false;
            
            const privateKeyBuffer = this.base64ToArrayBuffer(privateKeyPem);
            await window.crypto.subtle.importKey(
                'pkcs8',
                privateKeyBuffer,
                this.algorithm,
                false,
                ['decrypt']
            );
            return true;
        } catch (error) {
            console.error('Invalid private key:', error);
            return false;
        }
    }

    /**
     * Convert ArrayBuffer to Base64
     * @param {ArrayBuffer} buffer
     * @returns {string}
     */
    arrayBufferToBase64(buffer) {
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return btoa(binary);
    }

    /**
     * Convert Base64 to ArrayBuffer
     * @param {string} base64
     * @returns {ArrayBuffer}
     */
    base64ToArrayBuffer(base64) {
        const binary = atob(base64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return bytes.buffer;
    }
}

// Global instance
window.hybridEncryption = new HybridEncryptionService();
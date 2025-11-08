/**
 * Philippine ID Parser Module
 * 
 * Detects ID type and extracts relevant information from OCR text
 * Supports: Driver's License, National ID (PhilSys), PhilHealth, UMID, Postal ID, Student ID
 * 
 * @version 1.0.0
 * @author BHCARE Development Team
 */

(function(global) {
    'use strict';

    /**
     * ID Type Detection Patterns
     * Each pattern includes keywords to identify the ID type
     */
    const ID_TYPE_PATTERNS = {
        driversLicense: {
            name: "Driver's License",
            keywords: [
                /DRIVER'?S?\s*LICENSE/i,
                /LAND\s*TRANSPORTATION\s*OFFICE/i,
                /\bLTO\b/i,
                /DEPARTMENT\s*OF\s*TRANSPORTATION/i,
                /PROFESSIONAL\s*DRIVER/i,
                /NON[\-\s]?PROFESSIONAL\s*DRIVER/i
            ]
        },
        nationalId: {
            name: "National ID (PhilSys)",
            keywords: [
                /PHILSYS/i,
                /PHILIPPINE\s*IDENTIFICATION\s*SYSTEM/i,
                /\bPSN\b/i,
                /NATIONAL\s*ID/i,
                /PHILIPPINE\s*NATIONAL\s*ID/i
            ]
        },
        philhealth: {
            name: "PhilHealth ID",
            keywords: [
                /PHILHEALTH/i,
                /PHIL[\-\s]?HEALTH/i,
                /PHILIPPINE\s*HEALTH\s*INSURANCE/i,
                /MEMBER\s*ID/i,
                /PHILHEALTH\s*NO/i
            ]
        },
        umid: {
            name: "UMID",
            keywords: [
                /\bUMID\b/i,
                /UNIFIED\s*MULTI[\-\s]?PURPOSE\s*ID/i,
                /\bGSIS\b/i,
                /\bSSS\b/i,
                /\bCRN\b/i,
                /SOCIAL\s*SECURITY/i
            ]
        },
        postalId: {
            name: "Postal ID",
            keywords: [
                /POSTAL\s*ID/i,
                /PHILIPPINE\s*POSTAL/i,
                /PHLPOST/i,
                /POST\s*OFFICE/i
            ]
        },
        votersId: {
            name: "Voter's ID",
            keywords: [
                /VOTER'?S?\s*ID/i,
                /VOTER'?S?\s*IDENTIFICATION/i,
                /COMELEC/i,
                /COMMISSION\s*ON\s*ELECTIONS/i
            ]
        },
        studentId: {
            name: "Student ID",
            keywords: [
                /STUDENT\s*ID/i,
                /STUDENT\s*IDENTIFICATION/i,
                /UNIVERSITY/i,
                /COLLEGE/i,
                /\bSCHOOL\b/i,
                /ACADEMIC/i
            ]
        }
    };

    /**
     * Detect the ID type from OCR text
     * @param {string} text - Raw OCR text
     * @returns {string} - Detected ID type name or "Unknown"
     */
    function detectIdType(text) {
        const upperText = text.toUpperCase();
        
        // Check each ID type pattern
        for (const [key, idType] of Object.entries(ID_TYPE_PATTERNS)) {
            for (const pattern of idType.keywords) {
                if (pattern.test(upperText)) {
                    console.log(`ID Type detected: ${idType.name}`);
                    return idType.name;
                }
            }
        }
        
        console.log('ID Type: Unknown - using generic parsing');
        return 'Unknown';
    }

    /**
     * Validate if extracted value is a real name (not a label)
     * @param {string} value - Extracted value
     * @returns {boolean}
     */
    function isValidNameValue(value) {
        if (!value) return false;
        const upper = value.toUpperCase().trim();
        
        // Skip if it's just a label phrase
        const labelPhrases = [
            'GIVEN NAME', 'GIVEN NAMES', 'FIRST NAME', 'LAST NAME', 'MIDDLE NAME',
            'SURNAME', 'APELYIDO', 'MGA PANGALAN', 'GITNANG APELYIDO',
            'FAMILY NAME', 'UNANG PANGALAN', 'NAME', 'NAMES'
        ];
        
        if (labelPhrases.some(phrase => upper === phrase || upper === phrase + 'S')) {
            return false;
        }
        
        // Skip if it contains period followed by space (multiple labels)
        if (value.includes('. ')) {
            return false;
        }
        
        // Must have at least 2 characters
        return value.length >= 2;
    }

    /**
     * Parse name fields from OCR text
     * Handles multiple formats: labeled, comma-separated, etc.
     * @param {string} text - Raw OCR text
     * @param {string[]} lines - Text split by lines
     * @returns {object} - {firstName, middleName, lastName}
     */
    function parseName(text, lines) {
        const result = {
            firstName: null,
            middleName: null,
            lastName: null
        };

        // Pattern definitions with support for English and Filipino labels
        const surnamePattern = /(SURNAME|APELYIDO|LAST\s*NAME|FAMILY\s*NAME)[:\-\s\/]+([A-Z][A-Z\s]+?)(?:\n|$)/i;
        const givenNamePattern = /(GIVEN\s*NAME|GIVEN\s*NAMES|MGA\s*PANGALAN|FIRST\s*NAME|UNANG\s*PANGALAN)[:\-\s\/]+([A-Z][A-Z\s]+?)(?:\n|$)/i;
        const middleNamePattern = /(MIDDLE\s*NAME|GITNANG\s*APELYIDO)[:\-\s\/]+([A-Z][A-Z\s]+?)(?:\n|$)/i;
        const nameCommaPattern = /^([A-Z]{2,}),\s*([A-Z\s]+?)(?:\s+([A-Z]{2,}(?:\s+[A-Z]{2,})?))?$/;
        const fullNameCommaPattern = /^([A-Z]{2,}),\s*([A-Z\s]+),\s*([A-Z\s]+)$/;

        // Process each line
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const upperLine = line.toUpperCase();
            const nextLine = i + 1 < lines.length ? lines[i + 1] : '';
            
            // Skip label header lines
            if (upperLine.match(/LAST\s*NAME.*FIRST\s*NAME.*MIDDLE\s*NAME/i) ||
                upperLine.match(/APELYIDO.*PANGALAN.*GITNANG/i)) {
                console.log('Skipping label header line:', line);
                continue;
            }
            
            // CHECK FOR MULTI-LINE FORMAT (National ID style)
            // Format: "Mga Pangalan/Given Names" on one line, "RHYLLE LANDER" on next line
            
            // Check if current line is a given name label (and value is on next line)
            if (!result.firstName && upperLine.match(/^(MGA\s*PANGALAN|GIVEN\s*NAME|GIVEN\s*NAMES|FIRST\s*NAME|UNANG\s*PANGALAN)[\s\/]/i)) {
                if (nextLine && isValidNameValue(nextLine) && !nextLine.match(/\//)) {
                    result.firstName = nextLine.trim();
                    console.log('Found first name (multi-line):', result.firstName);
                }
            }
            
            // Check if current line is a surname label (and value is on next line)
            if (!result.lastName && upperLine.match(/^(APELYIDO|SURNAME|LAST\s*NAME|FAMILY\s*NAME)[\s\/]/i)) {
                if (nextLine && isValidNameValue(nextLine) && !nextLine.match(/\//)) {
                    result.lastName = nextLine.trim();
                    console.log('Found last name (multi-line):', result.lastName);
                }
            }
            
            // Check if current line is a middle name label (and value is on next line)
            if (!result.middleName && upperLine.match(/^(GITNANG\s*APELYIDO|MIDDLE\s*NAME)[\s\/]/i)) {
                if (nextLine && isValidNameValue(nextLine) && !nextLine.match(/\//)) {
                    result.middleName = nextLine.trim();
                    console.log('Found middle name (multi-line):', result.middleName);
                }
            }
            
            // CHECK FOR SAME-LINE FORMAT (Driver's License, older IDs)
            // Format: "SURNAME: LOPEZ"
            
            // Check for labeled surname
            if (!result.lastName) {
                const surnameMatch = upperLine.match(surnamePattern);
                if (surnameMatch && isValidNameValue(surnameMatch[2])) {
                    result.lastName = surnameMatch[2].trim();
                    console.log('Found surname:', result.lastName);
                }
            }
            
            // Check for labeled given name
            if (!result.firstName) {
                const givenMatch = upperLine.match(givenNamePattern);
                if (givenMatch && isValidNameValue(givenMatch[2])) {
                    result.firstName = givenMatch[2].trim();
                    console.log('Found given name:', result.firstName);
                }
            }
            
            // Check for labeled middle name
            if (!result.middleName) {
                const middleMatch = upperLine.match(middleNamePattern);
                if (middleMatch && isValidNameValue(middleMatch[2])) {
                    result.middleName = middleMatch[2].trim();
                    console.log('Found middle name:', result.middleName);
                }
            }
            
            // Check for "Last Name, First Name Middle Name" format (Driver's License)
            if (!result.lastName && !result.firstName) {
                const commaMatch = line.match(nameCommaPattern);
                if (commaMatch && isValidNameValue(commaMatch[1])) {
                    result.lastName = commaMatch[1].trim();
                    const firstAndMiddle = commaMatch[2].trim().split(/\s+/);
                    result.firstName = firstAndMiddle[0];
                    if (firstAndMiddle.length > 1) {
                        result.middleName = firstAndMiddle.slice(1).join(' ');
                    }
                    console.log('Found comma format name:', result);
                }
            }
            
            // Check for "Last, First, Middle" format
            if (!result.lastName && !result.firstName && !result.middleName) {
                const fullCommaMatch = line.match(fullNameCommaPattern);
                if (fullCommaMatch && isValidNameValue(fullCommaMatch[1])) {
                    result.lastName = fullCommaMatch[1].trim();
                    result.firstName = fullCommaMatch[2].trim();
                    result.middleName = fullCommaMatch[3].trim();
                    console.log('Found full comma format:', result);
                }
            }
        }

        return result;
    }

    /**
     * Parse address from OCR text
     * @param {string} text - Raw OCR text
     * @param {string[]} lines - Text split by lines
     * @returns {string|null} - Extracted address
     */
    function parseAddress(text, lines) {
        const addressKeywords = ['BLK', 'LOT', 'HOUSE', 'ST', 'STREET', 'BARANGAY', 'BRGY', 'CITY', 'PROVINCE', 'REGION', 'ZONE', 'PHASE', 'PUROK', 'SITIO'];
        const addressLines = [];
        let addressStarted = false;
        
        // Skip fields that should NOT be treated as address
        const skipPatterns = [
            /LAST\s*NAME|SURNAME|APELYIDO/i,
            /FIRST\s*NAME|GIVEN|PANGALAN/i,
            /MIDDLE\s*NAME|GITNANG/i,
            /NATIONALITY|NASYONALIDAD/i,
            /LICENSE|LISENSYA/i
        ];
        
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const upperLine = line.toUpperCase();
            
            // Skip name-related keywords
            if (skipPatterns.some(pattern => pattern.test(upperLine))) {
                continue;
            }
            
            // Skip comma-separated name format
            if (upperLine.match(/^[A-Z]{2,},\s*[A-Z\s]+$/)) {
                continue;
            }
            
            // Check if line contains address label (with or without colon)
            // Format 1: "Address: 123 Main St" (same line)
            // Format 2: "Tirahan/Address" (label only, address on next lines)
            if (upperLine.includes('ADDRESS') || upperLine.includes('TIRAHAN')) {
                if (upperLine.includes(':')) {
                    // Same-line format
                    const parts = line.split(':');
                    if (parts.length > 1 && parts[1].trim().length > 0) {
                        const addrPart = parts[1].trim();
                        if (!addrPart.match(/^[A-Z]{2,},\s*[A-Z\s]+$/)) {
                            addressLines.push(addrPart);
                        }
                    }
                }
                // Start collecting address from current or next line
                addressStarted = true;
                continue;
            }
            
            // If address started or line contains address keywords
            if (addressStarted || addressKeywords.some(kw => upperLine.includes(kw))) {
                // Stop at metadata
                if (upperLine.match(/^(BIRTH|DATE|SEX|GENDER|HEIGHT|WEIGHT|BLOOD|PETSA|KASARIAN|EXPIRATION|AGENCY|CODE|RESTRICTIONS|CONDITIONS)/i)) {
                    addressStarted = false;
                    break;
                }
                
                // Stop at metadata keywords
                if (upperLine.match(/EXPIRATION|AGENCY\s*CODE|RESTRICTIONS|CONDITIONS|LICENSE\s*NO/i)) {
                    addressStarted = false;
                    break;
                }
                
                // Stop at dates
                if (line.match(/\d{4}[\/\-]\d{2}[\/\-]\d{2}/)) {
                    addressStarted = false;
                    break;
                }
                
                // Stop at agency codes
                if (line.match(/[A-Z]\d{2}-\d{2}-\d{6}/)) {
                    addressStarted = false;
                    break;
                }
                
                // Skip if it's just a name
                if (!line.match(/\d+/) && !addressKeywords.some(kw => upperLine.includes(kw))) {
                    if (addressStarted && line.length > 10 && line.length < 50) {
                        addressLines.push(line);
                    }
                    continue;
                }
                
                // Add line if it has address characteristics
                if (line.match(/\d+/) || addressKeywords.some(kw => upperLine.includes(kw))) {
                    addressLines.push(line);
                    addressStarted = true;
                }
                
                // Stop if we collected enough
                if (addressLines.length >= 2) break;
            }
        }
        
        if (addressLines.length > 0) {
            let cleanAddress = addressLines.join(', ');
            
            // Remove metadata
            cleanAddress = cleanAddress.replace(/,\s*Expiration\s*Date[^,]*/gi, '');
            cleanAddress = cleanAddress.replace(/,\s*Agency\s*Code[^,]*/gi, '');
            cleanAddress = cleanAddress.replace(/,\s*[A-Z]\d{2}-\d{2}-\d{6}[^,]*/g, '');
            cleanAddress = cleanAddress.replace(/,?\s*\d{4}[\/\-]\d{2}[\/\-]\d{2}/g, '');
            cleanAddress = cleanAddress.replace(/,\s*$/, '').trim();
            
            console.log('Found address:', cleanAddress);
            return cleanAddress;
        }
        
        return null;
    }

    /**
     * Parse birth date from OCR text
     * Supports multiple formats and converts to YYYY-MM-DD
     * @param {string} text - Raw OCR text
     * @returns {string|null} - Birth date in YYYY-MM-DD format
     */
    function parseBirthDate(text) {
        // Month name to number mapping
        const monthMap = {
            'JANUARY': '01', 'JAN': '01',
            'FEBRUARY': '02', 'FEB': '02',
            'MARCH': '03', 'MAR': '03',
            'APRIL': '04', 'APR': '04',
            'MAY': '05',
            'JUNE': '06', 'JUN': '06',
            'JULY': '07', 'JUL': '07',
            'AUGUST': '08', 'AUG': '08',
            'SEPTEMBER': '09', 'SEP': '09', 'SEPT': '09',
            'OCTOBER': '10', 'OCT': '10',
            'NOVEMBER': '11', 'NOV': '11',
            'DECEMBER': '12', 'DEC': '12'
        };
        
        const birthDatePatterns = [
            // Month name format: "JUNE 12, 2003" or "12 JUNE 2003"
            /(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER|JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|SEPT|OCT|NOV|DEC)\s+(\d{1,2}),?\s+(\d{4})/i,
            /(\d{1,2})\s+(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER|JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|SEPT|OCT|NOV|DEC)\s+(\d{4})/i,
            // Numeric formats
            /(BIRTH\s*DATE|DATE\s*OF\s*BIRTH|BIRTHDAY|PETSA\s*NG\s*KAPANGANAKAN|KAARAWAN)[:\-\s\/]*(\d{2}[\/\-]\d{2}[\/\-]\d{4})/i,
            /\b(\d{2}[\/\-]\d{2}[\/\-]\d{4})\b/,
            /\b(\d{4}[\/\-]\d{2}[\/\-]\d{2})\b/
        ];
        
        for (const pattern of birthDatePatterns) {
            const match = text.match(pattern);
            if (match) {
                // Check if it's a month name format
                if (match[0].match(/(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER|JAN|FEB|MAR|APR|JUN|JUL|AUG|SEP|SEPT|OCT|NOV|DEC)/i)) {
                    let month, day, year;
                    
                    // Format: "JUNE 12, 2003"
                    if (match[1] && match[1].match(/[A-Z]/i)) {
                        month = monthMap[match[1].toUpperCase()];
                        day = match[2].padStart(2, '0');
                        year = match[3];
                    }
                    // Format: "12 JUNE 2003"
                    else if (match[2] && match[2].match(/[A-Z]/i)) {
                        day = match[1].padStart(2, '0');
                        month = monthMap[match[2].toUpperCase()];
                        year = match[3];
                    }
                    
                    if (month && day && year) {
                        const result = `${year}-${month}-${day}`;
                        console.log('Found birth date (month name):', result);
                        return result;
                    }
                } else {
                    // Numeric format
                    let dateStr = match[match.length - 1];
                    
                    // Convert to YYYY-MM-DD format
                    if (dateStr.includes('/')) {
                        const parts = dateStr.split('/');
                        if (parts[0].length === 4) {
                            // YYYY/MM/DD
                            return dateStr.replace(/\//g, '-');
                        } else if (parts[2].length === 4) {
                            // MM/DD/YYYY or DD/MM/YYYY - assume MM/DD/YYYY
                            const result = `${parts[2]}-${parts[0].padStart(2, '0')}-${parts[1].padStart(2, '0')}`;
                            console.log('Found birth date:', result);
                            return result;
                        }
                    } else if (dateStr.includes('-')) {
                        const parts = dateStr.split('-');
                        if (parts[0].length === 4) {
                            console.log('Found birth date:', dateStr);
                            return dateStr;
                        } else {
                            // DD-MM-YYYY
                            const result = `${parts[2]}-${parts[1].padStart(2, '0')}-${parts[0].padStart(2, '0')}`;
                            console.log('Found birth date:', result);
                            return result;
                        }
                    }
                }
            }
        }
        
        return null;
    }

    /**
     * Parse gender from OCR text
     * @param {string} text - Raw OCR text
     * @returns {string|null} - "Male" or "Female"
     */
    function parseGender(text) {
        const upperText = text.toUpperCase();
        const genderPatterns = [
            /(SEX|GENDER|KASARIAN)[:\-\s\/]*(M|F|MALE|FEMALE|LALAKI|BABAE)/i,
            /\b(MALE|FEMALE|LALAKI|BABAE)\b/i
        ];
        
        for (const pattern of genderPatterns) {
            const match = upperText.match(pattern);
            if (match) {
                const genderText = match[match.length - 1];
                if (genderText.startsWith('M') || genderText.startsWith('L')) {
                    console.log('Found gender: Male');
                    return 'Male';
                } else if (genderText.startsWith('F') || genderText.startsWith('B')) {
                    console.log('Found gender: Female');
                    return 'Female';
                }
            }
        }
        
        return null;
    }

    /**
     * Parse barangay number (158-161)
     * @param {string} text - Raw OCR text
     * @returns {string|null} - Barangay number or null
     */
    function parseBarangay(text) {
        const upperText = text.toUpperCase();
        const barangayPattern = /BARANGAY\s*(158|159|160|161)/i;
        const match = upperText.match(barangayPattern);
        
        if (match) {
            console.log('Found barangay:', match[1]);
            return match[1];
        }
        
        return null;
    }

    /**
     * Validate PhilHealth ID format
     * Format: XX-XXXXXXXXX-X (12 digits total with hyphens)
     * Example: 02-027851766-8
     * @param {string} philhealthId - PhilHealth ID number
     * @returns {boolean} - True if valid format
     */
    function validatePhilHealthId(philhealthId) {
        if (!philhealthId) return false;
        
        // Remove any whitespace
        const cleanId = philhealthId.trim();
        
        // Check format: XX-XXXXXXXXX-X (2 digits, hyphen, 9 digits, hyphen, 1 digit)
        const philhealthPattern = /^\d{2}-\d{9}-\d{1}$/;
        
        return philhealthPattern.test(cleanId);
    }

    /**
     * Parse PhilHealth ID number from OCR text
     * @param {string} text - Raw OCR text
     * @returns {string|null} - PhilHealth ID or null
     */
    function parsePhilHealthId(text) {
        // Pattern for PhilHealth ID: XX-XXXXXXXXX-X
        const philhealthPattern = /\b(\d{2}-\d{9}-\d{1})\b/;
        const match = text.match(philhealthPattern);
        
        if (match && validatePhilHealthId(match[1])) {
            console.log('Found PhilHealth ID:', match[1]);
            return match[1];
        }
        
        return null;
    }

    /**
     * Parse PhilHealth-specific format
     * PhilHealth IDs have a unique format with all info on one line after the ID number
     * Format: "LAST NAME, FIRST MIDDLE NAME\nBIRTHDATE - GENDER\nADDRESS"
     * Example: "RANIDO, HARRY MARK PARIÑAS\nDECEMBER 29, 2002 - MALE\n228 BAESA ROAD, BARANGAY 160..."
     * @param {string} text - Raw OCR text
     * @returns {object} - Parsed PhilHealth data
     */
    function parsePhilHealthFormat(text) {
        const result = {
            firstName: null,
            middleName: null,
            lastName: null,
            birthDate: null,
            gender: null,
            address: null
        };

        // Check if this is a PhilHealth ID
        if (!text.toUpperCase().includes('PHILHEALTH') && !parsePhilHealthId(text)) {
            return result;
        }

        const lines = text.split(/[\r\n]+/).map(line => line.trim()).filter(line => line.length > 0);

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const upperLine = line.toUpperCase();

            // Look for name in "LASTNAME, FIRSTNAME MIDDLENAME" format (after the ID number)
            // Pattern: All caps name with comma, following a line with PhilHealth ID
            if (!result.lastName && i > 0) {
                const prevLine = lines[i - 1];
                // Check if previous line has PhilHealth ID number OR if current line comes after a line containing PhilHealth ID
                const hasPhilHealthInPrev = prevLine.match(/\d{2}-\d{9}-\d{1}/) || prevLine.toUpperCase().includes('PHILHEALTH');
                
                // Also check if this line looks like a name (comma-separated, all caps)
                const looksLikeName = line.match(/^[A-Z\u00D1\u00F1]+,\s*[A-Z\u00D1\u00F1\s]+$/);
                
                if (hasPhilHealthInPrev || looksLikeName) {
                    // Try to match name pattern: LASTNAME, FIRSTNAME MIDDLENAME
                    // More flexible pattern to handle names with Ñ and multiple words
                    const nameMatch = line.match(/^([A-ZÑ]+),\s*(.+)$/i);
                    if (nameMatch) {
                        result.lastName = nameMatch[1].trim();
                        const remainder = nameMatch[2].trim();
                        const words = remainder.split(/\s+/);
                        
                        console.log('PhilHealth name parsing:', {
                            line: line,
                            lastName: result.lastName,
                            remainder: remainder,
                            words: words
                        });
                        
                        if (words.length === 1) {
                            // Only first name, no middle
                            result.firstName = words[0];
                            result.middleName = null;
                        } else if (words.length === 2) {
                            // First and middle name
                            result.firstName = words[0];
                            result.middleName = words[1];
                        } else {
                            // Multiple words - treat last word as middle name, rest as first
                            result.firstName = words.slice(0, -1).join(' ');
                            result.middleName = words[words.length - 1];
                        }
                        
                        console.log('Found PhilHealth name:', {
                            lastName: result.lastName,
                            firstName: result.firstName,
                            middleName: result.middleName
                        });
                        continue;
                    }
                }
            }

            // Look for "BIRTHDATE - GENDER" format
            // Example: "DECEMBER 29, 2002 - MALE"
            if (!result.birthDate || !result.gender) {
                const birthGenderMatch = upperLine.match(/(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER)\s+(\d{1,2}),?\s+(\d{4})\s*-\s*(MALE|FEMALE)/i);
                if (birthGenderMatch) {
                    const monthMap = {
                        'JANUARY': '01', 'FEBRUARY': '02', 'MARCH': '03', 'APRIL': '04',
                        'MAY': '05', 'JUNE': '06', 'JULY': '07', 'AUGUST': '08',
                        'SEPTEMBER': '09', 'OCTOBER': '10', 'NOVEMBER': '11', 'DECEMBER': '12'
                    };
                    const month = monthMap[birthGenderMatch[1].toUpperCase()];
                    const day = birthGenderMatch[2].padStart(2, '0');
                    const year = birthGenderMatch[3];
                    result.birthDate = `${year}-${month}-${day}`;
                    result.gender = birthGenderMatch[4].charAt(0).toUpperCase() + birthGenderMatch[4].slice(1).toLowerCase();
                    console.log('Found PhilHealth birth date and gender:', result.birthDate, result.gender);
                    continue;
                }
            }

            // Look for address (after name and birthdate lines)
            // PhilHealth address format: street, barangay, city, metro area - zip
            if (!result.address && result.lastName && result.birthDate) {
                // Skip header lines
                if (upperLine.includes('REPUBLIC') || upperLine.includes('PHILHEALTH') || 
                    upperLine.includes('INSURANCE') || upperLine.match(/\d{2}-\d{9}-\d{1}/)) {
                    continue;
                }
                
                // Check if line looks like an address (has numbers, street keywords, barangay, etc.)
                if (line.match(/\d+/) && (
                    upperLine.includes('ROAD') || upperLine.includes('STREET') || 
                    upperLine.includes('BARANGAY') || upperLine.includes('CITY') ||
                    upperLine.includes('METRO') || upperLine.includes('MANILA')
                )) {
                    result.address = line;
                    console.log('Found PhilHealth address:', result.address);
                    break; // Found address, we're done
                }
            }
        }

        return result;
    }

    /**
     * Main parsing function - detects ID type and extracts all information
     * @param {string} ocrText - Raw OCR text from Azure Computer Vision
     * @returns {object} - Parsed ID information
     */
    function parsePhilippineID(ocrText) {
        console.log('=== Philippine ID Parser ===');
        console.log('Starting parse with text length:', ocrText.length);
        
        if (!ocrText || ocrText.trim().length === 0) {
            console.warn('Empty OCR text provided');
            return {
                idType: 'Unknown',
                firstName: null,
                middleName: null,
                lastName: null,
                birthDate: null,
                gender: null,
                barangay: null,
                address: null,
                success: false,
                message: 'No text to parse'
            };
        }

        // Split text into lines
        const lines = ocrText.split(/[\r\n]+/).map(line => line.trim()).filter(line => line.length > 0);
        
        // Detect ID type
        const idType = detectIdType(ocrText);
        
        // Try PhilHealth-specific parsing first if it's a PhilHealth ID
        let nameData, address, birthDate, gender;
        const philhealthId = parsePhilHealthId(ocrText);
        
        if (philhealthId || idType === 'PhilHealth ID') {
            console.log('=== Detected PhilHealth ID - using PhilHealth-specific parser ===');
            const philhealthData = parsePhilHealthFormat(ocrText);
            console.log('PhilHealth parser returned:', philhealthData);
            
            // Use PhilHealth data if found, otherwise fall back to generic parsing
            nameData = {
                firstName: philhealthData.firstName || null,
                middleName: philhealthData.middleName || null,
                lastName: philhealthData.lastName || null
            };
            address = philhealthData.address || null;
            birthDate = philhealthData.birthDate || null;
            gender = philhealthData.gender || null;
            
            console.log('After PhilHealth parsing - Name data:', nameData);
            console.log('After PhilHealth parsing - Birth Date:', birthDate);
            console.log('After PhilHealth parsing - Gender:', gender);
            
            // If PhilHealth parser didn't get everything, try generic parsers as fallback
            if (!nameData.firstName || !nameData.lastName) {
                const genericName = parseName(ocrText, lines);
                nameData.firstName = nameData.firstName || genericName.firstName;
                nameData.middleName = nameData.middleName || genericName.middleName;
                nameData.lastName = nameData.lastName || genericName.lastName;
            }
            if (!address) address = parseAddress(ocrText, lines);
            if (!birthDate) birthDate = parseBirthDate(ocrText);
            if (!gender) gender = parseGender(ocrText);
        } else {
            // Use generic parsers for other ID types
            nameData = parseName(ocrText, lines);
            address = parseAddress(ocrText, lines);
            birthDate = parseBirthDate(ocrText);
            gender = parseGender(ocrText);
        }
        
        const barangay = parseBarangay(ocrText);
        
        // Build result object
        const result = {
            idType: idType,
            firstName: nameData.firstName,
            middleName: nameData.middleName,
            lastName: nameData.lastName,
            birthDate: birthDate,
            gender: gender,
            barangay: barangay,
            address: address,
            philhealthId: philhealthId,
            success: true,
            message: 'Parsing completed'
        };
        
        // Count successfully extracted fields
        const extractedFields = Object.keys(result).filter(key => 
            key !== 'idType' && key !== 'success' && key !== 'message' && result[key] !== null
        ).length;
        
        console.log(`Extracted ${extractedFields} out of 8 fields`);
        console.log('Final parsed result:', result);
        
        return result;
    }

    /**
     * Helper function to auto-fill form fields with parsed data
     * @param {object} parsedData - Result from parsePhilippineID()
     * @param {object} fieldSelectors - Custom field selectors (optional)
     */
    function autoFillForm(parsedData, fieldSelectors = {}) {
        const selectors = {
            firstName: fieldSelectors.firstName || 'input[name="Input.FirstName"]',
            middleName: fieldSelectors.middleName || 'input[name="Input.MiddleName"]',
            lastName: fieldSelectors.lastName || 'input[name="Input.LastName"]',
            address: fieldSelectors.address || 'textarea[name="Input.Address"]',
            birthDate: fieldSelectors.birthDate || 'input[name="Input.BirthDate"]',
            genderMale: fieldSelectors.genderMale || 'input[name="Input.Gender"][value="Male"]',
            genderFemale: fieldSelectors.genderFemale || 'input[name="Input.Gender"][value="Female"]',
            barangay: fieldSelectors.barangay || 'select[name="Input.Barangay"]',
            philhealthId: fieldSelectors.philhealthId || 'input[name="Input.PhilHealthId"]'
        };

        const filledFields = [];

        // Helper function to set value and trigger events
        function setValueAndTrigger(element, value) {
            element.value = value;
            element.dispatchEvent(new Event('input', { bubbles: true }));
            element.dispatchEvent(new Event('change', { bubbles: true }));
        }

        // Fill first name
        if (parsedData.firstName) {
            const firstNameInput = document.querySelector(selectors.firstName);
            if (firstNameInput) {
                setValueAndTrigger(firstNameInput, parsedData.firstName);
                filledFields.push('First Name');
                console.log('Filled First Name:', parsedData.firstName);
            } else {
                console.warn('First name input not found');
            }
        } else {
            console.warn('No first name in parsed data');
        }

        // Fill middle name
        if (parsedData.middleName) {
            const middleNameInput = document.querySelector(selectors.middleName);
            if (middleNameInput) {
                setValueAndTrigger(middleNameInput, parsedData.middleName);
                filledFields.push('Middle Name');
                console.log('Filled Middle Name:', parsedData.middleName);
            }
        }

        // Fill last name
        if (parsedData.lastName) {
            const lastNameInput = document.querySelector(selectors.lastName);
            if (lastNameInput) {
                setValueAndTrigger(lastNameInput, parsedData.lastName);
                filledFields.push('Last Name');
                console.log('Filled Last Name:', parsedData.lastName);
            } else {
                console.warn('Last name input not found');
            }
        } else {
            console.warn('No last name in parsed data');
        }

        // Fill address
        if (parsedData.address) {
            const addressInput = document.querySelector(selectors.address);
            if (addressInput) {
                setValueAndTrigger(addressInput, parsedData.address);
                filledFields.push('Address');
            }
        }

        // Fill birth date
        if (parsedData.birthDate) {
            const birthDateInput = document.querySelector(selectors.birthDate);
            if (birthDateInput) {
                setValueAndTrigger(birthDateInput, parsedData.birthDate);
                filledFields.push('Birth Date');
                console.log('Filled Birth Date:', parsedData.birthDate);
            } else {
                console.warn('Birth date input not found');
            }
        } else {
            console.warn('No birth date in parsed data');
        }

        // Select gender
        if (parsedData.gender) {
            const genderInput = document.querySelector(
                parsedData.gender === 'Male' ? selectors.genderMale : selectors.genderFemale
            );
            if (genderInput) {
                genderInput.checked = true;
                filledFields.push('Gender');
            }
        }

        // Select barangay
        if (parsedData.barangay) {
            const barangaySelect = document.querySelector(selectors.barangay);
            if (barangaySelect) {
                barangaySelect.value = parsedData.barangay;
                filledFields.push('Barangay');
            }
        }

        // Fill PhilHealth ID
        if (parsedData.philhealthId) {
            const philhealthInput = document.querySelector(selectors.philhealthId);
            if (philhealthInput) {
                philhealthInput.value = parsedData.philhealthId;
                filledFields.push('PhilHealth ID');
            }
        }

        console.log(`Auto-filled ${filledFields.length} fields:`, filledFields.join(', '));
        return filledFields;
    }

    // Export functions
    if (typeof module !== 'undefined' && module.exports) {
        // Node.js environment
        module.exports = {
            parsePhilippineID: parsePhilippineID,
            autoFillForm: autoFillForm,
            detectIdType: detectIdType,
            validatePhilHealthId: validatePhilHealthId
        };
    } else {
        // Browser environment - attach to global object
        global.PhilippineIDParser = {
            parse: parsePhilippineID,
            autoFill: autoFillForm,
            detectIdType: detectIdType,
            validatePhilHealthId: validatePhilHealthId
        };
    }

})(typeof window !== 'undefined' ? window : global);

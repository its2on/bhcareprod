# Fix: Single Underscore vs Double Underscore Issue

## 🚨 Problem Found!

Your settings are using **single underscore** `_` but ASP.NET Core requires **double underscore** `__` for nested configuration.

**Current (WRONG):**
- `AzureOCR_Endpoint` ❌
- `AzureOCR_Key` ❌

**Required (CORRECT):**
- `AzureOCR__Endpoint` ✅ (double underscore)
- `AzureOCR__Key` ✅ (double underscore)

---

## 🔧 Quick Fix Steps

### Step 1: Delete the Wrong Settings

1. In Azure Portal → Your App Service → **Environment variables** (or **Configuration**)
2. Find `AzureOCR_Endpoint` (single underscore)
3. Click the **trash icon** (🗑️) to delete it
4. Find `AzureOCR_Key` (single underscore)
5. Click the **trash icon** (🗑️) to delete it

### Step 2: Add the Correct Settings

1. Click **"+ Add"** button (top of the table)
2. **Setting 1:**
   - **Name:** `AzureOCR__Endpoint` ← **Double underscore `__`**
   - **Value:** `https://bhcare-ocr.cognitiveservices.azure.com/`
   - Click **"OK"**

3. Click **"+ Add"** button again
4. **Setting 2:**
   - **Name:** `AzureOCR__Key` ← **Double underscore `__`**
   - **Value:** `3g63cprcznccb3aep9seb4wbzPG32Mo1k6ET8LzBW7w3IDc9uLlxJQQJ99BKACqBBLyXJ3w3AAAFA`
   - Click **"OK"**

### Step 3: Save and Restart

1. Click **"Apply"** button at the bottom (or **"Save"** at the top)
2. Wait for the save to complete
3. Go to **Overview** → Click **"Restart"** button
4. Wait 2-3 minutes for restart to complete

### Step 4: Test

1. Go to your website Sign Up page
2. Upload an ID image
3. It should work now! ✅

---

## 📝 Why Double Underscore?

In ASP.NET Core, environment variables use **double underscore `__`** to represent nested configuration:

- Environment variable: `AzureOCR__Endpoint` 
- Maps to JSON: `AzureOCR:Endpoint`
- Maps to C# code: `_configuration["AzureOCR:Endpoint"]`

**Single underscore `_` doesn't work** because it's treated as a flat key, not nested.

---

## ✅ Verification

After fixing, verify the settings:

1. Go to **Environment variables** (or **Configuration**)
2. You should see:
   - `AzureOCR__Endpoint` (with **double underscore**)
   - `AzureOCR__Key` (with **double underscore**)
3. Both should have your values

---

## 🎯 Summary

**The Problem:**
- Settings exist but use wrong format (single `_` instead of double `__`)

**The Solution:**
- Delete `AzureOCR_Endpoint` and `AzureOCR_Key`
- Add `AzureOCR__Endpoint` and `AzureOCR__Key` (with double `__`)
- Save and restart

**After Fix:**
- OCR should work without "Unauthorized" error! ✅


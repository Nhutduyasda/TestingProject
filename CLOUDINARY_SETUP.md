# Cloudinary Configuration Guide

## Setup Instructions

1. **Create Cloudinary Account** (if you don't have one):
   - Go to https://cloudinary.com/
   - Sign up for a free account
   - Verify your email

2. **Get Your Cloudinary Credentials**:
   - Login to your Cloudinary dashboard
   - Go to Dashboard -> Account Details
   - Copy your **Cloud Name**, **API Key**, and **API Secret**

3. **Configure appsettings.json**:
   Replace the placeholder in `appsettings.json`:
   ```json
   "Cloudinary": {
     "CloudinaryUrl": "cloudinary://YOUR_API_KEY:YOUR_API_SECRET@YOUR_CLOUD_NAME"
   }
   ```
   
   Example:
   ```json
   "Cloudinary": {
     "CloudinaryUrl": "cloudinary://123456789012345:abcdefghijklmnopqrstuvwxyz123456@mycloud"
   }
   ```

4. **Security Best Practice**:
   - Never commit real credentials to Git
   - Use User Secrets for development:
     ```bash
     dotnet user-secrets set "Cloudinary:CloudinaryUrl" "cloudinary://YOUR_API_KEY:YOUR_API_SECRET@YOUR_CLOUD_NAME"
     ```
   - For production, use Environment Variables or Azure Key Vault

## CloudinaryService Features

### Upload Single Image
```csharp
var imageUrl = await _cloudinaryService.UploadImageAsync(file, "products");
```

### Upload Multiple Images
```csharp
var imageUrls = await _cloudinaryService.UploadMultipleImagesAsync(files, "products");
```

### Delete Image
```csharp
var success = await _cloudinaryService.DeleteImageAsync(publicId);
// Or delete by URL:
var success = await _cloudinaryService.DeleteImageAsync(imageUrl);
```

## Image Upload Limits

- **Allowed formats**: .jpg, .jpeg, .png, .gif, .webp
- **Max file size**: 10MB
- **Auto optimization**: Images are automatically optimized and limited to 1000x1000px

## Folder Structure

Images are organized by folder:
- `products/` - Product images
- You can customize folder names when calling the service

## Free Tier Limits

- 25 GB storage
- 25 GB monthly bandwidth
- 25,000 transformations per month

Perfect for development and small projects!

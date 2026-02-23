# Deploying StreamAxis API to Google Cloud Run

## Overview
This guide will help you deploy your StreamAxis API to Google Cloud Run so it can be accessed from any device.

## Prerequisites

1. Install Google Cloud CLI:
   ```bash
   # On macOS
   brew install google-cloud-sdk
   
   # Or download from: https://cloud.google.com/sdk/docs/install
   ```

2. Create a Google Cloud account and project
3. Enable billing for your project
4. Enable required APIs:
   ```bash
   gcloud services enable run.googleapis.com
   gcloud services enable cloudbuild.googleapis.com
   ```

## Database Migration (Required)

Cloud Run containers are stateless, so SQLite won't work for production. You need to migrate to a cloud database:

### Option 1: Google Cloud SQL (Recommended)

1. Create a PostgreSQL instance:
   ```bash
   gcloud sql instances create streamaxis-db \
       --database-version=POSTGRES_14 \
       --tier=db-f1-micro \
       --region=us-central1 \
       --root-password=your-secure-password
   ```

2. Create a database:
   ```bash
   gcloud sql databases create streamaxis \
       --instance=streamaxis-db
   ```

3. Update your project to use PostgreSQL:
   Add this NuGet package to your StreamAxis.Api.csproj:
   ```xml
   <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="7.0.0" />
   ```

4. Update your Program.cs to use PostgreSQL:
   ```csharp
   // Replace the SQLite line with:
   builder.Services.AddDbContext<AppDbContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

5. Update appsettings.json:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=YOUR_INSTANCE_IP;Database=streamaxis;Username=postgres;Password=your-secure-password"
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "AllowedHosts": "*"
   }
   ```

## Building and Deploying

1. Authenticate with Google Cloud:
   ```bash
   gcloud auth login
   gcloud config set project YOUR_PROJECT_ID
   ```

2. Build and deploy:
   ```bash
   # Build and push the container image
   gcloud builds submit --tag gcr.io/YOUR_PROJECT_ID/streamaxis-api

   # Deploy to Cloud Run
   gcloud run deploy streamaxis-api \
     --image gcr.io/YOUR_PROJECT_ID/streamaxis-api \
     --platform managed \
     --region us-central1 \
     --port 8080 \
     --allow-unauthenticated \
     --set-env-vars "ConnectionStrings__DefaultConnection=Host=YOUR_INSTANCE_IP;Database=streamaxis;Username=postgres;Password=your-secure-password"
   ```

## Updating Mobile App Settings

After deployment, update your mobile app's ApiSettings.cs:

```csharp
public static string BaseUrl { get; set; } = "https://your-service-url-from-cloud-run.a.run.app";
```

## Alternative: Local Testing with External Access

If you want to temporarily allow external access to your local server for testing:

1. Stop the current server
2. Run the server listening on all interfaces:
   ```bash
   cd /Users/iphonerepairman/Projects/StreamAxis
   dotnet run --project StreamAxis.Api/StreamAxis.Api.csproj --urls=http://0.0.0.0:5261
   ```

3. Find your computer's IP address:
   ```bash
   ifconfig | grep "inet " | grep -v 127.0.0.1
   ```

4. Update your mobile app to use your computer's IP address instead of localhost

## Important Notes

- Cloud Run instances are ephemeral, so local file storage won't persist between requests
- For production, always use a managed database service like Cloud SQL
- Consider security implications of using --allow-unauthenticated
- Monitor your usage as Cloud Run charges based on CPU, memory, and requests
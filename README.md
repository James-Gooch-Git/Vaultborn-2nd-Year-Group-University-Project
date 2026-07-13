# Read Me

## How to install: 

Go to the google drive below and download 'Vaultborn_Installer' within the 'installer' folder  

Once downloaded, you should be able to run the installer  

Select Next and Install until you see Finish  

Click Finish and the application will run, and create start menu and desktop shortcuts for you  


Google drive for installer: https://drive.google.com/drive/folders/1OgJxdZ7nH_pw3yrnzpK_nyZ86B-3TFeC?usp=drive_link


---

## About

This is a system for versioning, managing, and collaborating on 3D model assets. 

The app allows you to sign in with Autodesk, and link your Autodesk Hubs with team members/friends/anyone for real time collaboration.

It also versions updates for testing and cloud-save version control, allowing rollback of any changes made to a 3D model. 

With the app you can create, upload, download, buy (with PayPal), sell, version, share and delete 3D models and create custom collections. 

---

## Configuring credentials

No credentials are stored in the repository. Before running the app, set the
following environment variables (process or user scope):

| Variable | Value |
| --- | --- |
| `VAULTBORN_MONGODB_URI` | MongoDB Atlas connection string |
| `VAULTBORN_APS_CLIENT_ID` | Autodesk Platform Services app client ID |
| `VAULTBORN_APS_CLIENT_SECRET` | Autodesk Platform Services app client secret |
| `VAULTBORN_PAYPAL_CLIENT_ID` | PayPal (sandbox) client ID |
| `VAULTBORN_PAYPAL_CLIENT_SECRET` | PayPal (sandbox) client secret |

For example, in PowerShell:

```powershell
[Environment]::SetEnvironmentVariable("VAULTBORN_APS_CLIENT_ID", "<your id>", "User")
```

Session tokens are persisted encrypted (Windows DPAPI, current-user scope) under
`%LOCALAPPDATA%\Vaultborn\session.bin` — never in environment variables or the registry.


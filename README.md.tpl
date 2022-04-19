# {{ name }}
## {{ integration_type }}

{{ description }}

<!-- add integration specific information below -->

## Use Cases

The PKCS12 orchestrator extension implements the following capabilities:
1. Discovery - Find PKCS12 certificate stores on a server.
2. Create - Create an empty PKCS12 certificate store.
3. Inventory - Return all certificates for a define certificate store.
4. Management (Add) - Add a certificate to a defined certificate store.
5. Management (Remove) - Remove a certificate from a defined certificate store.

The PKCS12 orchestrator extension supports the following types of certificate stores:
1. Trust stores (multiple public certificates with no private keys)
2. Stores with one or more aliases (friendly names)
3. Stores with certificate chains included in the entry

This orchestrator extension supports Java Keystores of type PKCS12 along with any other certificate stores created using the PKCS#12 standard.  It does NOT at this time support Java Keystores of type JKS or any other types besides PKCS12.  It differs from the [Java Keystore orchestrator extension](https://github.com/Keyfactor/jks-remote-orchestrator) in that it uses the BouncyCastle .Net code library to manage certificate stores rather than remoting keytool commands.


## Versioning

The version number of a the PKCS12 orchestrator extension can be verified by right clicking on the PKCS12.dll file, selecting Properties, and then clicking on the Details tab.


## Keyfactor Version Supported

The PKCS12 orchestrator extension has been tested against Keyfactor Universal Orchestrator version 9.5 on a Windows server but should work with later versions of the Keyfactor Universal Orchestrator on Windows or Linux.


## Security Considerations

**For Linux orchestrated servers:**
1. The PKCS12 orchestrator extension makes use of SFTP to upload and download certificate and certificate store files as well as the following common Linux shell commands:
    * find
2. If the credentials you will be connecting with will need elevated access to run these commands, you must set the id up as a sudoer with no password necessary and set the config.json "UseSudo" value to "Y" (See Section 4 regarding the config.json file).
3. As mentioned in #1, the PKCS12 orchestrator extension makes use of SFTP to transfer files to and from the orchestrated server.  SFTP will not mske use of sudo, so all folders containing certificate stores will need to allow SFTP file transfer.  If this is not possible, set the values in the config.json apprpriately to use an alternative upload/download folder that will allow SFTP file transfer (See Section 4 regarding the config.json file).

**For Windows orchestrated servers:**
1. Make sure that WinRM is set up on the orchestrated server and that the WinRM port is part of the certificate store path when setting up your certificate stores (See Section 3a below). 
2. The PKCS12 orchestrator extension makes use of the following Powershell cmdlets:
    * Get-ChildItem
    * Get-WmiObject


## PKCS12 Orchestrator Extension Configuration

**1. Create the New Certificate Store Type for the PKCS12 orchestrator extension**

In Keyfactor Command create a new Certificate Store Type similar to the one below by clicking Settings (the gear icon in the top right) => Certificate Store Types => Add:

![](Images/image1.png)
![](Images/image2.png)
![](Images/image5.png) 
![](Images/image6.png) 

- **Name** – Required. The display name of the new Certificate Store Type
- **Short Name** – Required. **MUST** be &quot;PKCS12&quot;
- **Custom Capability** - Unchecked
- **Supported Job Types** – Inventory, Add, Remove, Create, and Discovery are the 5 job types implemented by this extension
- **Needs Server, Blueprint Allowed, Requires Store Password, Supports Entry Password** – All checked/unchecked as shown
- **Requires Store Password** - Checked
- **Supports Entry Password** - Unchecked
- **Store PathType** – Freeform (user will enter the the location of the store)
- **Supports Custom Alias** – Required. Each certificate MUST have an alias associated with it for the store.
- **Private Key Handling** – Optional (Certificates in a PKCS12 store generally contain private keys, but may not in a trust store configuration)
- **PFX Password Style** – Default

- **Linux File Permissions on Store Creation** - Optional.  Overrides the optional config.json DefaultLinuxPermissionsOnStoreCreation setting (see section 4 below) for a specific certificate store.  This value will set the file permissions (Linux only) of a new certificate store created via a Management-Create job.  If this parameter is not added or added but not set, the permissions used will be derived from the DefaultLinuxPermissionsOnStoreCreation setting. 

No Entry Parameters should be entered.

**2. Create the proper extension folder and move the installation binaries to this location**

Download the desired [PKCS12 orchestrator extension version](https://github.com/Keyfactor/pkcs12-remote-orchestrator). Within Windows File Explorer, navigate to the Keyfactor Orchestrator installation folder (usually C:\Program Files\Keyfactor\Keyfactor Orchestrator), find the "extensions" folder, and under that create a new folder named "PKCS12". Under the PKCS12 folder copy all of the files from the downloaded release to this location.

**3a. (Optional) Create a PKCS12 Certificate Store within Keyfactor Command**

If you choose to manually create a PKCS12 store In Keyfactor Command rather than running a Discovery job to automatically find the store, you can navigate to Certificate Locations =\> Certificate Stores within Keyfactor Command to add a PKCS12 certificate store. Below are the values that should be entered.

![](Images/image3.png)

- **Category** – Required. The PKCS12 type name must be selected.
- **Container** – Optional. Select a container if utilized.
- **Client Machine &amp; Credentials** – Required. The server name or IP Address and login credentials for the server where the Certificate Store is located.  The credentials for server login can be any of:
  
  - UserId/Password
  
  - UserId/SSH private key (entered in the password field)
  
  - PAM provider information to pass the UserId/Password or UserId/SSH private key credentials
  
When setting up a Windows server, the format of the machine name must be – http://ServerName:5985, where &quot;5985&quot; is the WinRM port number. 5985 is the standard, but if your organization uses a different, use that.  The credentials used can the credentials entered or the Keyfactor Command service account if you leave the credentials blank.  **However, if you choose to not enter credentials and use the Keyfactor Command service account, it is required that the *Change Credentials* link still be clicked on and the resulting dialog closed by clicking OK.**
  
- **Store Path** – Required. The FULL PATH and file name of the pkcs12 certificate store being managed. File paths on Linux servers will always begin with a &quot;/&quot;. Windows servers will always begin with the drive letter, colon, and backslash, such as &quot;c:\\&quot;.  Valid characters for Linux store paths include any alphanumeric character, space, forward slash, hyphen, underscore, and period. For Windows servers, the aforementioned characters as well as a colon and backslash.
- **Orchestrator** – Select the orchestrator you wish to use to manage this store
- **Store Password** – Required. Set the store password or set no password after clicking the supplied button
- **Create Certificate Store** - Check this box IF you wish to schedule a CREATE job that will physically create this store on the destination server.  If the actual physical store already exists, leave this unchecked.
- **Linux File Permissions on Store Creation** - Optional (Linux only). Set the Linux file permissions you wish to be set when creating a new physical certificate store via checking Create Certificate Store above.  This value must be 3 digits all betwwen 0-7.
- **Inventory Schedule** – Set a schedule for running Inventory jobs or none, if you choose not to schedule Inventory at this time.

**3b. (Optional) Schedule a PKCS12 Discovery Job

Rather than manually creating PKCS12 certificate stores, you can schedule a Discovery job to search an orchestrated server and find them.

First, in Keyfactor Command navigate to Certificate Locations => Certificate Stores. Select the Discover tab and then the Schedule button. Complete the dialog and click Done to schedule.

![](Images/image4.png)

- **Category** – Required. The PKCS12 type name must be selected.
- **Orchestrator** – Select the orchestrator you wish to use to manage this store
- **Client Machine & Credentials** – Required. The server name or IP Address and login credentials for the server where the Certificate Store is located.  The credentials for server login can be any of:

  - UserId/Password

  - UserId/SSH private key (entered in the password field)

  - PAM provider information to pass the UserId/Password or UserId/SSH private key credentials

  When setting up a Windows server, the format of the machine name must be – http://ServerName:5985, where &quot;5985&quot; is the WinRM port number. 5985 is the standard, but if your organization uses a different, use that.  The credentials used can the credentials entered or the Keyfactor Command service account if you leave the credentials blank.  **However, if you choose to not enter credentials and use the Keyfactor Command service account, it is required that the *Change Credentials* link still be clicked on and the resulting dialog closed by clicking OK.**
- **When** – Required. The date and time when you would like this to execute.
- **Directories to search** – Required. A comma delimited list of the FULL PATHs and file names where you would like to recursively search for PKCS12 certificate stores. File paths on Linux servers will always begin with a "/". Windows servers will always begin with the drive letter, colon, and backslash, such as "c:\\".  Entering the string "fullscan" when Discovering against a Windows server will automatically do a recursive search on ALL local drives on the server.
- **Directories to ignore** – Optional. A comma delimited list of the FULL PATHs that should be recursively ignored when searching for PKCS12 certificate stores. Linux file paths will always begin with a "/". Windows servers will always begin with the drive letter, colon, and backslash, such as "c:\\".
- **Extensions** – Optional but suggested. A comma delimited list of the file extensions (no leading "." should be included) the job should search for. If not included, only files in the searched paths that have **no file extension** will be returned. If providing a list of extensions, using "noext" as one of the extensions will also return valid PKCS12 certificate stores with no file extension. For example, providing an Extensions list of "p12, noext" would return all locations within the paths being searched with a file extension of "p12" and files with no extensions.
- **File name patterns to match** – Optional. A comma delimited list of full or partial file names (NOT including extension) to match on.  Use "\*" as a wildcard for begins with or ends with.  Example: entering "ab\*, \*cd\*, \*ef, ghij" will return all stores with names that _**begin with**_ "ab" AND stores with names that _**contain**_ "cd" AND stores with names _**ending in**_ "ef" AND stores with the _**exact name**_ of "ghij".
- **Follow SymLinks** – NOT IMPLEMENTED. Leave unchecked.
- **Include PKCS12 Files** – NOT APPLICABLE. Leave unchecked.

**4. Update Settings in config.json**

As a configuration step, you must modify the config.json file, found in the extensions folder of your Keyfactor orchestrator PKCS12 installation (usually C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions\PKCS12). This file contains the following JSON:

{
    
&quot;UseSudo&quot;: &quot;N&quot;,

&quot;UseSeparateUploadFilePath&quot;: &quot;N&quot;,

&quot;SeparateUploadFilePath&quot;: &quot;/path/to/upload/folder/&quot;,

Modify the three values as appropriate (all must be present regardless of Linux or Windows server orchestration):

**UseSudo** (Linux only) - to determine whether to prefix certain Linux command with "sudo". This can be very helpful in ensuring that the user id running commands ssh uses "least permissions necessary" to process each task. Setting this value to "Y" will prefix all Linux commands with "sudo" with the expectation that the command being executed on the orchestrated Linux server will look in the sudoers file to determine whether the logged in ID has elevated permissions for that specific command. For orchestrated Windows servers, this setting has no effect. Setting this value to "N" will result in "sudo" not being added to Linux commands.

**UseSeparateUploadFilePath** (Linux only) – When adding a certificate to a PKCS12 certificate store, the orchestrator extension must upload the certificate being deployed to the server where the certificate store resides. Setting this value to &quot;Y&quot; looks to the next setting, SeparateUploadFilePath, to determine where this file should be uploaded. Set this value to &quot;N&quot; to use the same path where the certificate store being managed resides. The certificate file uploaded to either location will be removed at the end of the process.

**SeparateUploadFilePath** (Linux only) – Only used when UseSeparateUploadFilePath is set to &quot;Y&quot;. Set this to the path you wish to use as the location to upload and later remove certificates to be added to the PKCS12 certificate store being maintained.

**DefaultLinuxPermissionsOnStoreCreation** (Linux only) - Optional.  Value must be 3 digits all between 0-7.  The Linux file permissions that will be set on a new certificate store created via a Management Create job.  This value will be used for all certificate stores managed by this orchestrator instance unless overridden by the optional "Linux File Permissions on Store Creation" custom parameter setting on a specific certificate store.  If "Linux File Permissions on Store Creation" and DefaultLinuxPermissionsOnStoreCreation are not set, a default permission of 600 will be used.

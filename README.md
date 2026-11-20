# Microsoft Office Inspectors for Clearinet (forked from Fiddler version)

This README document provides the instruction on installing Microsoft Office Inspectors for Clearinet.

The MAPI Inspector evaluates the captured traffic between Outlook and Exchange; whereas the FSSHTTPandWOPI Inspector evaluates the captured traffic between client and SharePoint. These Microsoft Office Inspectors are able to display the protocol content in a user-friendly format. You may choose to install one or both inspectors.

For details on how to use the inspectors: see [*MAPI Inspector for Fiddler User Guide*](https://github.com/OfficeDev/MAPI-Inspector-for-Fiddler/wiki), see also [*FSSHTTPandWOPI Inspector for Fiddler User Guide*](https://github.com/OfficeDev/Office-Inspectors-for-Fiddler/wiki/FSSHTTPandWOPI).

## Overview

The Messaging Application Programming Interface (MAPI) Inspector for Clearinet decodes the MAPI message payload of an HTTP POST request and response according to [MS-OXCMAPIHTTP](https://msdn.microsoft.com/en-us/library/Dn530952(v=EXCHG.80).aspx). The MAPI Inspector is displayed in Clearinet's request and response inspector tabs.

In addition to the MAPI Inspector, the File Synchronization via SOAP over HTTP Protocol (FSSHTTP) and the Web Application Open Platform Interface Protocol (WOPI)—combined as FSSHTTPandWOPI—also have a Clearinet inspector that decodes:

* FSSHTTP protocol message that enables one or more clients to synchronize changes done on shared files stored on a server.
* WOPI protocol message that enables a client to access and change files stored by a server.

The FSSHTTPandWOPI Inspector is displayed in Clearinet's request and response inspector tabs and decodes message payloads according to [MS-FSSHTTP](https://msdn.microsoft.com/en-us/library/dd943623%28v=office.12%29.aspx), [MS-FSSHTTPB](https://msdn.microsoft.com/en-us/library/dd965780%28v=office.12%29.aspx), [MS-FSSHTTPD](https://msdn.microsoft.com/en-us/library/ee365790%28v=office.12%29.aspx) and [MS-WOPI](https://msdn.microsoft.com/en-us/library/hh622722%28v=office.12%29.aspx).

The repository also retains legacy FiddlerScript that adds an *MS Protocol* column in Fiddler. That script is separate from the Clearinet inspector extensions and is not loaded by Clearinet.

## Building the Inspectors

Build the Clearinet targets from their Source folders:

- `MAPIInspector\Source\MAPIInspector.Clearinet.csproj`
- `FSSHTTPWOPIInspector\Source\FSSHTTPandWOPIInspector.csproj`

The projects target .NET Framework 4.8.1 and expect the Clearinet source/build at `..\..\..\cin` by default. Override `ClearinetPath` and `ClearinetExtensionsPath` when building against a different Clearinet location. Successful builds copy the `CAE-*.dll` extension to the current user's `Documents\Clearinet\Extensions` folder.

## Installation

Install Clearinet, then place the built `CAE-MAPIInspector.dll` and/or `CAE-FSSHTTPWOPIInspector.dll` in the user's `Documents\Clearinet\Extensions` folder. The build post-event copies them there automatically. Restart Clearinet to load the extensions.

The repository's `MAPI.js` and `FSSHTTPAndWOPI.js` files are legacy FiddlerScript custom-column scripts; they are not loaded by Clearinet and have not been ported as part of these inspector extensions.



This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

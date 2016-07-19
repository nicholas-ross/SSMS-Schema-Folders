
# [SSMS Schema Folders](http://ssmsschemafolders.codeplex.com/)

This an extension for SQL Server Management Studio 2012, 2014 and 2016.
It groups sql objects in Object Explorer (tables, views, etc.) into schema folders.

Source code, documentation and issues can be found at <http://SsmsSchemaFolders.codeplex.com/>

## Install

Copy this folder into the SSMS extension folder. Remove or replace any previous version.
Run the included reg file to skip the load error.

* 2012 - `C:\Program Files (x86)\Microsoft SQL Server\110\Tools\Binn\ManagementStudio\Extensions`
* 2014 - `C:\Program Files (x86)\Microsoft SQL Server\120\Tools\Binn\ManagementStudio\Extensions`
* 2016 - `C:\Program Files (x86)\Microsoft SQL Server\130\Tools\Binn\ManagementStudio\Extensions`

Depending on your web browser, you may need to unblock the zip file before extracting.
Right click on the zip file and select Properties. 
If you see an `Unblock` button or checkbox then click it. 
If you don't see this then continue as above.

## Options

There are a few user options which change the style and behaviour of the schema folders.
`Tools > Options > SQL Server Object Explorer > Schema Folders`

* Append Dot - Add a dot after the schema name on the folder label.
* Clone Parent Node - Add the right click and connection properties of the parent node to the schema folder node.
* Use Object Icon - Use the icon of the last child node as the folder icon. If false then use the parent node (i.e. folder) icon.

## Known Issues

### Load error
The first time SSMS is run with the extension it will show an error message. Click 'No' and restart SSMS. The included reg file sets the same registry setting as when you click the no button.
This should be fixed when there is official support for SSMS extensions.

### Compatibility with other extensions
This extension moves nodes in the Object Explorer tree view. This could cause problems with other extensions that are not expecting it. At this point in time, I am not aware of any extensions where this is an issue. If you do have problems then let me know.

Please report any issues to <http://ssmsschemafolders.codeplex.com/workitem/list/basic>.

## Change Log

### v1.2 (TBA)
* No new features.
* Single deployable version for multiple SSMS versions.
* Fixed: Folder expanding wait time on single core cpu.

### v1.1 (2016-07-14)
* Added user options.
* Fixed: Error when running mulitple SSMS instances. (#1)

**Debug Build**
* Added output window pane for debug messages.

### v1.0 (2016-07-05)
* Public beta release.

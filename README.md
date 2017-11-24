
# [SSMS Schema Folders](https://github.com/nicholas-ross/SSMS-Schema-Folders)

This an extension for SQL Server Management Studio 2012, 2014, 2016 and 17.
It groups sql objects in Object Explorer (tables, views, etc.) into schema folders.

![Object Explorer](ObjectExplorerView.png)

Source code, documentation and issues can be found at <https://github.com/nicholas-ross/SSMS-Schema-Folders>

This is a fork of [SSMS2012Extender](https://ssms2012extender.codeplex.com/) that adds support for SSMS 2014 and 2016.

You can download the latest version of SSMS for free from [Microsoft](https://msdn.microsoft.com/en-US/library/mt238290.aspx).

## Install

[Download the latest release.](https://github.com/nicholas-ross/SSMS-Schema-Folders/releases)

Depending on your web browser, you may need to unblock the zip file before extracting.
Right click on the zip file and select Properties. 
If you see an `Unblock` button or checkbox then click it. 

Extract the zip file and copy the folder into the SSMS extension folder. Remove or replace any previous version.
Run the included reg file to skip the load error.

* 2012 - `C:\Program Files (x86)\Microsoft SQL Server\110\Tools\Binn\ManagementStudio\Extensions`
* 2014 - `C:\Program Files (x86)\Microsoft SQL Server\120\Tools\Binn\ManagementStudio\Extensions`
* 2016 - `C:\Program Files (x86)\Microsoft SQL Server\130\Tools\Binn\ManagementStudio\Extensions`
* 17 - `C:\Program Files (x86)\Microsoft SQL Server\140\Tools\Binn\ManagementStudio\Extensions`

## Options

There are a few user options which change the style and behaviour of the schema folders.
`Tools > Options > SQL Server Object Explorer > Schema Folders`

* Append Dot - Add a dot after the schema name on the folder label.
* Clone Parent Node - Add the right click and connection properties of the parent node to the schema folder node.
* Use Object Icon - Use the icon of the child node as the folder icon. If false then use the parent node (i.e. folder) icon.

## Known Issues

### Load error
The first time SSMS is run with the extension it will show an error message. Click 'No' and restart SSMS. The included reg file sets the same registry setting as when you click the no button.
This should be fixed when there is official support for SSMS extensions.

### Compatibility with other extensions
This extension moves nodes in the Object Explorer tree view. This could cause problems with other extensions that are not expecting it. At this point in time, I am not aware of any extensions where this is an issue. If you do have problems then let me know.

Please report any issues to <https://github.com/nicholas-ross/SSMS-Schema-Folders/issues>.

## Change Log

### v1.2.1 (2016-12-22)
* Fixed: Folder expanding wait time.

### v1.2 (2016-12-12)
* Added support for v17.0 RC1.
* Show wait cursor while creating folders.
* Single deployable version for multiple SSMS versions.
* Fixed: Folder expanding wait time on single core cpu.

### v1.1 (2016-07-14)
* Added user options.
* Fixed: Error when running mulitple SSMS instances.

**Debug Build**
* Added output window pane for debug messages.

### v1.0 (2016-07-05)
* Public beta release.

# AttributeWrangler

A tool for bulk editing ArchestrA attributes.  Some example use cases:

* Updating a the description on an attribute that has a typo for all objects descending from a template
* Change all the alarm priorities on objects hosted in a particular area
* Changing the IO references for all objects using a particular topic name

To use it, first select the objects you want to include in the search.  You can either pick objects out of the derivation tree or search for them using the advanced search function (recommended).  You can search by area, template, and object name.  All object search options are regex pattern matched, so if you want to match a template remember to escape the $ character (i.e. \$).

Then, set the attribute name pattern (also regex aware).  Then select the action you want to perform, the options are:
* Find:  List the matched attributes in the log.  You can also match values against a regex pattern if you fill out the find value.
* Find-Replace: Perform a find and replace operation in the value of the attribute.  This can only be performed on attributes of type string and MxReference.
* Find-Update: For attributes that have a value matching the find-value (exactly), replace the entire value with the replace-value.  Applies to strings, floats, doubles, integers and references.
* Update: Update the value of all matching attributes with the replace-value.  Applies to the same data types as Find-Update.
* Set-Lock: Set the lock status for the attribute.
* Set-Security:  Set the security classification for the attribute.

The find and replace value text fields are not regex aware (except in Find mode), although they do support the token '~%obj', which will be replaced with the object name.

Multiple operations can be done at once to save time on checkout and checkin operations, use the Add/Delete buttons to add or remove extra operations.  The Load/Save buttons can load or save the parameters to a file.  This is useful when reusing complex expressions.

If you only want to preview the operations that would be performed, check the whatif box.  This will log all the operations that would otherwise be performed.  Note that the GRAccess SDK is not fast, so if you are performing operations on many objects, expect it to take some time (checkout time via GRAccess is about as slow as through the IDE).

Additional info:

* If your galaxy uses OS security, you don't need to fill out the username and password fields when logging in.
* The tool accesses the galaxy database using integrated security, so ensure that the current user has access to the database.
* This tool will not stop you from doing stupid things.  I strongly suggest you backup your galaxy before using this tool.  Use the whatif option to test your operations.

### Bulk updating IO references via csv file
The Wrangler can bulk  update IO references via csv file.  This is primarily designed to be used in conjunction with the aaIOChecker tool, which can automatically produce these files, but any valid csv file can be used.  The csv file should contain at least the following columns,
* Address - the new IO reference
* Attribute - the name of the attribute (not including the object name)
* Object - the object that the attribute belongs to

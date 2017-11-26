# DataAnnotationsValidatorRecursive

Multi-targeting cross-platform validator library pointing at .netstandard 2.0 and .net framework 4.7. 

Inspired by [DataAnnotationsValidatorRecursive](https://github.com/reustmd/DataAnnotationsValidatorRecursive) owned by @reustmd

- The recursiveness algorithm has been rewritten.

- Added cache to hold properties info by type

- Included as part of the member name

	- the zero-based index of the objects contained in enumerations 
		- `Child.GrandChildren[1].PropertyA`

	- the key-based index of the objects contained in a enumeration of `KeyValuePair<,>` type 
		- `DataList[0][Index=1, Key="key1"]`

- Improved unit tests

- Enhanced code with some handy features of the latest c# version
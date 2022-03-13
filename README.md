# pc20chapters
Winforms-based app for generating Podcast-2.0 compliant chapters file

Creates a series of text boxes into which you can put your podcast chapter data.
When you click "Save", it emits a .json file which you can publish with your RSS feed.

Requires .NET framework v5.0.  If prompted, make sure you install the *desktop* version, and not console.

## Usage notes

 * All timestamps are stored as integer number of seconds from the start of the episode.  You can also enter a timestamp in "hh:mm:ss" or "mm:ss" format, and it will be automatically converted.
 * Blank fields will not be written to the json.  Endtime will only be written if greater than zero.  Start time is required, so is always written.
 * The program has very little validation.  The text in the text boxes ends up in the chapters file.  Broken URLs, negative timestamps, or other tomfoolery will most likely be ignored by the user's podcast app.

## Download

Most recent binaries are in a zipfile in the cs/bin folder.  There are three files in there: DLL, EXE, and JSON
All three must be in the same directory.  (If anyone knows how to make .NET build only one distributable file, let me know.)

## Build

Source code is in cs folder.  csproj used to build is provided.  Visual Studio will know what to do.

## Version history

1.03 = Port app to C# exe.  Some new features.

0.99 = Powershell version.  Found in v1-powershell folder, if you want to see it for any reason
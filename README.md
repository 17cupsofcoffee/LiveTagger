# LiveTagger

Ableton Live 12's new browser is really cool, but setting up the metadata for all of your existing samples is extremely tedious - especially since [there is no way to apply tags to an entire folder at once](https://ableton.centercode.com/project/feedback/view.html?cap=ea2ce822bd02401dba446c068717bc68&uf=e788c9befb6e46408e9ad7a0f7979a05).

LiveTagger is a utility which allows you to batch tag files from the command line.

> [!CAUTION]
> This software is not endorsed or supported by Ableton. Use at your own risk!

## Installing

TODO - for now you can compile from source.

## Usage

Adding some tags to the files in the current folder is as simple as:

```bash
livetagger add "Drums|Hihat" "Drums|HiHat|Closed" --commit
```

This command has various options you can use to tweak the behaviour:

* `--include` (or `-i`) allows you to specify which files will be processed, using a [glob pattern](https://www.digitalocean.com/community/tools/glob). Some fun ways to use this:
    * To process nested folders, pass `--include "**/*"`.
    * To process files containing the word 'Kick', pass `--include "*Kick*"`.
* `--commit` (or `-c`) makes the command save its changes.
    * Without this, the command will just log what files would be impacted.
    * **I strongly suggest running without `--commit` before making any big changes, to make sure the command is going to do what you're expecting!**

There are also several other commands available:

* `livetagger remove` removes certain tags from the specified files.
* `livetagger remove-all` removes *all* tags from the specified files.

For more detailed info on the options available, run `livetagger --help`.

### Tag Naming

Live stores its tags in the format `Category|Tag|Sub Tag`, with the names matching what is displayed in the 'Edit' panel of Live's browser (including spaces).

If you specify a category/tag/subtag that does not exist, Live will create it automatically. Watch out for typos!

## Notes

* This tool works by manually modifying the XMP metadata files that Live creates. If Ableton change the format of those files, this tool will probably break!
* Whenever LiveTagger makes a change to the metadata, it will write a backup first. If you need to revert the changes, go to the `Ableton Folder Info` subfolder of the folder you ran the tool on, and replace the `.xmp` file with the backup. Later versions may add commands for this. 
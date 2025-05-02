# LiveTagger

LiveTagger is a utility which allows you to batch edit Ableton Live 12's library tags from the command line.

It allows you to quickly automate tasks that would be tedious to do via Live's UI - for example, with a single command, you can tag all files with 'Kick' in the name as kick drums, no matter how deeply nested they are in your sample folder.

> [!CAUTION]
> * This software is **not** endorsed or supported by Ableton.
> * Careless use (or bugs) may mess up your existing manual tagging.
> * **Use at your own risk!**

## Installing

To download LiveTagger, go to the [releases page](https://github.com/17cupsofcoffee/LiveTagger/releases) and download the .zip file for your platform. Extract this into a location that is on your system `PATH`.

Alternatively, if you want to build from source, clone this repo and run `cargo build`.

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

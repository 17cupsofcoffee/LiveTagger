mod commands;

use std::collections::{HashMap, HashSet};
use std::ffi::OsStr;

use std::fs;
use std::path::PathBuf;

use anyhow::Context;
use clap::{Args, Parser, Subcommand};
use glob::glob;

use livemeta::{self, FolderMetadata};
use tracing::info;

#[derive(Parser, Debug)]
#[command(version, about, long_about = None)]
#[command(propagate_version = true)]
struct Cli {
    #[command(subcommand)]
    command: Command,
}

#[derive(Subcommand, Debug)]
enum Command {
    /// Adds tags to a set of files.
    Add(TagChangeArgs),

    /// Removes tags from a set of files.
    Remove(TagChangeArgs),

    /// Removes all tags from a set of files.
    RemoveAll(FilesystemArgs),
}

/// CLI flags for operating on files.
#[derive(Args, Debug)]
struct FilesystemArgs {
    /// A glob pattern specifying which files should be processed.
    #[arg(short, long, global(true), value_name = "GLOB", default_value = "*")]
    include: String,

    /// Saves changes to the filesystem. Run without this first, to make sure you're tagging the correct files!
    #[arg(short, long, global(true))]
    commit: bool,

    /// Creates backups of any changed metadata.
    #[arg(short, long, global(true))]
    backup: bool,
}

/// CLI flags for batch tag operations.
#[derive(Args, Debug)]
struct TagChangeArgs {
    /// The tags to apply to the matched files.
    #[arg(required(true))]
    tags: Vec<String>,

    #[command(flatten)]
    fs: FilesystemArgs,
}

fn main() -> anyhow::Result<()> {
    let cli = Cli::parse();

    tracing_subscriber::fmt().with_target(false).init();

    match cli.command {
        Command::Add(args) => process_xmp(&args.fs, |doc, files| {
            commands::add_tags(doc, files, &args.tags)
        })?,

        Command::Remove(args) => process_xmp(&args.fs, |doc, files| {
            commands::remove_tags(doc, files, &args.tags)
        })?,

        Command::RemoveAll(args) => process_xmp(&args, commands::remove_all_tags)?,
    }

    Ok(())
}

/// Finds all files matching the provided parameters, applies some logic to each folder's
/// metadata document (creating one from scratch if needed), then saves to disk if
/// changes have been made.
fn process_xmp<F>(args: &FilesystemArgs, mut action: F) -> anyhow::Result<()>
where
    F: FnMut(&mut FolderMetadata, HashSet<String>) -> anyhow::Result<()>,
{
    let folders = search_for_sample_folders(&args.include)?;

    for (folder, files) in folders {
        info!("Processing {}", folder.display());

        let xmp_path = livemeta::get_folder_metadata_path(&folder);

        let (mut xmp, new_file) = if xmp_path.exists() {
            (FolderMetadata::from_xmp_file(&xmp_path)?, false)
        } else {
            (FolderMetadata::new()?, true)
        };

        action(&mut xmp, files)?;

        if xmp.is_dirty() {
            xmp.set_creator_tool("Updated by LiveTagger")?;

            if new_file {
                xmp.update_create_date()?;
            } else {
                xmp.update_metadata_date()?;
            }

            if args.commit {
                if args.backup {
                    let backup_path = xmp_path.with_extension("xmp.bak");

                    fs::rename(&xmp_path, &backup_path)?;
                    info!("Backup written to {}", backup_path.display())
                }

                fs::write(&xmp_path, &xmp.to_xml()?)?;

                info!("Metadata updated for {}", folder.display())
            }
        } else {
            info!("No changes required for {}", folder.display());
        }
    }

    Ok(())
}

/// Finds all sample files matching a given glob, as well as their corresponding parent folders.
fn search_for_sample_folders(include: &str) -> anyhow::Result<HashMap<PathBuf, HashSet<String>>> {
    let mut folders: HashMap<PathBuf, HashSet<String>> = HashMap::new();

    for entry in glob(include).context("Invalid include glob")? {
        let path = entry.context("Invalid path")?;

        if livemeta::is_metadata(&path) || path.is_dir() {
            continue;
        }

        if !livemeta::is_supported_sample_format(&path) {
            info!(
                "Skipping {} as it doesn't look like an audio file",
                path.display()
            );

            continue;
        }

        let (Some(parent), Some(filename)) =
            (path.parent(), path.file_name().and_then(OsStr::to_str))
        else {
            continue;
        };

        match folders.get_mut(parent) {
            Some(folder) => {
                folder.insert(filename.to_string());
            }

            None => {
                let mut set = HashSet::new();
                set.insert(filename.to_string());

                folders.insert(parent.to_path_buf(), set);
            }
        }
    }

    Ok(folders)
}

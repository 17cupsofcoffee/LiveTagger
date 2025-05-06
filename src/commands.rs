use std::collections::HashSet;

use livemeta::{FolderMetadata, ItemSelector};
use tracing::info;

/// Adds tags to the specified files.
///
/// If an entry for a file does not exist yet in the metadata document, it will be added.
pub fn add_tags(
    doc: &mut FolderMetadata,
    mut files: HashSet<String>,
    tags: &[String],
) -> anyhow::Result<()> {
    let item_count = doc.item_count();

    for i in 1..=item_count {
        let mut tags_added = Vec::new();

        let item = ItemSelector::new(i)?;

        let filename = doc.get_filename(&item)?;

        if files.take(&filename).is_some() {
            let keyword_count = doc.keyword_count(&item);
            let mut keywords = HashSet::new();

            for i in 1..=keyword_count {
                keywords.insert(doc.get_keyword(&item, i)?);
            }

            for tag in tags {
                if !keywords.contains(tag) {
                    doc.push_keyword(&item, tag.clone())?;
                    tags_added.push(tag.as_str());
                }
            }

            if !tags_added.is_empty() {
                info!("Added tags to {}: {}", filename, tags_added.join(", "));
            }
        }
    }

    for (i, new_file) in files.into_iter().enumerate() {
        let mut tags_added = Vec::new();

        let item = ItemSelector::new(item_count + i + 1)?;

        doc.set_filename(&item, new_file.clone())?;

        for tag in tags {
            doc.push_keyword(&item, tag.clone())?;
            tags_added.push(tag.as_str());
        }

        if !tags_added.is_empty() {
            info!("Added tags to {}: {}", &new_file, tags_added.join(", "));
        }
    }

    Ok(())
}

/// Removes tags from the specified files.
///
/// This will not remove the files themselves from the metadata document, even
/// if all the keywords are gone - while keywords are currently the only
/// metadata stored for each file, Ableton could potentially add
/// additional data in future versions.
pub fn remove_tags(
    doc: &mut FolderMetadata,
    mut files: HashSet<String>,
    tags: &[String],
) -> anyhow::Result<()> {
    let item_count = doc.item_count();

    for i in 1..=item_count {
        let mut tags_removed = Vec::new();

        let item = ItemSelector::new(i)?;

        let filename = doc.get_filename(&item)?;

        if files.take(&filename).is_some() {
            let keyword_count = doc.keyword_count(&item);

            let mut deleted_count = 0;

            // We iterate in reverse to avoid invalidating the indices
            // when elements get deleted.
            for i in (1..=keyword_count).rev() {
                let keyword = doc.get_keyword(&item, i)?;

                if tags.contains(&keyword) {
                    doc.delete_keyword(&item, i)?;
                    tags_removed.push(keyword);

                    deleted_count += 1;
                }
            }

            if deleted_count == keyword_count {
                doc.delete_keywords(&item)?;
            }

            if !tags_removed.is_empty() {
                info!(
                    "Removed tags from {}: {}",
                    &filename,
                    tags_removed.join(", ")
                );
            }
        }
    }

    Ok(())
}

/// Removed all tags from the specified files.
///
/// This will not remove the files themselves from the metadata document -
/// while keywords are currently the only metadata stored for each file,
/// Ableton could potentially add additional data in future versions.
pub fn remove_all_tags(doc: &mut FolderMetadata, mut files: HashSet<String>) -> anyhow::Result<()> {
    let item_count = doc.item_count();

    for i in 1..=item_count {
        let item = ItemSelector::new(i)?;

        let filename = doc.get_filename(&item)?;

        if files.take(&filename).is_some() {
            doc.delete_keywords(&item)?;

            info!("Removed all tags from {}", &filename);
        }
    }

    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn should_add_tags() -> anyhow::Result<()> {
        let initial = include_str!("test_data/initial.xml");
        let expected = include_str!("test_data/tags_added.xml");

        let mut meta = FolderMetadata::from_xmp_str(initial)?;

        let mut files = HashSet::new();

        files.insert("bd1.wav".into());
        files.insert("bd2.wav".into());
        files.insert("bd3.wav".into());

        add_tags(&mut meta, files, &["Drums|Kick".into(), "CustomTag".into()])?;

        assert!(meta.is_dirty());
        pretty_assertions::assert_eq!(meta.to_xml().unwrap(), expected.replace("\r\n", "\n"));

        Ok(())
    }

    #[test]
    fn should_remove_tags() -> anyhow::Result<()> {
        let initial = include_str!("test_data/initial.xml");
        let expected = include_str!("test_data/tags_removed.xml");

        let mut meta = FolderMetadata::from_xmp_str(initial)?;

        let mut files = HashSet::new();

        files.insert("bd1.wav".into());
        files.insert("bd2.wav".into());
        files.insert("bd3.wav".into());

        remove_tags(
            &mut meta,
            files,
            &["Creator|17cupsofcoffee".into(), "NonExistentTag".into()],
        )?;

        assert!(meta.is_dirty());
        pretty_assertions::assert_eq!(meta.to_xml().unwrap(), expected.replace("\r\n", "\n"));

        Ok(())
    }

    #[test]
    fn should_remove_all_tags() -> anyhow::Result<()> {
        let initial = include_str!("test_data/initial.xml");
        let expected = include_str!("test_data/tags_removed_all.xml");

        let mut meta = FolderMetadata::from_xmp_str(initial)?;

        let mut files = HashSet::new();

        files.insert("bd1.wav".into());
        files.insert("bd2.wav".into());
        files.insert("bd3.wav".into());

        remove_all_tags(&mut meta, files)?;

        assert!(meta.is_dirty());
        pretty_assertions::assert_eq!(meta.to_xml().unwrap(), expected.replace("\r\n", "\n"));

        Ok(())
    }
}

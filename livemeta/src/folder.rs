use std::ffi::OsStr;
use std::fs;
use std::path::{Path, PathBuf};

use xmp_toolkit::{FromStrOptions, ToStringOptions, XmpDateTime, XmpMeta, XmpValue, xmp_ns};

use crate::error::{Error, Result};

const ABLETON_NS: &str = "https://ns.ableton.com/xmp/fs-resources/1.0/";

static METADATA_TEMPLATE: &str = r#"
<x:xmpmeta xmlns:x="adobe:ns:meta/" x:xmptk="XMP Core 5.6.0">
    <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
        <rdf:Description rdf:about="" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:ablFR="https://ns.ableton.com/xmp/fs-resources/1.0/" xmlns:xmp="http://ns.adobe.com/xap/1.0/">
            <dc:format>application/vnd.ableton.folder</dc:format>
            <ablFR:resource>folder</ablFR:resource>
            <ablFR:items>
                <rdf:Bag></rdf:Bag>
            </ablFR:items>
        </rdf:Description>
    </rdf:RDF>
</x:xmpmeta>"#;

/// Returns the path to a given folder's Ableton Live metadata.
pub fn get_folder_metadata_path(folder: &Path) -> PathBuf {
    folder.join("Ableton Folder Info/dc66a3fa-0fe1-5352-91cf-3ec237e9ee90.xmp")
}

/// Returns whether or not a path points at Ableton Live folder metadata.
///
/// This includes the 'Ableton Folder Info' subdirectory itself, as well as
/// any files contained within it.
pub fn is_folder_metadata(path: &Path) -> bool {
    path.iter()
        .filter_map(OsStr::to_str)
        .any(|component| component == "Ableton Folder Info")
}

/// Ableton Live metadata for a folder.
#[derive(Debug)]
pub struct FolderMetadata {
    xmp: XmpMeta,
    dirty: bool,
}

impl FolderMetadata {
    /// Create an empty document.
    pub fn new() -> Result<FolderMetadata> {
        Self::from_xmp_str(METADATA_TEMPLATE)
    }

    /// Reads a document from a `&str`.
    pub fn from_xmp_str(data: &str) -> Result<FolderMetadata> {
        Ok(FolderMetadata {
            xmp: XmpMeta::from_str_with_options(data, FromStrOptions::default())?,
            dirty: false,
        })
    }

    /// Reads a document from a `.xmp` file.
    pub fn from_xmp_file(path: &Path) -> Result<FolderMetadata> {
        let data = fs::read_to_string(path)?;

        Self::from_xmp_str(&data)
    }

    /// Returns whether the document has changed since it was loaded.
    pub fn is_dirty(&self) -> bool {
        self.dirty
    }

    /// Outputs the document as XML.
    pub fn to_xml(&self) -> Result<String> {
        let xml = self.xmp.to_string_with_options(
            ToStringOptions::default()
                .omit_packet_wrapper()
                .set_indent_string("    ".into()),
        )?;

        Ok(xml)
    }

    /// Sets the 'CreatorTool' property on the document.
    pub fn set_creator_tool(&mut self, value: impl Into<String>) -> Result {
        self.xmp
            .set_property(xmp_ns::XMP, "CreatorTool", &XmpValue::new(value.into()))?;

        self.dirty = true;

        Ok(())
    }

    /// Sets the 'CreateDate' property on the document to the current date and time.
    pub fn update_create_date(&mut self) -> Result {
        self.xmp.set_property_date(
            xmp_ns::XMP,
            "CreateDate",
            &XmpValue::new(XmpDateTime::current()?),
        )?;

        self.dirty = true;

        Ok(())
    }

    /// Sets the 'MetadataDate' property on the document to the current date and time.
    pub fn update_metadata_date(&mut self) -> Result {
        self.xmp.set_property_date(
            xmp_ns::XMP,
            "MetadataDate",
            &XmpValue::new(XmpDateTime::current()?),
        )?;

        self.dirty = true;

        Ok(())
    }

    /// Returns the number of items (aka tagged files) in the document.
    pub fn item_count(&self) -> usize {
        self.xmp.array_len(ABLETON_NS, "items")
    }

    /// Reads the filename of an item in the document.
    pub fn get_filename(&self, item: &ItemSelector) -> Result<String> {
        self.xmp
            .property(ABLETON_NS, &item.filename)
            .map(|v| v.value)
            .ok_or(Error::MissingField(""))
    }

    /// Sets the filename of an item in the document.
    pub fn set_filename(&mut self, item: &ItemSelector, value: impl Into<String>) -> Result {
        self.xmp
            .set_property(ABLETON_NS, &item.filename, &XmpValue::new(value.into()))?;

        self.dirty = true;

        Ok(())
    }

    /// Returns the number of keywords (aka tags) for an item in the document.
    pub fn keyword_count(&self, item: &ItemSelector) -> usize {
        self.xmp.array_len(ABLETON_NS, &item.keywords.value)
    }

    /// Reads a keyword from an item in the document.
    pub fn get_keyword(&self, item: &ItemSelector, i: usize) -> Result<String> {
        self.xmp
            .array_item(ABLETON_NS, &item.keywords.value, i as i32)
            .map(|v| v.value)
            .ok_or(Error::MissingField(""))
    }

    /// Adds a keyword to an item in the document.
    pub fn push_keyword(&mut self, item: &ItemSelector, value: impl Into<String>) -> Result {
        self.xmp
            .append_array_item(ABLETON_NS, &item.keywords, &XmpValue::new(value.into()))?;

        self.dirty = true;

        Ok(())
    }

    /// Deletes a keyword from an item in the document.
    ///
    /// This will not remove the item itself, even if all the keywords are gone -
    /// while keywords are currently the only metadata stored for each file,
    /// Ableton could potentially add additional data in future versions.
    pub fn delete_keyword(&mut self, item: &ItemSelector, i: usize) -> Result {
        self.xmp
            .delete_array_item(ABLETON_NS, &item.keywords.value, i as i32)?;

        self.dirty = true;

        Ok(())
    }

    /// Deletes all keywords from an item in the document.
    ///
    /// This will not remove the item itself - while keywords are currently the
    /// only metadata  stored for each file, Ableton could potentially add
    /// additional data in future versions.
    pub fn delete_keywords(&mut self, item: &ItemSelector) -> Result {
        self.xmp.delete_property(ABLETON_NS, &item.keywords.value)?;

        self.dirty = true;

        Ok(())
    }
}

/// Paths for an individual item in a metadata document.
pub struct ItemSelector {
    filename: String,
    keywords: XmpValue<String>,
}

impl ItemSelector {
    /// Creates a new item selector for an item in a documents.
    ///
    /// This does not validate that the item actually exists!
    pub fn new(i: usize) -> Result<ItemSelector> {
        let item_path = XmpMeta::compose_array_item_path(ABLETON_NS, "items", i as i32)?;

        let filename =
            XmpMeta::compose_struct_field_path(ABLETON_NS, &item_path, ABLETON_NS, "filePath")?;

        let keywords =
            XmpMeta::compose_struct_field_path(ABLETON_NS, &item_path, ABLETON_NS, "keywords")?;

        Ok(ItemSelector {
            filename,
            keywords: XmpValue::new(keywords).set_is_array(true),
        })
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn should_detect_folder_metadata() {
        assert!(is_folder_metadata(Path::new(
            "C:/foo/Ableton Folder Info/file.txt"
        )));
    }

    #[test]
    fn should_ignore_non_metadata_file() {
        assert!(!is_folder_metadata(Path::new("C:/foo/sound.wav")));
    }

    #[test]
    fn should_get_folder_metadata_path() {
        assert_eq!(
            get_folder_metadata_path(Path::new("C:/foo")),
            PathBuf::from("C:/foo/Ableton Folder Info/dc66a3fa-0fe1-5352-91cf-3ec237e9ee90.xmp")
        );
    }

    #[test]
    fn should_add_tags() -> Result {
        let initial = include_str!("test_data/initial.xml");
        let expected = include_str!("test_data/tags_added.xml");

        let mut meta = FolderMetadata::from_xmp_str(initial)?;

        let mut files = HashSet::new();

        files.insert("bd1.wav".into());
        files.insert("bd2.wav".into());
        files.insert("bd3.wav".into());

        let tags = ["Drums|Kick".into(), "CustomTag".into()];

        add_tags(&mut meta, files, &tags)?;

        assert!(meta.is_dirty());
        pretty_assertions::assert_eq!(meta.to_xml().unwrap(), expected.replace("\r\n", "\n"));

        Ok(())
    }

    #[test]
    fn should_remove_tags() -> Result {
        let initial = include_str!("test_data/initial.xml");
        let expected = include_str!("test_data/tags_removed.xml");

        let mut meta = FolderMetadata::from_xmp_str(initial)?;

        let mut files = HashSet::new();

        files.insert("bd1.wav".into());
        files.insert("bd2.wav".into());
        files.insert("bd3.wav".into());

        let tags = ["Creator|17cupsofcoffee".into(), "NonExistentTag".into()];

        remove_tags(&mut meta, files, &tags)?;

        assert!(meta.is_dirty());
        pretty_assertions::assert_eq!(meta.to_xml().unwrap(), expected.replace("\r\n", "\n"));

        Ok(())
    }

    #[test]
    fn should_remove_all_tags() -> Result {
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

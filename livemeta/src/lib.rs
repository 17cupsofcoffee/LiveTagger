mod error;
mod folder;
mod sample;

pub use error::*;
pub use folder::*;
pub use sample::*;

use std::path::Path;

/// Returns whether or not a path points at Ableton Live metadata.
pub fn is_metadata(path: &Path) -> bool {
    is_sample_metadata(path) || is_folder_metadata(path)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn should_detect_folder_metadata() {
        assert!(is_metadata(Path::new(
            "C:/foo/Ableton Folder Info/file.txt"
        )));
    }

    #[test]
    fn should_detect_sample_metadata() {
        assert!(is_metadata(Path::new("C:/foo/metadata.asd")));
        assert!(is_metadata(Path::new("C:/foo/metadata.ASD")));
    }

    #[test]
    fn should_ignore_non_metadata_file() {
        assert!(!is_metadata(Path::new("C:/foo/sound.wav")));
    }
}

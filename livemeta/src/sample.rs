use std::ffi::OsStr;
use std::path::Path;

static SUPPORTED_EXTS: &[&str] = &[
    "wav", "wave", "aif", "aiff", "flac", "ogg", "mp3", "mp4", "m4a",
];

/// Returns whether or not a path points at an Ableton Live sample analysis file.
pub fn is_sample_metadata(path: &Path) -> bool {
    path.extension()
        .and_then(OsStr::to_str)
        .filter(|ext| ext.eq_ignore_ascii_case("asd"))
        .is_some()
}

/// Returns whether a path's extension matches any of Ableton Live's supported
/// sample formats.
///
/// See <https://help.ableton.com/hc/en-us/articles/211427589-Supported-Audio-File-Formats>.
pub fn is_supported_sample_format(path: &Path) -> bool {
    let Some(ext) = path.extension().and_then(OsStr::to_str) else {
        return false;
    };

    SUPPORTED_EXTS
        .iter()
        .any(|supported| supported.eq_ignore_ascii_case(ext))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn should_detect_sample_files() {
        let samples = [
            "sound.wav",
            "sound.wave",
            "sound.aif",
            "sound.aiff",
            "sound.flac",
            "sound.ogg",
            "sound.mp3",
            "sound.mp4",
            "sound.m4a",
        ];

        for sample in samples {
            assert!(is_supported_sample_format(Path::new(sample)));
            assert!(is_supported_sample_format(Path::new(
                &sample.to_uppercase()
            )));
        }

        assert!(!is_supported_sample_format(Path::new("sound.exe")));
    }
}

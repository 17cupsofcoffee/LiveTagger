use std::fmt::{self, Display};

use xmp_toolkit::XmpError;

#[derive(Debug)]
pub enum Error {
    Io(std::io::Error),
    Xmp(XmpError),
    MissingField(&'static str),
}

impl std::error::Error for Error {
    fn source(&self) -> Option<&(dyn std::error::Error + 'static)> {
        match self {
            Error::Io(e) => Some(e),
            Error::Xmp(e) => Some(e),
            Error::MissingField(_) => None,
        }
    }
}

impl Display for Error {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Error::Io(_) => write!(f, "io error"),
            Error::Xmp(_) => write!(f, "xmp error"),
            Error::MissingField(field) => write!(f, "missing field: {}", field),
        }
    }
}

impl From<std::io::Error> for Error {
    fn from(value: std::io::Error) -> Self {
        Error::Io(value)
    }
}

impl From<XmpError> for Error {
    fn from(value: XmpError) -> Self {
        Error::Xmp(value)
    }
}

pub type Result<T = ()> = std::result::Result<T, Error>;

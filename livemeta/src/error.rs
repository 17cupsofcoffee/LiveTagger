use thiserror::Error;
use xmp_toolkit::XmpError;

#[derive(Error, Debug)]
pub enum Error {
    #[error("io error")]
    Io(#[from] std::io::Error),

    #[error("xmp error")]
    Xmp(#[from] XmpError),

    #[error("missing field: {0}")]
    MissingField(&'static str),
}

pub type Result<T = ()> = std::result::Result<T, Error>;

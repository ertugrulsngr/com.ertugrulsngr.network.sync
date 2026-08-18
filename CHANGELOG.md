# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Authority send runs on a configurable network update stage instead of the time-service tick event.
- Send and interpolation stages are inspector fields.

### Fixed

- Relative position always uses the anchor scale. Relative scale only affects the scale channel.


# Security Policy

Fileway processes arbitrary user-uploaded files. Please report vulnerabilities responsibly.

## Reporting a vulnerability

**Do not open a public GitHub issue for security vulnerabilities.**

Email: security@fileway.io

Please include:
- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Any suggested fix

We will respond within 48 hours and coordinate a fix before any public disclosure.

## Scope

Areas of particular interest given the nature of this project:
- File upload handling and validation
- Format detection bypass
- Path traversal via filenames
- Zip bomb or decompression bomb vulnerabilities
- Output content injection (macros in DOCX, scripts in SVG/HTML output)
- Session token handling

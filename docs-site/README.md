# Loopai Documentation Site

This directory contains the Docusaurus-based documentation website for Loopai.

## 🌐 Live Site

Documentation is deployed to GitHub Pages: **https://iyulab.github.io/loopai/**

## 🚀 Quick Start

### Installation

```bash
npm install
```

### Local Development

```bash
npm start
```

Starts a local development server at `http://localhost:3000` with live reload.

### Build

```bash
npm run build
```

Generates static files in the `build/` directory.

### Sync Documentation

```bash
npm run sync-docs
```

Syncs markdown files from `/docs` to `/docs-site/docs` with Docusaurus formatting.

## 📁 Structure

- `docs/` - Documentation content (synced from `/docs`)
- `blog/` - Blog posts and release notes
- `src/` - Custom React components and pages
- `static/` - Static assets (images, files)
- `scripts/` - Build and sync scripts
- `docusaurus.config.ts` - Site configuration
- `sidebars.ts` - Sidebar structure

## 🚀 Deployment

Automatic deployment via GitHub Actions on push to `main` branch.

Manual deployment:

```bash
GIT_USER=<username> npm run deploy
```

## 📚 More Information

See the full README in this directory for detailed documentation on:
- Content management
- Customization
- Troubleshooting
- Contributing guidelines

## 📄 License

MIT License

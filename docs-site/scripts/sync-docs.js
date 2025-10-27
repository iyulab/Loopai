/**
 * Sync Documentation Script
 *
 * This script syncs markdown files from /docs to /docs-site/docs
 * and performs necessary transformations for Docusaurus.
 */

const fs = require('fs');
const path = require('path');

// Paths
const ROOT_DIR = path.join(__dirname, '../..');
const SOURCE_DOCS_DIR = path.join(ROOT_DIR, 'docs');
const TARGET_DOCS_DIR = path.join(__dirname, '../docs');
const SDK_DIR = path.join(ROOT_DIR, 'sdk');

/**
 * Add Docusaurus frontmatter to markdown content
 */
function addFrontmatter(content, metadata) {
  const { title, sidebar_label, sidebar_position, description } = metadata;

  let frontmatter = '---\n';
  if (title) frontmatter += `title: ${title}\n`;
  if (sidebar_label) frontmatter += `sidebar_label: ${sidebar_label}\n`;
  if (sidebar_position !== undefined) frontmatter += `sidebar_position: ${sidebar_position}\n`;
  if (description) frontmatter += `description: ${description}\n`;
  frontmatter += '---\n\n';

  // Remove existing title if present (Docusaurus handles it)
  content = content.replace(/^#\s+.*\n/, '');

  return frontmatter + content;
}

/**
 * Transform internal links for Docusaurus
 */
function transformLinks(content) {
  // Transform relative links from /docs to Docusaurus format
  content = content.replace(/\[([^\]]+)\]\(\.\.\/docs\/([^\)]+)\.md\)/g, '[$1](/docs/$2)');
  content = content.replace(/\[([^\]]+)\]\(\.\/([^\)]+)\.md\)/g, '[$1](./$2)');
  content = content.replace(/\[([^\]]+)\]\(([^\/][^\)]+)\.md\)/g, '[$1](./$2)');

  // Transform SDK links
  content = content.replace(/\[([^\]]+)\]\(\.\.\/sdk\/(dotnet|python|typescript)\/([^\)]+)\)/g, '[$1](/docs/sdks/$2)');

  return content;
}

/**
 * Copy and transform a markdown file
 */
function copyAndTransform(sourcePath, targetPath, metadata) {
  console.log(`Processing: ${path.relative(ROOT_DIR, sourcePath)}`);

  let content = fs.readFileSync(sourcePath, 'utf-8');

  // Transform content
  content = transformLinks(content);
  content = addFrontmatter(content, metadata);

  // Ensure target directory exists
  const targetDir = path.dirname(targetPath);
  if (!fs.existsSync(targetDir)) {
    fs.mkdirSync(targetDir, { recursive: true });
  }

  // Write file
  fs.writeFileSync(targetPath, content, 'utf-8');
  console.log(`  ✓ Written to: ${path.relative(ROOT_DIR, targetPath)}`);
}

/**
 * Main sync function
 */
function syncDocs() {
  console.log('🔄 Syncing documentation...\n');

  // Ensure target directory exists
  if (!fs.existsSync(TARGET_DOCS_DIR)) {
    fs.mkdirSync(TARGET_DOCS_DIR, { recursive: true });
  }

  // 1. Create intro page
  const introContent = `# Welcome to Loopai

Loopai is a **Human-in-the-Loop AI Self-Improvement Framework** - infrastructure middleware for building adaptive AI-powered applications.

## What is Loopai?

Transform expensive LLM calls into self-improving programs that run anywhere with complete observability and data sovereignty.

### Core Capabilities

- 🚀 **Multi-Language SDKs**: .NET, Python, TypeScript
- ⚡ **High Performance**: <10ms execution latency
- 💰 **Cost Efficient**: 82-97% cost reduction vs direct LLM
- 🔌 **Plugin System**: Extensible architecture
- 📊 **Production Ready**: 170+ tests passing

## Quick Links

- [Getting Started](/docs/guides/getting-started)
- [Architecture Overview](/docs/guides/architecture)
- [SDK Documentation](/docs/sdks/overview)
- [API Reference](/docs/api/overview)

## Version

Current: **v0.3** - SDK & Extensibility Complete

See [Blog](/blog) for release notes and updates.
`;

  fs.writeFileSync(
    path.join(TARGET_DOCS_DIR, 'intro.md'),
    addFrontmatter(introContent, {
      title: 'Welcome to Loopai',
      sidebar_position: 1,
      description: 'Human-in-the-Loop AI Self-Improvement Framework'
    }),
    'utf-8'
  );
  console.log('✓ Created intro.md\n');

  // 2. Copy guides
  console.log('📁 Processing Guides...');
  const guidesDir = path.join(TARGET_DOCS_DIR, 'guides');
  if (!fs.existsSync(guidesDir)) {
    fs.mkdirSync(guidesDir, { recursive: true });
  }

  const guidesMappings = [
    { source: 'GETTING_STARTED.md', target: 'getting-started.md', title: 'Getting Started', position: 1 },
    { source: 'ARCHITECTURE.md', target: 'architecture.md', title: 'Architecture', position: 2 },
    { source: 'DEPLOYMENT.md', target: 'deployment.md', title: 'Deployment', position: 3 },
    { source: 'DEVELOPMENT.md', target: 'development.md', title: 'Development', position: 4 },
    { source: 'PLUGIN_DEVELOPMENT_GUIDE.md', target: 'plugin-development.md', title: 'Plugin Development', position: 5 },
  ];

  guidesMappings.forEach(({ source, target, title, position }) => {
    const sourcePath = path.join(SOURCE_DOCS_DIR, source);
    const targetPath = path.join(guidesDir, target);

    if (fs.existsSync(sourcePath)) {
      copyAndTransform(sourcePath, targetPath, {
        title,
        sidebar_position: position,
      });
    } else {
      console.log(`  ⚠ Not found: ${source}`);
    }
  });

  console.log('\n📚 Sync complete!\n');
  console.log('Next steps:');
  console.log('  1. Review generated files in docs-site/docs/');
  console.log('  2. Create additional pages for SDKs, API, and Examples');
  console.log('  3. Run: cd docs-site && npm start');
}

// Run sync
try {
  syncDocs();
} catch (error) {
  console.error('❌ Error syncing docs:', error);
  process.exit(1);
}

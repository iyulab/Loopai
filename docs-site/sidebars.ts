import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

/**
 * Creating a sidebar enables you to:
 - create an ordered group of docs
 - render a sidebar for each doc of that group
 - provide next/previous navigation

 The sidebars can be generated from the filesystem, or explicitly defined here.

 Create as many sidebars as you want.
 */
const sidebars: SidebarsConfig = {
  // Main documentation sidebar
  docsSidebar: [
    'intro',
    {
      type: 'category',
      label: 'Introduction',
      items: [
        'introduction/what-is-loopai',
        'introduction/key-features',
        'introduction/use-cases',
      ],
    },
    {
      type: 'category',
      label: 'Guides',
      items: [
        'guides/getting-started',
        'guides/architecture',
        'guides/deployment',
        'guides/plugin-development',
        'guides/performance-tuning',
      ],
    },
    {
      type: 'category',
      label: 'SDKs',
      items: [
        'sdks/overview',
        'sdks/dotnet',
        'sdks/python',
        'sdks/typescript',
      ],
    },
    {
      type: 'category',
      label: 'Examples',
      items: [
        'examples/spam-detection',
        'examples/sentiment-analysis',
        'examples/batch-processing',
      ],
    },
  ],

  // API Reference sidebar
  apiSidebar: [
    {
      type: 'category',
      label: 'REST API',
      items: [
        'api/overview',
        'api/authentication',
        'api/tasks',
        'api/execution',
        'api/batch-operations',
      ],
    },
    {
      type: 'category',
      label: 'Advanced',
      items: [
        'api/streaming',
        'api/webhooks',
        'api/metrics',
      ],
    },
  ],
};

export default sidebars;

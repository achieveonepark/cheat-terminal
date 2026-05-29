// @ts-check
const {themes} = require('prism-react-renderer');

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'Cheat Terminal',
  tagline: 'Unity 런타임 개발자 콘솔 — [Terminal] 어트리뷰트로 명령 추가',

  url: 'https://somiri.dev',
  baseUrl: '/cheat-terminal/',
  organizationName: 'achieveonepark',
  projectName: 'cheat-terminal',
  deploymentBranch: 'gh-pages',
  trailingSlash: true,

  onBrokenLinks: 'warn',
  markdown: {
    hooks: {
      onBrokenMarkdownLinks: 'warn',
    },
  },

  i18n: {
    defaultLocale: 'ko',
    locales: ['ko', 'en', 'ja', 'zh-CN'],
    localeConfigs: {
      ko: {
        label: '한국어',
        htmlLang: 'ko-KR',
      },
      en: {
        label: 'English',
        htmlLang: 'en-US',
      },
      ja: {
        label: '日本語',
        htmlLang: 'ja-JP',
      },
      'zh-CN': {
        label: '中文',
        htmlLang: 'zh-CN',
      },
    },
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          routeBasePath: '/', // docs-only site (no landing page split)
          sidebarPath: require.resolve('./sidebars.js'),
          editUrl:
            'https://github.com/achieveonepark/cheat-terminal/tree/main/Documentation~/',
        },
        blog: false,
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      navbar: {
        title: 'Cheat Terminal',
        items: [
          {
            type: 'docSidebar',
            sidebarId: 'docs',
            label: 'Docs',
            position: 'left',
          },
          {
            type: 'doc',
            docId: 'changelog',
            label: 'Changelog',
            position: 'left',
          },
          {
            type: 'localeDropdown',
            position: 'right',
          },
          {
            href: 'https://github.com/achieveonepark/cheat-terminal',
            label: 'GitHub',
            position: 'right',
          },
        ],
      },
      footer: {
        style: 'dark',
        copyright: 'Cheat Terminal — Unity Runtime Developer Console.',
      },
      prism: {
        theme: themes.github,
        darkTheme: themes.dracula,
        additionalLanguages: ['csharp', 'bash', 'json'],
      },
      colorMode: {
        defaultMode: 'dark',
        respectPrefersColorScheme: true,
      },
    }),
};

module.exports = config;

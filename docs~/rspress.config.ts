import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig } from '@rspress/core';

const docsDirectory = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  root: 'content/docs',
  base: '/cheat-terminal/',
  outDir: 'out',
  title: 'Cheat Terminal',
  description: 'Unity runtime developer terminal documentation.',
  lang: 'en',
  icon: '/icon.svg',
  logo: {
    light: '/logo.svg',
    dark: '/logo.svg',
  },
  globalStyles: path.join(docsDirectory, 'styles/global.css'),
  locales: [
    {
      lang: 'en',
      label: 'English',
      title: 'Cheat Terminal',
      description: 'Unity runtime developer terminal documentation.',
    },
    {
      lang: 'ko',
      label: '한국어',
      title: '치트 터미널',
      description: 'Unity 런타임 개발자 터미널 문서.',
    },
    {
      lang: 'ja',
      label: '日本語',
      title: 'Cheat Terminal',
      description: 'Unity ランタイム開発者ターミナルのドキュメント。',
    },
    {
      lang: 'zh',
      label: '简体中文',
      title: 'Cheat Terminal',
      description: 'Unity 运行时开发者终端文档。',
    },
  ],
  themeConfig: {
    darkMode: 'dark',
    socialLinks: [
      {
        icon: 'github',
        mode: 'link',
        content: 'https://github.com/achieveonepark/cheat-terminal',
      },
    ],
  },
});

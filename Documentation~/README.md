# Cheat Terminal — Documentation site

[Docusaurus](https://docusaurus.io/) 기반 문서 사이트입니다.
(이 폴더는 `~` 로 끝나므로 Unity 패키지 import에서 제외됩니다.)

## 로컬 실행

```bash
cd Documentation~
npm install
npm run start      # http://localhost:3000/cheat-terminal/
```

## 빌드

```bash
npm run build      # build/ 에 정적 사이트 생성
npm run serve      # 빌드 결과 미리보기
```

## 배포 (GitHub Pages)

`docusaurus.config.js` 의 `url` / `baseUrl` 은
`https://achieveonepark.github.io/cheat-terminal/` 기준으로 설정돼 있습니다.

```bash
GIT_USER=achieveonepark npm run deploy   # gh-pages 브랜치로 배포
```

> CI 자동 배포가 필요하면 `.github/workflows` 에 GitHub Pages 액션을 추가하면 됩니다.

## 구조

```
Documentation~/
├─ docs/                 # 문서 (intro, getting-started, adding-commands, commands)
├─ src/css/custom.css    # 테마 (터미널 그린)
├─ static/               # 정적 파일
├─ docusaurus.config.js
└─ sidebars.js
```

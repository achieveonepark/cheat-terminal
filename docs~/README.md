# Cheat Terminal — Documentation site

Rspress 기반 다국어 문서 사이트입니다.
(이 폴더는 `~` 로 끝나므로 Unity 패키지 import에서 제외됩니다.)

## 로컬 실행

```bash
cd docs~
npm install
npm run dev      # http://localhost:3000/cheat-terminal/
```

## 빌드 / 미리보기

```bash
npm run build
npm run preview
```

## 배포 (GitHub Pages)

자동 배포는 `.github/workflows/docs.yml`가 `docs~/` 변경을 push할 때마다 실행합니다.
빌드 결과는 `docs~/out`에 생성되며, 문서 허브가 이 폴더를 `site/cheat-terminal/`에 복사합니다.
최초 1회만 저장소 설정이 필요합니다:

> GitHub repo → **Settings → Pages → Build and deployment → Source: GitHub Actions**

## 구조

```text
docs~/
├─ content/docs/         # ko/en/ja/zh 문서 및 Rspress 내비게이션
├─ public/               # 정적 파일
├─ rspress.config.ts
└─ package.json
```

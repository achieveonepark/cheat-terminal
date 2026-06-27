# Cheat Terminal — Documentation site

Fumadocs + Next.js 기반 문서 사이트입니다.
(이 폴더는 `~` 로 끝나므로 Unity 패키지 import에서 제외됩니다.)

## 로컬 실행

```bash
cd docs~
npm install
npm run dev      # http://localhost:3000/
```

## 빌드 / 미리보기

```bash
npm run build
npm run start    # 빌드 결과를 Next server로 미리보기
```

## 배포 (GitHub Pages)

자동 배포는 `.github/workflows/docs.yml`가 `docs~/` 변경을 push할 때마다 실행합니다.
최초 1회만 저장소 설정이 필요합니다:

> GitHub repo → **Settings → Pages → Build and deployment → Source: GitHub Actions**

## 구조

```text
docs~/
├─ app/                  # Next.js app router
├─ content/docs/         # ko/en/ja/zh 문서
├─ lib/                  # Fumadocs source 설정
├─ public/               # 정적 파일
├─ source.config.ts      # Fumadocs MDX 설정
└─ package.json
```

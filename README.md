# A4 Image Cutter / A4 이미지 분할 편집기

Version 1.0.0 · Windows desktop application · MIT License

## Overview / 개요

**English**  
A4 Image Cutter is an offline Windows desktop application for arranging a large image across multiple A4 pages. It provides interactive grid editing, per-page locking, configurable print margins, image export, and print preview.

**한국어**  
A4 이미지 분할 편집기는 큰 이미지를 여러 장의 A4 용지에 배치하는 오프라인 Windows 데스크톱 프로그램입니다. 격자 편집, 페이지별 이미지 고정, 인쇄 여백 설정, 이미지 파일 저장 및 인쇄 미리보기를 지원합니다.

## Key Features / 주요 기능

- **English:** Move, resize, rotate, and flip an image directly on the A4 grid.  
  **한국어:** A4 격자에서 이미지를 직접 이동·확대·축소·회전·반전할 수 있습니다.
- **English:** Preview each A4 page with independent top, bottom, left, and right margins in millimetres.  
  **한국어:** 상·하·좌·우 여백을 mm 단위로 설정하고 각 A4 페이지에서 확인할 수 있습니다.
- **English:** Lock selected page fragments and continue editing the remaining image independently.  
  **한국어:** 선택한 페이지의 이미지 조각을 고정한 후 나머지 이미지를 독립적으로 편집할 수 있습니다.
- **English:** Select one or multiple pages by clicking empty page space, Ctrl-clicking, or dragging a selection rectangle.  
  **한국어:** 빈 페이지 공간 클릭, Ctrl+클릭 또는 선택 사각형 드래그로 하나 이상의 페이지를 선택할 수 있습니다.
- **English:** Exclude selected pages from saved files and printing.  
  **한국어:** 선택한 페이지를 이미지 저장 및 인쇄 대상에서 제외할 수 있습니다.
- **English:** Use the enhanced print preview for printer settings, page navigation, one/two-page layouts, and zoom.  
  **한국어:** 확장 인쇄 미리보기에서 프린터 설정, 페이지 이동, 한/두 페이지 보기 및 확대·축소를 사용할 수 있습니다.
- **English:** Undo with `Ctrl+Z` and redo with `Ctrl+X`.  
  **한국어:** `Ctrl+Z`로 이전 작업, `Ctrl+X`로 다음 작업을 실행할 수 있습니다.

## Requirements / 실행 환경

**English**

- Windows 10 or Windows 11
- .NET Framework 4.x
- A printer driver is required only when printing
- No network connection is required while using the application

**한국어**

- Windows 10 또는 Windows 11
- .NET Framework 4.x
- 인쇄 기능을 사용할 때만 프린터 드라이버 필요
- 프로그램 사용 중 인터넷 연결 불필요

## Download / 다운로드

**English**  
Download `A4ImageCutter-v1.0.0-win.zip` from the [GitHub Releases page](https://github.com/ds2lie85/A4ImageCutter/releases), extract it, and run `A4ImageCutter.exe`. Windows may display a SmartScreen warning because the executable is not code-signed.

**한국어**  
[GitHub Releases 페이지](https://github.com/ds2lie85/A4ImageCutter/releases)에서 `A4ImageCutter-v1.0.0-win.zip`을 내려받아 압축을 푼 다음 `A4ImageCutter.exe`를 실행합니다. 실행 파일에 코드 서명이 없으므로 Windows SmartScreen 경고가 표시될 수 있습니다.

## Quick Start / 빠른 시작

1. **English:** Click **Open Image** or drag an image file into the window.  
   **한국어:** **이미지 열기**를 누르거나 이미지 파일을 창으로 끌어놓습니다.
2. **English:** Arrange the image in **Grid View**. Drag the image to move it, drag a blue edge to resize it, and right-drag to rotate it.  
   **한국어:** **격자 보기**에서 이미지를 배치합니다. 이미지를 드래그하면 이동하고, 파란 경계를 드래그하면 크기가 조절되며, 우클릭 드래그하면 회전합니다.
3. **English:** Open **Temporary Split** to inspect individual pages, margins, and locked fragments.  
   **한국어:** **임시 분할**을 열어 개별 페이지, 여백 및 고정 조각을 확인합니다.
4. **English:** Use the right-click menu to lock fragments or exclude pages from output.  
   **한국어:** 우클릭 메뉴에서 이미지 조각을 고정하거나 저장 대상에서 제외합니다.
5. **English:** Click **Save** to create page images or **Print** to open the enhanced preview.  
   **한국어:** **저장**으로 페이지별 이미지 파일을 만들거나 **인쇄**로 확장 미리보기를 엽니다.

## Controls / 조작 방법

| English | 한국어 |
|---|---|
| Left-drag on an image: move | 이미지 위 좌클릭 드래그: 이동 |
| Drag a blue image edge: resize | 파란 이미지 경계 드래그: 크기 조절 |
| Right-drag on an image: rotate | 이미지 위 우클릭 드래그: 회전 |
| Mouse wheel: resize the editable image | 마우스 휠: 편집 이미지 확대·축소 |
| Ctrl+mouse wheel: zoom the workspace around the pointer | Ctrl+마우스 휠: 커서 중심 화면 확대·축소 |
| Ctrl while moving/resizing: keep the image inside the printable boundary | Ctrl+이동/크기 조절: 인쇄 가능 경계 내부로 제한 |
| Click empty page space: select a page | 이미지가 없는 페이지 공간 클릭: 격자 선택 |
| Ctrl+click empty page space: add/remove selection | Ctrl+빈 공간 클릭: 선택 추가·해제 |
| Drag on the empty workspace: select multiple pages | 빈 배경 드래그: 여러 페이지 선택 |
| Ctrl+Z / Ctrl+X: undo / redo | Ctrl+Z / Ctrl+X: 이전 작업 / 다음 작업 |

## Supported Image Formats / 지원 이미지 형식

**English**  
JPEG, PNG, GIF, BMP, and TIFF are supported through Windows image codecs. Animated GIF files are handled as a still image.

**한국어**  
Windows 이미지 코덱을 통해 JPEG, PNG, GIF, BMP 및 TIFF를 지원합니다. 움직이는 GIF는 정지 이미지로 처리됩니다.

## Saving and Printing / 저장 및 인쇄

**English**  
Saved page images are created next to the original image with names such as `name_part001.png`. Pages marked **Exclude from Save** are omitted from both saving and printing. Printing uses the configured margins and opens the enhanced preview before a job is sent to the selected Windows printer.

**한국어**  
분할 이미지는 원본 이미지와 같은 폴더에 `이름_part001.png` 형식으로 생성됩니다. **저장 제외**로 지정한 페이지는 저장과 인쇄에서 모두 빠집니다. 인쇄 시 설정된 여백이 유지되며, Windows 프린터로 작업을 보내기 전에 확장 미리보기가 열립니다.

## Build from Source / 소스에서 빌드

**English**  
The project intentionally has no third-party runtime dependencies. Open PowerShell on Windows and run:

**한국어**  
이 프로젝트는 외부 런타임 라이브러리를 사용하지 않습니다. Windows PowerShell에서 다음 명령을 실행합니다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

**English:** The executable is written to `dist\A4ImageCutter.exe`.  
**한국어:** 실행 파일은 `dist\A4ImageCutter.exe`에 생성됩니다.

## Create a Release Package / 배포 패키지 생성

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\package.ps1 -Version 1.0.0
```

**English:** The release archive is written to `release\A4ImageCutter-v1.0.0-win.zip`.  
**한국어:** 배포 압축 파일은 `release\A4ImageCutter-v1.0.0-win.zip`에 생성됩니다.

## Project Structure / 프로젝트 구조

| Path | English | 한국어 |
|---|---|---|
| `Program.cs` | Application entry point and version metadata | 프로그램 진입점 및 버전 정보 |
| `MainForm.cs` | Editor, preview, printing, and UI implementation | 편집기, 미리보기, 인쇄 및 UI 구현 |
| `build.ps1` | Local Windows build script | Windows 로컬 빌드 스크립트 |
| `package.ps1` | Versioned ZIP packaging script | 버전별 ZIP 패키지 생성 스크립트 |
| `.github/workflows` | GitHub build and release automation | GitHub 빌드 및 릴리스 자동화 |
| `docs` | Release notes and supporting documentation | 릴리스 노트 및 보조 문서 |

## Privacy / 개인정보 및 네트워크

**English**  
Image processing is performed locally. The application does not upload images, collect analytics, or require an account.

**한국어**  
모든 이미지 처리는 로컬에서 수행됩니다. 이미지를 업로드하거나 사용 통계를 수집하지 않으며 계정도 필요하지 않습니다.

## License / 라이선스

**English**  
Copyright (c) 2026 ds2lie85. Released under the [MIT License](LICENSE). Modification and redistribution are permitted under its terms.

**한국어**  
Copyright (c) 2026 ds2lie85. [MIT License](LICENSE)로 배포됩니다. 라이선스 조건에 따라 수정 및 재배포할 수 있습니다.

## Links / 링크

- Repository / 저장소: <https://github.com/ds2lie85/A4ImageCutter>
- Issues / 문제 보고: <https://github.com/ds2lie85/A4ImageCutter/issues>
- Releases / 배포 파일: <https://github.com/ds2lie85/A4ImageCutter/releases>

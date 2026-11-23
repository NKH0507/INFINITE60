# INFINITE 60 — Claude Code 인수인계

claude.ai 대화(11/16~11/23)에서 정한 내용을 정리한 파일입니다. 이 파일을 먼저 읽고 이어서 작업하세요.

## 프로젝트
- 모바일(안드로이드) 2D 벽돌깨기 + 성장 게임. 참고작: 벽돌깨기, 벽돌 주식회사, Tasty Planet
- 대학생 4명: 남규혁(기획 + 프로그래밍, GitHub `NKH0507`, 이 파일의 사용자), woonge06(프로그래머), gboon-gboon(프로그래머, 강화·UI 담당), 아티스트 1명
- 마감 11/30. 게임잼은 11/20에 진행함
- 유니티 **6000.3.21f1** 고정 (다른 버전으로 열면 ProjectVersion.txt와 packages-lock.json이 바뀌니 커밋하지 말 것)
- 사용자 PC 프로젝트 경로: `C:\Unity\INFINITE60`
- 사용자 PC에 Unity MCP(MCP for Unity)가 설치되어 있음. 콘솔 읽기, 플레이 모드, 씬 확인에 사용 가능

## 사용자와 일하는 방식
- 한국어로, 쉬운 말로 설명. 결과 위주로 짧게
- **main에 머지는 사용자가 "머지해"라고 할 때만.** 작업은 브랜치(`nkh-xxx`)에서 하고 PR로 합침
- 커밋 메시지는 한국어로 무엇을 왜 바꿨는지
- 다른 팀원 브랜치(gboon-ui 등)는 사용자 허락 없이 수정하지 않음
- 유니티 씬/프리팹을 YAML로 직접 고칠 때는 fileID/guid를 조심. 가능하면 Unity MCP로 에디터에서 확인

## 깃허브 상태 (11/23 기준)
- 저장소: https://github.com/NKH0507/INFINITE60
- main @ `54a3bc6` (PR #14: nkh-art 아트·연출 + gboon-ui 강화 시스템 합침)
- `nkh-upgrade-price`: 강화 가격 상승 + 이 CLAUDE.md. **아직 main에 안 합침**
- `nkh-art`, `merge-art-upgrade`: main에 다 들어감. 지워도 됨
- `gboon-ui`: gboon님 작업 브랜치 (main에 다 들어간 상태)
- 윈도우는 파일 이름 대소문자를 구분하지 않음. 이름 대소문자만 바꿀 땐 `git mv`로

## 핵심 규칙 (확정)
- 벽돌을 부수면 그 벽돌 **최대 체력의 10%**를 공격력으로 흡수 (관통으로 부숴도 흡수)
- **공 크기 = 공격력**. 공이 벽돌에 주는 대미지 = 공격력
- **관통**: 공격력 ≥ 벽돌 최대 체력 × 3이면 튕기지 않고 뚫음. 관통 가능한 벽돌은 노란 테두리
- **줌아웃(다음 스테이지)**: 공격력이 기준에 도달하면 화면이 2배 줌아웃. 이전 스테이지는 화면 아래 가운데에 절반 크기로 남음. 공 크기도 절반이 되지만 공격력은 유지
  - 기준 = 0.8 × (시작 공격력 10 + 0.1 × 1~n스테이지 모든 벽돌 체력 합). 흡수율 강화와 상관없이 항상 10% 기준(`GameManager.BaseAbsorbRate`)
  - 줌 판정은 흡수할 때만
- **공 크기 수식**: 기본 크기 × (1 + 진행도), 진행도 = (공격력 − 스테이지 시작값) / (줌 기준 − 시작값). 다음 줌 직전에 2배가 되고, 줌아웃되면 원래 크기로 돌아옴
- **시간**: 60초 시작, 공 놓치면 −20초(강화로 감소), 스테이지 클리어 시간 추가는 기본 0초(강화로 최대 +5초)
- **스테이지 9개, 챕터 3개**: 1~3 방, 4~6 도시, 7~9 우주. **9스테이지 = 보스**(`BossManager.StartBoss(1)`), 보스 방식은 현재 구현 그대로 유지
- 멀티볼은 없음 (제거 확정)

## 강화 (gboon 구현, `PlayerStats.cs`, `StatUpgradeButton.cs`)
최대 10레벨, 레벨은 PlayerPrefs에 저장. 시작 화면 `Canvas/UpgradePanel`의 버튼 6개.

| 강화 | 0레벨 → 10레벨 | 레벨당 |
|---|---|---|
| 시작 공격력 | 10 → 15 | +0.5 (×5%) |
| 흡수율 | 10% → 15% | +0.5%p |
| 패들 가로 크기 | 1.5 → 2.5 | +0.1 |
| 클리어 시간 추가 | 0초 → 5초 | +0.5초 |
| 공 놓침 페널티 | −20초 → −10초 | −1초 |
| 코인 획득 | 100% → 200% | +10% |

- 가격: 50 × 1.25^현재레벨 (50, 62, 78, ... 373). `nkh-upgrade-price` 브랜치에 구현됨
- F2 키 = 강화 레벨 초기화(테스트용, 출시 전 제거)

## 아트·연출 (nkh-art에서 구현, main에 있음)
- 아트 스타일: **배경·벽돌·공·패들은 파스텔 도트**, 글꼴과 UI 버튼은 도트 아님(둥글고 납작한 스타일)
- 이미지 위치: `Assets/Art/` (Backgrounds, Bricks, Cracks, Balls, Paddle)
- 도트 이미지 설정: Filter Mode Point, 압축 None, Mesh Type Full Rect. 벽돌은 Draw Mode Sliced, Size 1×1로 기존 크기에 맞춤
- 벽돌 크기 0.8 × 0.4 (2:1)
- **벽돌 이미지**: 1~3 나무, 4~6 하늘색 벽돌, 7~9 은하. 스테이지 프리팹(Stage_1~9)의 오버라이드로 지정
- **금 간 단계**: `Brick.cs` + 프리팹의 Crack 자식. 체력 75/50/25% 이하에서 1/2/3단계. 1~6은 `Crack_1~3`, 7~9는 `Crack_Chapter3_1~3`(초신성)
  - 알려진 문제: `Crack_1~3`은 해상도가 커서(1774px) 게임에서 줄이면 점선처럼 보임. 벽돌과 같은 64×32칸 도트(512×256 파일)로 다시 받기로 함
- **흡수 연출** `AbsorbFx.cs`: 벽돌이 깨지면 벽돌 색 삼각형 조각 5개가 흩어졌다가(0.2초) 멈칫(0.1초) 후 0.05초 간격으로 공에 빨려 들어감. 공격력은 즉시 오르고, 보이는 크기는 조각이 닿을 때 커지며 공이 반짝(`Ball.OnFragmentAbsorbed`)
- **배경** `ChapterBackground.cs` (GameScene의 Background 오브젝트)
  - 챕터 안에서 4배 → 2배 → 1배로 줌아웃. 기준점은 패들 윗면(벽돌 축소 기준점과 같음). StageManager가 전환 시작 때 `PlayZoomOut` 호출
  - 3→4(방→도시): 방이 도시 가운데 건물 **창문** 속으로 빨려 들어감. 4스테이지 동안 창문에 흐리게 남고 5스테이지에서 사라짐
  - 6→7(도시→우주): 도시가 줌아웃되며 사라지는 동안 지구 위 **구름 하나**를 크게 확대한 모습이 화면을 덮고, 우주 배경이 크게 줌아웃되며 구름이 작아져 7스테이지(4배)가 됨. 구름은 지구 위 작은 섬 자리에 계속 있음
  - 구름 그림은 지금 코드로 만든 임시. 아티스트 구름을 받으면 인스펙터 Cloud Sprite에 넣기
- 패들: 빨간 도트 패들, 9-slice(양 끝 유지). 패들 크기 강화가 Scale X를 키워서 최대 강화에서 양 끝이 넓어짐 → Size로 늘리는 방식으로 바꾸면 좋음
- 공 스킨 이미지(미사용, 나중에 스킨 탭): `Ball_Default`(기본), `Ball_Skin2_Lamp`(가로등), `Ball_Skin3_Moon`(달)

## 주요 스크립트
`GameManager`(스테이지·줌 판정·결과), `StageManager`(스테이지 전환 연출), `Ball`, `Brick`, `PaddleMove`, `TimeManager`, `CoinManager`, `PlayerStats`, `StatUpgradeButton`, `ChapterBackground`, `AbsorbFx`, `BossManager`, `Chapter1Boss`, `PanelChange`, `SceneChanger`. 씬: StartScene(메인·강화), GameScene

## 남은 일 / 정하지 않은 것
- `nkh-upgrade-price` 머지 여부 확인
- 스테이지별 벽돌 체력: 1~7이 체력 10으로 같아서 난이도 곡선이 없음. 8, 9는 체력 50 은하 벽돌 24개로 같은 배치
- 금 간 이미지(1~6용) 다시 받기, 구름 그림 받기
- 화면: 세로 9:16(1080×1920)으로 바꾸기로 했지만 프로젝트 설정은 아직 가로
- 진동(Haptic), 화면 흔들림, 일시정지·결과 화면 UI 마무리
- 스킨 탭(공 스킨 3종)

## 참고 링크 (claude.ai 아티팩트)
- 기획서 v0.4: https://claude.ai/artifact/AdAg2aimnPabLbWUzAQFFw
- 밸런스 시트: https://claude.ai/artifact/P9YwRdCMeSg885fnePg7Aw
- UI 초안: https://claude.ai/artifact/JTJFaKeBQSDdbRNPR8WUYe
- 게임잼 체크리스트: https://claude.ai/artifact/CMstVg9La5CHALfBn6Ji3u

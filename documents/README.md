# 설계 문서

게임의 아이템/인벤토리/캐릭터/AI 시스템의 **엔지니어링 설계**(클래스, 인터페이스, 네임스페이스)를 정리한 문서 모음입니다. 실제 코드는 [`Assets/Scripts`](../Assets/Scripts)에 있으며(아직 설계 문서만 있고 코드가 없는 항목은 표에 "설계만 완료"로 표시), 아래 문서는 그 설계 의도와 확장 지점을 설명합니다.

게임 자체의 **기획 문서**(장르/조작/UX/수치 밸런싱)는 별도로 [`Docs/`](../Docs) 폴더에 있습니다(디자인 브랜치에서 병합). 두 폴더는 관점이 다르므로 서로 대체하지 않으며, 겹치는 부분의 정합성은 [design-conflict-review.md](design-conflict-review.md)에서 검증합니다.

실제 코드가 어떻게 나뉘어 있고 모듈 간 의존 관계가 어떤 그래프를 이루는지는 [codebase-map.md](codebase-map.md)에 별도로 정리되어 있습니다 — 이 문서는 사람의 가독성보다 코드 파악 속도를 우선한 기계 판독용 인덱스입니다.

### 아이템 / 인벤토리

- [item-system.md](item-system.md) — 아이템 데이터, 플랫 인벤토리(창고형 슬롯), 3D 월드 아이템, 제작 시스템의 기초 뼈대
- [inventory-system.md](inventory-system.md) — pocket/rig/backpack 등 장비에 따라 크기·모양이 달라지는 그리드 인벤토리, 장비 슬롯, 플레이어 상태(수분/허기), 인벤토리 화면 UI(우측 Rig/Pocket/Backpack 스크롤 스택 포함)
- [quickslot-and-skills.md](quickslot-and-skills.md) — 아이템과 스킬을 함께 담는 `IQuickSlottable` 퀵슬롯(뱅크 2개 × 7칸 = 14칸, 키 `4`~`0`, `` ` `` 로 스왑 — `1`/`2`/`3`은 무기 전용 고정 키라 제외), 확장 가능한 스킬(`SkillData`) 뼈대
- [world-item-factory.md](world-item-factory.md) — 아이템/무기를 월드 오브젝트로 만드는 경로를 `WorldItemFactory` 하나로 모음(구현됨): 종류별 스포너 등록, 프리팹 없을 때 기본 표현, 드롭 흩뿌리기·바닥 스냅, `Items`→`Weapons` 순환을 피하는 방향 역전 + 기존 문서와의 충돌 검토
- [interaction-system.md](interaction-system.md) — 문 열기/대화하기 등 "키 입력으로 트리거되는 동작"을 `IInteractable` 하나로 묶고, 감지(`InteractionDetector`)·입력·실행을 분리해 새 상호작용 종류를 계속 추가할 수 있게 설계
- [window-system.md](window-system.md) — 인벤토리/설정/맵/임무 확인 같은 전체화면 창을 `FullScreenWindowEntry` 카탈로그로 등록해 관리(상호 배타, 새 종류는 카탈로그에 항목만 추가), 퍼즐/이벤트를 띄우는 팝업(스택)도 `WindowManager` 하나로 통합
- [weapon-system.md](weapon-system.md) — 총기 2정+근접무기 1개 장착(`WeaponLoadout`), 파츠 모딩(`WeaponPartData`가 곧 인벤토리 아이템), 탄약→탄창 삽탄→격발 파이프라인, `IInteractable` 재사용으로 적도 무기를 줍고 즉시 쓸 수 있는 `WeaponPickup`

### 캐릭터 / 전투 / AI

- [combat-system.md](combat-system.md) — "피격당할 수 있다"를 캐릭터와 무관한 `IDamageable` 인터페이스로 분리, 오브젝트(파괴 가능한 상자 등)에도 재사용 가능하도록 설계. `IDamageable`/`HealthComponent`/`ArmorComponent` 구현됨
- [character-system.md](character-system.md) — Player / NPC / Enemy(Normal, Elite, Boss)를 상속이 아닌 컴포넌트 합성으로 구성. Player·공통 요소(Faction/CharacterMotor 등) 구현됨, NPC/Enemy는 아직 설계 문서만 존재
- [ai-state-machine.md](ai-state-machine.md) — Enemy AI를 상태 패턴(State Pattern)으로 설계, Elite/Boss(다중 페이즈) 확장 방식 포함. 프레임워크(`IAiState`/`AiStateMachine`/`AiContext`/`AiSensor`)와 공통 상태 5개 구현됨
- [npc-roles.md](npc-roles.md) — NPC를 전투 도움용(Companion)/마을용(Village)으로 분화, `IAiBrain`을 Enemy 전용에서 공용 프레임워크로 일반화(설계 문서만 존재)
- [player-attributes.md](player-attributes.md) — 스태미나(달리기/점프 제한)와 지구력·힘·행운 등 기초 스탯이 각종 확률·능력치에 반영되는 구조. 구현됨
- [hud-system.md](hud-system.md) — 1인칭/3인칭 슈팅 HUD 레이아웃(체력·방어구·무기 / 상태 / 퀵슬롯 / 나침반 / 미니맵). 나침반·미니맵이 `IWorldMarker` 레지스트리를 공유. 구현됨
- [dialogue-system.md](dialogue-system.md) — 텍스트 대화: 노드 그래프(`DialogueSequence`)를 순수 C# `DialogueRunner`가 재생하고, 뷰(`IDialogueView`)·이벤트 핸들러(`IDialogueEventHandler`)·스토리 플래그(세이브 연동)를 분리. 대화 중 게임 입력 차단은 `WindowManager.AddInputBlocker`. 시네마틱·퀘스트/상점 모달은 후속
- [dialogue-text-effects.md](dialogue-text-effects.md) — 대사 안 특정 구간의 색·움직임(좌우 흔들림·물결·떨림·무지개)·일시정지 연출. 인라인 태그 + 순수 C# 파서/효과 + TMP 글자별 정점 조작. **1단계 구현됨**, 폰트는 NeoHyundai 동적 TMP 애셋

### 씬 / 지속성

- [scene-and-persistence-system.md](scene-and-persistence-system.md) — Title/Loading(재사용)/InGameLobby/InGameCombat 씬 분리와 전환(`SceneFlowController`), 씬을 오가도 사라지면 안 되는 런타임 데이터(`PlayerRuntimeContext`), 게임을 껐다 켜도 유지되는 세이브 데이터(`ISaveDataProvider`/`SaveGameService`)를 분리해 설계. 프레임워크 구현됨(기존 서브시스템의 `ISaveDataProvider` 연동, 실제 씬 자산, `PlayerRuntimeContext` 배선은 아직)

### 테스트 환경

- [testing.md](testing.md) — 자동 EditMode 테스트(122개): 어셈블리 분리(`Game`/`Game.Editor`/`Game.Tests.EditMode`), 실행 방법, 범위, 변이 확인, 아직 없는 곳
- [test-scenes.md](test-scenes.md) — 시스템별 테스트 씬(`Assets/Scenes/Tests`)과 OnGUI 디버그 하니스 목록, 사용법, 헤드리스 CLI로 검증할 수 없어서 에디터에서 직접 확인해야 하는 항목

### 설계 검증

- [design-conflict-review.md](design-conflict-review.md) — `documents/`(엔지니어링 설계)와 [`Docs/`](../Docs)(기획 문서, `claude/shooting-game-design-doc-h4cm46` 브랜치에서 병합)를 대조 검증한 결과. 퀵슬롯 vs 무기 슬롯의 숫자키 충돌은 해결됨(`1`/`2`/`3` 무기 고정, 퀵슬롯은 `4`~`0`), 무기 정확도/반동 모델 불일치는 여전히 남음, 나머지는 통합 공백 또는 정합 확인

## 코드 위치 요약

| 영역 | 경로 | 네임스페이스 | 상태 |
|---|---|---|---|
| 아이템 코어 데이터 | `Assets/Scripts/Items/Core` | `Game.Items` | 구현됨 |
| 플랫 인벤토리(창고형) | `Assets/Scripts/Items/Inventory` | `Game.Items` | 구현됨 |
| 3D 월드 아이템 | `Assets/Scripts/Items/World` | `Game.Items` (`IInteractable` 구현) | 구현됨 |
| 제작 | `Assets/Scripts/Items/Crafting` | `Game.Items` | 구현됨 |
| 그리드 인벤토리 | `Assets/Scripts/Items/Grid` | `Game.Items.Grid` | 구현됨 |
| 장비(포켓/릭/백팩) | `Assets/Scripts/Items/Equipment` | `Game.Items.Equipment` | 구현됨 |
| 아이템/인벤토리 UI | `Assets/Scripts/Items/UI` | `Game.Items.UI` | 구현됨 |
| 공용 UI(스탯바, 배경 가림) | `Assets/Scripts/UI` | `Game.UI` | 구현됨 |
| 플레이어 상태 | `Assets/Scripts/Player/Stats` | `Game.Player` | 구현됨 |
| 스킬 뼈대(`SkillData`, `ScanSkillData` 예시) | `Assets/Scripts/Skills` | `Game.Skills` | 구현됨 |
| 퀵슬롯(`IQuickSlottable`, 4~0 7칸/뱅크 스왑) | `Assets/Scripts/QuickSlot` | `Game.QuickSlot` | 구현됨 |
| 퀵슬롯 UI(상시 HUD) | `Assets/Scripts/QuickSlot/UI` | `Game.QuickSlot.UI` | 구현됨 |
| 상호작용 인터페이스/감지/입력 | `Assets/Scripts/Interaction` | `Game.Interaction` | 구현됨 |
| 상호작용 프롬프트 UI | `Assets/Scripts/Interaction/UI` | `Game.Interaction.UI` | 구현됨 |
| 창 프레임워크(`IWindow`, `WindowManager`, 팝업) | `Assets/Scripts/UI/Windows` | `Game.UI.Windows` | 구현됨 |
| 팝업 콘텐츠 예시(퍼즐/이벤트) | `Assets/Scripts/UI/Windows/Popups` | `Game.UI.Windows.Popups` | 구현됨 |
| 피격 가능 인터페이스/체력/방어구 | `Assets/Scripts/Combat` | `Game.Combat` | 구현됨 |
| 캐릭터 공통(Faction, 스탯, 이동) | `Assets/Scripts/Characters/Core` | `Game.Characters` | 구현됨 |
| 플레이어 컨트롤러/스태미나/기초 스탯 | `Assets/Scripts/Characters/Player` | `Game.Characters.Player` | 구현됨 |
| NPC 컨트롤러(Village/CombatHelper 공통) | `Assets/Scripts/Characters/Npc` | `Game.Characters.Npc` | 구현됨 |
| Enemy 컨트롤러/데이터(Normal/Elite/Boss) | `Assets/Scripts/Characters/Enemy` | `Game.Characters.Enemy` | 구현됨 |
| AI 상태 머신 프레임워크(`IAiBrain` 포함) | `Assets/Scripts/AI/StateMachine` | `Game.AI` | 구현됨 |
| AI 구체 상태들(Idle/Patrol/Chase/Attack/Dead) | `Assets/Scripts/AI/States` | `Game.AI.States` | 구현됨 |
| HUD 표시 제어/핫바 레이아웃/체력·방어구·무기 패널 | `Assets/Scripts/HUD` | `Game.HUD` | 구현됨 |
| 월드 마커 레지스트리(나침반/미니맵 공유) | `Assets/Scripts/HUD/Markers` | `Game.HUD.Markers` | 구현됨 |
| 나침반 | `Assets/Scripts/HUD/Compass` | `Game.HUD.Compass` | 구현됨 |
| 미니맵 | `Assets/Scripts/HUD/Minimap` | `Game.HUD.Minimap` | 구현됨 |
| 무기 시스템(총기/근접/파츠/탄약/장착/픽업/1·2·3 고정키) | `Assets/Scripts/Weapons` | `Game.Weapons` | 구현됨 |
| 무기 슬롯 UI | `Assets/Scripts/Weapons/UI` | `Game.Weapons.UI` | 구현됨 |
| 씬 카탈로그/전환 컨트롤러 | `Assets/Scripts/SceneFlow` | `Game.SceneFlow` | 구현됨 |
| 세이브/로드(ISaveDataProvider) | `Assets/Scripts/Persistence` | `Game.Persistence` | 구현됨(PlayerVitals/Health/컨테이너·인벤토리/대화 플래그 어댑터 연동 완료, 퀵슬롯/AttributeSet/무기는 아직) |
| 대화(시퀀스/러너/뷰/이벤트 핸들러/플래그) | `Assets/Scripts/Dialogue` | `Game.Dialogue` | 구현됨(텍스트 대화만, 시네마틱·퀘스트/상점 이벤트는 후속) |
| 프리팹(Player/Enemy/NPC/월드 아이템/그리드·대화 UI) | `Assets/Prefabs` | — | 구현됨 |
| 테스트 씬/디버그 하니스 | `Assets/Scenes/Tests`, `Assets/Scripts/DebugHarness` | `Game.DebugHarness` | 구현됨 — [test-scenes.md](test-scenes.md) |

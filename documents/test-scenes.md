# 테스트 씬 & 디버그 하니스

시스템별로 따로 확인할 수 있게 만든 씬 모음. 씬은 `Assets/Scenes/Tests`, 씬 전용 데이터는 `Assets/Data/Tests`, OnGUI 디버그 스크립트는 `Assets/Scripts/DebugHarness`(`Game.DebugHarness`)에 있다. 하니스는 전부 테스트 전용이며 실제 빌드에 넣지 않는다 — 각 파일 doc comment에도 명시돼 있다. 모든 씬은 Build Settings에 등록돼 있다.

씬 구성은 손으로 YAML을 고치지 않고 열려 있는 Unity Editor에 `unity` CLI(`run_script`)로 만들었다.

## 씬 목록

| 씬 | 검증 대상 | 사용법 |
|---|---|---|
| `Test_Inventory` | 플랫 인벤토리, Pocket/Rig/Backpack 그리드, 월드 아이템 줍기/드롭, **드래그 앤 드롭·회전**, 더미 데이터 | 아래 "Test_Inventory" |
| `Test_QuickSlotWeapons` | 퀵슬롯 뱅크/무기 로드아웃, 스킬, 투척물 | 하니스 버튼 |
| `Test_CombatEnemy` | Normal/Elite/Boss 적 AI 브레인 | 하니스에서 피해 주기·상태 확인 |
| `Test_Npc` | Village/Companion NPC | 하니스 버튼 |
| `Test_InteractionWindows` | `WindowManager`, 문 상호작용, 플레이어 HUD | 하니스 버튼 |
| `Test_Prefabs` | `Enemy`/`VillageNpc`/`CompanionNpc` 프리팹을 `EnemySpawner`/`NpcSpawner`로 런타임 스폰 | Play만 누르면 스폰, 좌상단에 상태 표시 |
| `Test_PlayerMovement` | `Player.prefab`, WASD/Shift/Space/좌클릭 | 화면에 위치·속도·스태미나·`Grounded` 표시 |
| `Test_Dialogue` | 대화 시스템: NPC 대화(F), 선택지, 플래그 분기, 아이템 보상, 로그, 스킵 | 촌장/경비병 옆에서 F. 좌상단 하니스로 상태·플래그 확인, 시퀀스 직접 재생, 플래그 리셋 — 아래 "Test_Dialogue" |
| `SceneFlow/Boot` → `Title`/`Loading`/`Lobby`/`Combat` | 씬 전환, 세이브/로드, `PlayerRuntimeContext` | **Boot에서** Play. `SceneFlowTestHarness` 버튼으로 전환·저장·불러오기 |

`Lobby`/`Combat`의 `PlayerStandIn`에는 `HealthComponent`/`PlayerVitals`와 실제 세이브 어댑터가 붙어 있다. `PlayerStateTestHarness`로 체력/허기를 바꾸고 Boot의 Save/Load로 복원되는지 확인한다. Boot를 거치지 않고 직접 열면 `PlayerRuntimeContext`가 없다는 경고 한 줄이 정상적으로 뜬다.

## Test_Inventory

- **UGUI 패널** — Pocket/Rig/Backpack 세 `GridInventoryUIView`(화면 아래쪽). Rig/Backpack은 장착 전에는 모양이 없어 셀이 0개다(정상) — 먼저 장착해야 보인다.
- **플레이어** — 이제 `Player.prefab`이라 `WASD`/`Space`로 걸어 다닌다(예전엔 손으로 만든 오브젝트라 못 움직였음). 월드의 아이템·컨테이너를 `F`로 줍는다.
- **`InventoryTestHarness`**(왼쪽 위) — 플랫 인벤토리 추가, Pocket에 추가, Rig/Backpack 장착·해제. **해제/교체하면 컨테이너가 안의 아이템을 담은 채 하나로 월드에 떨어지고**(`F`로 다시 착용하면 내용물이 돌아옴), 교체로 밀려난 컨테이너도 자기 내용물과 함께 떨어진다.
- **`InventoryDummyDataHarness`**(오른쪽 위) — "Fill all grids randomly" / "Clear all grids" / 컨테이너 4종 장착 / 아이템 12종 개별 추가(Pocket→Rig→Backpack 순).
- **드래그** — 아이템을 잡고 끌면 실제 아이콘이 잡은 위치 그대로 따라오고, 아래 그리드에 놓일 자리가 초록(들어감)/빨강(안 들어감, 자동 배치 폴백)으로 표시된다. `R`로 90° 회전. 드롭 칸은 마우스가 아니라 아이콘 왼쪽 위 모서리 기준이다.
- **줍기** — 월드 아이템 근처에서 `F` → Pocket→Rig→Backpack 순으로 들어간다.

자동 테스트(EditMode)는 [testing.md](testing.md)에 있다.

## 헤드리스 CLI로 검증할 수 없는 것

이 씬들은 CLI로 Play 모드를 돌려 콘솔 에러 0건을 확인했지만, **Play 모드 프레임이 실제로 흐르지 않는 환경**(`Time.time`이 0.02에서 안 올라감, `Destroy`가 지연됨, 비동기 씬 로드가 끝나지 않음, 키·마우스 입력 없음)이라 아래는 에디터에서 직접 확인해야 한다.

- 실제 걷기/점프 궤적, 공중 모멘텀 유지, 착지 후 재조향(`Test_PlayerMovement`)
- 마우스 드래그·드롭 제스처, 미리보기 색, 고스트 정리, `R` 회전(`Test_Inventory`)
- Boot → Loading → Lobby/Combat 씬 전환과 저장/불러오기 왕복(`SceneFlow`)
- 대화: 타이핑 연출·선택지 포커스 색·`Esc` 취소/`Tab` 빨리 넘기기/`L` 기록 키 반응·한글 폰트 표시·**텍스트 이펙트가 실제로 움직이는 모습**(흔들림·물결·무지개·타이핑 중 깜빡임/줄바꿈 튐, 아래 Test_Dialogue), 대화 시작/종료 프레임의 F 키 충돌 방지(`Test_Dialogue`) — 러너 로직·선택지·플래그·아이템 보상·차단자 해제는 API 호출로 검증함

로직 자체(`GridItemDragMover` 이동/병합/원위치 복귀, 회전 배치, 자동 회전, 스냅 계산)는 API 호출로 검증했다.

## 새 테스트 씬을 만들 때

- Editor가 열려 있으면 `unity command run_script --file <파일.cs> --entry 클래스.메서드`로 씬/프리팹/에셋을 만든다(`eval`은 `using`/클래스 선언을 지원하지 않는다). Git Bash에서는 `/`로 시작하는 인자에 `MSYS_NO_PATHCONV=1`이 필요하다.
- 스크립트에서 `EditorSceneManager.OpenScene(..., Single)`을 호출하면 이전에 로드한 에셋 참조가 해제되므로, 씬을 연 **뒤에** `AssetDatabase.LoadAssetAtPath`로 다시 불러와 연결한다.
- 씬 UI에는 `InputSystemUIInputModule`을 쓴다(`StandaloneInputModule`은 Active Input Handling = Input System 설정에서 예외를 던진다).
- `AddSceneToBuild` 후에는 `AssetDatabase.SaveAssets()`를 호출해야 `EditorBuildSettings.asset`이 디스크에 반영된다.

## Test_Dialogue

[dialogue-system.md](dialogue-system.md) 참고. `Player.prefab`으로 걸어가서 NPC 근처(2.5m)에서 `F`.

- **촌장**(노란색) — 첫 대화: 인사 → 선택지 3개 중 "도움이 필요합니다"를 고르면 동전 5개(그리드에 자리가 없으면 발밑에 드롭)와 `got_reward` 플래그. 다시 말을 걸면 "또 왔군" 분기로 시작하고, 보상을 받은 뒤에는 세 번째 선택지("감사 인사")가 새로 나타난다. `Reset flags`로 처음 상태로 되돌린다.
- **경비병**(파란색) — 스킵 불가 시퀀스(`Tab` 빨리 넘기기가 무시됨, `Esc` 취소는 가능)와 1초 `Wait` 노드.
- **키** — `F`/`Enter` 다음(타이핑 중이면 즉시 완성), `W`/`S`·`↑`/`↓` 선택지 이동, 마우스 클릭으로도 선택, `Esc` = 대화 취소(다시 말을 걸면 처음부터), `Tab` = 빨리 넘기기(경비병은 불가), `L` = 대화 기록.
- 대화 중에는 이동·점프·달리기·상호작용·퀵슬롯이 막히고(`WindowManager` 입력 차단), 끝난 다음 프레임에 풀린다. (`PlayerLocomotion`이 Space/Shift를 직접 읽어서 처음엔 대화 중 점프가 됐다 — 이제 `windowManager`로 게이팅한다. 프리팹은 씬 오브젝트를 못 가지므로 씬마다 연결한다.)
- 상점·퀘스트 같은 **실제 창이 대화 위에 열려 있으면**(`WindowManager.IsWindowOpen`) 대화 입력은 멈추고 `Esc`는 그 창만 닫는다.
- **텍스트 이펙트 샘플**(촌장, [dialogue-text-effects.md](dialogue-text-effects.md)): 첫 인사의 "손님"이 위아래로 물결(`wave`), 다음 줄의 "무슨 일"이 파란색으로 좌우 흔들림(`color`+`sway`), 도움 수락 줄에서 동전 얘기 뒤 0.6초 멈춤(`pause`), "몸조심하게"의 "밤"이 떨림(`shake`), 감사 인사 줄이 무지개(`rainbow`), "둘러보는 중이에요" 답변은 파란색 두 구간이 좌우로 흔들리는 긴 문장. 대화 기록(`L`)에는 색만 남아야 한다.
- **다국어 확인**: 왼쪽 위 하니스의 `Language` 줄에서 `en`을 누른 뒤 촌장에게 말을 걸면(또는 `Play elder_intro`) 영어 대사가 나오고 **태그가 같은 구간에 그대로 적용**된다(`ko`로 되돌리면 한국어). `jp`는 번역이 없어서 한국어 원문이 나온다. 하니스가 언어를 바꾸는 것은 임시이며 다음 줄부터 적용된다. ([dialogue-localization.md](dialogue-localization.md))
- 폰트는 `NeoHyundai R SDF`(동적 TMP 폰트, `Assets/Fonts`) — 처음 나오는 글자는 실행 중에 아틀라스에 채워지므로 새 글자가 처음 뜰 때 미세하게 끊길 수 있다.

## 보류 중인 확인 (아직 할 수 없는 것)

- **`Test_Prefabs`, `Test_Npc`, `Test_CombatEnemy`** — 적/NPC 모델(비주얼)이 들어온 뒤에 확인한다. 지금은 캡슐이라 추격·공격·동료 따라가기를 눈으로 판단하기 어렵다.
- **`Test_QuickSlotWeapons`, `Test_InteractionWindows`** — 퀵슬롯/무기/창 UI가 실제 UI로 들어온 뒤에 확인한다.

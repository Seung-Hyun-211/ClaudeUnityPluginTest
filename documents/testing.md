# 자동 테스트 (EditMode)

순수 로직을 씬 없이 검증하는 NUnit 테스트. 씬으로 눈으로 확인하는 테스트 환경은 [test-scenes.md](test-scenes.md), 이 문서는 자동으로 돌리는 쪽이다. 테스트는 `Assets/Tests/EditMode`.

## 어셈블리 구성

Unity의 테스트 어셈블리는 기본 어셈블리(`Assembly-CSharp`)를 참조할 수 없다. 그래서 2026-09-19에 게임 코드를 이름 있는 어셈블리로 분리했다.

| 어셈블리 | 위치 | 비고 |
|---|---|---|
| `Game` | `Assets/Scripts/Game.asmdef` | 게임 코드 전체. `Unity.InputSystem`, `UnityEngine.UI`, URP(`Core.Runtime`, `Universal.Runtime`) 참조 |
| `Game.Editor` | `Assets/Scripts/Editor/Game.Editor.asmdef` | 에디터 전용(`ItemDatabaseEditor`). `Editor` 폴더 규칙은 asmdef 안에서는 적용되지 않아서 별도 asmdef가 필요하다 |
| `Game.Tests.EditMode` | `Assets/Tests/EditMode/Game.Tests.EditMode.asmdef` | 에디터 전용, `Game` 참조, NUnit |

새 외부 패키지(TextMeshPro 등)를 게임 코드에서 쓰기 시작하면 `Game.asmdef`의 참조에 추가해야 컴파일된다. 기존 씬/프리팹의 스크립트 참조는 GUID 기반이라 분리로 깨지지 않았다(14개 씬 확인).

## 실행

- 에디터: Window > General > Test Runner > EditMode.
- 열려 있는 에디터에 CLI로 실행하려면 `TestRunnerApi`를 `runSynchronously = true`로 호출하는 스크립트를 `unity command run_script`로 돌린다(`unity test`는 프로젝트를 여는 별도 에디터 프로세스를 띄우므로 이미 열린 프로젝트와 충돌한다).

## 현재 범위 (64개)

| 파일 | 대상 |
|---|---|
| `GridInventoryTests` | 배치/겹침/모양 밖, 회전 발자국, 스택 병합, 자동 회전, 가득 참, 모양 변경 시 밀려남, `Clear` |
| `GridItemDragMoverTests` | 이동, 회전 적용, 점유된 칸 → 자동 배치, 대상이 가득 차면 **원래 칸·원래 방향으로 복귀**, 스택 병합 시 수량 보존, 낡은 참조 무시 |
| `DialogueRunnerTests` | 줄 진행·로그, 타이핑 중 Submit 무시, 선택지 조건·가시 인덱스, Branch, `SetFlag`, 핸들러 없는 이벤트, 모달 일시정지와 수락/거절 분기, 스킵 후 늦은 완료 무시, 스킵 가능/불가, Wait, 없는 노드·무한 루프·빈 선택지 종료 |
| `DialogueFlagStoreTests` | 플래그·변경 이벤트, 저장 왕복, `Clear` |
| `ItemDatabaseTests` | id 조회, 빈/중복/null 검출 |
| `DropPlacementTests` / `DropPlacementGroundTests` | 흩뿌리기(단일, 간격, 결정성), 지면 탐색(트리거·`Rigidbody`·인터랙터블 무시), 지면에 얹기 |
| `WorldItemFactoryTests` / `ItemWorldSpawnerTests` | 스포너 선택 순서, 거절 시 다음 후보, 같은 타입 재등록 교체, 기본 스포너 폴백, 잘못된 요청 경고, 기본 표현·트리거·`WorldItem` 부착 |
| `SaveProviderTests` | 컨테이너/그리드 내용 저장 왕복(컨테이너, 스택, 칸, 회전), 삭제된 id·안 맞는 칸·없는 컨테이너 건너뜀, 기본 포켓 모양 유지, 플랫 인벤토리 왕복 |

## 검증 방법: 변이 확인

테스트가 실제로 버그를 잡는지 확인하려고 코드를 일부러 망가뜨려 봤다. 세 곳 모두 테스트가 실패했다(원복 후 64개 통과).

1. `GridItemDragMover` 원위치 복귀가 회전을 잊게 함 → `Move_TargetFull_RestoresTheItemToItsOriginalCellAndOrientation` 실패
2. `DialogueRunner`가 거절 분기를 무시하게 함 → `ModalEvent_PausesUntilTheHandlerCompletes_ThenFollowsAcceptedOrDeclined` 실패
3. `DropPlacement`가 인터랙터블을 지면으로 치게 함 → 처음엔 **어떤 테스트도 실패하지 않았고**(빈틈), `DropPlacementGroundTests`를 추가한 뒤 잡힘

## 테스트 작성 규칙

- `TestBase`가 만든 오브젝트를 `[TearDown]`에서 파괴한다. 항상 `NewAsset`/`NewGameObject`/`AddComponent`/`Track`으로 만든다.
- **EditMode에서는 `Awake`/`Start`가 돌지 않는다.** `Awake`가 초기화하는 컴포넌트(`Inventory`의 슬롯 배열)는 `Invoke(component, "Awake")`로 명시 호출한다. `GridInventory`는 `Awake`가 `initialShape`를 넣으므로 테스트에서는 `SetShape`로 모양을 준다(`MakeGrid`).
- 인스펙터로 설정하는 private `[SerializeField]`는 `Set(target, "field", value)`(리플렉션)로 넣는다.
- 코드가 `Debug.LogWarning`/`LogError`를 내는 경로는 `LogAssert.Expect`로 기대해야 한다 — 예상 밖의 `Error`는 테스트를 실패시킨다.

## 아직 테스트가 없는 곳

- **물리·프레임이 필요한 것**: `CharacterMotor`(접지·점프 모멘텀), 이동/입력 핸들러, 타이핑 연출, 드래그 UI(`GridItemUIView`/`GridInventoryUIView`). PlayMode 테스트(`Game.Tests.PlayMode`)가 필요하다.
- **AI/전투**: 상태 머신, `HealthComponent`, `FirearmInstance`/`WeaponLoadout`(순수 로직이 많아 다음 후보).
- **씬 전환/세이브 서비스**: `SaveGameService`, `SceneFlowController`.
- `WeaponWorldSpawner`(무기 인스턴스 보존, 기본 인스턴스 생성)는 Play 모드 API 호출로만 검증했다.

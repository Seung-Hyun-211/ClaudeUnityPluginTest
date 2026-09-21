# 설계 충돌 검증 — `documents/`(엔지니어링 설계) vs `Docs/`(기획 문서)

`claude/shooting-game-design-doc-h4cm46` 브랜치를 병합해 `Docs/기획문서_*.md` 13개를 받아왔다(2026-09-17, fast-forward 병합, git 충돌 없음 — 경로가 `documents/`와 겹치지 않아 파일 단위 충돌은 애초에 발생하지 않았다). 이 문서는 **내용 수준의 설계 충돌**을 검증한 결과다. 결론부터: **하드 충돌이던 숫자키 문제는 해결됨**(1번, `1`/`2`/`3` 무기 고정 + 퀵슬롯 `4`~`0`), **설계 모델 자체가 안 맞는 곳이 1건**(무기 정확도/반동, 2번) 남아 있고, 나머지는 대부분 "우리가 아직 안 만든 것을 기획 쪽이 구체화해준" 정합적 확장이다.

## 요약표

| # | 종류 | 대상 | 내용 |
|---|---|---|---|
| 1 | ✅ 해결됨 | 숫자키 1~4 | `1`/`2`/`3`은 무기 고정, 퀵슬롯 뱅크는 `4`~`0`(7칸)으로 축소 — 아래 1번 참고 |
| 2 | 🟠 모델 불일치 | 무기 정확도/반동 | 우리의 스칼라 스탯 vs 기획의 Spread+Recoil 다중 파라미터 모델 |
| 3 | ✅ 해결됨 | 입력 모드 전환 | `WindowManager.IsAnyWindowOpen`을 게임플레이 입력 핸들러 3종이 구독 + Escape로 닫기 — 아래 3번 참고 |
| 4 | 🟡 통합 공백(훅만 추가) | 스태미나 | `StaminaController`에 `Exhausted`/`Recovered` 이벤트 노출 완료, 실제 디버프 발동 로직은 디버프 시스템 만들 때 구독 |
| 5 | 🟡 통합 공백 | 부적(Talisman) 효과 | 우리 두 모디파이어 시스템(속성/무기스탯) 중 어디로 라우팅할지 미정 |
| 6 | ⚪ 미착수 영역 | 1인칭/3인칭 시점 전환 | 코드 전무, 기획은 상세 설계 완료 |
| 7 | ✅ 정합 확인 | Interact 키 `F` | 양쪽 독립적으로 같은 결론 |
| 8 | ✅ 정합 확인 | 월드 마커 | hud-system.md의 설계가 퀘스트 UI의 요구를 이미 충족 |
| 9 | ✅ 정합 확인 | 상호작용/대화/상점 진입점 | 기획 쪽 모든 문서가 우리 `IInteractable`/`DialogueInteractable` 자리를 정확히 비워둠 |
| 10 | ✅ 해결됨 | 무기 슬롯 개수 | 3슬롯(주/보조/근접, 키 `1`/`2`/`3`) 유지 확정. 투척물은 4번째 로드아웃 슬롯을 만들지 않고 퀵슬롯 아이템(`ThrowableItemData`)으로 처리 — 아래 10번 참고 |
| 11 | ✅ 설계·구현 완료 | 월드 아이템 팩토리 | 생성 경로 3갈래 통합 설계. `Items`→`Weapons` 순환 의존(🔴)은 방향 역전으로 해소, 나머지는 문서 갱신·기획 확인 — 아래 11번 참고 |
| 12 | ✅ 해결됨 | 컨테이너 내용물의 소유권 | 벗은/버린 리그·가방이 내용물과 따로 놀거나 컨테이너 자체가 사라지던 문제 — 내용물이 컨테이너에 속하도록 변경(플레이 테스트에서 발견), 아래 12번 참고 |
| 13 | ✅ 1단계 구현됨 | 건축(맵빌딩) 시스템 | 결정 4개(1단계 범위·저장·키·적 대응)를 받아 [building-system.md](building-system.md)로 재설계하고 **1단계(벽·바닥·문·필러)를 구현**(2026-09-21, 테스트 198개·프로브 33개 통과). 계단·사다리·NavMesh·적 대응은 2단계. 발견 내용: 구현을 막는 것은 없지만 시작 전에 정할 것 4개: 숫자키 `1`~`5`가 이미 무기·퀵슬롯(전제가 옛 상태) → 모드별 입력 게이팅, 적 AI에 길찾기(NavMesh)가 없음, 모터가 경사·계단·사다리 미지원, 저장·씬 전환 미정. 기획 문서 내부 불일치도 정리 — 아래 13번 참고 |

---

## 1. ✅ 해결됨 — 숫자키 1~4

- **우리(변경 전)**: [quickslot-and-skills.md](quickslot-and-skills.md) / `QuickSlotInputHandler.cs` — `1`~`9`,`0`을 퀵슬롯 0~9번 슬롯 활성화에 바인딩.
- **기획**: `Docs/기획문서_무기전투조작설계.md` 2장 — `1`,`2`,`3`,`4`를 `WeaponSlot1~4`(무기 슬롯 직접 선택)에 바인딩. 둘 다 같은 `Player` 컨텍스트에서 항상 동시에 켜져 있는 입력이라 **물리적으로 겹쳤다**.
- **결정**(사용자 확정): `1`/`2`/`3`은 무기(Primary/Secondary/Melee) 전용 고정 키로 완전히 분리하고, `` ` `` 키로 스왑되는 퀵슬롯 뱅크는 `4`~`9`,`0`(7칸)으로 줄인다. 무기 슬롯은 뱅크 스왑의 영향을 받지 않는다.
- **반영된 코드**: [`WeaponLoadoutInputHandler`](../Assets/Scripts/Weapons/WeaponLoadoutInputHandler.cs)(`1`/`2`/`3` → `WeaponLoadout.SwitchTo`), [`QuickSlotInputHandler`](../Assets/Scripts/QuickSlot/QuickSlotInputHandler.cs)(`4`~`9`,`0`만 사용하도록 축소), [`QuickSlotController`](../Assets/Scripts/QuickSlot/QuickSlotController.cs)(`slotsPerBank` 기본값 10→7). UI 쪽은 [`WeaponLoadoutBarUIView`](../Assets/Scripts/Weapons/UI/WeaponLoadoutBarUIView.cs)(무기 3칸) + [`QuickSlotBarUIView`](../Assets/Scripts/QuickSlot/UI/QuickSlotBarUIView.cs)(퀵슬롯 7칸)가 한 핫바처럼 나란히 배치된다. 자세한 내용은 [weapon-system.md](weapon-system.md)/[quickslot-and-skills.md](quickslot-and-skills.md) 참고.

## 2. 🟠 모델 불일치 — 무기 정확도/반동

- **우리**: [weapon-system.md](weapon-system.md)의 `WeaponStatType.Accuracy`/`RecoilControl`은 파츠 모디파이어가 합산되는 **스칼라 값 하나**([`FirearmInstance.GetEffectiveStat`](../Assets/Scripts/Weapons/FirearmInstance.cs)).
- **기획**: `Docs/기획문서_크로스헤어탄퍼짐설계.md`는 Accuracy를 훨씬 복잡한 모델로 요구한다 — **탄퍼짐(Spread)**: 자세별 기본값(Relaxed/Shouldered) + 발사당 누적 + 시간 경과 회복 + 이동/스프린트/웅크리기/공중 가산·감산 + 디버프 배율(`debuffSpreadMultiplier`)이 매 프레임 갱신되는 **상태값**이고, **반동(Recoil)**은 완전히 별개로 고정 패턴+지터가 카메라 회전에 프레임마다 가산/회복되는 **또 다른 상태값**이다. 둘 다 "정적인 스탯 하나"가 아니라 "매 프레임 진화하는 런타임 상태"다.
- **결론**: 우리 `WeaponStatType.Accuracy`/`RecoilControl` 필드는 이 모델을 표현할 수 없다. 파츠 모디파이어가 영향을 주는 대상을 (예를 들어) `baseSpreadHipfire`/`recoilKickVertical` 같은 **여러 파라미터**로 확장하고, `FirearmInstance` 안에 `WeaponSpreadState`/`WeaponRecoilState` 같은 프레임 단위 런타임 객체를 추가해야 한다. 이 문서 수준의 발견이라 실제 재설계는 후속 작업으로 남기고, 여기서는 "현재 구조로는 부족하다"는 사실만 기록해 둔다.
- 참고로 기획 문서 자신도 9장에서 "1인칭/3인칭 시점별 정확도 차등이 `GetCurrentSpread`에 반영 안 됨"을 스스로 미해결 항목(`[검토 필요]`)으로 남겨뒀다 — 우리 쪽 문제만이 아니라 기획 쪽 문서 간에도 아직 안 채워진 연결고리다.

## 3. ✅ 해결됨 — 입력 모드 전환(Player ↔ UI)

- **기획**: `Docs/기획문서_캐릭터조작설계.md` 2장 — 인벤토리/설정 같은 UI 화면이 열리면 `Player` 맵은 OFF, `UI` 맵이 ON으로 배타 전환된다. `Docs/기획문서_UI조작설계.md`는 `Cancel`(Esc)로 현재 UI를 닫는다.
- **문제였던 것**: [`QuickSlotInputHandler`](../Assets/Scripts/QuickSlot/QuickSlotInputHandler.cs), [`PlayerInteractionController`](../Assets/Scripts/Interaction/PlayerInteractionController.cs), [`WeaponLoadoutInputHandler`](../Assets/Scripts/Weapons/WeaponLoadoutInputHandler.cs) 셋 다 [`WindowManager`](../Assets/Scripts/UI/Windows/WindowManager.cs)의 상태(전체화면/팝업이 열려 있는지)를 전혀 확인하지 않고 매 프레임 `Keyboard.current`를 무조건 읽었다 — 인벤토리가 열려 있어도 숫자키로 퀵슬롯/무기가 바뀌고 F로 상호작용이 발동했다.
- **결정**: 실제 Input System Action Map 전환(Player/UI 맵 분리) 대신, `WindowManager`에 `IsAnyWindowOpen`(전체화면 또는 팝업이 하나라도 열려 있는지) 하나만 노출하고 3개 입력 핸들러가 이를 구독해 게이팅한다 — 결과는 기획이 요구하는 것과 동일(UI가 열리면 게임플레이 입력 무시)하지만 Action Map 자산을 새로 안 만들어도 되는 더 단순한 구현(KISS). 실제로 Player/UI 맵을 물리적으로 분리해야 할 필요가 생기면(예: 게임패드 지원) 이 게이트를 Action Map 활성화/비활성화로 교체하면 된다.
- **반영된 코드**: `WindowManager.IsAnyWindowOpen`/`CloseTopMost()` 추가. `QuickSlotInputHandler`/`PlayerInteractionController`/`WeaponLoadoutInputHandler`가 `windowManager.IsAnyWindowOpen`일 때 `Update()` 초반에 즉시 리턴. 신규 [`WindowCloseInputHandler`](../Assets/Scripts/UI/Windows/WindowCloseInputHandler.cs)가 Escape로 `WindowManager.CloseTopMost()`(팝업 우선, 없으면 전체화면)를 호출 — `Docs/기획문서_UI조작설계.md`의 `Cancel` 액션에 대응.
- **참고**: 이 변경으로 `Game.Interaction`이 처음으로 `Game.UI.Windows`에 의존하게 됐다(`codebase-map.md`의 "완전 격리 모듈" 목록에서 `Interaction` 제외 필요).

## 4. 🟡 통합 공백 — 스태미나

- **우리**: [player-attributes.md](player-attributes.md)에서 이미 `StaminaController`/`PlayerActionCosts`로 스태미나가 달리기·점프를 게이팅하도록 설계해 뒀다(처음부터 스태미나 자원이 존재).
- **기획**: `Docs/기획문서_피해디버프시스템설계.md` 5.3절은 "탈진(Exhaustion)" 디버프를 위해 스태미나 자원을 **신규로 도입**한다고 적었고, 그 근거로 "현재 [캐릭터조작설계]의 `Sprint`는 무제한 홀드 방식으로 설계돼 있었다"고 서술한다 — 즉 **기획 쪽 문서군 자체가 우리 설계의 존재를 모르고 있었을 뿐**, 최종적으로 원하는 결과("스태미나가 달리기를 제한한다")는 우리와 동일하다. 충돌이 아니라 **우리가 이미 답을 갖고 있던 것**이다.
- 다만 기획 쪽이 스태미나에 추가로 요구하는 것들은 우리 설계에 없다: 스태미나 0 도달 시 자동으로 "탈진" 디버프 발동(정확도/이동속도 페널티), 회복 조건이 "비전투 5초 유지"라는 조건부 회복(우리는 `regenDelayAfterUse`라는 단순 딜레이만 있음). **필요한 조치**(후속): `StaminaController`에 "게이지가 0에 도달/일정 비율 이상 회복" 이벤트를 노출해서, 나중에 만들 디버프 시스템이 구독할 수 있게 한다.

## 5. 🟡 통합 공백 — 부적(Talisman) 효과가 어디로 라우팅되는지 불명확

- `Docs/기획문서_부적제단시스템설계.md`의 `TalismanData.effectType`은 `AttackPower`, `MoveSpeed`, `CritChance`, `DamageReduction`, `ReloadSpeed`, `Luck` 등을 한 enum에 섞어 둔다.
- 이 중 `Luck`은 우리 [player-attributes.md](player-attributes.md)의 `AttributeType`(캐릭터 속성, `AttributeSet`이 관리) 소속이고, `ReloadSpeed`는 우리 [weapon-system.md](weapon-system.md)의 `WeaponStatType`(무기 스탯, `FirearmInstance`가 관리) 소속이다 — **서로 다른 두 모디파이어 시스템**인데, 부적 하나가 이 둘 중 아무 효과나 낼 수 있다고 기획되어 있다.
- 지금 코드에는 이 둘을 잇는 다리가 없다. 부적 시스템을 실제로 만들 때 "부적의 `effectType`이 무기 스탯 계열이면 장착된 무기의 `FirearmInstance`에, 캐릭터 속성 계열이면 `AttributeSet`에 반영한다"는 라우팅 계층이 필요하다는 것만 기록해 둔다.

## 6. ⚪ 미착수 영역 — 1인칭/3인칭 시점 전환

`Docs/기획문서_시점전환슈팅게임.md`/`시점전환조작설계.md`가 `ViewSwitch`(MMB), 강제 시점 규칙(스코프 ADS·등반·사망 시 강제 전환), 시점별 카메라 리그까지 상세히 설계해 뒀지만, 우리 쪽엔 이 개념 자체가 코드로도 문서로도 전혀 없다([hud-system.md](hud-system.md)는 "카메라 시점과 무관하게 설계한다"는 제약 조건만 언급했을 뿐, 시점 전환 자체를 설계한 적은 없다). 충돌은 아니고 **완전히 비어 있던 영역이 기획 쪽에서 채워진 것** — 다음에 캐릭터 컨트롤러/카메라 시스템을 실제로 만들 때 이 문서를 그대로 기준으로 삼으면 된다. 다만 [weapon-system.md](weapon-system.md)의 `FirearmData`에는 기획이 요구하는 `forcesFirstPersonOnADS`(저격총 등 ADS 시 강제 1인칭) 같은 필드가 없다는 점은 적어 둔다.

## 7~9. ✅ 정합 확인 (좋은 소식)

- **Interact 키**: 우리 [`PlayerInteractionController`](../Assets/Scripts/Interaction/PlayerInteractionController.cs)가 기본값으로 쓴 `Key.F`가, `Docs/기획문서_캐릭터조작설계.md`가 "`E`는 `Lean`과 충돌해서 `F`로 확정"이라고 명시한 것과 **독립적으로 일치**한다.
- **월드 마커**: `Docs/기획문서_퀘스트UI설계.md`가 후속 과제로 남긴 "월드/미니맵 퀘스트 마커 UI"는 [hud-system.md](hud-system.md)의 `IWorldMarker`/`WorldMarkerRegistry`가 이미 정확히 그 용도로 설계되어 있다 — 퀘스트 시스템이 만들어지면 마커를 `Register()`만 호출하면 된다.
- **상호작용 진입점**: `Docs/기획문서_대화시네마틱구조설계.md`, `_퀘스트UI설계.md`, `_상점시스템설계.md`, `_부적제단시스템설계.md` 전부 "NPC/오브젝트 상호작용 트리거 자체(감지 범위, `Interact`(F) 판정)는 별도 문서에서 다룬다"고 명시적으로 자리를 비워뒀다 — 그 자리가 정확히 우리 [interaction-system.md](interaction-system.md)/`IInteractable`/`InteractionDetector`다. 대화·상점·제단 UI를 열 때 시작점이 될 `DialogueInteractable`(현재 TODO 스텁)도 이미 있다.

## 10. ✅ 해결됨 — 무기 슬롯 개수(3 vs 4)

- **우리**: [weapon-system.md](weapon-system.md)의 `WeaponLoadout` = 주무기/보조무기/근접 **3슬롯**, 키 `1`/`2`/`3` 고정(1번 항목에서 확정).
- **기획**: `Docs/기획문서_시점전환슈팅게임.md` 2.2절 = 주무기/보조무기/근접/투척물 **4슬롯**(투척물=수류탄류).
- **결정**(사용자 확정): `WeaponLoadoutSlot`에 4번째 항목을 추가하지 않는다. 투척물은 "장착하고 쿨다운 관리하는 무기"가 아니라 **퀵슬롯(`4`~`0`)의 `IQuickSlottable` 소비 아이템**으로 다룬다 — 즉 기획 문서의 4슬롯 요구는 로드아웃이 아니라 퀵슬롯 쪽에서 흡수한다. 이미 확정된 "무기는 `1`/`2`/`3` 고정, 그 외 어떤 것도 별도 키를 새로 갖지 않는다"는 원칙(1번 항목)을 그대로 유지 — 투척물 전용 키를 새로 만들지 않고, 플레이어가 퀵슬롯 중 원하는 칸에 배치한 키 하나로만 사용한다.
- **반영된 코드**: [`Game.Items.ItemData.OnUse(GameObject)`](../Assets/Scripts/Items/Core/ItemData.cs) — 퀵슬롯에서 아이템이 사용될 때의 효과를 아이템 자신이 정의하는 가상 훅(기본은 no-op). [`ItemQuickSlotEntry.Use`](../Assets/Scripts/QuickSlot/ItemQuickSlotEntry.cs)는 이제 이 훅을 그대로 호출 — 새 "사용 가능한 아이템" 종류가 늘어나도 `QuickSlot` 쪽 코드는 안 바뀐다(개방-폐쇄). [`Game.Weapons.ThrowableItemData`](../Assets/Scripts/Weapons/ThrowableItemData.cs)가 `OnUse`를 오버라이드해 투사체를 던진다. `WeaponLoadoutSlot`/`WeaponLoadout`/`WeaponLoadoutInputHandler`는 변경 없음(여전히 3슬롯, 1/2/3 고정).

## 11. 🟡 설계 검토 — 월드 아이템 팩토리

- **배경**: 아이템을 월드 오브젝트로 만드는 경로가 `WorldItemSpawner`(정적), `WeaponPickup.dropPrefab`, `ThrowableItemData` 세 갈래이고, 프리팹이 없는 아이템은 드롭 시 사라지며 무기 상태는 일반 경로에서 보존되지 않는다. 설계는 [world-item-factory.md](world-item-factory.md).
- **🔴 반영된 충돌**: 팩토리 API가 `IWeapon`을 직접 받으면 `Items` → `Weapons` 의존이 생겨 기존 `Weapons` → `Items`와 순환한다. 상태를 `object`로 받고 무기 쪽이 `IWorldItemSpawner`를 등록하는 **방향 역전**으로 바꿨다.
- **🟡 문서 갱신**: `item-system.md`의 `WorldItem` 설명(낡은 `IInventory` 서술), `weapon-system.md`의 `dropPrefab` 서술(팩토리 경유로 변경 예정) — 이번에 표시를 남겼다.
- **✅ 기획 확인 완료(2026-09-19)**: [인벤토리 기획](../Docs/기획문서_인벤토리아이템시스템설계.md) 4장 컨텍스트 메뉴의 "버리기"는 **월드에 떨어뜨리는 것**으로 확정. 설계 가정과 일치.
- **⚪ 충돌 없음(경계 명시)**: 루팅 테이블·드랍 확률(부적/상점/`player-attributes.md` 행운)은 후속 루팅 시스템 몫이고 팩토리는 "어디에 놓는가"만 다룬다.
- **한계로 남김**: 무기를 인벤토리에 넣으면 상태를 잃는 문제(개별 아이템 상태 모델 필요), 바닥 아이템이 씬 전환에서 사라지는 문제.

## 12. ✅ 해결됨 — 컨테이너 내용물의 소유권

- **발견**: 플레이 테스트(2026-09-19)에서 "아이템이 들어 있는 리그/가방을 버리면 그 상태 그대로 하나의 리그/가방으로 버려져야 한다"는 요구가 나왔다. 조사해 보니 기존 동작은 더 나빴다 — 슬롯 클릭(해제)이 안의 아이템만 낱개로 흩뿌리고 **컨테이너 자체는 어디에도 남지 않았다**.
- **원인**: 그리드가 슬롯에 붙어 있고 컨테이너 아이템은 "모양을 주는 데이터"일 뿐이라 내용물이 컨테이너에 속한다는 개념이 없었다. `Equip` 교체 시에도 안 맞는 스택만 evicted로 나왔다.
- **결정(사용자)**: 컨테이너를 버리면 내용물과 함께 하나의 컨테이너로 월드에 놓이고, 주우면 내용물이 돌아온다.
- **반영**: `ContainerContents`/`EquippedContainer`/`EquipResult`, `ContainerEquipmentController.Detach`/`EquipWithContents`, `ContainerPickup`, `ContainerWorldSpawner`, 호출부 이전 — [inventory-system.md](inventory-system.md), [world-item-factory.md](world-item-factory.md) §10.
- **영향**: 기획 문서의 "가방 안의 가방"(진짜 중첩)과 같은 방향이라 이후 구조 개편의 기반이 된다. `Equip`/`Unequip`은 세이브 복원용 저수준 연산으로 유지.

## 13. ✅ 1단계 구현됨 — 건축(맵빌딩) 시스템
> **2026-09-21 결정 반영**: 아래 🔴 4개는 사용자 결정(1단계 = 벽·바닥·문·필러 / 세이브 저장·씬별 복원 / `1`~`5` 재해석 + 모드 게이팅 / 적 대응은 2단계)으로 정리되어 **[building-system.md](building-system.md)에 재설계**했다. 아래는 그 근거가 된 검토 원문이다. 재설계 중 추가로 발견한 것: `SaveGameService.SaveToDisk`는 **그 순간 등록된 provider만** 파일에 쓰므로 씬 단위 provider의 데이터는 다른 씬에서 저장할 때 사라진다 — 구조물은 지속 저장소(`StructureRepository`)로 해결(building-system.md 10장).

`origin/claude/shooting-game-design-doc-h4cm46`의 `8424a4b`(건축 시스템 설계 추가)를 병합했다(2026-09-21, 병합 커밋 `98e0c5b`, git 충돌 없음). 새 문서 [`Docs/기획문서_건축시스템설계.md`](../Docs/기획문서_건축시스템설계.md)와, 기존 문서 두 곳에 한 줄씩(`기획문서_시점전환슈팅게임.md` 2.4절 링크 추가, `기획문서_캐릭터조작설계.md` 입력 표에 건축 참조). 아래는 그 문서를 **현재 코드·설계와 대조**한 결과다.

**결론: 구현을 막는 것은 없다(설계는 우리 구조와 방향이 맞는다). 다만 시작 전에 정해야 할 것이 4개, 기획 문서를 고쳐야 할 것이 몇 개 있다.** 4개 중 1·2번은 코드 쪽 선행 작업이 필요하다.

### 🔴 시작 전에 정해야 하는 것

1. **숫자키 `1`~`5`의 전제가 옛 상태다.** 건축 문서는 "무기 슬롯은 `1~4`뿐이라 `5`는 빈 키"라고 쓰지만, 우리 쪽은 1번(#1, #10)에서 **`1`/`2`/`3` = 무기 고정, `4`~`9`,`0` = 퀵슬롯**으로 확정·구현했다([`WeaponLoadoutInputHandler`](../Assets/Scripts/Weapons/WeaponLoadoutInputHandler.cs), [`QuickSlotInputHandler`](../Assets/Scripts/QuickSlot/QuickSlotInputHandler.cs)). 즉 건축 모드의 `1`~`5`는 **다섯 개 모두 이미 쓰이는 키**(`4`·`5`는 퀵슬롯)다. 건축 모드에서 키를 재해석하는 발상 자체는 무기 문서 6장의 "같은 물리 입력을 상태에 따라 다르게 해석"과 같고 모드가 배타적이면 충돌이 아니지만, **그러려면 건축 모드일 때 무기·퀵슬롯 핸들러(그리고 `PlayerInputHandler`의 LMB 공격, 휠)가 입력을 무시해야 한다.**
   - 지금 게이팅 수단인 `WindowManager.IsAnyWindowOpen`/입력 차단자는 **이동까지 막는다**(창·대화용). 건축 모드는 **이동은 허용하고 전투·퀵슬롯만 막는** 다른 종류라서 재사용할 수 없다.
   - **권장**: `IPlayerActionMode`(전투/건축) 같은 작은 인터페이스 하나를 만들고 각 핸들러가 구독한다 — #3과 같은 패턴(핸들러가 상태를 폴링해 즉시 리턴). 기획 문서의 "새 Action Map 없이 재해석"은 우리 구현에서 **핸들러 게이팅**으로 번역된다(우리는 Action Map을 쓰지 않음, #3).
   - 건축 카테고리 `1`~`5`는 HUD 핫바(퀵슬롯 UI)와 같은 자리에 뜨므로 **건축 모드일 때 핫바가 피스 팔레트로 바뀌는 규칙**도 필요하다(HUD 문서에 없음).
2. **적 AI에 길찾기가 없다 — 건축의 핵심 목적("적의 진입을 막아낸다")이 성립하려면 선행 작업이 필요하다.** `com.unity.ai.navigation` 패키지가 없고, `CharacterMotor`는 목표를 향해 **직선으로 조향**한다([`CharacterMotor`](../Assets/Scripts/Characters/Core/CharacterMotor.cs) 주석: NavMesh는 "later task"). 벽을 지으면 적은 우회하지 못하고 그냥 벽에 붙어 선다. 건축 문서 11장이 NavMesh 재계산을 "후속 문서"로 넘겼는데, 사실은 **NavMesh 자체가 아직 없다.**
   - **구조물 타기팅 정책도 필요하다.** [`AiSensor`](../Assets/Scripts/AI/StateMachine/AiSensor.cs)는 `FactionMember` + `IDamageable`인 **가장 가까운 적대 대상**만 잡는다. 벽에 `FactionMember`를 달면 적이 플레이어를 두고 **가장 가까운 벽을 때리러** 가고, 안 달면 벽을 공격할 수 없다. "길이 막혔을 때만 구조물을 공격"하려면 감지가 아니라 **경로 막힘**을 트리거로 삼는 새 규칙이 필요하다(기획도 "문 앞에서 멈출지 부술지"를 미정으로 남김).
   - **권장 단계**: ① 벽·바닥·문(+필러)만, `IDamageable` 파괴까지(적은 아직 막힌 채로) → ② NavMesh 도입과 `CharacterMotor`의 경로 추종 → ③ 구조물 타기팅.
3. **캐릭터 모터가 경사·계단·사다리를 지원하지 않는다.**
   - `CharacterMotor`는 수평 속도만 덮어쓰고 `IsGrounded`가 **물리 스텝 하나 늦게** 반영되며, 공중이면 수평 조향을 **완전히 멈춘다**(점프 모멘텀 규칙). 계단은 1m 전진에 1m 상승(45°)이라 **내려갈 때 짧게 공중 판정이 나면 그 순간 이동 제어를 잃는다**(경사 투영·스냅 없음). 45°는 접지 판정(법선 `y > 0.5` = 최대 60°)은 통과하지만 오르막 속도 손실도 예상된다.
   - 계단이 **경사로인지 실제 단이 있는 계단인지** 문서에 없다(1m 상승/칸 = 단으로 만들면 오를 수 없음). 단이 있다면 스텝업이 필요하다.
   - **사다리**는 새 이동 상태(중력 끔, 점프/스프린트 게이팅, 점프 모멘텀과의 상호작용)가 필요하고, 메인 기획서 2절의 "**벽타기 등 고급 이동은 백로그(MVP 제외)**"와의 경계를 확인해야 한다(사다리는 그 범주가 아닌지). 적/NPC가 사다리를 쓰는지도 미정.
   - **권장**: 계단·사다리는 모터 작업(경사 투영/스텝업, 등반 상태)과 함께 **2단계로 미룬다.**
4. **저장과 씬 전환이 문서에 없다.** 씬이 Lobby / Combat으로 나뉘고 저장 서비스가 있는데([scene-and-persistence-system.md](scene-and-persistence-system.md)) 구조물이 **어느 씬에 짓는지, 씬을 나가면 사라지는지, 저장되는지**가 미정이다. 저장한다면 `ISaveDataProvider`로 **격자 좌표 + 피스 종류 + 회전 + HP**를 직렬화하고 `supportRefs`는 로드 후 좌표로 재구성하면 된다(참조 직렬화 불필요). 어느 쪽이든 정해야 데이터 모델(`PlacedPieceInstance`)이 확정된다.

### 🟡 통합 공백 (구현하면서 처리)

5. **재료 소모 API가 없다.** 플랫 `Inventory.RemoveItem(ItemData, quantity)`는 있지만 그리드는 `RemoveItem(PlacedItem)`뿐이고, 아이템은 **컨테이너 3개 그리드 + 플랫**에 흩어져 있다. 건축에는 "모든 보관 공간에서 이 아이템을 N개 차감"(그리고 7.3의 자원 충분 검사용 `Count`)이 필요하다 → 집계·차감을 하는 작은 서비스를 새로 둔다. `buildCost`는 문서의 `resourceId`(문자열) 대신 **`ItemData` 참조**로 하는 편이 우리 `ItemDatabase` 관례와 맞다. **상점은 아직 코드가 없어서**(설계 문서만 있음) 재료 조달은 당분간 월드 아이템/드롭([`WorldItemFactory`](world-item-factory.md))으로 한다 — 상점 등록 방식("일반 아이템처럼 카탈로그에 추가")은 그대로 호환.
6. **구조물 HP는 기존 것으로 된다.** [`IDamageable`](../Assets/Scripts/Combat/IDamageable.cs)의 주석이 이미 "later, a destructible object"를 예정하고 `HealthComponent`도 재사용할 수 있어 좋다. 확인할 점: ① 플레이어의 자기 공격이 자기 벽을 부수지 않도록 **진영 필터**, ② 폭발/범위 피해가 `IDamageable`을 순회하는지(무기 피해 판정 구현 시). 문은 [`DoorInteractable`](../Assets/Scripts/Interaction/DoorInteractable.cs)(`IInteractable`)를 그대로 쓸 수 있다.
7. **배치 레이가 카메라에 의존한다.** 시점/카메라 시스템이 아직 없고(#6) 테스트 씬은 탑다운이다. 문서의 "화면 중앙 레이 + 플레이어로부터 5m"는 3인칭에서 **카메라가 플레이어 뒤에 있으므로** 레이는 카메라에서 쏘고 거리 제한은 플레이어 기준으로 자르는 규칙이 필요하다.
8. **`Buildable` 태그** — Unity 태그는 TagManager에 등록해야 하고 오타에 조용히 실패한다. 우리는 컴포넌트/인터페이스 기반 판정(`IInteractable`, `IDamageable`)을 써 왔으니 **레이어 + 컴포넌트(`BuildPiece`)** 로 하는 편이 낫다.
9. **소소한 연동**: [`DropPlacement`](../Assets/Scripts/Items/World/DropPlacement.cs) 지면 탐색이 벽 윗면을 지면으로 잡을 수 있음(트리거·`Rigidbody`·`IInteractable`만 건너뜀) — 구조물 레이어를 건너뛰게 하거나 반대로 바닥 위에 놓이게 정책 결정. HUD 핫바가 건축 팔레트로 바뀌는 것(1번), 피스 이름 등 UI 문자열은 `UIStrings`로([dialogue-localization.md](dialogue-localization.md)).

### 📝 기획 문서 안의 불일치 (문서를 고쳐야 함)

- 5장 "8.3절 유효성 검사" → **7.3절**, 5장 "철거(Demolish, 7장)" → **6.1절**, 11장 "12장 후속 범위" → **15장**(12장은 입력 요약).
- **4장 ↔ 9장**: 4장은 "필러가 파괴되면 그 정점의 모든 벽이 지지를 잃어 무너진다"인데 9장 벽 지지 조건은 **바닥만** 본다. 필러를 지지 관계에 넣어야 하고, 필러는 벽이 만들고 벽은 필러에 기대므로 **순환 의존** — "지면에서 시작하는 도달 가능성"에서 필러를 뿌리로 볼지(그러면 벽 하나가 양끝 필러 중 하나만 무너져도 붕괴) 규칙을 정해야 한다.
- **7.3 "겹침 없음"**: 설계상 **겹치는 부분이 원래 있다** — 0.5×0.5 필러가 벽 끝과 0.25m 겹치고, 직각 벽끼리 끝에서 겹치고, 두께 0.5m 벽이 인접 두 셀로 0.25m씩 들어간다. 겹침 판정에 예외(같은 구조물 세트 무시 또는 축소 박스)가 필요하다. **캐릭터가 서 있는 칸에 배치할 수 있는지**(끼임)도 없다.
- **계단**: 시작 셀만 바닥을 검사하고 나머지 2칸은 규칙이 없다. **위층 바닥은 "같은 셀 아래에 벽/필러/바닥"이 있어야** 하는데 계단 **도착 칸** 아래는 비어 있을 수 있어 그 칸이 지지 조건을 못 채운다(계단이 위층과 이어지지 않음). 계단 머리 위 **개구부**(그 셀에 위층 바닥 금지)도 정의돼 있지 않다.
- **지면 높이**: "지면 위 바닥"의 Y 결정 규칙이 없다(경사 지형이면 떠 있거나 파묻힘). 평지 가정인지 명시 필요.
- 3.4 **사다리**의 "근접 시 자동 오르기"의 조건(어느 방향을 보고 어떤 입력에)과 4장의 붕괴 시 오르던 캐릭터 처리도 없다.

### ✅ 정합 확인

- `T` 키는 어떤 문서·코드에서도 쓰이지 않는다(비어 있음). LMB/RMB/휠 재해석은 무기 문서 6장(휠 컨텍스트 분리)과 같은 관용구이고, `Interact`(F)로 문을 여는 것은 #7·#9와 일치.
- "새 Action Map을 만들지 않는다"는 원칙은 우리 폴링 관용구·#3과 부합. Unlit 반투명 고스트 셰이더는 URP에서 문제없다(셀셰이딩 파이프라인과 분리한다는 결정도 합리적). 1m 그리드·이동 5m/s 등 단위 스케일에도 무리가 없다.
- **구현 구조 제안**(KISS·테스트 용이성): 스냅 계산과 지지·연쇄 파괴(`StructureGraph`)는 **순수 C#**으로 만들어 EditMode 테스트(그리드 반올림, 지지 규칙, 연쇄 붕괴, 필러 생성/공유/제거)로 잠그고, 고스트·입력·`MonoBehaviour`는 얇게 — 기존 `GridInventory`/`DialogueRunner`와 같은 방식.

## 다음 단계 제안

1. ~~1번(숫자키 충돌)~~ — **해결됨**(위 1번 참고).
2. ~~10번(투척물 슬롯)~~ — **해결됨**(위 10번 참고, 퀵슬롯 아이템으로 확정).
3. ~~3번(입력 모드 전환)~~ — **해결됨**(위 3번 참고).
4. ~~4번(스태미나 훅)~~ — **해결됨**(이벤트만 노출, 위 4번 참고). 실제 탈진 디버프 로직은 디버프 시스템을 만들 때.
5. **2번(정확도/반동 모델)** 은 무기 시스템에서 가장 손이 큰 재설계라, 실제로 총기 조작감을 구현하는 단계에 들어갈 때 별도로 다시 설계하는 것을 권장.
6. 5번(부적 라우팅)은 부적 시스템을 실제로 만들 때 자연스럽게 처리하면 된다 — 지금 당장 코드를 바꿀 필요는 없다.
7. **11번(월드 아이템 팩토리)** — 설계 검토와 구현 완료(2026-09-19). 기획 확인(버리기 = 월드 드롭)도 끝남.
8. **13번(건축 시스템)** — 결정 완료, [building-system.md](building-system.md)로 재설계하고 1단계 구현까지 끝(2026-09-21). 원래의 선행 결정 4개(입력 모드 게이팅 / NavMesh와 구조물 타기팅 / 모터 경사·사다리 / 저장·씬 범위)와 기획 문서 수정 항목. **권장 착수 순서**: 입력 모드 인터페이스 → 재료 집계·차감 서비스 → 순수 C# 격자·지지 로직 + 테스트 → 벽·바닥·문·필러(계단·사다리·NavMesh는 2단계).

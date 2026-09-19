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

## 다음 단계 제안

1. ~~1번(숫자키 충돌)~~ — **해결됨**(위 1번 참고).
2. ~~10번(투척물 슬롯)~~ — **해결됨**(위 10번 참고, 퀵슬롯 아이템으로 확정).
3. ~~3번(입력 모드 전환)~~ — **해결됨**(위 3번 참고).
4. ~~4번(스태미나 훅)~~ — **해결됨**(이벤트만 노출, 위 4번 참고). 실제 탈진 디버프 로직은 디버프 시스템을 만들 때.
5. **2번(정확도/반동 모델)** 은 무기 시스템에서 가장 손이 큰 재설계라, 실제로 총기 조작감을 구현하는 단계에 들어갈 때 별도로 다시 설계하는 것을 권장.
6. 5번(부적 라우팅)은 부적 시스템을 실제로 만들 때 자연스럽게 처리하면 된다 — 지금 당장 코드를 바꿀 필요는 없다.
7. **11번(월드 아이템 팩토리)** — 설계 검토와 구현 완료(2026-09-19). 기획 확인(버리기 = 월드 드롭)도 끝남.

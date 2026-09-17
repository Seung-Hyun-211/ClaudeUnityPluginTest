# UI 조작 설계 (UI Action Map 세부 바인딩)

> [기획문서_캐릭터조작설계.md](./기획문서_캐릭터조작설계.md) 2장에서 "Input System 기본 제공 UI 액션맵 재사용 권장"으로 남겨두었던 `UI` Action Map을 구체화하는 문서.
> 일시정지 메뉴, 인벤토리, 설정 화면 등 `Player` 맵이 꺼지고 `UI` 맵이 켜지는 모든 상황에서 공통으로 쓰는 탐색/확인/취소 조작을 정의한다.
> 작성일: 2026-09-17

---

## 1. 설계 원칙

1. **`UI` 맵은 `Player` 맵과 배타적으로 활성화**된다([캐릭터조작설계](./기획문서_캐릭터조작설계.md) 2장 상태표 참고) — 따라서 `UI` 맵의 바인딩은 `Player`/`Global`/`Debug` 맵과 물리 키가 겹쳐도 실제로 충돌하지 않는다. 다만 사용자가 "이 키가 상황마다 다른 의미"라고 느끼지 않도록, 가능한 한 직관적으로 겹치는 키를 고른다(예: 이동에 쓰던 WASD를 메뉴 탐색에도 그대로 사용).
2. **키보드/마우스 탐색과 포인터 탐색을 동시에 지원**한다(하이브리드 내비게이션) — 마지막으로 입력을 준 장치 기준으로 포커스 하이라이트가 전환되도록 한다(Unity Input System의 UI Input Module이 기본 지원).
3. **공용 액션 + 화면별 특수 액션을 분리**한다. `Navigate`/`Submit`/`Cancel`/`Point`/`Click` 등은 모든 UI 화면에 공통이고, 탭 전환(`TabNext`/`TabPrev`) 같은 액션은 필요한 화면(인벤토리, 설정 등)에서만 리스너를 붙여 사용한다 — 액션 자체는 `UI` 맵에 항상 존재하되, 해당 화면 스크립트가 구독 여부를 결정.
4. **Unity Input System 기본 제공 "UI" 액션맵을 뼈대로 사용**하고, VR/트래킹 전용 액션(TrackedDevicePosition, TrackedDeviceOrientation)은 이 프로젝트에 불필요하므로 제거한다. 부족한 부분(TabNext/TabPrev 등)만 추가한다.

---

## 2. 액션 정의

| 액션명 | 타입 | 바인딩(키보드/마우스) | 방식 | 설명 |
|---|---|---|---|---|
| `Navigate` | Value / Vector2 | WASD + 방향키 (2D Vector Composite, 두 세트 모두 바인딩) | 아날로그 | 포커스를 상하좌우로 이동 |
| `Submit` | Button | Enter, Space | Press | 선택된 항목 확인/실행 |
| `Cancel` | Button | Escape | Press | 뒤로 가기 / 현재 창 닫기 |
| `Point` | Value / Vector2 (Pass Through) | Mouse Position | 아날로그 | 마우스 커서 위치 (UI 레이캐스트 기준) |
| `Click` | Button | 마우스 왼쪽 버튼(LMB) | Press | 포인터 위치의 UI 요소 클릭 |
| `RightClick` | Button | 마우스 오른쪽 버튼(RMB) | Press | 컨텍스트 동작(아이템 정보, 우클릭 메뉴 등) |
| `ScrollWheel` | Value / Vector2 | Mouse Scroll | 아날로그 | 리스트/스크롤 뷰 스크롤 |
| `TabNext` / `TabPrev` | Button | `E` / `Q` | Press | 상단 카테고리 탭 전환 (인벤토리·설정 등 탭이 있는 화면에서만 사용) |

> `MiddleClick`은 기본 UI 액션맵에 존재하지만, 이번 설계에서는 UI 쪽에 아직 용도가 없어 **제외**한다(필요해지면 후속 추가). `Player` 맵의 `ViewSwitch`가 MMB를 쓰고 있지만 맵이 배타적이라 문제 없음 — 다만 "MMB는 시점 전환"이라는 사용자 기억과 섞이지 않도록 UI에서 굳이 MMB를 재사용하지 않는 것으로 결정.

**바인딩 상세**

- `Navigate`: `2D Vector Composite` — Up: W/↑, Down: S/↓, Left: A/←, Right: D/→. 게임패드는 Left Stick + D-Pad 모두 추가 바인딩.
- `TabNext`/`TabPrev`: `Q`/`E`는 `Player` 맵에서 `Lean`(몸 기울이기)에 쓰이는 키와 동일한 물리 키이지만, 맵이 배타적으로 전환되므로 충돌이 아니다. 다만 나중에 `Tab`/`Shift+Tab` 조합으로 바꿀 여지를 열어둔다(9장 권장 사항 참고).

---

## 3. 화면별 적용 예시

| 화면 | 사용하는 액션 | 비고 |
|---|---|---|
| 일시정지 메뉴 | `Navigate`, `Submit`, `Cancel`, `Point`, `Click` | `Cancel`(Esc)을 누르면 메뉴를 닫고 `Player` 맵으로 복귀(= [캐릭터조작설계](./기획문서_캐릭터조작설계.md) 2장의 `TogglePause`와 동일한 효과) |
| 인벤토리 | 공용 액션 전체 + `TabNext`/`TabPrev`(무기/소모품/장비 탭) + `RightClick`(아이템 컨텍스트 메뉴) | 아이템 드래그 앤 드롭은 `Point`+`Click`의 press/hold/release 조합으로 구현(9장 권장) |
| 설정 화면 | 공용 액션 전체 + `TabNext`/`TabPrev`(그래픽/사운드/조작 탭) + `ScrollWheel`(슬라이더가 많은 목록 스크롤) | 키/마우스 리바인딩 UI는 `Submit`으로 "새 키 입력 대기" 모드에 진입 후, 다음 입력을 캡처 — 이 캡처 순간에는 `UI` 맵 자체를 잠시 무시하고 로우 입력을 직접 읽는 특수 모드로 처리(9장 권장) |
| 게임오버/결과 화면 | `Submit`(재시작/확인), `Cancel`(메인 메뉴로) | `Navigate` 불필요(버튼이 1~2개뿐인 경우가 많음) |

---

## 4. 텍스트 입력 필드 처리 (특수 컨텍스트)

- 닉네임 입력, 세이브 이름 변경 등 텍스트 필드에 포커스가 있을 때는 `UI` 맵의 `Cancel`(Esc)·`Submit`(Enter) 의미가 "창 닫기/확인"이 아니라 **"편집 취소/편집 확정"**으로 바뀐다.
- **결정 사항**: 별도의 Action Map을 새로 만들지 않고, `UI` 맵은 그대로 활성 상태를 유지한 채 **텍스트 필드 컴포넌트가 `Cancel`/`Submit` 이벤트를 가로채 자체적으로 재해석**하는 방식을 채택한다 — 텍스트 입력 중에도 다른 UI 탐색 액션(예: 스크롤)은 그대로 살아있어야 자연스럽기 때문에, 맵째로 바꾸기보다 이벤트 재해석이 더 적합.
- 텍스트 입력 자체(문자 키 입력)는 Input System의 `OnScreenKeyboard`/`InputField`가 별도로 문자 이벤트를 받아 처리하며, `Navigate`(WASD)가 텍스트 필드 안에서 커서 이동으로 오인되지 않도록 텍스트 필드 포커스 중에는 `Navigate` 리스너를 해당 필드 컴포넌트가 일시적으로 우선 소비하게 한다.

---

## 5. 구현 메모 (Unity Input System)

### 5.1 Input Actions 에셋 구조 (개념)

```
Action Map: UI
├─ Navigate    <Vector2>  [WASD/방향키 2D Vector Composite, 게임패드 Stick/D-Pad]
├─ Submit      <Button>   [Keyboard/enter, Keyboard/space]
├─ Cancel      <Button>   [Keyboard/escape]
├─ Point       <Vector2>  [Mouse/position, Pass Through]
├─ Click       <Button>   [Mouse/leftButton]
├─ RightClick  <Button>   [Mouse/rightButton]
├─ ScrollWheel <Vector2>  [Mouse/scroll]
├─ TabNext     <Button>   [Keyboard/e]
└─ TabPrev     <Button>   [Keyboard/q]
```

### 5.2 EventSystem 연동

- Unity UI(uGUI)의 `EventSystem` + **Input System UI Input Module** 컴포넌트를 사용하고, 위 `UI` 액션맵의 각 액션을 모듈의 대응 슬롯(Point/Click/Navigate/Submit/Cancel 등)에 직접 연결한다 — 별도의 입력 폴링 코드 없이 uGUI가 자동으로 액션을 구독하도록 한다.
- `PlayerInput.SwitchCurrentActionMap("UI")`로 전환되는 시점에 `EventSystem`이 첫 포커스 요소를 자동 지정하도록(예: 메뉴가 열릴 때 첫 버튼에 자동 포커스) 각 화면 스크립트에서 `EventSystem.current.SetSelectedGameObject(firstButton)`을 호출하는 규칙을 둔다 — 그래야 마우스 없이 키보드/패드만으로 진입 직후 바로 탐색 가능.

---

## 6. 결정 사항 요약

- `UI` 맵은 Unity 기본 제공 UI 액션맵을 뼈대로 하되, VR 전용 액션(TrackedDevice*)은 제거하고 `TabNext`/`TabPrev`를 추가.
- `Navigate`는 **WASD + 방향키 모두 바인딩**하여 캐릭터 이동 감각을 메뉴 탐색에도 그대로 연결.
- 탭 전환은 `Q`(이전)/`E`(다음)로 결정 — `Player` 맵의 `Lean` 키와 물리적으로 같지만 맵이 배타적이라 문제 없음.
- `MiddleClick`은 이번 설계에서 제외(용도 없음, `Player` 맵의 `ViewSwitch`와 키 기억이 섞이지 않도록).
- 텍스트 입력 필드는 별도 Action Map을 만들지 않고, `Cancel`/`Submit` 이벤트를 필드 컴포넌트가 재해석하는 방식으로 처리.
- EventSystem은 Input System UI Input Module로 연동하고, 화면 진입 시 첫 포커스를 코드로 명시적으로 지정.

---

## 7. 추가 권장 사항

- **탭 키 재검토**: 향후 `Tab`을 스코어보드/전적 토글(멀티플레이 확장 시) 용도로 예약하고 싶다면, 지금의 `Q`/`E` 탭 전환은 유지하되 `Tab` 단독 키는 UI에서 사용하지 않는 편이 좋다(현재 설계와 일치, 특별 조치 불필요).
- **드래그 앤 드롭**: 인벤토리 아이템 이동은 `Click`의 press(드래그 시작)-hold(이동 중)-release(드롭) 3단계를 프레임마다 조회하는 방식으로 구현할 것을 권장. Input System 액션 자체를 추가로 늘리지 않고 기존 `Click`+`Point`만으로 충분.
- **키 리바인딩 UI**: 설정 화면에서 사용자가 직접 키를 바꾸는 기능은 `InputActionRebindingExtensions.RebindingOperation`을 사용하고, 리바인딩 진행 중에는 일반 `UI` 맵 입력을 일시 비활성화해 "다음 키 입력을 그대로 캡처"하도록 구현할 것을 권장.
- **게임패드 UI 내비게이션**: `Navigate`→Left Stick/D-Pad, `Submit`→South 버튼, `Cancel`→East 버튼, `TabNext`/`TabPrev`→어깨 버튼(LB/RB 또는 L1/R1)으로 매핑할 것을 권장 — 패드에서 `Q`/`E`에 대응하는 개념은 어깨 버튼이 가장 자연스러움.
- **툴팁**: 마우스 호버 시 짧은 지연(예: 0.3초) 후 툴팁을 띄우는 것을 권장하며, 이는 별도 입력 액션이 아니라 `Point` 값의 정지 시간을 코드에서 측정해 처리.

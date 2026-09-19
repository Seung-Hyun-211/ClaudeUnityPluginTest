# 씬 분리 & 데이터 지속성 설계

게임을 **Title / Loading / InGameLobby / InGameCombat** 씬으로 분리하고, Loading 씬은 모든 전환에서 재사용한다. 플레이어 정보처럼 사라지면 안 되는 데이터는 씬 전환 중에도(런타임 지속성), 게임 자체를 껐다 켜도(세이브 지속성) 유지되어야 한다. 이 둘은 요구사항이 다르므로 서로 다른 메커니즘으로 분리해 설계한다(단일 책임).

## 1. 씬 구성

| 씬 | 성격 | 설명 |
|---|---|---|
| `Boot` | Additive, 절대 언로드되지 않음 | 게임 최초 실행 시 단 한 번 로드. 지속되어야 하는 런타임 매니저(§3)와 세이브 서비스(§4)를 초기화 |
| `Title` | Single | 타이틀 화면(새 게임/이어하기/설정/종료) |
| `Loading` | Single, **재사용** | 다음 씬을 비동기로 로드하는 동안 진행률을 보여주는 범용 화면. 무엇을 로드하는지 알지 못함(§2) |
| `InGameLobby` | Single | 마을/기지 등 비전투 구간 |
| `InGameCombat` | Single | 실제 전투 씬. 씬 자체는 여러 개(맵마다) 있을 수 있음 |

`Boot`는 실제 씬 자산이지만 게임 로직상 "화면"이 아니라 항상 배경에 additive로 떠 있는 컨테이너다. 나머지 네 개(Title/Loading/Lobby/Combat)는 서로 배타적인 "현재 화면"이며 항상 하나만 활성 상태다.

### 확장 가능한 씬 카탈로그

`window-system.md`의 `FullScreenWindowEntry` 카탈로그와 동일한 패턴을 그대로 재사용한다 — 새 씬 종류를 추가할 때 `SceneFlowController` 코드를 바꾸지 않고 카탈로그에 항목만 추가한다(개방-폐쇄 원칙).

```csharp
namespace Game.SceneFlow
{
    public enum SceneKind { Boot, Title, Loading, Lobby, Combat }

    [CreateAssetMenu(menuName = "Game/SceneFlow/Scene Definition")]
    public class SceneDefinitionData : ScriptableObject
    {
        [SerializeField] private SceneKind kind;
        [SerializeField] private string sceneName; // SceneUtility로 빌드 설정과 대조 검증
        [SerializeField] private bool requiresSaveDataLoaded; // Lobby/Combat = true, Title/Loading = false

        public SceneKind Kind => kind;
        public string SceneName => sceneName;
        public bool RequiresSaveDataLoaded => requiresSaveDataLoaded;
    }

    [CreateAssetMenu(menuName = "Game/SceneFlow/Scene Catalog")]
    public class SceneCatalog : ScriptableObject
    {
        [SerializeField] private SceneDefinitionData[] scenes;

        public SceneDefinitionData Get(SceneKind kind) =>
            System.Array.Find(scenes, s => s.Kind == kind);
    }
}
```

전투 맵이 여러 개가 되면 `SceneKind.Combat` 하나에 맵별 `SceneDefinitionData`를 여러 개 두고, 어떤 걸 로드할지는 "Lobby에서 어떤 미션을 선택했는가"라는 게임플레이 데이터가 결정한다 — 씬 카탈로그 자체는 몰라도 된다.

## 2. 씬 전환은 항상 Loading을 거친다

`SceneFlowController`(Boot 씬에 상주, `DontDestroyOnLoad`)가 유일한 전환 창구다. 임의의 스크립트가 `SceneManager.LoadScene`을 직접 호출하지 않고, 반드시 이 컨트롤러에 요청한다 — 그래야 "전환은 항상 Loading 경유"라는 규칙이 한 곳에서만 강제된다.

```csharp
namespace Game.SceneFlow
{
    public interface ISceneFlowController
    {
        void RequestTransition(SceneKind target);
    }

    public class SceneFlowController : MonoBehaviour, ISceneFlowController
    {
        [SerializeField] private SceneCatalog catalog;

        public event Action<SceneKind> TransitionStarted;
        // Loading 씬이 실제로 화면에 떠서 다음 씬을 로드하기 "직전"에 발생.
        // 세이브 시스템은 TransitionStarted가 아니라 이 이벤트에서 디스크에 쓴다(§4).
        public event Action LoadingScreenEntered;
        public event Action<SceneKind> TransitionCompleted;
        // Loading 씬의 진행률 표시용. SceneFlowController는 UI 타입을 전혀
        // 몰라야 하므로(의존성 역전) LoadingProgressUIView 같은 구체 UI를
        // 직접 참조하지 않고 이벤트로만 진행률을 흘려보낸다 — Loading 씬의
        // UI 스크립트가 이 이벤트를 구독해서 자기 프로그레스 바를 채운다.
        public event Action<float> LoadingProgressChanged;

        public void RequestTransition(SceneKind target)
        {
            StartCoroutine(TransitionRoutine(target));
        }

        private IEnumerator TransitionRoutine(SceneKind target)
        {
            TransitionStarted?.Invoke(target);

            var loading = catalog.Get(SceneKind.Loading);
            yield return SceneManager.LoadSceneAsync(loading.SceneName, LoadSceneMode.Single);

            LoadingScreenEntered?.Invoke();

            var targetDef = catalog.Get(target);
            var op = SceneManager.LoadSceneAsync(targetDef.SceneName, LoadSceneMode.Single);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                LoadingProgressChanged?.Invoke(op.progress / 0.9f);
                yield return null;
            }

            LoadingProgressChanged?.Invoke(1f);
            op.allowSceneActivation = true;
            yield return op;

            TransitionCompleted?.Invoke(target);
        }
    }
}
```

`Loading` 씬은 `op`가 무엇을 로드하는지, 로드가 끝난 뒤 뭘 해야 하는지 전혀 모른다 — `LoadingProgressChanged` 이벤트를 구독해서 그냥 진행률만 그린다. 이 덕분에 Title→Lobby든 Lobby→Combat이든 Combat→Lobby든 동일한 Loading 씬 하나로 처리된다("재활용" 요구사항 충족).

`TransitionStarted`/`LoadingScreenEntered`/`TransitionCompleted` 이벤트는 §4의 세이브 시스템이 구독한다 — `SceneFlowController`는 `Game.Persistence`를 참조하지 않고, 반대로 `Persistence` 쪽이 `SceneFlow`의 이벤트를 구독하는 단방향 의존성을 유지한다(의존성 역전 — 씬 전환이라는 저수준 메커니즘이 세이브라는 정책을 몰라도 되게). `TransitionStarted`는 "이 전환이 자동 저장 대상인가"를 판단하는 시점, `LoadingScreenEntered`는 실제로 디스크에 쓰는 시점으로 역할을 분리한다(§4) — Loading 씬이 화면에 떠 있는 동안에만 저장 I/O가 일어나므로, 아직 언로드 중인 게임플레이 씬 상태를 저장하거나 로딩 화면 없이 저장으로 인한 끊김을 노출하는 일이 없다.

## 3. 씬 전환 중 유지되는 데이터 (런타임 지속성)

플레이어가 Lobby↔Combat을 오가는 동안 Vitals, 인벤토리 내용물, 장비, 퀵슬롯 배치, 무기 로드아웃(§ `character-system.md`, `inventory-system.md`, `quickslot-and-skills.md`, `weapon-system.md`에서 이미 정의된 컴포넌트들)은 파괴되면 안 된다. `PlayerController`를 매 씬에서 다시 만드는 대신, **Boot 씬에 상주하는 하나의 플레이어 런타임 컨테이너**가 씬이 바뀌어도 살아남는다.

```csharp
namespace Game.SceneFlow
{
    // Boot 씬 오브젝트. 플레이어의 "논리적" 상태(스탯/인벤토리/장비/퀵슬롯/무기)를
    // 소유하고, Lobby/Combat 씬이 로드되면 그 씬의 플레이어 스폰 지점에 있는
    // "표현" 오브젝트(모델/카메라/컨트롤러)와 재결합(rebind)한다.
    public class PlayerRuntimeContext : MonoBehaviour
    {
        public static PlayerRuntimeContext Instance { get; private set; }

        // 실제 구현은 PlayerController가 아니라 GameObject로 타입을 뺐다 —
        // Game.SceneFlow가 Game.Characters.Player에 하드 컴파일 의존을 갖지
        // 않게 하기 위해서다(그 반대 방향 의존은 이미 있지만, 씬 관리라는
        // 저수준 모듈이 특정 캐릭터 구현 타입을 알 필요는 없다 — 의존성
        // 역전). 필요한 쪽이 GetComponent<PlayerController>()로 꺼내 쓴다.
        public GameObject ActivePlayer { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // 씬 로드 후, 그 씬의 스폰 포인트에 생성된 PlayerController가 자신의
        // gameObject로 이 메서드를 호출해 등록한다.
        public void BindActivePlayer(GameObject player) => ActivePlayer = player;
    }
}
```

`character-system.md`가 이미 컴포지션(Faction/CharacterMotor/AttributeSet 등)으로 `PlayerController`를 구성해두었기 때문에, "지속되는 데이터"와 "씬마다 새로 생기는 표현"을 분리하는 것은 자연스럽게 이어진다:

- **지속(Boot에 상주)**: `AttributeSet`, `PlayerVitals`(Hunger/Thirst), 인벤토리·장비 컨테이너 내용물, `QuickSlotController`의 슬롯 배치, `WeaponLoadout`의 장착 무기/탄약 상태 — 전부 순수 C# 데이터/`ScriptableObject` 인스턴스이지 `MonoBehaviour` 표현이 아니다.
- **씬마다 재생성**: `CharacterMotor`(Rigidbody), 3인칭/1인칭 카메라 리그, 애니메이터 — Lobby와 Combat은 배경/조명/충돌체가 다른 별개 씬이므로 물리 오브젝트를 씬 로드마다 새로 스폰하는 게 자연스럽다.

Combat 씬이 로드되면 그 씬의 스폰 지점 프리팹이 `PlayerController`를 만들면서 생성자/초기화 시점에 `PlayerRuntimeContext.Instance`가 들고 있던 `AttributeSet`/인벤토리/퀵슬롯/무기 로드아웃 인스턴스를 그대로 주입받는다(새로 만들지 않음). 이 재결합 지점 하나만 지키면 나머지 시스템(Item/Inventory/QuickSlot/Weapon)은 이미 설계된 그대로 코드 변경 없이 재사용된다.

## 4. 게임 재시작 후에도 유지되는 데이터 (세이브 지속성)

런타임 지속성(§3)은 게임을 끄면 사라진다. "게임 자체를 껐다 켜도 유지"되려면 디스크에 직렬화해야 한다. 이 부분은 씬 전환과 무관한 별도 관심사이므로 `Game.Persistence` 네임스페이스로 분리한다.

### 4-1. 저장 대상을 인터페이스로 분리

무엇을 저장할지 `SaveGameService`가 직접 알 필요는 없다 — 각 서브시스템이 "나는 저장할 데이터가 있다"고 스스로 표시하게 한다(인터페이스 분리 + 개방-폐쇄: 나중에 퀘스트/상점 진행도 같은 새 서브시스템이 생겨도 `SaveGameService` 코드는 그대로).

```csharp
namespace Game.Persistence
{
    // 각 서브시스템이 자신의 저장 가능한 상태를 이 하나의 형태로 내보낸다.
    public interface ISaveDataProvider
    {
        string SaveKey { get; }      // 예: "player.vitals", "player.inventory"
        object CaptureState();       // 저장 시점 스냅샷 (POCO, 직렬화 가능해야 함)
        void RestoreState(object state);
    }

    // "왜" 저장이 필요해졌는지 트리거 쪽에서 밝히는 태그. 로깅/디버깅과
    // §4-2의 "언제 실제로 쓸지" 정책 분기에 쓰인다. 새 트리거 종류가
    // 생기면 여기 값만 추가한다(개방-폐쇄).
    public enum SaveTriggerReason
    {
        SceneTransition,  // Lobby/Combat 등 씬 전환에 딸린 자동 저장 — 실제 기록은 Loading에서
        ManualSavePoint,  // 플레이어가 세이브 포인트 등 인게임 오브젝트와 상호작용
        QuestCompleted,   // 특정 퀘스트/이벤트 완료
        AppQuit,          // 게임 종료
        Custom            // 그 외 디자이너/스크립트가 임의 시점에 요청
    }

    // 트리거(무엇이 저장을 요청하는가)가 참조하는 창구. 실제로 언제/어떻게
    // 디스크에 쓰는지는 몰라도 되며, 구현(SaveGameService)과 분리해 둔다
    // (인터페이스 분리 — 세이브 포인트 오브젝트가 SaveGameService 전체를 몰라도 됨).
    public interface ISaveRequestSink
    {
        void RequestSave(SaveTriggerReason reason);
    }

    // Boot 씬에 상주. 씬 전환이 끝나도 등록 목록은 유지된다.
    public class SaveDataRegistry : MonoBehaviour
    {
        public static SaveDataRegistry Instance { get; private set; }
        private readonly List<ISaveDataProvider> providers = new();

        public void Register(ISaveDataProvider provider) => providers.Add(provider);
        public void Unregister(ISaveDataProvider provider) => providers.Remove(provider);
        public IReadOnlyList<ISaveDataProvider> Providers => providers;
    }

    public class SaveGameService : MonoBehaviour, ISaveRequestSink
    {
        [SerializeField] private SaveDataRegistry registry;
        [SerializeField] private SceneFlowController sceneFlow; // TransitionStarted/LoadingScreenEntered 구독용
        private const string SaveFileName = "save.json";
        private bool pendingSceneTransitionSave;

        private void OnEnable()
        {
            sceneFlow.LoadingScreenEntered += FlushPendingSceneTransitionSave;
        }

        private void OnDisable()
        {
            sceneFlow.LoadingScreenEntered -= FlushPendingSceneTransitionSave;
        }

        // 모든 저장 트리거의 유일한 진입점. 어디서 호출되든(씬 전환/세이브
        // 포인트/퀘스트/종료) 이 메서드 하나만 알면 된다(DRY).
        public void RequestSave(SaveTriggerReason reason)
        {
            if (reason == SaveTriggerReason.SceneTransition)
            {
                // 지금 당장 쓰지 않는다 — Loading 씬이 뜬 뒤(LoadingScreenEntered)에
                // 실제로 기록한다. 아직 언로드 중인 씬의 상태를 저장하거나,
                // 로딩 화면 없이 저장 부하가 게임플레이 프레임을 끊는 걸 방지한다.
                pendingSceneTransitionSave = true;
                return;
            }

            // 그 외 트리거(세이브 포인트/퀘스트 완료/종료 등)는 로딩 화면을
            // 거치지 않는 시점에서 호출되므로 즉시 기록한다.
            SaveToDisk();
        }

        private void FlushPendingSceneTransitionSave()
        {
            if (!pendingSceneTransitionSave) return;
            SaveToDisk();
            pendingSceneTransitionSave = false;
        }

        private void SaveToDisk()
        {
            var snapshot = new Dictionary<string, object>();
            foreach (var provider in registry.Providers)
                snapshot[provider.SaveKey] = provider.CaptureState();

            var json = JsonUtility.ToJson(new SaveEnvelope(snapshot));
            File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveFileName), json);
        }

        public bool TryLoadFromDisk(out Dictionary<string, object> snapshot)
        {
            var path = Path.Combine(Application.persistentDataPath, SaveFileName);
            if (!File.Exists(path)) { snapshot = null; return false; }

            snapshot = SaveEnvelope.Parse(File.ReadAllText(path));
            return true;
        }

        public void ApplyLoadedState(Dictionary<string, object> snapshot)
        {
            foreach (var provider in registry.Providers)
                if (snapshot.TryGetValue(provider.SaveKey, out var state))
                    provider.RestoreState(state);
        }
    }
}
```

`JsonUtility` + `Dictionary<string, object>` 조합은 실제로는 각 provider별 상태를 개별 JSON 조각으로 감싸는 구현이 필요하다(`JsonUtility`가 다형적 `object` 직렬화를 못 하므로, `SaveEnvelope`는 실제로는 `List<KeyValuePair<string, string>>`처럼 이미 문자열로 직렬화된 조각들을 감싸는 형태가 된다). 이 문서는 구조(누가 무엇을 언제 저장/복원하는가)를 확정하는 것이 목적이고, 직렬화 포맷의 세부 구현은 코드 작성 단계에서 정한다.

### 4-2. 언제 저장/로드하는가

- **로드**: Title에서 "이어하기" 선택 → `SceneFlowController.RequestTransition(Lobby)` 호출 전에 `SaveGameService.TryLoadFromDisk` + `ApplyLoadedState`로 `PlayerRuntimeContext`(§3)의 데이터를 먼저 채운다. "새 게임"은 이 단계를 건너뛰고 각 provider의 기본값을 그대로 사용한다.
- **저장 — 씬 전환에 딸린 자동 저장**: `SceneFlowController.TransitionStarted`(예: Combat→Lobby)를 `SaveGameService`가 구독해 `RequestSave(SaveTriggerReason.SceneTransition)`을 호출하지만, 실제 디스크 기록은 그 직후가 아니라 **Loading 씬이 화면에 뜬 시점**(`LoadingScreenEntered`)에 일어난다 — 로딩 화면이라는, 플레이어가 이미 "전환 중"임을 인지하고 있고 게임플레이가 멈춰 있는 안전한 타이밍에만 저장 I/O가 일어나게 하기 위함이다. 전투 중(Combat 씬 안에 있는 동안)에는 씬 전환 자체가 없으므로 이 경로로는 저장되지 않는다 — 필요하면 §4-3의 트리거로 별도 처리한다.
- **저장 — 인게임 이벤트 트리거**: §4-3.
- **종료 시**: `Application.quitting`에서 `RequestSave(SaveTriggerReason.AppQuit)`을 호출한다. 로딩 화면을 거칠 수 없는 시점이므로 `RequestSave`는 이 reason에 대해 즉시 기록한다(§4-1의 `SaveGameService.RequestSave` 분기 참고).

### 4-3. 세이브 트리거 (원하는 인게임 이벤트에서 저장)

씬 전환 외에도 "이 순간에 저장하고 싶다"는 요구는 계속 늘어난다(세이브 포인트, 퀘스트 완료, 야영/휴식, 디버그 메뉴 등). 이런 트리거들은 `SaveGameService`가 `MonoBehaviour`인지, 씬 어디에 있는지, 디스크 포맷이 뭔지 전혀 몰라야 한다 — `ISaveRequestSink.RequestSave(reason)` 하나만 호출하면 된다(인터페이스 분리 + 단일 진입점이라 DRY).

가장 흔한 형태인 "월드에 놓인 세이브 포인트"는 이미 있는 `interaction-system.md`의 `IInteractable`을 그대로 구현해서 새 개념을 추가하지 않는다:

```csharp
namespace Game.Persistence
{
    // 상호작용 가능한 세이브 포인트. interaction-system.md의 감지/입력 파이프라인을
    // 그대로 타므로 "F키로 상호작용" 같은 처리가 이미 다 되어 있다.
    public class SaveTriggerPoint : MonoBehaviour, IInteractable
    {
        // Unity 인스펙터는 순수 인터페이스 필드를 직렬화하지 못하므로,
        // WindowManager.backgroundObscurerSource와 동일한 패턴으로
        // MonoBehaviour로 받아 Awake에서 ISaveRequestSink로 캐스팅한다.
        [SerializeField] private MonoBehaviour saveSinkSource;
        private ISaveRequestSink saveSink;

        private void Awake() => saveSink = saveSinkSource as ISaveRequestSink;

        public void Interact(GameObject interactor) =>
            saveSink.RequestSave(SaveTriggerReason.ManualSavePoint);
    }
}
```

씬 전환과 무관한 스크립트 이벤트(퀘스트 완료, 특정 컷씬 종료 등)도 동일한 창구를 쓴다 — 예를 들어 퀘스트 시스템이 만들어지면 퀘스트 완료 콜백에서 `saveSink.RequestSave(SaveTriggerReason.QuestCompleted)`를 호출하기만 하면 되고, `SaveGameService` 쪽 코드는 전혀 바뀌지 않는다(개방-폐쇄). `SceneTransition`을 제외한 모든 reason은 호출 즉시 `SaveToDisk()`로 이어진다 — 로딩 화면이 없는 시점에서 호출된다고 전제하기 때문이다. 만약 특정 트리거가 "저장 중" 연출이 필요할 만큼 무겁다면, 그 트리거 자신이 `SceneFlowController.RequestTransition`으로 짧은 Loading 왕복을 거치게 해서 §4-2의 경로를 재사용할 수도 있다.

### 4-4. 무엇을 저장 대상으로 등록하는가 (1차 범위)

| 데이터 | 소속 시스템 | 비고 |
|---|---|---|
| Hunger/Thirst | `player-attributes.md` (`PlayerVitals`) | |
| AttributeSet(지구력/힘/행운 등) | `player-attributes.md` | |
| 인벤토리/장비 컨테이너 내용물 | `inventory-system.md` | Pocket/Rig/Backpack 각각 |
| 퀵슬롯 배치(뱅크 2개) | `quickslot-and-skills.md` | |
| 무기 로드아웃(장착 무기, 파츠, 장전된 탄창) | `weapon-system.md` | |

구현 현황(2026-09-19): **Hunger/Thirst**(`Game.Player.PlayerVitalsSaveProvider`, 키 `player.vitals`)와 플레이어 체력(`Game.Combat.HealthSaveProvider`, 키 `player.health`)이 첫 실제 어댑터로 구현됐다. 나머지(AttributeSet, 인벤토리/장비 컨테이너, 퀵슬롯, 무기 로드아웃)는 아직 `ISaveDataProvider`를 구현하지 않았다 — 같은 방식(어댑터를 하나씩 붙이는 개방-폐쇄)으로 추가한다.

- 어댑터는 `DummyCounterSaveProvider`(테스트용 템플릿)와 같은 모양이지만, **플레이어에 붙는 어댑터는 Boot가 아닌 Lobby/Combat 씬에 있으므로** `SaveDataRegistry`를 직렬화 필드로 들 수 없다 — `OnEnable`/`OnDisable`에서 `SaveDataRegistry.Instance?.Register/Unregister`를 쓴다(`PlayerRuntimeContext.Instance` 패턴과 동일).
- 복원용 훅은 각 서브시스템이 직접 제공한다: `PlayerVitals.RestoreVitals`(감소 로직 우회), `HealthComponent.RestoreHealth`(`Damaged`/`Died` 이벤트 없이 값만 복원 — 로드는 전투 피해가 아니므로).

## 5. 전체 흐름 요약

```
[Boot] --(최초 1회, additive, 상주)--
   ├─ SceneFlowController
   ├─ PlayerRuntimeContext   (§3, 런타임 지속)
   └─ SaveDataRegistry / SaveGameService  (§4, 디스크 지속)
        │
        ▼
     [Title] --새 게임/이어하기--> [Loading] --재사용--> [InGameLobby]
                                                              │ 미션 선택
                                                              ▼
                                                          [Loading]
                                                              │
                                                              ▼
                                                       [InGameCombat]
                                                              │ 귀환(자동 저장)
                                                              ▼
                                                          [Loading]
                                                              │
                                                              ▼
                                                       [InGameLobby]
```

## 6. 확장 지점

- **새 씬 종류 추가**(예: 상점 전용 씬, 컷씬 씬): `SceneCatalog`에 `SceneDefinitionData` 하나 추가. `SceneFlowController`/`Loading` 씬 코드는 변경 없음.
- **새로 저장해야 할 데이터 추가**(예: 퀘스트 진행도, 상점 재고): 해당 서브시스템에 `ISaveDataProvider`를 구현해 `SaveDataRegistry`에 등록만 하면 됨. `SaveGameService` 코드는 변경 없음.
- **새 저장 트리거 추가**(예: 야영 휴식, 특정 컷씬 종료): `SaveTriggerReason`에 값 하나 추가하고, 트리거 쪽에서 `ISaveRequestSink.RequestSave(reason)`을 호출하기만 하면 됨. `SceneTransition`이 아닌 이상 즉시 저장되므로 `SaveGameService` 내부 분기도 대부분 건드릴 필요 없음.
- **전투 중 체크포인트 저장**이 필요해지면, `SceneFlowController`의 전환 이벤트가 아니라 별도의 "체크포인트 도달" 이벤트를 `SaveGameService`가 추가로 구독하면 된다 — 지금 구조를 깨지 않고 얹을 수 있음.
- **여러 세이브 슬롯**이 필요해지면 `SaveFileName`을 슬롯 인덱스 기반 경로로 바꾸면 됨(`SaveGameService` 내부만 수정, 다른 시스템 영향 없음).

## 7. 기존 문서와의 관계

- 씬이 바뀌어도 `documents/window-system.md`의 `WindowManager`(인벤토리/맵/임무확인 등 풀스크린 창)는 각 씬 안에서 독립적으로 동작 — Lobby에도 Combat에도 각각 있을 수 있고, 씬 전환 시스템과는 무관하다.
- `documents/design-conflict-review.md`에 이 문서로 새로 닫히는 항목은 없음(기존 충돌 목록과 겹치지 않는 새 영역). 다만 향후 "전투 중 저장 가능 여부"가 `Docs/` 기획 문서 쪽 특정 시스템(예: 부적 제단, 상점 구매)과 상호작용한다면 이 문서의 §4 범위를 넓혀야 한다 — 지금은 범위 밖으로 명시적으로 제외.

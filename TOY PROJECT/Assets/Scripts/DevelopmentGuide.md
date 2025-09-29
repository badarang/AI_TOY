# 개발 가이드 및 방향성 제안

이 문서는 `프로젝트: 그리드 택틱스`의 코드 구조를 이해하고, 직접 새로운 콘텐츠를 추가하거나 기능을 수정하는 방법을 안내하기 위해 작성되었습니다. 또한, 기획 문서와 현재 코드 구조를 바탕으로 앞으로의 개발 방향성을 제안합니다.

## 1부: 코드 수정 가이드

이 프로젝트는 매우 유연하고 확장성 있게 설계되어 있습니다. 몇 가지 핵심 개념만 이해하면 새로운 유닛, 스킬 등을 쉽게 추가할 수 있습니다.

### 핵심 구조

-   **`UnitBase.cs`**: 모든 유닛(플레이어, 적)의 기반이 되는 클래스입니다. 체력, 행동력(AP), 스킬 사용 등 공통 기능을 모두 여기서 관리합니다.
-   **`SkillData.cs`**: 스킬의 모든 정보를 담는 데이터 객체(ScriptableObject)입니다. 스킬의 이름, AP 소모량, 쿨다운, 사거리, 그리고 가장 중요한 **어떤 동작을 할지**를 정의합니다.
-   **`SkillBehavior.cs`**: 스킬의 실제 동작을 정의하는 추상 클래스(ScriptableObject)입니다. `MoveBehavior`, `DamageBehavior`처럼 "이동한다", "피해를 준다" 등의 개별 동작을 나타냅니다.
-   **매니저 클래스 (`...Manager.cs`)**: `TurnManager`, `GridManager`, `InputManager` 등 게임의 핵심 시스템을 관리합니다.

### 새로운 스킬 추가 방법 (가장 중요)

이 게임의 핵심은 **`SkillData`와 `SkillBehavior`를 조합하여 스킬을 만드는 것**입니다. 예를 들어 "적에게 돌진하여 피해를 준다"는 스킬은 `MoveBehavior`와 `DamageBehavior`를 순서대로 실행하는 `SkillData`를 만들면 됩니다.

1.  **`SkillBehavior` 만들기 (필요 시)**
    -   만약 "체력 회복", "기절" 등 기존에 없는 새로운 동작을 만들고 싶다면, `Skills` 폴더에 새로운 C# 스크립트를 만듭니다.
    -   `SkillBehavior`를 상속받고, `Execute(SkillContext context)` 메서드를 구현합니다. 이 메서드 안에 실제 동작할 코드를 작성합니다.
        ```csharp
        // 예시: HealBehavior.cs
        [CreateAssetMenu(menuName = "SkillBehaviors/HealBehavior")]
        public class HealBehavior : SkillBehavior
        {
            public int healAmount;

            public override void Execute(SkillContext context)
            {
                // 시전자의 체력을 회복시킵니다.
                context.Caster.TakeDamage(-healAmount); // TakeDamage를 음수로 사용하여 회복
                Debug.Log($"{context.Caster.name}이(가) {healAmount}만큼 체력을 회복했습니다.");
            }
        }
        ```

2.  **`SkillData` 에셋 만들기**
    -   Unity 에디터의 `Project` 창에서 우클릭 -> `Create` -> `Data` -> `SkillData`를 선택하여 새로운 스킬 데이터 에셋을 만듭니다.
    -   만들어진 에셋의 Inspector 창에서 세부 정보를 설정합니다.
        -   **Skill Meta**: 스킬의 이름, 설명 (`SkilMeta` 에셋을 만들어 연결).
        -   **Ap Cost**, **Cooldown**, **Range** 등을 설정합니다.
        -   **Initial Behaviors**: 스킬 사용 즉시 발동될 동작들입니다. 여기에 위에서 만든 `MoveBehavior`, `DamageBehavior`, `HealBehavior` 등의 에셋을 순서대로 끌어다 놓습니다.
        -   **Sub Target Behaviors**: (현재 로직 상) 첫 번째 동작 이후, 플레이어가 추가 대상을 선택했을 때 발동될 동작들입니다.

3.  **유닛에게 스킬 부여하기**
    -   유닛의 `UnitData` 에셋을 찾습니다.
    -   `Skills` 배열에 방금 만든 `SkillData` 에셋을 추가합니다.

### 새로운 유닛 추가 방법

1.  **`UnitData` 에셋 만들기**: `Create` -> `Data` -> `UnitData`로 새 에셋을 만듭니다.
2.  유닛의 최대 체력(Max Hp), 행동력(Max Ap) 등을 설정하고, 위에서 만든 스킬들을 `Skills` 배열에 추가합니다.
3.  유닛의 Prefab을 만들고, `PlayerUnit` 또는 `EnemyUnit` 컴포넌트를 붙인 뒤, `UnitData` 에셋을 연결해 주면 됩니다.

---

## 2부: 앞으로의 방향성 및 로그라이크 구현 제안

기획 문서에 제시된 "턴제 전략 RPG + 로그라이크"라는 방향성은 현재 코드 구조와 매우 잘 맞습니다. 몇 가지 제안을 통해 게임의 깊이를 더할 수 있습니다.

### 1. 모듈화된 스킬 시스템의 확장

현재의 `SkillBehavior` 시스템은 이미 훌륭한 기반입니다. 이를 더욱 발전시켜 "로그라이크"적인 재미를 극대화할 수 있습니다.

-   **Behavior 조합의 극대화**: "이동 후 주변 적에게 피해", "피해를 준 적을 밀어내기", "밀려난 적이 벽에 부딪히면 기절" 등 여러 `SkillBehavior`를 조합하여 수십, 수백 가지의 스킬을 쉽게 만들 수 있습니다. 이는 개발 리소스를 크게 절약하며 다채로운 게임 경험을 제공합니다.
-   **조건부 Behavior**: 특정 조건에서만 발동하는 `SkillBehavior`를 만들면 전략의 깊이가 생깁니다. (예: `ConditionalDamageBehavior` - 대상이 출혈 상태일 경우 추가 피해).

### 2. 로그라이크 요소 구현 제안

기획 문서의 "기회의 땅", "Die to Grow" 시스템을 구체적으로 구현하기 위한 아이디어입니다.

-   **"기회의 땅" 시스템 구현**:
    1.  `TurnManager`나 새로운 `EventManager`에서 매 턴 시작 시 랜덤한 타일 좌표를 결정합니다.
    2.  `GridManager`가 해당 타일에 특수 하이라이트를 표시합니다.
    3.  유닛이 해당 타일로 이동하는 `MoveBehavior`가 끝나면, 보상 선택 UI를 띄웁니다.
    4.  플레이어가 스킬을 선택하면, 해당 유닛의 `UnitData.skills` 목록에 새 `SkillData`를 추가하거나 교체합니다. 이는 **영구적인 변경이 아니라 해당 판에서만 적용**되어야 로그라이크의 재미를 살릴 수 있습니다.

-   **절차적 스테이지 생성**:
    -   사용자가 언급하신 "AI가 만드는 스테이지"의 첫 단계입니다. `StageData`를 사용하는 대신, `StageManager`가 게임 시작 시 `GridManager`를 통해 무작위로 장애물과 적 유닛을 배치하도록 수정할 수 있습니다.
    -   간단한 규칙(예: "적은 플레이어와 최소 5칸 이상 떨어져 배치")을 기반으로 맵을 생성하고, 점차 알고리즘을 고도화하여 매번 새로운 전장을 만들 수 있습니다.

### 3. 샌드박스 모드 및 사용자 설정 스테이지

-   새로운 UI 씬(Scene)을 만들어, 사용 가능한 모든 플레이어 유닛과 적 유닛 목록을 보여줍니다.
-   플레이어가 드래그 앤 드롭으로 유닛을 그리드에 배치하고, "전투 시작" 버튼을 누르면 현재 배치 정보를 바탕으로 `StageManager`와 `GridManager`를 초기화하여 전투를 시작할 수 있습니다.
-   이는 새로운 스킬이나 유닛의 밸런스를 테스트하는 데 매우 유용하며, 그 자체로도 재미있는 콘텐츠가 될 것입니다.

이 가이드가 앞으로의 개발에 도움이 되기를 바랍니다. 현재 코드 구조는 매우 유연하므로, 어떤 아이디어든 자신감을 가지고 시도해 보셔도 좋습니다.

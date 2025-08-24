# New Input System 설정 가이드

## 개요
이 프로젝트는 Unity의 New Input System을 사용하여 크로스 플랫폼 입력을 지원합니다.
PC, 스팀덱, 컨트롤러, 휴대폰 등 다양한 플랫폼에서 동일한 입력 시스템을 사용할 수 있습니다.

## 설정 단계

### 1. Input System 패키지 설치
1. Unity에서 `Window` → `Package Manager` 열기
2. `Input System` 패키지 검색 및 설치
3. 설치 후 Unity 재시작

### 2. Input Action Asset 설정
1. `Assets/InputActions/PlayerInput.inputactions` 파일 더블클릭
2. Input Actions 창에서 다음 설정:

#### Action Maps
- **Gameplay**: 게임플레이 관련 입력

#### Actions
- **Click**: 클릭/터치/트리거 입력
  - Type: Button
  - Binding: 
    - `<Mouse>/leftButton` (PC 마우스)
    - `<Touchscreen>/primaryTouch/tap` (모바일 터치)
    - `<Gamepad>/rightTrigger` (컨트롤러)
    - `<Keyboard>/space` (키보드 대안)

- **Point**: 포인터 위치 입력
  - Type: Value
  - Expected Control Type: Vector2
  - Binding:
    - `<Mouse>/position` (PC 마우스)
    - `<Touchscreen>/primaryTouch/position` (모바일 터치)
    - `<Gamepad>/rightStick` (컨트롤러)

### 3. C# 코드에서 사용
```csharp
using UnityEngine.InputSystem;

public class Example : MonoBehaviour
{
    private PlayerInputActions inputActions;

    void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        inputActions.Gameplay.Click.performed += OnClick;
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Gameplay.Click.performed -= OnClick;
        inputActions.Disable();
    }

    void OnDestroy()
    {
        inputActions?.Dispose();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        // 클릭 처리
    }
}
```

## 지원 플랫폼

### PC
- 마우스 좌클릭
- 키보드 스페이스바 (대안)

### 모바일/태블릿
- 터치스크린 탭
- 터치 위치

### 컨트롤러 (Xbox, PlayStation, Steam Deck)
- 오른쪽 트리거
- 오른쪽 스틱

### Steam Deck
- 트랙패드 클릭
- 트랙패드 위치
- 컨트롤러 입력

## 장점

1. **크로스 플랫폼**: 하나의 코드로 모든 플랫폼 지원
2. **자동 매핑**: Unity가 자동으로 적절한 입력 장치 매핑
3. **확장성**: 새로운 입력 장치 쉽게 추가 가능
4. **성능**: 이벤트 기반 입력 처리로 성능 향상
5. **디버깅**: Input Debugger로 입력 상태 실시간 확인

## 문제 해결

### Input System이 인식되지 않는 경우
1. Input System 패키지가 설치되었는지 확인
2. Unity 재시작
3. 프로젝트 설정에서 Input System 활성화

### 특정 플랫폼에서 입력이 작동하지 않는 경우
1. Input Action Asset의 Binding 확인
2. 플랫폼별 입력 장치 경로 확인
3. Input Debugger로 입력 상태 확인

## 추가 설정

### 새로운 입력 액션 추가
1. Input Action Asset에서 새 Action 추가
2. 적절한 Binding 설정
3. C# 코드에서 이벤트 연결

### 입력 프로세서 추가
- Deadzone: 입력 노이즈 제거
- Scale: 입력 값 스케일링
- Invert: 입력 값 반전

## 참고 자료
- [Unity Input System Manual](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/index.html)
- [Input System Examples](https://github.com/Unity-Technologies/InputSystem)
- [Input System Migration Guide](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Migration.html)

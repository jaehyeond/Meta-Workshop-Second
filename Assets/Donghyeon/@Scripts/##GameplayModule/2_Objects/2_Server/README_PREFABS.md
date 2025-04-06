# 2048 Snake 게임 프리팹 생성 안내

## 필요한 프리팹

### 1. Apple 프리팹
Apple 프리팹은 Snake가 먹을 아이템입니다. 다음 단계로 생성합니다:

1. 빈 게임 오브젝트 생성하고 이름을 "Apple"로 설정
2. 다음 컴포넌트 추가:
   - Sphere Collider (IsTrigger = true, Radius = 0.5)
   - Rigidbody (UseGravity = false, IsKinematic = true)
   - NetworkObject
   - Apple 스크립트
3. Apple 스크립트 설정:
   - Value Increment: 1 (Snake가 먹었을 때 증가할 값)
   - Rotation Speed: 30 (회전 속도)
   - Rotation Axis: (0, 1, 0) (Y축 회전)
4. 자식 오브젝트로 시각적 요소 추가:
   - Sphere 모델이나 커스텀 모델
   - Material 적용 (빨간색 또는 구분하기 쉬운 색상)
5. Tag를 "Apple"로 설정
6. 프리팹 저장 위치: `Assets/Donghyeon/@Resources/Prefabs/Snake/Apple.prefab`

### 2. AppleManager 프리팹
AppleManager는 Apple 스폰을 관리합니다:

1. 빈 게임 오브젝트 생성하고 이름을 "AppleManager"로 설정
2. 다음 컴포넌트 추가:
   - AppleManager 스크립트
   - NetworkObject
3. AppleManager 스크립트 설정:
   - Initial Apple Count: 1 (게임 시작 시 생성할 Apple 수)
   - Spawn Area Radius: 10 (스폰 영역 크기)
   - Spawn Height: 0.5 (Apple 스폰 높이)
4. 프리팹 저장 위치: `Assets/Donghyeon/@Resources/Prefabs/InGame/AppleManager.prefab`

### 3. Snake Body Detail 프리팹 업데이트
현재 Body Detail 프리팹에 SnakeBodySegment 스크립트와 TextMeshPro 컴포넌트를 추가하여 값을 표시합니다:

1. `Assets/Donghyeon/@Resources/Prefabs/Snake/Body Detail.prefab` 열기
2. SnakeBodySegment 스크립트 추가
3. 자식 오브젝트로 TextMeshPro 추가:
   - Create Empty 자식 오브젝트 추가하고 이름을 "ValueText"로 설정
   - TextMeshPro - Text 컴포넌트 추가
   - 다음 설정 적용:
     - Font Size: 3
     - Color: 흰색
     - Alignment: Center
     - Position: 세그먼트 위에 보이도록 설정 (예: y=1)
4. SnakeBodySegment 스크립트의 Inspector에서 valueText 필드에 생성한 TextMeshPro 참조 설정
5. 프리팹 저장

### 4. SnakeHead 업데이트
SnakeHead에도 값을 표시할 TextMeshPro 추가:

1. Snake 프리팹 열기
2. Head 오브젝트 선택
3. 자식 오브젝트로 TextMeshPro 추가:
   - Create Empty 자식 오브젝트 추가하고 이름을 "ValueText"로 설정
   - TextMeshPro - Text 컴포넌트 추가
   - 다음 설정 적용:
     - Font Size: 4
     - Color: 흰색
     - Alignment: Center
     - Position: 헤드 위에 보이도록 설정 (예: y=1)
4. SnakeHead 스크립트의 Inspector에서 valueText 필드에 생성한 TextMeshPro 참조 설정
5. 프리팹 저장

## 태그 설정
프로젝트의 Tags & Layers 설정에서 다음 태그를 추가해야 합니다:
- "Apple" - Apple 오브젝트 식별용

## 주의사항
1. 모든 NetworkObject는 필수입니다.
2. TextMeshPro 컴포넌트가 없는 경우 패키지 매니저에서 TextMeshPro 패키지를 설치하세요.
3. Apple 프리팹이 적절한 크기로 설정되었는지 확인하세요 (Snake가 쉽게 먹을 수 있도록).
4. 충돌 감지를 위해 모든 콜라이더 설정이 올바른지 확인하세요.

## 테스트 방법
1. BasicGame 씬으로 전환
2. 플레이 모드 진입
3. Snake가 생성되고 Apple이 맵에 스폰되는지 확인
4. Snake가 Apple을 먹으면 값이 증가하고 크기가 변하는지 확인
5. 값이 2, 4, 8, 16 등 2의 제곱수에 도달하면 Body 세그먼트가 추가되는지 확인 
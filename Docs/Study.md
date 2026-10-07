# 전력 타이쿤 프로젝트 학습 문서

> 대상: 이 프로젝트를 직접 만들었지만 "왜 이렇게 나눴는지"를 정리하고 싶은 사람 (고등학생 눈높이)
> 범위: `Assets/_LDY/` 아래의 모든 스크립트 (아이템 7개 / 업그레이드 4개 / 캐릭터 클릭 / 초당 수익 / 아이콘 확장 반영)

---

## 0. 이 게임은 뭘 하는 게임?

- 아이템 7개(전구, 컴퓨터, 선풍기, 서버, 채굴기, 공장 설비, 데이터센터)를 **사서** 켜 두면 **주기(기본 1초, 업그레이드로 단축)마다 돈**이 들어옵니다.
- 제품을 켜면 **전력**을 씁니다. 전력 한도를 넘으면 **정전(Blackout)** 이 되고, 정전 중에는 **수익이 0**입니다.
- 돈으로 **업그레이드**(전력 한도, 수익 배율, 수익 주기 단축, 클릭 수익)를 살 수 있습니다.
- **캐릭터를 클릭**하면 클릭 수익을 즉시 받습니다. (정전 중에는 무시)
- 캐릭터가 상황(수익, 구매, 위험, 정전, 복구)에 따라 **반응**합니다.

이 프로젝트의 핵심 아이디어는 딱 하나입니다.

> **"계산하는 부분(Core)"과 "화면 보여주는 부분(UI)"을 완전히 분리하고, 둘 사이의 소통은 *이벤트*로만 한다.**

---

## 1. 전체 흐름 (이벤트가 어떻게 흘러가는가)

### 1-1. 게임 시작 (한 번만 일어남)

```
Unity가 씬을 로드
  └→ GameBootstrapper.Awake()
       1. BuildContext()  : 모든 부품(Wallet, PowerGrid, ...)을 new 로 만들고 서로 연결
       2. ticker.OnTick += Payout.Pay   : "틱이 울리면 수익을 지급해라" 연결
       3. ticker.Bind(stats)            : 티커가 PlayerStats 의 틱 주기를 읽고 변경을 구독
       4. uiBinder.Bind(context)        : 화면(UI)들에게 부품을 넘겨주고 이벤트 구독시킴
```

### 1-2. 틱 → 수익 지급 → 지갑 → 화면/캐릭터 (주기마다 반복, 기본 1초)

```
GameTicker.Update()                    ← 매 프레임 시간을 누적, 주기(PlayerStats.TickInterval)가 차면
  └→ OnTick 이벤트 발생
       └→ IncomePayout.Pay()
            ├ IncomeCalculator.CalculateTickIncome()
            │    · 정전이면 0 반환 (수익 없음)
            │    · 아니면 켜진 모든 아이템의 (기본수익 × 아이템배율 × 플레이어배율) 합
            ├ income <= 0 이면 여기서 끝
            ├ Wallet.Add(income)
            │    └→ OnMoneyChanged 발생
            │         ├ HudView.ShowMoney            → "돈: 123" 글자 갱신
            │         ├ ItemInfoProvider.OnChanged   → ItemShopView.RefreshAll   (카드 "구매 가능/돈 부족" 갱신)
            │         └ UpgradeInfoProvider.OnChanged→ UpgradeShopView.RefreshAll(카드 활성/비활성 갱신)
            └ IncomePayout.OnPaid(income) 발생
                 ├ CoinPopupView.Show            → "+5" 글자가 떠오름 (클릭 수익도 같은 팝업 사용)
                 └ ReactionResolver (Income)     → OnReaction → CharacterView.Play (캐릭터 반응)
```

### 1-3. 구매 → 전력 합산 → 정전 판정 → 캐릭터 반응

```
카드를 클릭 (ItemCardView 의 Button)
  └→ ItemShopView.OnItemClicked
       └→ ShopClickRouter.OnItemClicked(item)
            ├ 이미 가진 아이템이면 → ItemToggleService.Toggle (켜기/끄기)
            └ 아니면 → PurchaseService.TryPurchase(item)
                 1. Wallet.TrySpend(price)        실패하면(돈 부족) 종료
                      └→ OnMoneyChanged → HUD 돈 갱신
                 2. owned 목록에 추가
                 3. PowerGrid.Activate(item)      구매 즉시 켜짐
                      └→ Recalculate()
                           · CurrentPower = 켜진 아이템 전력 합
                           · OnPowerChanged(현재, 한도)
                                ├ HudView.ShowPower  → 게이지/색 갱신
                                └ ReactionResolver.HandlePower → 70% 넘으면 NearLimit
                           · CurrentPower > Limit 이면 정전
                                └ (상태가 바뀌었을 때만) OnBlackoutChanged(true)
                                     ├ HudView.ShowBlackout     → 경고창 켜기
                                     ├ ReactionResolver         → Blackout 반응
                                     └ IncomeForecast.Recalculate → "틱당 수익: +0"
                      └→ OnActiveItemsChanged
                           └ IncomeForecast.Recalculate → "틱당 수익" 갱신
                 4. PurchaseService.OnItemPurchased
                      └ ReactionResolver → Purchase 반응 (한 번 재생)
```

### 1-4. 정전에서 복구되는 경우

정전은 **아이템을 끄거나**, **전력 한도 업그레이드를 사면** 풀립니다.

```
아이템 끄기:         ItemToggleService.Toggle → PowerGrid.Deactivate → Recalculate
한도 업그레이드:     UpgradeService.TryUpgrade → PowerLimitEffect.Apply
                       → PlayerStats.SetPowerLimit → OnStatsChanged
                       → PowerGrid.Recalculate  (PowerGrid 가 OnStatsChanged 를 구독 중)
                       
Recalculate 결과 CurrentPower <= Limit 이 되면
  └→ OnBlackoutChanged(false)
       ├ HudView: 경고창 끄기
       ├ ReactionResolver: Recovery 반응 재생
       └ IncomeForecast: 예상 수익 복구
```

### 1-5. 캐릭터 반응이 끝나는 방법

`CharacterView` 가 `Update()` 에서 시간을 재다가 `Duration` 이 지나면
`resolver.Complete(type)` 을 호출 → `ReactionResolver` 가 "원래 상태(Idle / NearLimit / Blackout)"로 되돌립니다.

> **한 줄 요약:** 상태가 바뀌는 곳(Wallet, PowerGrid, PlayerStats…)은 **"바뀌었다!"** 라고 외치기만 하고,
> 화면(HudView, CardView…)은 그 외침을 **듣고 알아서** 자기 모습을 바꿉니다.

### 1-6. 캐릭터 클릭 → 클릭 수익 (신규)

```
캐릭터 이미지를 클릭
  └→ CharacterClickView.OnPointerClick → OnClicked 콜백   (뷰는 "눌렸다"만 알림)
       └→ UiBinder 가 연결해 둔 ClickService.Click()
            ├ grid.IsBlackout 이면 → 아무 일 없이 종료 (지급 X, 팝업 X)
            ├ amount = stats.ClickIncome  (기본 1, 업그레이드 레벨당 +2)
            ├ Wallet.Add(amount)
            │    └→ OnMoneyChanged → HUD 돈, 카드/업그레이드 버튼 활성 갱신
            └ OnClicked(amount) 발생
                 └ CoinPopupView.Show → "+N" 팝업
```

### 1-7. 수익 주기 단축 업그레이드 (신규)

```
UpgradeService.TryUpgrade (UpgradeService 는 무수정)
  └→ IncomeIntervalEffect.Apply → PlayerStats.SetTickInterval(새 주기)
       └→ OnStatsChanged
            ├ GameTicker.ReadInterval          → 다음 프레임부터 새 주기로 틱
            └ IncomeForecast.Recalculate       → "초당 수익" 다시 계산 → HudView.ShowPerSecond
```

> 초당 예상 수익 = 틱당 수익 ÷ 틱 주기. 주기만 바뀌고 틱당 수익은 같아도 `OnPerSecondIncomeChanged` 는 발생합니다.

---

## 2. 읽는 순서 추천

처음부터 `GameBootstrapper` 를 보면 부품이 너무 많아 어지럽습니다. **작은 것 → 큰 것** 순서로 읽으세요.

| 단계 | 읽을 파일 | 이유 |
|---|---|---|
| ① 데이터 | `ItemData` → `UpgradeData` | 게임에 "무엇이 있는지" 알아야 나머지가 이해됨 |
| ② 가장 단순한 Core | `IWallet` → `Wallet` | 이벤트(`event Action`)의 가장 쉬운 예시 |
| ③ 상태 Core | `IPlayerStats`, `IPlayerStatsMutator` → `PlayerStats` | 인터페이스를 둘로 쪼갠 이유를 이해 |
| ④ 전력 Core | `IPowerGrid` → `PowerGrid` | 이 게임의 심장 (전력 합산 / 정전 판정) |
| ⑤ 수익 Core | `IIncomeCalculator` → `IncomeCalculator` → `IIncomePayout` → `IncomePayout` → `IncomeForecast` | 돈이 어떻게 계산되고 지급되는지 |
| ⑥ 구매 Core | `IPurchaseService` → `PurchaseService` → `ItemToggleService` | 사고 켜고 끄기 |
| ⑦ 업그레이드 Core | `IUpgradeEffect` → `PowerLimitEffect`, `IncomeMultiplierEffect`, `IncomeIntervalEffect`, `ClickIncomeEffect` → `UpgradeService` | 전략 패턴 (효과를 갈아 끼우기) |
| ⑦-2 클릭 Core | `IClickService` → `ClickService` | 클릭 수익 (정전 중 무시) |
| ⑧ 화면용 데이터 | `ItemInfo`, `UpgradeInfo` → `ItemInfoProvider`, `UpgradeInfoProvider` | UI 가 쓸 "요약본" 만드는 법 |
| ⑨ 반응 | `ReactionType` → `ReactionResolver` | 우선순위 규칙이 있어 조금 어려움 (나중에!) |
| ⑩ Runtime | `GameTicker` → `GameContext` → `GameBootstrapper` → `UiBinder` → `ShopClickRouter` | 지금까지 본 부품들이 어디서 조립되는지 |
| ⑪ UI | `HudView` → `ItemCardView` → `ItemShopView` → `UpgradeCardView` → `UpgradeShopView` → `CoinPopupView/Item` → `CharacterView` → `CharacterClickView` → `ShopTabsView` | 이벤트를 "듣는 쪽" |
| ⑫ 에디터 | `TycoonSceneBuilder` | 씬을 자동으로 만들어 주는 도구 (게임 로직 아님) |

> 팁: 파일을 읽을 때 **"이 클래스의 `event` 는 누가 구독하지?"** 를 항상 검색(Ctrl+Shift+F)해 보세요.
> 이벤트의 시작과 끝을 따라가면 흐름이 보입니다.

---

## 3. 파일별 설명

### 폴더 구조 한눈에 보기

```
Scripts/
├ Data/        ItemData, UpgradeData           ← 설정값 (ScriptableObject, 둘 다 icon 슬롯 보유)
├ Core/        게임 규칙. Unity 화면과 무관한 순수 C# 클래스
│  ├ Interfaces/   I○○○ 계약서들
│  ├ Models/       ItemInfo, UpgradeInfo, ReactionType (값 묶음)
│  └ Upgrades/     업그레이드 효과 (PowerLimit / IncomeMultiplier / IncomeInterval / ClickIncome)
├ Runtime/     GameBootstrapper 등. 부품을 조립하고 시간을 흘려보냄
├ UI/          화면 담당 MonoBehaviour 들
└ Editor/      TycoonSceneBuilder (에디터 전용 도구)
```

> **SOLID 약어 복습**
> - **S** 단일 책임: 클래스는 한 가지 일만
> - **O** 개방-폐쇄: 기능을 *추가*할 때 기존 코드를 *고치지 않아도* 되게
> - **L** 리스코프 치환: 인터페이스를 구현한 클래스는 서로 바꿔 끼워도 동작
> - **I** 인터페이스 분리: 필요한 메서드만 담은 작은 인터페이스
> - **D** 의존성 역전: 구체 클래스가 아니라 *인터페이스*에 의존

---

## 3-A. Data (데이터)

### `ItemData.cs`
- **한 줄 역할:** 가전제품 하나의 설정값(이름, 가격, 전력, 수익…)을 담은 ScriptableObject.
- **주요 필드:** `price`(가격), `powerConsumption`(전력 소비), `baseIncome`(틱당 기본 수익), `incomeMultiplier`(아이템 자체 배율), `displayName/icon/description`(표시용). 아이템은 현재 7개(전구, 컴퓨터, 선풍기, 서버, 채굴기 8000/180/120, 공장 설비 30000/320/400, 데이터센터 120000/560/1500 — 가격/전력/기본 수익).
  모두 `private` + `[SerializeField]` 로 인스펙터에서만 수정, 코드에서는 읽기 전용 프로퍼티(`Price` 등)로만 접근.
- **누가 호출?** `PurchaseService`(가격), `PowerGrid`(전력), `IncomeCalculator`(수익), `ItemCardView`(표시), `GameBootstrapper`(배열로 보관), `TycoonSceneBuilder`(생성).
- **무엇을 호출?** 없음 (순수 데이터).
- **왜 따로?** 수치를 코드에 박아 넣지 않고 **데이터 파일**로 빼서, 새 아이템을 **코드 수정 없이** 추가 가능 → **O(개방-폐쇄)**, 그리고 "데이터"와 "동작"을 분리 → **S(단일 책임)**.
- **이해 확인 질문**
  1. `price` 필드가 `private` 인데도 다른 클래스가 가격을 읽을 수 있는 이유는? (`Price` 프로퍼티의 역할)
  2. 새 아이템 "냉장고"를 추가하려면 **코드를 고쳐야 할까?** 어떤 작업만 하면 될까?

### `UpgradeData.cs`
- **한 줄 역할:** 업그레이드 하나의 설정(종류, 기본 비용, 비용 증가율, 레벨당 효과, 최대 레벨)을 담는 ScriptableObject. (파일 안에 `UpgradeType` enum 도 같이 있음)
- **주요 멤버:**
  - `UpgradeType` : `PowerLimit`, `IncomeMultiplier`, `IncomeInterval`, `ClickIncome` (업그레이드 종류 표식. 새 값은 **맨 끝에** 추가해야 기존 에셋의 저장값이 안 깨짐)
  - `icon` / `Icon` : 카드에 표시할 스프라이트 슬롯 (신규). 비어 있으면 카드에서 아이콘이 숨겨짐.
  - `CostAt(level)` : `baseCost × costGrowth^level` 을 반올림. 레벨이 오를수록 비용이 **지수적으로** 증가.
  - `EffectPerLevel`, `MaxLevel` — `EffectPerLevel` 의 **뜻은 종류마다 다름**: 한도/클릭은 레벨당 더하는 값, 수익 주기는 레벨당 곱하는 비율(0.9).
- **누가 호출?** `UpgradeService`(비용, 최대 레벨), `PowerLimitEffect/IncomeMultiplierEffect`(레벨당 효과), `UpgradeInfoProvider`, `UpgradeCardView`.
- **무엇을 호출?** `Mathf.RoundToInt`, `Mathf.Pow` 만.
- **왜 따로?** `ItemData` 와 같은 이유(데이터 분리, **O**). 또한 "비용 계산 공식"을 데이터 자신이 알고 있어서 `UpgradeService` 가 공식을 몰라도 됨(**S**).
- **이해 확인 질문**
  1. `baseCost=200, costGrowth=1.6` 일 때 레벨 0, 1, 2 의 비용은 각각 얼마일까?
  2. `UpgradeType` enum 은 어떤 클래스가 "이 업그레이드를 처리할 효과 클래스"를 찾을 때 쓰일까?

---

## 3-B. Core / Interfaces (계약서)

> 인터페이스는 **"이런 기능을 제공한다"는 약속**만 적어 둔 파일입니다. 내용(구현)은 없습니다.
> 이 프로젝트의 규칙: **다른 클래스는 `Wallet` 이 아니라 `IWallet` 을 붙잡는다.**

### `IWallet.cs`
- **역할:** 지갑이 제공해야 할 기능의 약속 (`Money`, `OnMoneyChanged`, `CanAfford`, `TrySpend`, `Add`).
- **구현:** `Wallet`. **사용:** `PurchaseService`, `UpgradeService`, `IncomePayout`, `ItemInfoProvider`, `UpgradeInfoProvider`, `HudView`.
- **왜?** 사용하는 쪽이 `Wallet` 의 내부를 몰라도 되게 → **D(의존성 역전)**. 나중에 "저장되는 지갑"으로 바꿔도 사용 쪽은 무수정 → **L**.
- **질문**
  1. `TrySpend` 와 `CanAfford` 가 따로 있는 이유는? (UI 는 어느 것만 필요할까?)
  2. `HudView` 가 `Wallet` 대신 `IWallet` 을 받으면 어떤 점이 좋을까?

### `IPlayerStats.cs`
- **역할:** 플레이어 능력치를 **읽기 전용**으로 보여주는 약속 (`PowerLimit`, `IncomeMultiplier`, `TickInterval`, `ClickIncome`, `OnStatsChanged`).
- **구현:** `PlayerStats`. **사용:** `PowerGrid`, `IncomeCalculator`, `IncomeForecast`, `ItemInfoProvider`, `ClickService`, `GameTicker`.
- **왜?** 읽기만 하는 클래스에게 "수정 권한"을 주지 않기 위해 → **I(인터페이스 분리)**.
- **질문**
  1. `IPlayerStats` 에는 왜 `SetPowerLimit` 이 없을까?
  2. `OnStatsChanged` 를 구독해야 하는 클래스 2개를 찾아보세요.

### `IPlayerStatsMutator.cs`
- **역할:** 능력치를 **바꾸는** 약속 (`SetPowerLimit`, `SetIncomeMultiplier`, `SetTickInterval`, `SetClickIncome`).
- **구현:** `PlayerStats`. **사용:** `PowerLimitEffect`, `IncomeMultiplierEffect`, `IncomeIntervalEffect`, `ClickIncomeEffect` (업그레이드 효과만).
- **왜?** 읽기(`IPlayerStats`)와 쓰기(`IPlayerStatsMutator`)를 **나눔** → 오직 업그레이드 효과만 능력치를 바꿀 수 있고, 다른 클래스는 실수로 못 바꿈 → **I**.
- **질문**
  1. 같은 `PlayerStats` 객체인데 왜 두 인터페이스로 나눴을까?
  2. `IncomeCalculator` 가 `IPlayerStatsMutator` 를 받는다면 어떤 문제가 생길 수 있을까?

### `IPowerGrid.cs`
- **역할:** 전력망의 약속 (현재 전력, 한도, 정전 여부, 켜진 아이템 목록, 3개의 이벤트, 켜기/끄기/조회).
- **구현:** `PowerGrid`. **사용:** `PurchaseService`, `ItemToggleService`, `IncomeCalculator`, `IncomeForecast`, `ItemInfoProvider`, `ReactionResolver`, `HudView`.
- **이벤트 3개:** `OnPowerChanged(현재, 한도)`, `OnBlackoutChanged(정전여부)`, `OnActiveItemsChanged`.
- **왜?** 전력 규칙을 쓰는 곳이 많아서, 구체 클래스에 묶이지 않게 → **D**.
- **질문**
  1. `ActiveItems` 의 타입이 `List` 가 아니라 `IReadOnlyCollection` 인 이유는?
  2. 이벤트가 3개로 나뉜 이유는? (각각 누가 필요로 하는지 생각해 보기)

### `IIncomeCalculator.cs`
- **역할:** 수익 계산 약속 (`CalculateTickIncome` 전체 틱 수익, `CalculateItemIncome` 아이템 하나의 수익).
- **구현:** `IncomeCalculator`. **사용:** `IncomePayout`, `IncomeForecast`, `ItemInfoProvider`.
- **왜?** 계산만 맡기고 지급/표시는 다른 곳에서 → **S, D**.
- **질문**
  1. 카드의 "+5/틱" 표시는 어느 메서드로 얻을까?
  2. `CalculateTickIncome` 과 `CalculateItemIncome` 의 가장 큰 차이는?

### `IIncomePayout.cs`
- **역할:** 수익 지급의 약속 (`Pay()` 와 `OnPaid(금액)` 이벤트).
- **구현:** `IncomePayout`. **사용:** `GameBootstrapper`(틱에 연결), `CoinPopupView`, `ReactionResolver`.

### `IClickService.cs` (신규)
- **역할:** 캐릭터 클릭 수익의 약속 (`Click()` 과 `OnClicked(금액)` 이벤트).
- **구현:** `ClickService`. **사용:** `UiBinder`(클릭 연결), `CoinPopupView`(팝업).
- **질문**
  1. `IIncomePayout.OnPaid` 와 `IClickService.OnClicked` 를 따로 둔 이유는? (수익 반응 `ReactionResolver` 는 어느 쪽만 듣는지 생각해 보기)
  2. 클릭을 호출하는 쪽(`UiBinder`)과 듣는 쪽(`CoinPopupView`)은 서로 알고 있을까?
- **질문**
  1. `Pay()` 를 호출하는 쪽과 `OnPaid` 를 듣는 쪽은 각각 누구인가?
  2. UI 가 `IIncomePayout` 만 알고 `GameTicker` 는 몰라도 되는 이유는?

### `IIncomeForecast.cs`
- **역할:** "지금 상태에서 다음 틱에 얼마 들어올지" 예상치(`TickIncome`)와 "초당 얼마인지"(`PerSecondIncome`), 각각의 변경 이벤트(`OnTickIncomeChanged`, `OnPerSecondIncomeChanged`).
- **구현:** `IncomeForecast`. **사용:** `HudView`.
- **왜?** HUD 가 계산기·그리드·스탯을 직접 다 알 필요 없이 이것 하나만 보면 되게 → **I, D**.
- **질문**
  1. `HudView` 는 "틱당 수익"을 어디서 얻을까?
  2. 이 인터페이스에 이벤트가 없다면 HUD 는 어떻게 갱신해야 할까? (불편한 점은?)

### `IPurchaseService.cs`
- **역할:** 구매 약속 (`IsOwned`, `TryPurchase`, `OnItemPurchased`).
- **구현:** `PurchaseService`. **사용:** `ShopClickRouter`, `ItemToggleService`, `ItemInfoProvider`, `ReactionResolver`.
- **질문**
  1. `TryPurchase` 가 `bool` 을 돌려주는 이유는?
  2. `OnItemPurchased` 를 구독하는 클래스를 2개 찾아보세요.

### `IItemToggleService.cs`
- **역할:** 소유한 아이템을 켜고 끄는 약속 (`Toggle`).
- **구현:** `ItemToggleService`. **사용:** `ShopClickRouter`.
- **질문**
  1. 메서드가 하나뿐인 인터페이스도 의미가 있을까? 있다면 왜?
  2. `Toggle` 의 호출자는 아이템을 "소유했는지"를 직접 검사할 필요가 있을까?

### `IUpgradeService.cs`
- **역할:** 업그레이드 시스템 약속 (목록, 레벨 조회, 최대 여부, 비용, 효과, `TryUpgrade`, `OnLevelChanged`).
- **구현:** `UpgradeService`. **사용:** `ShopClickRouter`, `UpgradeInfoProvider`, `ReactionResolver`, `UiBinder`.
- **질문**
  1. `Upgrades` 가 `IReadOnlyList` 인 이유는?
  2. `OnLevelChanged` 는 어떤 정보 두 개를 전달할까?

### `IUpgradeEffect.cs`
- **역할:** **"업그레이드 한 종류가 하는 일"** 의 약속 (`Type`, `ValueAt`, `Apply`, `Format`).
- **구현:** `PowerLimitEffect`, `IncomeMultiplierEffect`, `IncomeIntervalEffect`, `ClickIncomeEffect`. **사용:** `UpgradeService`, `UpgradeInfoProvider`.
- **왜?** 새 업그레이드 종류(예: 틱 속도)를 추가할 때 `UpgradeService` 를 **고치지 않고** 이 인터페이스를 구현한 클래스만 추가 → **O(개방-폐쇄)** 의 대표 예시.
- **질문**
  1. (실제로 해 본 예) "클릭 수익 업"을 추가할 때 새로 만든 파일과 수정한 파일은? `UpgradeService` 는 건드렸을까?
  2. `ValueAt` 과 `Apply` 가 따로 있는 이유는? (UI 미리보기 vs 실제 적용)

### `IItemInfoProvider.cs` / `IUpgradeInfoProvider.cs`
- **역할:** UI 에게 "카드를 그리기 위한 요약 정보(`ItemInfo`/`UpgradeInfo`)"를 주는 약속 + "바뀌었으니 다시 그려라" 이벤트(`OnChanged`).
- **구현:** `ItemInfoProvider` / `UpgradeInfoProvider`. **사용:** `ItemShopView` / `UpgradeShopView`.
- **왜?** UI 가 Wallet, PowerGrid, 계산기… 여러 시스템을 직접 알지 않고 **창구 하나**만 보게 → **D, I**.
- **질문**
  1. `ItemShopView` 가 `IWallet` 을 직접 받지 않고 `IItemInfoProvider` 만 받는 이점은?
  2. `OnChanged` 에는 왜 매개변수가 없을까? (무엇이 바뀌었는지 알려주지 않아도 되는 이유)

### `IReactionResolver.cs`
- **역할:** 캐릭터 반응의 약속 (`Current`, `OnReaction`, `Complete`).
- **구현:** `ReactionResolver`. **사용:** `CharacterView`.
- **질문**
  1. `Complete` 는 누가 언제 호출할까?
  2. `Current` 가 필요한 순간은 언제일까? (`CharacterView.Bind` 를 보세요)

---

## 3-C. Core / Models (값 묶음)

### `ItemInfo.cs`
- **역할:** 아이템 카드 하나를 그리는 데 필요한 정보 묶음 (`ItemCardState` enum + 예상 수익 + 한도 초과 여부). `readonly struct`.
- **상태 4가지:** `Purchasable`(구매 가능), `Unaffordable`(돈 부족), `On`, `Off`.
- **누가 만들고 쓰나?** 만드는 쪽: `ItemInfoProvider.GetInfo`. 쓰는 쪽: `ItemCardView.Refresh`.
- **왜?** 로직은 "상태가 뭔지"만 결정하고, 색/글자는 UI 가 정함 → **S**. `readonly struct` 라서 만든 뒤 못 바꾸므로 안전.
- **질문**
  1. 카드의 색깔을 정하는 코드는 `ItemInfo` 에 있을까, `ItemCardView` 에 있을까? 왜?
  2. `ItemCardState` 의 4가지 상태는 각각 어떤 조건에서 만들어질까?

### `UpgradeInfo.cs`
- **역할:** 업그레이드 카드 하나를 그리는 정보 묶음 (레벨, 최대 레벨, 비용, 최대 여부, 구매 가능 여부, 현재/다음 효과 글자).
- **만드는 쪽:** `UpgradeInfoProvider`. **쓰는 쪽:** `UpgradeCardView`.
- **왜?** 위와 동일 (UI 는 값을 **받아서 보여주기만**).
- **질문**
  1. `CurrentText`, `NextText` 는 어디서 만들어질까? (힌트: `IUpgradeEffect.Format`)
  2. 만렙일 때 카드에 어떤 글자가 표시될까?

### `ReactionType.cs`
- **역할:** 캐릭터 반응 종류 enum: `Idle, Income, Purchase, NearLimit, Blackout, Recovery`.
- **사용:** `ReactionResolver`(결정), `CharacterView`(모양 선택).
- **질문**
  1. 이 중 "한 번 재생되고 끝나는 반응"과 "상태가 유지되는 반응"을 구분해 보세요.
  2. 새 반응 `Jackpot` 을 추가하려면 어느 파일들을 건드려야 할까?

---

## 3-D. Core / 구현 클래스

### `Wallet.cs`
- **한 줄 역할:** 돈을 보관하고, 쓰고, 번다.
- **주요 멤버:**
  - `Money` : `private set` → 외부에서 직접 못 바꿈.
  - `CanAfford(n)` : 돈이 충분한가?
  - `TrySpend(n)` : 충분하면 차감하고 `true`, 부족하면 아무 일 없이 `false`.
  - `Add(n)` : 0 이하는 무시, 돈 증가.
  - `OnMoneyChanged` : 돈이 바뀔 때마다 새 금액과 함께 발생.
- **누가 호출?** `PurchaseService.TryPurchase`(TrySpend), `UpgradeService.TryUpgrade`(TrySpend), `IncomePayout.Pay`(Add).
- **무엇을 호출?** 없음. 이벤트를 **내보내기만** 함 (구독자: `HudView`, `ItemInfoProvider`, `UpgradeInfoProvider`).
- **왜 따로?** "돈 관리"라는 **하나의 책임(S)**. 누가 돈을 쓰는지 몰라도 되고, 돈이 바뀌면 이벤트만 외침.
- **질문**
  1. `Money` 에 `private set` 을 붙인 이유는? 붙이지 않으면 어떤 버그가 생길 수 있을까?
  2. `TrySpend` 에서 돈이 부족하면 `OnMoneyChanged` 가 발생할까? 코드로 확인해 보세요.

### `PlayerStats.cs`
- **한 줄 역할:** 전력 한도, 수익 배율, 틱 주기, 클릭 수익(업그레이드로 변하는 능력치)을 보관한다.
- **주요 멤버:** `PowerLimit`, `IncomeMultiplier`, `TickInterval`, `ClickIncome`, 각 `Set…`, `OnStatsChanged`. 생성자가 네 값을 모두 받음. 값이 **실제로 바뀔 때만** 이벤트 발생(같은 값이면 무시).
- **누가 호출?** 네 개의 Effect 클래스가 `Set…` 호출 (`IPlayerStatsMutator` 통해). `PowerGrid`, `IncomeCalculator` 등이 값을 읽음 (`IPlayerStats` 통해).
- **무엇을 호출?** `Mathf.Approximately` (float 비교).
- **왜 따로?** 능력치를 한곳에 모아 두면 `PowerGrid` 가 한도를 직접 갖고 있을 필요가 없음 → **S**. 한 클래스가 **두 인터페이스**(읽기/쓰기)를 구현 → **I**.
- **질문**
  1. float 값을 `==` 대신 `Mathf.Approximately` 로 비교하는 이유는?
  2. 값이 같은데도 이벤트를 발생시키면 어떤 낭비가 생길까?

### `PowerGrid.cs`
- **한 줄 역할:** 켜진 아이템의 전력을 합산하고, 한도를 넘으면 정전으로 판정한다. (게임의 심장)
- **주요 멤버:**
  - `activeItems` : 지금 켜진 아이템 집합(`HashSet` — 중복 불가, 포함 여부 확인이 빠름).
  - `Activate / Deactivate` : 집합에 넣고/빼고 → `Recalculate()` → `OnActiveItemsChanged`.
  - `Recalculate()` : 전력 합 계산 → `OnPowerChanged` → 정전 상태가 **바뀌었으면** `OnBlackoutChanged`.
  - `WouldExceedLimit(item)` : "이걸 켜면 한도를 넘을까?" (카드의 경고 표시용). 이미 켜진 아이템이면 `false`.
  - 생성자에서 `stats.OnStatsChanged += Recalculate` → 한도가 올라가면 자동으로 재판정.
- **정전 판정 규칙:** `CurrentPower > Limit` (딱 같으면 정전 아님).
- **누가 호출?** `PurchaseService`(Activate), `ItemToggleService`(Activate/Deactivate/IsActive), `IncomeCalculator`(IsBlackout, ActiveItems), `ItemInfoProvider`(IsActive, WouldExceedLimit).
- **무엇을 호출?** `IPlayerStats`(한도 읽기, 변경 구독).
- **왜 따로?** "전력 관리"라는 한 책임 → **S**. 수익이나 UI 를 전혀 모르고 이벤트만 발생 → 확장하기 쉬움(**O**).
- **질문**
  1. 정전 이벤트(`OnBlackoutChanged`)가 "정전 상태가 **바뀔 때만**" 발생하도록 한 이유는? (`if (blackout == IsBlackout) return;`)
  2. 한도 80, 켜진 전력 합이 90 일 때 한도 업그레이드로 한도가 100 이 되면 어떤 일이 연쇄적으로 일어날까?

### `IncomeCalculator.cs`
- **한 줄 역할:** 지금 상태에서 한 틱에 얼마를 버는지 **계산만** 한다.
- **주요 멤버:**
  - `CalculateTickIncome()` : 정전이면 0, 아니면 켜진 아이템들의 `RawIncome` 합을 반올림.
  - `CalculateItemIncome(item)` : 아이템 하나의 예상 수익 (카드 표시용).
  - `RawIncome` = `BaseIncome × item.IncomeMultiplier × stats.IncomeMultiplier`.
- **누가 호출?** `IncomePayout.Pay`, `IncomeForecast.Recalculate/생성자`, `ItemInfoProvider.GetInfo`.
- **무엇을 호출?** `IPowerGrid`(정전 여부, 켜진 아이템), `IPlayerStats`(배율).
- **왜 따로?** "계산"과 "지급"을 분리 → 지급은 `IncomePayout`, 계산은 여기. 계산식이 바뀌면 이 파일만 수정 → **S**. 같은 계산을 지급·예측·카드가 **재사용**.
- **질문**
  1. 정전일 때 수익이 0 이 되는 로직은 어느 줄일까? 이 규칙을 `IncomePayout` 에 넣으면 어떤 불편이 생길까?
  2. 기본 수익 5, 아이템 배율 1, 플레이어 배율 1.4 인 아이템의 `CalculateItemIncome` 값은?

### `IncomePayout.cs`
- **한 줄 역할:** 틱마다 계산된 수익을 지갑에 넣고 "지급했다"고 알린다.
- **주요 멤버:** `Pay()` (수익 계산 → 0 이하면 종료 → `wallet.Add` → `OnPaid`), `OnPaid(금액)`.
- **누가 호출?** `GameBootstrapper` 가 `ticker.OnTick += Payout.Pay` 로 연결.
- **무엇을 호출?** `IIncomeCalculator`, `IWallet`.
- **왜 따로?** "틱이 울리면 → 지급"이라는 **절차**만 담당. 타이머는 `GameTicker`, 계산은 `IncomeCalculator`, 저장은 `Wallet` → **S**. `OnPaid` 덕분에 코인 팝업·캐릭터 반응이 `IncomePayout` 을 고치지 않고 붙음 → **O**.
- **질문**
  1. `Pay()` 안에서 `if (income <= 0) return;` 이 있는 덕분에 정전 중에 어떤 일이 일어나지 **않을까**?
  2. `OnPaid` 의 구독자를 2개 찾고, 각각 무엇을 하는지 말해 보세요.

### `IncomeForecast.cs`
- **한 줄 역할:** "지금 켜져 있는 상태에서 틱당/초당 얼마 들어오는지"를 미리 계산해 두고, 바뀔 때만 알린다 (HUD 의 "틱당 수익", "초당 수익" 용).
- **주요 멤버:** `TickIncome`, `PerSecondIncome`(= 틱 수익 ÷ `stats.TickInterval`), `OnTickIncomeChanged`, `OnPerSecondIncomeChanged`, `Recalculate()`. 두 값을 **따로** 비교해서 바뀐 쪽 이벤트만 발생.
  다음 상황에서 `Recalculate`: 켜진 아이템 변경 / 정전 상태 변경 / 능력치 변경. 값이 **같으면** 이벤트 생략.
- **누가 호출?** `HudView` 가 값을 읽고 구독. `GameBootstrapper` 가 생성.
- **무엇을 호출?** `IIncomeCalculator`, `IPowerGrid`, `IPlayerStats` 의 이벤트를 구독.
- **왜 따로?** 틱은 1초마다지만 "예상 수익"은 **상태가 바뀔 때 즉시** 보여야 함. 이 역할(예측 표시)을 `IncomePayout` 과 분리 → **S**.
- **질문**
  1. 왜 `OnBlackoutChanged` 도 구독해야 할까? (정전이 되면 예상 수익은?)
  2. 값이 변하지 않았는데도 이벤트를 쏘면 HUD 에서 무슨 낭비가 생길까?
  3. 주기만 0.9 배로 줄었을 때 두 이벤트 중 어느 것이 발생할까?

### `ClickService.cs` (신규)
- **한 줄 역할:** 클릭 수익을 계산해 지갑에 넣고 "클릭됐다"고 알린다.
- **주요 멤버:** `Click()` (정전이면 종료 → `stats.ClickIncome` → `wallet.Add` → `OnClicked(금액)`), `OnClicked`.
- **누가 호출?** `UiBinder` 가 `CharacterClickView.OnClicked` 에 연결. 구독자: `CoinPopupView`.
- **무엇을 호출?** `IWallet.Add`, `IPlayerStats.ClickIncome`(읽기), `IPowerGrid.IsBlackout`(읽기 전용).
- **왜 따로?** "클릭 수익"만 책임 → **S**. 정전 중이면 이벤트도 안 쏘기 때문에 **팝업이 자동으로 안 뜸**(View 에 정전 검사가 필요 없음).
- **질문**
  1. 정전 중 클릭할 때 `Wallet.Add` 와 `OnClicked` 는 호출될까? 코드로 확인해 보세요.
  2. 클릭 수익이 `수익 배율`의 영향을 받지 않는 것은 코드의 어느 부분에서 정해질까?

### `PurchaseService.cs`
- **한 줄 역할:** 아이템을 "구매"하고 소유 목록을 관리한다.
- **주요 멤버:** `owned`(소유 집합), `IsOwned`, `TryPurchase`, `OnItemPurchased`.
  `TryPurchase` 순서: null/중복 검사 → **돈 차감** → 소유 등록 → **그리드에 켜기** → 이벤트.
- **누가 호출?** `ShopClickRouter.OnItemClicked`, `ItemToggleService`, `ItemInfoProvider`, `ReactionResolver`.
- **무엇을 호출?** `IWallet.TrySpend`, `IPowerGrid.Activate`.
- **재미있는 점:** 한도를 넘는 아이템도 **살 수 있습니다**. 그 결과 정전이 되는 것이 게임의 긴장 요소(그래서 카드에 "한도 초과 예상!" 경고만 표시).
- **왜 따로?** 구매 절차(돈 → 소유 → 켜기)만 담당 → **S**. 구체 클래스 대신 `IWallet`, `IPowerGrid` 에 의존 → **D**.
- **질문**
  1. `TrySpend` 가 실패하면 `owned.Add` 는 실행될까? 이 순서가 중요한 이유는?
  2. 전력 한도를 넘을 아이템을 구매하면 어떤 연쇄 반응이 일어날까? (1-3 흐름 참고)

### `ItemToggleService.cs`
- **한 줄 역할:** 이미 산 아이템을 켜거나 끈다.
- **주요 멤버:** `Toggle(item)` : 소유하지 않았으면 무시 → 켜져 있으면 `Deactivate`, 꺼져 있으면 `Activate`.
- **누가 호출?** `ShopClickRouter.OnItemClicked` (이미 소유한 아이템을 클릭했을 때).
- **무엇을 호출?** `IPurchaseService.IsOwned`, `IPowerGrid.IsActive/Activate/Deactivate`.
- **왜 따로?** "구매"와 "토글"은 다른 책임 → `PurchaseService` 가 비대해지지 않음 → **S**.
- **질문**
  1. 소유하지 않은 아이템을 `Toggle` 하면 어떻게 될까?
  2. 정전 중에 아이템을 끄면 어떤 이벤트들이 발생할까?

### `UpgradeService.cs`
- **한 줄 역할:** 업그레이드의 레벨/비용을 관리하고, 구매하면 **해당 효과 클래스에게 적용을 맡긴다**.
- **주요 멤버:**
  - `effects` : `UpgradeType → IUpgradeEffect` 사전 (종류별 효과 담당 찾기).
  - `levels` : 업그레이드별 현재 레벨.
  - 생성자: 효과가 없거나 같은 종류가 중복된 데이터는 경고 후 **무시**(안전장치).
  - `TryUpgrade` : 만렙 검사 → 돈 차감 → 레벨 +1 → `effect.Apply` → `OnLevelChanged`.
- **누가 호출?** `ShopClickRouter.OnUpgradeClicked`, `UpgradeInfoProvider`, `ReactionResolver`.
- **무엇을 호출?** `IWallet.TrySpend`, `IUpgradeEffect.Apply/ValueAt`, `UpgradeData.CostAt`.
- **왜 따로?** "어떤 업그레이드든 공통인 절차(돈·레벨)"만 알고, **"무슨 효과인지"는 모름** → 새 종류가 추가돼도 이 파일 무수정 → **O**, **S**.
- **질문**
  1. `UpgradeService` 안에 `if (type == PowerLimit) ... else if ...` 같은 분기가 **없는** 이유는? (대신 무엇을 쓰고 있나?)
  2. 같은 종류(`PowerLimit`)의 업그레이드 데이터를 두 개 넣으면 어떻게 될까?

### `Upgrades/PowerLimitEffect.cs`
- **한 줄 역할:** "전력 한도 업그레이드"가 실제로 무엇을 하는지 정의한다.
- **주요 멤버:** `ValueAt(level)` = `기본한도 + 레벨당효과 × level`, `Apply` → `stats.SetPowerLimit(...)`, `Format` → `"한도 120"` 같은 표시 글자.
- **누가 호출?** `UpgradeService.TryUpgrade`(Apply), `UpgradeInfoProvider`(ValueAt, Format).
- **무엇을 호출?** `IPlayerStatsMutator.SetPowerLimit`.
- **왜 따로?** 업그레이드마다 클래스 하나 → **S**. `IUpgradeEffect` 를 구현 → **O/L** (전략 패턴).
- **질문**
  1. 기본 한도 80, 레벨당 +20 이면 3 레벨의 한도는? 이 값이 화면에 반영되기까지 어떤 이벤트들을 거칠까?
  2. 이 클래스가 `PlayerStats` 가 아니라 `IPlayerStatsMutator` 를 받는 이유는?

### `Upgrades/IncomeMultiplierEffect.cs`
- **한 줄 역할:** "수익 배율 업그레이드"의 효과 정의.
- **주요 멤버:** `ValueAt` = `기본배율 + 레벨당효과 × level`, `Apply` → `SetIncomeMultiplier`, `Format` → `"수익 x1.40"`.
- **호출 관계:** `PowerLimitEffect` 와 같은 구조.
- **왜 따로?** 위와 동일. 두 효과 클래스가 **같은 모양**이라 `UpgradeService` 가 구분 없이 다룸 → **L**.
- **질문**
  1. 기본 배율 1, 레벨당 0.2 일 때 2 레벨의 배율은? 이 배율이 `IncomeCalculator` 의 어디에 곱해질까?
  2. 두 Effect 클래스의 `ValueAt/Apply/Format` 이 비슷한데 하나로 합치지 않은 이유는 뭘까? (장단점을 생각해 보기)

### `Upgrades/IncomeIntervalEffect.cs` (신규)
- **한 줄 역할:** "수익 주기 단축 업그레이드"의 효과 정의.
- **주요 멤버:** `ValueAt(level)` = `max(최소 주기, 기본 주기 × 0.9^레벨)`, `Apply` → `SetTickInterval`, `Format` → `"주기 0.90초"`. 기본/최소 주기는 생성자로 받음(`GameBootstrapper` 의 설정값).
- **참고:** 기본 1초에서 최대 8레벨이면 약 0.43초라, 최소 0.3초는 현재 설정에서 닿지 않는 안전장치.
- **질문**
  1. 레벨 3 의 주기는? (1 × 0.9³)
  2. 최소 주기를 코드에 박지 않고 생성자로 받는 이유는?

### `Upgrades/ClickIncomeEffect.cs` (신규)
- **한 줄 역할:** "클릭 수익 업그레이드"의 효과 정의.
- **주요 멤버:** `ValueAt` = `기본 클릭 수익 + 레벨당 효과 × level`, `Apply` → `SetClickIncome`, `Format` → `"클릭 +3"`.
- **질문**
  1. 기본 1, 레벨당 +2 라면 15 레벨(만렙)의 클릭 수익은?
  2. 이 효과를 추가하면서 `UpgradeService` 를 수정하지 않아도 된 이유는?

### `ItemInfoProvider.cs`
- **한 줄 역할:** 아이템 카드 하나를 그릴 `ItemInfo` 를 만들어 주고, 카드가 바뀔 시점을 알려주는 "정보 창구".
- **주요 멤버:** `GetInfo(item)` : 상태 판정(소유? → 켜짐/꺼짐, 미소유? → 구매 가능/돈 부족), 예상 수익, 한도 초과 여부를 묶어 반환. 돈/구매/켜진 목록/전력/능력치가 바뀌면 `OnChanged`.
- **누가 호출?** `ItemShopView.RefreshAll` 가 `GetInfo` 호출, `OnChanged` 구독.
- **무엇을 호출?** `IWallet`, `IPurchaseService`, `IPowerGrid`, `IIncomeCalculator`, `IPlayerStats`.
- **왜 따로?** UI 가 5개 시스템을 직접 알면 UI 가 지저분해지고 로직이 UI 에 섞임. **여러 시스템의 정보를 하나로 모아주는 중간층**. → **S, D**.
- **질문**
  1. `ItemInfoProvider` 의 `OnChanged` 는 어떤 이벤트 5가지를 모아서 하나로 내보낼까?
  2. 돈이 100 이고 가격 150 짜리를 아직 사지 않았다면 `ItemCardState` 는? 돈이 150 으로 늘어나는 순간 카드가 갱신되는 경로를 설명해 보세요.

### `UpgradeInfoProvider.cs`
- **한 줄 역할:** 업그레이드 카드용 `UpgradeInfo` 를 만들어 주는 정보 창구.
- **주요 멤버:** `GetInfo(data)` : 레벨/만렙/비용/구매 가능/현재 효과 글자/다음 효과 글자. 돈과 레벨이 바뀌면 `OnChanged`.
- **누가 호출?** `UpgradeShopView`.
- **무엇을 호출?** `IWallet`, `IUpgradeService`, `IUpgradeEffect`(글자 포맷).
- **왜 따로?** `ItemInfoProvider` 와 같은 구조(일관성). UI 에서 계산·포맷 로직을 덜어냄 → **S**.
- **질문**
  1. 만렙일 때 `NextText` 가 `CurrentText` 와 같게 만든 이유는?
  2. 이 클래스가 구독하는 이벤트 2개는 무엇이고, 각각 카드의 어떤 부분을 바꾸게 할까?

### `ReactionResolver.cs`
- **한 줄 역할:** 여러 사건(수익, 구매, 위험, 정전, 복구) 중 **지금 캐릭터가 어떤 반응을 해야 하는지** 우선순위로 결정한다.
- **개념 정리:**
  - **기본 반응(Base):** 상태가 유지됨. 정전 → `Blackout`, 전력 70% 이상 → `NearLimit`, 아니면 `Idle`.
  - **일회성 반응(OneShot):** `Income`, `Purchase`, `Recovery` 처럼 잠깐 재생 후 끝남.
  - **우선순위:** Blackout 5 > Recovery 4 > Purchase 3 > NearLimit 2 > Income 1 > Idle 0.
    낮은 우선순위가 높은 반응을 **덮어쓰지 못함** (예: 정전 중엔 수익 반응이 안 뜸).
- **주요 메서드:** `HandlePower/HandleBlackout`(상태 갱신), `TryOneShot`(우선순위 비교 후 재생), `Complete`(재생 끝났다고 `CharacterView` 가 알려줌 → 기본 반응으로 복귀), `Emit`(`OnReaction` 발생).
- **누가 호출?** `CharacterView.Complete`. 이벤트 구독자: `CharacterView.Play`.
- **무엇을 호출?** 생성자에서 `PowerGrid`, `PurchaseService`, `UpgradeService`, `IncomePayout` 의 이벤트를 **구독**.
- **왜 따로?** "어떤 반응을 할지(규칙)"와 "어떻게 보일지(그림/움직임)"를 분리. 규칙은 이 클래스, 그림은 `CharacterView` → **S**. 새 반응 규칙은 여기만 수정.
- **질문**
  1. 정전(Blackout) 중에 수익이 지급되면 캐릭터 반응은 바뀔까? 코드의 어느 부분 때문인가?
  2. `Complete` 에서 `if (activeOneShot != finished) return;` 이 없다면 어떤 문제가 생길 수 있을까?

---

## 3-E. Runtime (조립과 시간)

### `GameTicker.cs`
- **한 줄 역할:** `PlayerStats.TickInterval` 주기(기본 1초)마다 `OnTick` 이벤트를 발생시키는 시계.
- **주요 멤버:** `Bind(IPlayerStats)`(주기를 읽고 `OnStatsChanged` 구독, 이전의 직렬화 `tickInterval` 필드는 제거됨), `ReadInterval`, `Update()` 에서 `Time.deltaTime` 누적 → 간격 이상이면 `OnTick`. `while` 이라서 프레임이 늦어도 밀린 틱을 모두 처리.
- **누가 호출?** Unity(`Update`). 구독자: `GameBootstrapper` 가 `IncomePayout.Pay` 연결.
- **무엇을 호출?** 이벤트만 발생 (누가 듣는지 모름).
- **왜 따로?** "시간"만 책임 → **S**. 틱이 필요한 기능이 생기면 이 클래스를 안 고치고 구독만 추가 → **O**.
- **질문**
  1. `if` 가 아니라 `while` 로 만든 이유는?
  2. `GameTicker` 는 `IncomePayout` 을 직접 알고 있을까? 아니라면 어떻게 연결될까?

### `GameContext.cs`
- **한 줄 역할:** 조립된 부품(서비스)들을 **한 상자에 담아** 전달하는 단순 데이터 묶음.
- **주요 멤버:** `Wallet`, `Grid`, `Payout`, `Click`, `Reaction`, `Items`, `CautionThreshold` 등. 전부 **인터페이스 타입**.
- **누가 호출?** `GameBootstrapper` 가 만들고, `UiBinder.Bind` 가 받아서 UI 에 분배.
- **무엇을 호출?** 없음.
- **왜 따로?** `Bind` 에 인자 12개를 일일이 넘기지 않고 한 상자로 → 가독성. 이 상자에는 **로직이 없음** → **S**.
- **질문**
  1. `GameContext` 의 모든 속성이 `Wallet` 이 아니라 `IWallet` 같은 인터페이스 타입인 이유는?
  2. `GameContext` 에 "돈을 두 배로 만드는 메서드"를 넣으면 어떤 점이 좋지 않을까?

### `GameBootstrapper.cs` ← **Composition Root**
- **한 줄 역할:** 게임이 시작될 때 모든 부품을 **만들고(new) 서로 연결**하는 유일한 장소.
- **주요 멤버:**
  - `[SerializeField]` 설정값: 시작 돈, 전력 한도, 배율, 기본/최소 틱 주기, 기본 클릭 수익, 경고 비율, 아이템/업그레이드 목록, 티커, UiBinder.
  - `Awake()` : `BuildContext()` → 틱에 `Pay` 연결 → `ticker.Bind(stats)` → `uiBinder.Bind(context)`.
  - `BuildContext()` : **의존하는 순서대로** 생성 (stats → wallet → grid → calculator → purchase → payout → effects 4개 → upgrade → `ClickService` 등 나머지).
  - `OnDestroy()` : 이벤트 구독 해제.
- **누가 호출?** Unity(`Awake`).
- **무엇을 호출?** 거의 모든 Core 클래스의 `new`, `UiBinder.Bind`.
- **왜 따로?** **`new` 는 여기에서만.** 다른 클래스들은 생성자로 인터페이스를 받아 쓰기만 하므로, 부품을 바꾸고 싶으면 이 파일 한 곳만 수정 → **D(의존성 역전)** 의 핵심.
- **질문**
  1. `BuildContext` 에서 `PlayerStats` 를 `PowerGrid` 보다 먼저 만들어야 하는 이유는?
  2. `OnDestroy` 에서 `ticker.OnTick -= ...` 를 하는 이유는? 안 하면 어떤 문제가 생길 수 있을까?

### `UiBinder.cs`
- **한 줄 역할:** `GameContext` 에서 필요한 부품을 꺼내 각 UI 화면에 **연결(Bind)** 해 주는 다리.
- **주요 멤버:** `Bind(context)` : `HudView.Bind`, `ItemShopView.Bind`, `UpgradeShopView.Bind`, `CharacterView.Bind`, `CoinPopupView.Bind(payout, click)` 호출. `ShopClickRouter` 를 만들어 카드 클릭 이벤트를 연결하고, `CharacterClickView.OnClicked` 를 `ClickService.Click` 에 연결. `OnDestroy` 에서 모두 해제.
- **누가 호출?** `GameBootstrapper.Awake`.
- **무엇을 호출?** 5개 View 의 `Bind`, `ShopClickRouter` 생성, `IClickService.Click`.
- **왜 따로?** `GameBootstrapper` 가 UI 세부사항을 몰라도 되게(→ **S**), UI 들이 서로의 존재를 몰라도 되게 한곳에서 연결.
- **질문**
  1. `HudView` 는 `GameBootstrapper` 를 알고 있을까? 어떻게 `IWallet` 을 얻을까?
  2. 카드를 클릭하면 `UiBinder` 가 만든 어떤 객체로 이벤트가 전달될까?

### `ShopClickRouter.cs`
- **한 줄 역할:** 상점 클릭을 올바른 서비스로 **교통정리**한다.
- **주요 멤버:** `OnItemClicked(item)` : 소유 중이면 `Toggle`, 아니면 `TryPurchase`. `OnUpgradeClicked(data)` : `TryUpgrade`.
- **누가 호출?** `ItemShopView.OnItemClicked`, `UpgradeShopView.OnUpgradeClicked` 이벤트가 `UiBinder` 에서 이 메서드에 연결됨.
- **무엇을 호출?** `IPurchaseService`, `IItemToggleService`, `IUpgradeService`.
- **왜 따로?** "클릭 → 어떤 행동?" 판단을 View 에서 빼냄. View 는 "눌렸다"만 알리고, **무슨 일이 일어날지는 모름** → **S**. MonoBehaviour 가 아닌 **순수 C# 클래스**라서 테스트하기도 쉬움.
- **질문**
  1. `ItemShopView` 는 클릭됐을 때 "구매할지, 토글할지"를 스스로 판단할까? 누가 판단할까?
  2. 새 동작 "아이템 판매"(길게 누르기 등)를 추가하려면 어디를 고치게 될까?

---

## 3-F. UI (화면)

> UI 클래스의 공통 규칙:
> ① `Bind(...)` 로 인터페이스를 **받는다**(주입). ② 이벤트를 **구독**한다. ③ `OnDestroy` 에서 **구독 해제**한다.
> ④ 게임 규칙은 모르고, 받은 값을 **보여주기만** 한다.

### `HudView.cs`
- **한 줄 역할:** 상단의 돈 / 전력 게이지 / 틱당 수익 / 초당 수익 / 정전 경고를 표시한다.
- **주요 메서드:** `Bind` (구독 + 첫 값 즉시 표시), `ShowMoney`, `ShowIncome`, `ShowPerSecond`("초당 수익: +N"), `ShowPower`(게이지 길이는 `anchorMax.x = 비율`, 색은 안전/주의/위험), `ShowBlackout`(경고창 켜기/끄기).
- **누가 호출?** `UiBinder.Bind`.
- **무엇을 호출?** 구독: `IWallet.OnMoneyChanged`, `IPowerGrid.OnPowerChanged/OnBlackoutChanged`, `IIncomeForecast.OnTickIncomeChanged/OnPerSecondIncomeChanged`.
- **왜 따로?** 화면 표시만 담당 → **S**. 인터페이스만 알아서 Core 가 바뀌어도 영향 적음 → **D**.
- **질문**
  1. `Bind` 마지막에 `ShowMoney(wallet.Money)` 등을 한 번씩 직접 호출하는 이유는?
  2. 전력 비율 0.5 / 0.8 / 1.2 일 때 게이지 색은 각각 무엇일까? (`cautionThreshold=0.7`)

### `ItemShopView.cs`
- **한 줄 역할:** 아이템 카드 목록을 만들고, 정보가 바뀌면 모든 카드를 새로고침한다.
- **주요 멤버:** `Bind`(카드 프리팹을 아이템 수만큼 `Instantiate` 후 `Init`), `cards` 사전, `RefreshAll`(각 카드에 `GetInfo` 결과 전달), `OnItemClicked` 이벤트(카드 클릭을 위로 전달).
- **누가 호출?** `UiBinder.Bind`. 클릭 이벤트 구독자: `ShopClickRouter`.
- **무엇을 호출?** `ItemCardView.Init/Refresh`, `IItemInfoProvider.GetInfo` 와 `OnChanged`.
- **왜 따로?** 카드 하나(`ItemCardView`)와 목록(`ItemShopView`)의 책임 분리 → **S**.
- **질문**
  1. `card.Init(item, clicked => OnItemClicked?.Invoke(clicked))` 에서 `?.` 는 무엇을 막아 줄까?
  2. 돈이 변할 때마다 `RefreshAll` 이 호출되는 경로를 `Wallet` 부터 순서대로 설명해 보세요.

### `ItemCardView.cs`
- **한 줄 역할:** 아이템 카드 한 장의 모양(이름, 가격, 상태, 색, 경고)을 책임진다.
- **주요 메서드:** `Init`(변하지 않는 값 표시 + 버튼 클릭 연결), `Refresh(ItemInfo)`(예상 수익, 경고, 상태별 글자/색/버튼 활성).
- **누가 호출?** `ItemShopView`.
- **무엇을 호출?** `Button.onClick` 연결(이것도 이벤트!), 텍스트/이미지 갱신.
- **왜 따로?** 카드 외형 변경은 이 클래스만 수정 → **S**. `ItemInfo` 만 받아서 규칙을 모름.
- **질문**
  1. `Init` 과 `Refresh` 를 나눈 이유는? (어떤 값은 한 번만, 어떤 값은 계속 바뀜)
  2. 상태가 `Unaffordable` 일 때 버튼은 눌릴까? 어디서 정하는가?

### `UpgradeShopView.cs`
- **한 줄 역할:** 업그레이드 카드 목록을 만들고 새로고침한다. (`ItemShopView` 와 같은 구조)
- **누가 호출?** `UiBinder`. **무엇을 호출?** `UpgradeCardView`, `IUpgradeInfoProvider`.
- **왜 따로?** 아이템과 업그레이드를 별도 View 로 → 한쪽 변경이 다른 쪽에 영향 없음 → **S**.
- **질문**
  1. `ItemShopView` 와 이 클래스의 코드가 거의 같은데, 하나로 합친다면 어떤 방법(제네릭 등)이 있을까?
  2. `OnUpgradeClicked` 이벤트는 최종적으로 어떤 메서드를 실행시킬까?

### `UpgradeCardView.cs`
- **한 줄 역할:** 업그레이드 카드 한 장(이름, 레벨, 효과, 비용)을 보여준다.
- **주요 메서드:** `Init`(이름 + **아이콘**(`UpgradeData.Icon`, 없으면 숨김) + 클릭 연결), `Refresh(UpgradeInfo)`(레벨 `Lv 2/10`, 효과 `현재 -> 다음`, 비용/`MAX`, 버튼 활성 여부 = **만렙 아님 & 돈 충분**).
- **누가 호출?** `UpgradeShopView`. **무엇을 호출?** UI 컴포넌트 갱신.
- **질문**
  1. 카드의 버튼이 비활성(회색)이 되는 두 가지 조건은?
  2. "다음 효과" 글자는 어디에서 만들어져서 여기로 올까?

### `CoinPopupView.cs`
- **한 줄 역할:** 수익이 지급되거나 캐릭터가 클릭될 때마다 "+N" 팝업을 생성한다.
- **주요 멤버:** `Bind(IIncomePayout, IClickService)`, `Show(amount)` : 프리팹 `Instantiate` 후 `Play`(좌우 랜덤 위치).
- **누가 호출?** `UiBinder`. **무엇을 호출?** `IIncomePayout.OnPaid` 와 `IClickService.OnClicked` 구독(같은 `Show` 사용), `CoinPopupItem.Play`.
- **왜 따로?** "팝업 만들기"와 "팝업 움직임"을 `CoinPopupItem` 과 분리 → **S**.
- **질문**
  1. 이 클래스는 `GameTicker` 나 `Wallet` 을 알고 있을까? 알고 있는 것은 무엇인가?
  2. `Random.Range(-spreadX, spreadX)` 는 왜 쓰였을까?

### `CoinPopupItem.cs`
- **한 줄 역할:** 팝업 하나가 위로 떠오르며 투명해지다가 사라진다.
- **주요 멤버:** `Play(text, start)`, `Update`(`t = elapsed / lifetime` 으로 위치와 투명도 보간, `t>=1` 이면 `Destroy`).
- **누가 호출?** `CoinPopupView.Show`. **무엇을 호출?** `Destroy(gameObject)` 로 자기 정리.
- **질문**
  1. 팝업이 알아서 사라지지 않으면 어떤 문제가 생길까?
  2. `t` 값이 0 → 1 로 변하는 것을 이용해 위치와 투명도가 함께 변하는 원리를 설명해 보세요.

### `CharacterView.cs`
- **한 줄 역할:** `ReactionType` 에 맞는 스프라이트/색/움직임으로 캐릭터를 연출한다.
- **주요 멤버:** `ReactionVisual`(반응 하나의 그림/색/움직임/지속시간 묶음, `[Serializable]`), `ReactionMotion`(Breathe/Bounce/Shake/Sink), `lookup`(타입 → 비주얼 사전), `Play`(교체), `Update`(경과 시간 → `Animate`, 지속시간이 끝나면 `resolver.Complete`).
- **누가 호출?** `UiBinder.Bind`. **무엇을 호출?** `IReactionResolver.OnReaction` 구독, `Complete` 호출.
- **왜 따로?** 규칙(`ReactionResolver`)은 모르고 "받은 반응을 그리는 일"만 → **S**. 새 반응 모양은 인스펙터 데이터 추가로 가능 → **O**.
- **질문**
  1. `Duration` 이 0 인 반응(예: Idle, Blackout)은 왜 `Complete` 를 호출하지 않을까?
  2. 반응의 종류를 알려주는 쪽(Resolver)과 그림을 그리는 쪽(View)이 서로 직접 참조하지 않고 이벤트로만 연결된 이점은?

### `CharacterClickView.cs` (신규)
- **한 줄 역할:** 캐릭터가 클릭되면 `OnClicked` 콜백만 알린다. (`IPointerClickHandler`)
- **주요 멤버:** `event Action OnClicked`, `OnPointerClick`. 규칙(정전 검사, 수익)은 전혀 모름.
- **누가 호출?** Unity `EventSystem`. 구독자: `UiBinder` 가 `ClickService.Click` 에 연결.
- **참고:** 캐릭터 `Image` 의 `raycastTarget` 이 켜져 있어야 클릭이 들어옴 (`TycoonSceneBuilder` 가 설정).
- **왜 따로?** 그림(`CharacterView`)과 입력(`CharacterClickView`)의 책임 분리 → **S**.
- **질문**
  1. 이 클래스에 `ClickService` 를 직접 넣지 않은 이유는?
  2. 정전 중 클릭을 무시하는 코드는 이 클래스에 있을까?

### `ShopTabsView.cs`
- **한 줄 역할:** 아이템 탭 / 업그레이드 탭 버튼으로 두 패널을 번갈아 보여준다.
- **주요 멤버:** `Awake` 에서 버튼 이벤트 연결, `Show(bool items)`.
- **특이점:** 게임 데이터가 필요 없는 **순수 UI 동작**이라 `Bind` 도 필요 없고 `UiBinder` 도 거치지 않음.
- **누가 호출?** Unity(`Awake`). **무엇을 호출?** `Button.onClick`, `SetActive`.
- **질문**
  1. 이 클래스는 왜 `GameContext` 가 필요 없을까?
  2. 아이템 탭이 선택된 상태에서는 어떤 버튼이 눌릴 수 있을까?

---

## 3-G. Editor / 기타

### `Editor/TycoonSceneBuilder.cs`
- **한 줄 역할:** 메뉴 `Tools > Tycoon > Build Scene` 한 번으로 데이터 에셋, 프리팹, Canvas UI, `GameSystems` 오브젝트와 연결(인스펙터 필드)까지 **자동 생성**하는 에디터 도구.
- **주요 메서드:** `Build`(전체 순서), `CreateItems`(7개)/`CreateUpgrades`(4개)(ScriptableObject 생성; 이미 있으면 **재사용**, 값을 덮어쓰지 않음), `AssignIcons`(색 사각형 임시 PNG 를 `Sprites/Icons/` 에 만들고 **`icon` 이 비어 있을 때만** 연결), `Create…Prefab`, `CreateHud/CreateShop/CreateCharacterArea`, `SetFields/SetValues/SetArray`(`SerializedObject` 로 private 필드 채우기).
- **누가 호출?** 사람이 메뉴로 실행. **무엇을 호출?** UnityEditor API 와 위의 거의 모든 View/Bootstrapper 타입.
- **신규 UI:** HUD 초당 수익 텍스트, `UpgradeCard` 아이콘, 캐릭터 `CharacterClickView`(+ `raycastTarget` 켜기), `UiBinder`/`GameBootstrapper` 연결.
- **주의:** 아이템 목록은 실제 에셋 이름과 맞춰야 함(예: `electric bulb`). 틀리면 빌더가 엉뚱한 에셋을 새로 만듦.
- **주의:** 게임 실행 중에는 쓰이지 않는 **에디터 전용**(폴더가 `Editor`) 이라 빌드에 포함되지 않음.
- **왜 따로?** "씬 구성"과 "게임 로직"을 분리 → **S**. 손으로 하면 실수하기 쉬운 연결을 자동화.
- **질문**
  1. `Editor` 폴더에 있는 스크립트가 게임 빌드에 포함되지 않는 이유는?
  2. `CreateItems` 가 "이미 있으면 새로 만들지 않는" 구조 덕분에 어떤 일이 보호될까? (직접 수정한 에셋)

### `Pixel Art City Backgrounds/Scenes/MoveBackground.cs`
- **한 줄 역할:** (외부 에셋 팩에 포함된 스크립트) 배경을 왼쪽으로 흘려보내다가 끝에 닿으면 처음 위치로 되돌려 무한 스크롤처럼 보이게 한다.
- **주요 멤버:** `speed`, `PontoDeDestino`(끝 지점), `PontoOriginal`(되돌아갈 지점). `Update` 에서 x 이동.
- **관계:** 게임 로직(Core/Runtime)과 **무관**. 시각 연출 전용.
- **참고:** 외부 팩의 코드라 이 프로젝트의 구조 규칙을 따르지 않음 (필드가 `public`, 포르투갈어 이름).
- **질문**
  1. `Time.deltaTime` 을 곱하는 이유는? (컴퓨터 성능에 따라 속도가 달라지지 않게)
  2. 이 스크립트는 `GameBootstrapper` 와 연결되어 있을까? 왜 그런 구조가 가능한가?

---

## 4. 헷갈리기 쉬운 개념

### 4-1. 이벤트 (`event Action`)

**비유:** 유튜브 채널 구독.
- 채널(= `Wallet`)은 영상을 올릴 때(= 돈이 바뀔 때) **알림**만 보냄.
- 구독자(= `HudView`)는 알림을 받고 자기 할 일을 함.
- 채널은 **누가 구독하는지 몰라도** 됨.

**이 프로젝트의 코드:**

```csharp
// Wallet.cs  (알림을 보내는 쪽)
public event Action<int> OnMoneyChanged;   // "int(새 금액)를 알려주는 알림" 선언
...
Money += amount;
OnMoneyChanged?.Invoke(Money);             // 알림 발송. ?. 는 "구독자가 없으면 건너뛰기"

// HudView.cs  (알림을 받는 쪽)
wallet.OnMoneyChanged += ShowMoney;        // 구독 (+=)
wallet.OnMoneyChanged -= ShowMoney;        // 구독 해제 (-=)  ← OnDestroy 에서
```

- `Action` = "반환값 없는 함수"의 타입. `Action<int>` = "int 하나를 받는 함수".
- `event` 키워드: **밖에서는 `+=`, `-=` 만 가능**하고, 밖에서 `Invoke` 는 **못 함**. (그래서 `Wallet` 이 외부에서 함부로 알림이 울리는 것을 막음.)
- **구독 해제를 안 하면?** 파괴된 UI 오브젝트에 이벤트가 계속 호출되어 오류. 그래서 모든 View 에 `OnDestroy` 가 있음.
- 이벤트 한 번에 여러 구독자가 가능(`OnMoneyChanged` 구독자: HUD, ItemInfoProvider, UpgradeInfoProvider).

### 4-2. 인터페이스와 주입 (Dependency Injection)

**인터페이스 = 콘센트 규격.** 어떤 가전이든 규격만 맞으면 꽂을 수 있듯이, `IWallet` 규격만 맞으면 어떤 지갑이든 쓸 수 있음.

**주입 = 부품을 생성자로 받는다 (스스로 `new` 하지 않는다).**

```csharp
// 나쁜 예 (스스로 만든다) — Wallet 에 단단히 묶임
public class PurchaseService {
    private Wallet wallet = new Wallet(50);
}

// 이 프로젝트의 방식 (바깥에서 받는다)
public class PurchaseService : IPurchaseService {
    private readonly IWallet wallet;
    public PurchaseService(IWallet wallet, IPowerGrid grid) {   // ← 생성자 주입
        this.wallet = wallet;
        this.grid = grid;
    }
}
```

이점:
1. 다른 지갑으로 **바꿔 끼우기 쉬움** (예: 테스트용 가짜 지갑).
2. **어디에 의존하는지** 생성자만 보면 보임.
3. 순환 의존/숨은 의존을 만들기 어려움.

### 4-3. ScriptableObject

- **"게임 데이터를 담는 파일"** 입니다. 씬에 올리지 않고 프로젝트 폴더(`Assets/_LDY/Data/Fan.asset` 등)에 **파일로** 존재.
- `MonoBehaviour` 는 씬의 게임 오브젝트에 붙는 스크립트, `ScriptableObject` 는 데이터 저장용.
- 이 프로젝트: `ItemData`, `UpgradeData`.

```csharp
[CreateAssetMenu(menuName = "Tycoon/Item Data", fileName = "NewItem")]
public class ItemData : ScriptableObject {
    [SerializeField] private int price;   // 인스펙터에서 편집 가능
    public int Price => price;            // 코드에서는 읽기만
}
```

- 장점: 코드 수정 없이 **인스펙터에서 수치 조정**, 여러 곳에서 같은 파일을 공유, 새 아이템 = 새 파일 하나.
- 주의: `HashSet<ItemData>` 처럼 **에셋 자체를 열쇠**로 쓰기 때문에, 같은 에셋을 가리켜야 같은 아이템으로 취급됨.
- 주의: 실행 중 값을 바꾸면 에셋 자체가 바뀔 수 있으니 이 프로젝트는 **읽기 전용 프로퍼티**만 제공(실행 중 상태는 `PurchaseService`, `UpgradeService` 가 따로 보관).

### 4-4. Composition Root (조립 지점)

- **"모든 부품을 만들고 연결하는 단 하나의 장소"**. 이 프로젝트에서는 `GameBootstrapper.BuildContext()`.
- 레고 조립에 비유: 부품(클래스)들은 서로를 직접 만들지 않고, **조립 설명서(Bootstrapper)** 가 순서대로 끼워 맞춤.
- 규칙: **`new` 는 Bootstrapper 에서만.** 다른 곳에서는 인터페이스로 받기만.
- 그래서 부품을 교체하려면 **이 파일 한 군데**만 고치면 됨.

```csharp
var stats  = new PlayerStats(powerLimit, baseIncomeMultiplier);
var wallet = new Wallet(startingMoney);
var grid   = new PowerGrid(stats);                 // stats 를 주입
var calculator = new IncomeCalculator(grid, stats);
var payout = new IncomePayout(wallet, calculator);
```

> 이 순서는 **"먼저 만들어져야 하는 것"** 이 앞에 오는 의존 순서입니다.

### 4-5. MonoBehaviour vs 순수 C# 클래스

| | MonoBehaviour | 순수 C# 클래스 |
|---|---|---|
| 예시 | `GameTicker`, `HudView`, `GameBootstrapper` | `Wallet`, `PowerGrid`, `ShopClickRouter` |
| 생성 | Unity 가 (씬에 붙여서) | `new` |
| `Update` 등 | 사용 가능 | 없음 |
| 용도 | 화면/시간/Unity 연동 | 게임 규칙 (Unity 없이도 테스트 가능) |

Core 가 전부 순수 C# 이라서 **Unity 를 실행하지 않고도 규칙을 테스트**할 수 있다는 것이 큰 장점.

### 4-6. 의존성 방향 (제일 중요한 그림)

```
        UI (View)  ─────────▶  Interfaces (I○○○)  ◀─────────  Core (구현)
          │ 구독/호출                  ▲                          ▲
          │                           │                          │
          └──────────  Runtime (GameBootstrapper) 가 모두 조립 ───┘
```

- **Core 는 UI 를 전혀 모릅니다.** (Core 에 `HudView` 를 쓰는 코드가 있나? → 없음!)
- UI 는 Core 의 **인터페이스**만 압니다.
- 둘을 이어주는 것은 **이벤트** + **Bootstrapper**.

### 4-7. "Info Provider" 패턴이 왜 있나?

카드를 그리려면 돈, 소유 여부, 켜짐 여부, 전력, 수익 등 **여러 정보**가 필요합니다.
View 가 이걸 전부 알면 View 가 Core 와 너무 엮입니다. 그래서:

`여러 시스템 → InfoProvider 가 요약(ItemInfo) → View 는 요약만 보고 그리기`

---

## 5. 마무리 종합 문제 (전체 이해 점검)

1. 게임이 시작될 때 `OnTick += Pay` 같은 연결은 **어디에서** 일어날까?
2. 정전 상태에서 1틱이 지나면 `Wallet`, `HUD`, `CoinPopup`, `Character` 중 **바뀌는 것**은?
3. 새 아이템 "에어컨 2"를 추가할 때 **수정해야 할 C# 파일은 몇 개**일까? (정답: 0개 → 에셋 추가 + 목록 등록. 채굴기/공장 설비/데이터센터가 실제 예)
4. 새 업그레이드 "틱 속도"(= 수익 주기 단축)를 추가하려면 **새로 만들 파일**과 **수정해야 할 파일**은?
   (힌트: `UpgradeType` 에 추가, 새 `IUpgradeEffect` 구현, `BuildContext` 에 등록. `UpgradeService` 는 무수정. 주기를 쓰는 `GameTicker` 는 `PlayerStats` 를 읽도록 바뀜)
5. 정전 중에 캐릭터를 클릭하면 `Wallet`, `HUD`, `CoinPopup` 중 바뀌는 것은? (정답: 없음. 왜?)
6. 수익 주기가 0.9 배로 줄면 `IncomeForecast` 의 어떤 값이 바뀌고, 어떤 이벤트가 HUD 에 전달될까?
7. `PowerGrid` 가 `HudView` 를 몰라도 HUD 가 갱신되는 이유를 "이벤트"라는 단어를 써서 설명해 보세요.
8. `ReactionResolver` 에서 우선순위 규칙을 없애면 게임에서 어떤 이상한 모습이 나올까?

---

## 6. 학습 팁

1. **이벤트를 따라가며 읽기:** `event` 를 찾고 → `+=` 를 검색해서 구독자 확인 → 구독자가 하는 일 읽기.
2. **디버그 로그로 확인:** 연습 삼아 `Wallet.TrySpend` 에 임시 `Debug.Log` 를 넣고 구매 순서를 눈으로 확인 (연습 후 지우기).
3. **작은 변경 연습:** "정전 기준을 `>` 에서 `>=` 로 바꾸면?" 같은 한 줄 변경이 어디에 영향을 주는지 예측해 보기.
4. **그려 보기:** 1-2, 1-3 의 흐름도를 종이에 직접 다시 그려 보기. 그릴 수 있으면 이해한 것!

---

## 7. 문서에서 설명한 코드 위치 (클릭하면 해당 줄로 이동)

> 줄 번호는 이 문서를 쓴 시점의 코드 기준입니다. 코드를 수정하면 몇 줄 어긋날 수 있어요.

### 7-1. 이벤트 (4-1, 옵저버 패턴)

| 무엇 | 위치 |
|---|---|
| 이벤트 선언 `OnMoneyChanged` | [Wallet.cs:6](../Assets/_LDY/Scripts/Core/Wallet.cs#L6) |
| 이벤트 발생 `?.Invoke` (쓸 때 / 벌 때) | [Wallet.cs:19](../Assets/_LDY/Scripts/Core/Wallet.cs#L19), [Wallet.cs:27](../Assets/_LDY/Scripts/Core/Wallet.cs#L27) |
| 구독 `+=` (HUD) | [HudView.cs:29-33](../Assets/_LDY/Scripts/UI/HudView.cs#L29-L33) |
| 구독 해제 `-=` (HUD) | [HudView.cs:44-54](../Assets/_LDY/Scripts/UI/HudView.cs#L44-L54) |
| 틱 이벤트 선언 / 발생 | [GameTicker.cs:10](../Assets/_LDY/Scripts/Runtime/GameTicker.cs#L10), [GameTicker.cs:32](../Assets/_LDY/Scripts/Runtime/GameTicker.cs#L32) |
| 틱에 `Pay` 연결 / 해제 | [GameBootstrapper.cs:23](../Assets/_LDY/Scripts/Runtime/GameBootstrapper.cs#L23), [GameBootstrapper.cs:30](../Assets/_LDY/Scripts/Runtime/GameBootstrapper.cs#L30) |
| 클릭 이벤트 선언 / 발생 (`OnClicked`) | [ClickService.cs:9](../Assets/_LDY/Scripts/Core/ClickService.cs#L9), [ClickService.cs:26](../Assets/_LDY/Scripts/Core/ClickService.cs#L26) |
| 캐릭터 클릭 → `ClickService` 연결 | [UiBinder.cs:23-24](../Assets/_LDY/Scripts/Runtime/UiBinder.cs#L23-L24) |
| 팝업이 클릭 이벤트도 구독 | [CoinPopupView.cs:16-17](../Assets/_LDY/Scripts/UI/CoinPopupView.cs#L16-L17) |
| 전력 이벤트 3개 선언 | [PowerGrid.cs:14-16](../Assets/_LDY/Scripts/Core/PowerGrid.cs#L14-L16) |
| 능력치 변경 이벤트 | [PlayerStats.cs:10](../Assets/_LDY/Scripts/Core/PlayerStats.cs#L10) |
| Core 가 이벤트를 구독하는 예 (PowerGrid 가 능력치를 구독) | [PowerGrid.cs:21](../Assets/_LDY/Scripts/Core/PowerGrid.cs#L21) |
| 여러 이벤트를 하나로 모으기 (`OnChanged`) | [ItemInfoProvider.cs:20-24](../Assets/_LDY/Scripts/Core/ItemInfoProvider.cs#L20-L24) |
| 버튼 이벤트 → 위로 전달 | [ItemShopView.cs:23](../Assets/_LDY/Scripts/UI/ItemShopView.cs#L23) |

### 7-2. 인터페이스 주입 (4-2)

| 무엇 | 위치 |
|---|---|
| 생성자 주입 (`IWallet`, `IPowerGrid` 를 받음) | [PurchaseService.cs:12-16](../Assets/_LDY/Scripts/Core/PurchaseService.cs#L12-L16) |
| 인터페이스 정의 `IWallet` | [IWallet.cs](../Assets/_LDY/Scripts/Core/Interfaces/IWallet.cs) |
| 인터페이스 구현 `: IWallet` | [Wallet.cs:3](../Assets/_LDY/Scripts/Core/Wallet.cs#L3) |
| 한 클래스가 인터페이스 둘을 구현 (읽기/쓰기 분리) | [PlayerStats.cs:3](../Assets/_LDY/Scripts/Core/PlayerStats.cs#L3) |
| 읽기 전용 / 쓰기 전용 인터페이스 | [IPlayerStats.cs](../Assets/_LDY/Scripts/Core/Interfaces/IPlayerStats.cs), [IPlayerStatsMutator.cs](../Assets/_LDY/Scripts/Core/Interfaces/IPlayerStatsMutator.cs) |
| UI 가 `Bind` 로 인터페이스를 받음 | [HudView.cs:22-27](../Assets/_LDY/Scripts/UI/HudView.cs#L22-L27) |
| `GameContext` 의 속성이 모두 인터페이스 타입 | [GameContext.cs:5-18](../Assets/_LDY/Scripts/Runtime/GameContext.cs#L5-L18) |

### 7-3. ScriptableObject (4-3)

| 무엇 | 위치 |
|---|---|
| `ItemData` 정의 + 메뉴 등록 | [ItemData.cs:3-4](../Assets/_LDY/Scripts/Data/ItemData.cs#L3-L4) |
| 읽기 전용 프로퍼티 (`Price` 등) | [ItemData.cs:14-20](../Assets/_LDY/Scripts/Data/ItemData.cs#L14-L20) |
| `UpgradeData` 비용 공식 `CostAt` | [UpgradeData.cs:28](../Assets/_LDY/Scripts/Data/UpgradeData.cs#L28) |
| 에셋 자체를 열쇠로 쓰는 `HashSet<ItemData>` | [PurchaseService.cs:8](../Assets/_LDY/Scripts/Core/PurchaseService.cs#L8), [PowerGrid.cs:7](../Assets/_LDY/Scripts/Core/PowerGrid.cs#L7) |
| 아이템 에셋을 코드로 생성 | [TycoonSceneBuilder.cs:74-112](../Assets/_LDY/Editor/TycoonSceneBuilder.cs#L74-L112) |
| 임시 아이콘 생성 / 연결 | [TycoonSceneBuilder.cs:150](../Assets/_LDY/Editor/TycoonSceneBuilder.cs#L150) |
| 실제 데이터 파일 | [Assets/_LDY/Data/](../Assets/_LDY/Data/) |

### 7-4. Composition Root (4-4)

| 무엇 | 위치 |
|---|---|
| 모든 부품을 `new` 하는 곳 `BuildContext` | [GameBootstrapper.cs:33-67](../Assets/_LDY/Scripts/Runtime/GameBootstrapper.cs#L33-L67) |
| 시작 순서 `Awake` | [GameBootstrapper.cs:20-26](../Assets/_LDY/Scripts/Runtime/GameBootstrapper.cs#L20-L26) |
| UI 에 부품 분배 `Bind` | [UiBinder.cs:15-30](../Assets/_LDY/Scripts/Runtime/UiBinder.cs#L15-L30) |
| 설정값 (시작 돈, 한도, 주기, 클릭 수익, 경고 비율) | [GameBootstrapper.cs:5-15](../Assets/_LDY/Scripts/Runtime/GameBootstrapper.cs#L5-L15) |

### 7-5. 흐름(1장)에 나온 핵심 코드

| 무엇 | 위치 |
|---|---|
| 틱 시간 누적 (`while`) | [GameTicker.cs:26-34](../Assets/_LDY/Scripts/Runtime/GameTicker.cs#L26-L34) |
| 틱 주기를 `PlayerStats` 에서 읽기 | [GameTicker.cs:12-24](../Assets/_LDY/Scripts/Runtime/GameTicker.cs#L12-L24) |
| 수익 지급 `Pay` | [IncomePayout.cs:16-23](../Assets/_LDY/Scripts/Core/IncomePayout.cs#L16-L23) |
| 틱 수익 계산 (정전이면 0) | [IncomeCalculator.cs:14-21](../Assets/_LDY/Scripts/Core/IncomeCalculator.cs#L14-L21) |
| 수익 공식 `RawIncome` | [IncomeCalculator.cs:25](../Assets/_LDY/Scripts/Core/IncomeCalculator.cs#L25) |
| 클릭 → 구매/토글 분기 | [ShopClickRouter.cs:14-18](../Assets/_LDY/Scripts/Runtime/ShopClickRouter.cs#L14-L18) |
| 구매 `TryPurchase` | [PurchaseService.cs:20-29](../Assets/_LDY/Scripts/Core/PurchaseService.cs#L20-L29) |
| 켜기/끄기 `Toggle` | [ItemToggleService.cs:12-18](../Assets/_LDY/Scripts/Core/ItemToggleService.cs#L12-L18) |
| 전력 합산 + 정전 판정 `Recalculate` | [PowerGrid.cs:43-55](../Assets/_LDY/Scripts/Core/PowerGrid.cs#L43-L55) |
| "켜면 한도 초과?" `WouldExceedLimit` | [PowerGrid.cs:26-27](../Assets/_LDY/Scripts/Core/PowerGrid.cs#L26-L27) |
| 예상 수익 재계산 (틱당 / 초당) | [IncomeForecast.cs:26-43](../Assets/_LDY/Scripts/Core/IncomeForecast.cs#L26-L43) |
| 클릭 수익 지급 `Click` | [ClickService.cs:18-27](../Assets/_LDY/Scripts/Core/ClickService.cs#L18-L27) |
| 업그레이드 구매 `TryUpgrade` | [UpgradeService.cs:50-60](../Assets/_LDY/Scripts/Core/UpgradeService.cs#L50-L60) |
| 업그레이드 효과 적용 (한도) | [PowerLimitEffect.cs:16-18](../Assets/_LDY/Scripts/Core/Upgrades/PowerLimitEffect.cs#L16-L18) |
| 업그레이드 효과 적용 (배율) | [IncomeMultiplierEffect.cs:14-16](../Assets/_LDY/Scripts/Core/Upgrades/IncomeMultiplierEffect.cs#L14-L16) |
| 업그레이드 효과 (수익 주기) | [IncomeIntervalEffect.cs:18-21](../Assets/_LDY/Scripts/Core/Upgrades/IncomeIntervalEffect.cs#L18-L21) |
| 업그레이드 효과 (클릭 수익) | [ClickIncomeEffect.cs:16-18](../Assets/_LDY/Scripts/Core/Upgrades/ClickIncomeEffect.cs#L16-L18) |
| 반응 구독 설정 | [ReactionResolver.cs:13-23](../Assets/_LDY/Scripts/Core/ReactionResolver.cs#L13-L23) |
| 반응 우선순위 `Priority` | [ReactionResolver.cs:74-85](../Assets/_LDY/Scripts/Core/ReactionResolver.cs#L74-L85) |
| 일회성 반응 `TryOneShot` / 종료 `Complete` | [ReactionResolver.cs:58-65](../Assets/_LDY/Scripts/Core/ReactionResolver.cs#L58-L65), [ReactionResolver.cs:25-30](../Assets/_LDY/Scripts/Core/ReactionResolver.cs#L25-L30) |
| 캐릭터가 `Complete` 호출 | [CharacterView.cs:72-80](../Assets/_LDY/Scripts/UI/CharacterView.cs#L72-L80) |
| 카드 상태 판정 `GetInfo` | [ItemInfoProvider.cs:27-34](../Assets/_LDY/Scripts/Core/ItemInfoProvider.cs#L27-L34) |
| 업그레이드 카드 정보 | [UpgradeInfoProvider.cs:19-30](../Assets/_LDY/Scripts/Core/UpgradeInfoProvider.cs#L19-L30) |
| 카드 전체 새로고침 `RefreshAll` | [ItemShopView.cs:36-39](../Assets/_LDY/Scripts/UI/ItemShopView.cs#L36-L39) |
| HUD 전력 게이지/색 | [HudView.cs:63-71](../Assets/_LDY/Scripts/UI/HudView.cs#L63-L71) |
| 코인 팝업 생성 / 애니메이션 | [CoinPopupView.cs:26-30](../Assets/_LDY/Scripts/UI/CoinPopupView.cs#L26-L30), [CoinPopupItem.cs:22-30](../Assets/_LDY/Scripts/UI/CoinPopupItem.cs#L22-L30) |
| 업그레이드 효과 사전 (`if` 분기 대신) | [UpgradeService.cs:8](../Assets/_LDY/Scripts/Core/UpgradeService.cs#L8), [UpgradeService.cs:48](../Assets/_LDY/Scripts/Core/UpgradeService.cs#L48) |
| 씬 자동 생성 `Build` | [TycoonSceneBuilder.cs:21-56](../Assets/_LDY/Editor/TycoonSceneBuilder.cs#L21-L56) |

### 7-6. 폴더별 전체 파일 목록

- **Data:** [ItemData.cs](../Assets/_LDY/Scripts/Data/ItemData.cs), [UpgradeData.cs](../Assets/_LDY/Scripts/Data/UpgradeData.cs)
- **Core 구현:** [Scripts/Core/](../Assets/_LDY/Scripts/Core/)
- **Core 인터페이스:** [Scripts/Core/Interfaces/](../Assets/_LDY/Scripts/Core/Interfaces/)
- **Core 모델:** [Scripts/Core/Models/](../Assets/_LDY/Scripts/Core/Models/)
- **업그레이드 효과:** [Scripts/Core/Upgrades/](../Assets/_LDY/Scripts/Core/Upgrades/)
- **Runtime:** [Scripts/Runtime/](../Assets/_LDY/Scripts/Runtime/)
- **UI:** [Scripts/UI/](../Assets/_LDY/Scripts/UI/)
- **Editor:** [TycoonSceneBuilder.cs](../Assets/_LDY/Editor/TycoonSceneBuilder.cs)

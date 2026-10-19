# 전력 테마 도트 UI 팩

직접 생성한 에셋이라 별도 출처 표기는 필요 없음. 팔레트는 남색/청록/노랑 계열로 통일.

## 임포트 설정 (전부 공통)
- Texture Type: Sprite (2D and UI)
- Filter Mode: Point (no filter)
- Compression: None
- Pixels Per Unit: 16

## 9-slice (Sprite Editor > Border, 이후 Image Type = Sliced)
| 파일 | Border (L/B/R/T) |
|---|---|
| ui/panel_frame | 6 / 6 / 6 / 6 |
| ui/card_available, card_locked, card_on, card_off | 5 / 5 / 5 / 5 |
| ui/tab_selected, tab_normal, button_normal, button_pressed, button_disabled | 5 / 5 / 5 / 5 |
| ui/gauge_bg | 4 / 4 / 4 / 4 |

## 게이지
- gauge_bg: 배경 프레임 (Sliced)
- gauge_fill: 흰색 채움. Image.color로 색을 바꾸면 초록/노랑/빨강 단계 전환 가능 (Filled 또는 Sliced)
- gauge_tick_tile: 눈금. Image Type = Tiled, 게이지 위에 겹쳐서 사용

## 아이콘 (16x16)
- 아이템: item_fan, item_computer, item_aircon, item_server, item_miner, item_factory, item_datacenter
- 업그레이드: upg_power_limit, upg_income_mult, upg_interval, upg_click
- HUD: icon_bolt, icon_coin

## 토글
- breaker_on / breaker_off (16x24)

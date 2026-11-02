---
name: Zuzong Taskbar Glass
description: Taskbar hai pill, Quick Settings, thumbnail và capsule khúc xạ; đang kiểm thử.
colors:
  primary: "#1768C5"
  editor-bg: "#F4F7FB"
  surface: "#FFFFFF"
  ink: "#17283C"
  muted: "#536A83"
  border: "#BBCBDE"
  hover: "#DFECFB"
  pressed: "#C6DDF6"
  preview-bg: "#D9E7F2"
  preview-ink: "#34546E"
  glass-rgb-default: "#DCEBFA"
typography:
  title:
    fontFamily: "Segoe UI"
    fontSize: "26px"
    fontWeight: 600
  section:
    fontFamily: "Segoe UI"
    fontSize: "18px"
    fontWeight: 600
  body:
    fontFamily: "Segoe UI"
    fontSize: "14px"
    fontWeight: 400
  caption:
    fontFamily: "Segoe UI"
    fontSize: "12px"
    fontWeight: 400
rounded:
  button: "8px"
  preview: "14px"
  glass-default: "24px"
spacing:
  small: "8px"
  field: "14px"
  panel: "20px"
  column-gap: "32px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
    rounded: "{rounded.button}"
    padding: "9px 14px"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.button}"
    padding: "9px 14px"
  text-input:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    padding: "7px 9px"
---

# Design System: Zuzong Taskbar Glass

## Overview

**North Star: Kính macOS trên shell Windows thật.** Zuzong yêu cầu taskbar thật có hai pill tách ứng dụng/tray, kính GPU cho Quick Settings và thumbnail, panel bớt tròn, núm volume/brightness là capsule kính lỏng bẻ cong nền và track khi kéo. Không dùng hình giọt nước hoặc thay taskbar bằng dock riêng.

Bộ chỉnh WPF và helper native hoạt động độc lập với Windhawk. Token bộ chỉnh trong frontmatter lấy từ `work/TaskbarGlass/src/GlassEditor.xaml`; cơ chế kính lấy từ `GlassEditor.cs`, `TaskbarTap.cpp`, `Renderer.cpp`, `Common.h`. Hình chụp và GPU dump cũ chỉ là bằng chứng của phiên bản tạo chúng, không xác nhận phạm vi mới.

**Trạng thái:** đa surface và capsule đang thử trong GPU lab riêng. Chưa áp dụng lại shell sau prototype v10 crash liên quan active surface rỗng và sao chép property record XAML non-agile qua thread. Source hiện giữ record theo UI thread sở hữu; sửa source chưa chứng minh shell ổn định. QA shell và benchmark RAM/VRAM cuối còn chờ.

**Key Characteristics:**

- Hai pill taskbar theo geometry native, không sắp xếp lại icon.
- Quick Settings và thumbnail có surface/tài nguyên riêng.
- Capsule slider khúc xạ scene nền và track; input, vị trí và giá trị vẫn theo control native.
- Hai chế độ rõ ràng: khúc xạ GPU và Acrylic nhẹ.
- Bộ chỉnh đóng hẳn; mở lại EXE để quản lý, không có tray riêng.

## Colors

### Primary

`primary` dành cho Áp dụng taskbar và viền focus. Hover dùng `hover` cùng chữ tối; pressed dùng `pressed`. Đây là màu bộ chỉnh, không ép toàn bộ shell mang màu nhấn này.

### Neutral

`editor-bg`, `surface`, `ink`, `muted` và `border` giữ nền, vùng nhập và hai cấp chữ rõ ràng. Preview có nền `preview-bg` và chữ `preview-ink` để giải thích đọc được cả khi chưa có frame.

`glass-rgb-default` là RGB của mẫu ban đầu. Màu kính đầy đủ có dạng `#AARRGGBB`; alpha và thanh độ đậm là cùng thuộc tính. Nội dung kính phải đến từ backdrop/scene thật, không thay bằng gradient trang trí. Track slider GPU hiện có phần nền xanh nhạt bán trong suốt và phần giá trị xanh sáng; màu source là trạng thái hiện tại, chưa là bằng chứng khớp shell.

## Typography

Segoe UI dùng xuyên suốt bộ chỉnh theo vai trò frontmatter. Nhãn tiếng Việt viết kiểu câu; giá trị số và đơn vị đặt cạnh thanh trượt. Shell giữ nhãn, icon và ngữ nghĩa native; không tự dựng nhãn giả để che điều khiển Windows.

## Layout

Bộ chỉnh giữ tiêu đề/Áp dụng, preview GPU có công bố capture, vùng tab với vật liệu/bề mặt/bố cục/hiệu năng/cấu hình, và trạng thái cùng Tắt hiệu ứng/Đóng bộ chỉnh dưới cùng. Tab vật liệu dùng hai cột và cuộn khi thiếu chiều cao. Không có bố cục mobile.

Mục tiêu taskbar chính là hai pill bao quanh cụm ứng dụng và tray, có khoảng trống giữa chúng. Quick Settings và thumbnail có surface riêng theo host và geometry native. Khoảng lùi chỉnh lớp kính, không đổi vị trí icon; Acrylic nhẹ không dùng toàn bộ điều khiển hình học GPU. Primary screen là giới hạn hiện tại. Môi trường có Windows build 26300 và một màn hình 2560×1600; chưa có shell QA cuối cho scope này hay phần cứng nhiều màn hình.

## Elevation & Depth

GPU dùng Windows Graphics Capture chung, Gaussian blur, displacement lens, màu phủ và nét viền. Mỗi surface sở hữu swapchain, region bitmap, clipping và tài nguyên hình học riêng. Giá trị khúc xạ có dấu; 0 bỏ displacement. Acrylic nhẹ dùng HostBackdrop của Windows, không có capture; độ blur do Windows quyết định. Điều khiển không áp dụng ở Acrylic phải được phân biệt/vô hiệu hóa.

Capture giữ hai frame buffer toàn màn hình chính và một cached texture toàn màn hình. Dựng crop theo surface và dùng chung capture không có nghĩa chỉ cấp phát vùng pill. Capsule có thêm offscreen scene chứa nền và track dựng từ geometry native; displacement scene này trong vùng capsule để track cũng bị bẻ cong. Chưa có benchmark RAM/VRAM cuối.

Bộ chỉnh không tự vẽ bóng đổ. Kính sạch và capture exclusion có thể ẩn taskbar, Quick Settings, thumbnail cùng overlay khỏi ảnh chụp/quay/chia sẻ; dump GPU chỉ xác nhận surface được dump. Không dùng dump lab hoặc screenshot bị exclusion để tự xác nhận shell hoàn chỉnh. Nút lấy khung hình tạo ảnh cục bộ theo yêu cầu, không phải ảnh chứa đủ icon và ngữ cảnh native.

## Shapes

Nút và preview dùng bán kính trong frontmatter. Taskbar là hai pill tách nhau. Panel Quick Settings bớt tròn theo sửa của Zuzong: renderer hiện giới hạn radius panel không quá 16 đơn vị trước DPI; host XAML có hình học riêng, cần đối chiếu trong QA shell. Không áp một bán kính pill quá lớn cho toàn panel.

Núm volume/brightness là capsule ngang, hai đầu tròn đối xứng; không có đầu nhọn, giọt nước hoặc hình tròn phóng to giả capsule. Source hiện dùng thumb XAML 46×30 và lens map capsule có bán kính bằng nửa chiều cao. Đây là thông số source hiện tại, chưa chứng minh kéo khớp shell. Viền sáng là nét mảnh có gradient alpha, không phải glow quanh cửa sổ. Geometry được quy đổi theo DPI.

## Components

### Actions and state

Áp dụng kết nối helper; trạng thái chạy, chờ frame, lỗi hoặc tắt có vùng cố định. Tắt hiệu ứng yêu cầu khôi phục thuộc tính và capture affinity trước đó. Đóng bộ chỉnh kết thúc tiến trình editor, không gỡ helper. Không gọi thao tác này là ẩn xuống tray.

### Inputs and modes

Button, TextBox, ComboBox, Slider, CheckBox và TabControl giữ hành vi WPF native. Nút có hover/pressed/focus/disabled; màu sai có viền đỏ và lời giải thích. Surface toggles chọn tách taskbar, Quick Settings, thumbnail và kiểu capsule; không biến scope còn chờ QA thành một nhãn thành công.

### Capsules and shell surfaces

Slider native cung cấp track/thumb geometry, value và input. Offscreen scene dựng nền/track rồi displacement capsule ở vị trí thumb. Chỉ ẩn track/thumb native khi surface đã báo frame và slider sẵn sàng; khi chưa sẵn sàng, nền native phải dùng được. Quick Settings/thumbnail không chia sẻ một target bitmap khiến panel này ghi đè panel khác.

### Preview and runtime

Preview trước frame hiện hướng dẫn; sau đó hiển thị ảnh surface thực theo tỷ lệ, ghi đúng scope. Ảnh lab ghi là lab, ảnh phiên bản cũ không gắn nhãn bản mới. Helper có thể tiếp tục trong các shell host khi editor đóng. Data nằm cạnh DLL; EXE mở lại tìm module đang hoạt động để dùng đúng Data.

Property record XAML và áp dụng/khôi phục giữ ở UI thread sở hữu. Chỉ dữ liệu hình học/giá trị thuần được chuyển sang renderer. Surface resources phải có active surface hợp lệ. App không tự restart Explorer. Đo tài nguyên gồm host process, full-monitor RAM/VRAM và GPU; không lấy RAM editor hoặc số liệu dock cũ thay thế. Source tham khảo Windhawk/dự án khác giữ provenance và giấy phép MIT/GPL phù hợp dù runtime độc lập.

## Do's and Don'ts

- **Do** giữ hai pill app/tray, panel bớt tròn và capsule slider ngang đối xứng.
- **Do** giữ native input/value, số và đơn vị, trạng thái và công bố capture rõ ràng.
- **Do** phân biệt bằng chứng source, compile, GPU lab, shell thật và benchmark.
- **Don't** tạo núm giọt nước hoặc gọi capsule chỉ đổi màu là khúc xạ.
- **Don't** quay lại dock riêng, thêm tray hoặc dùng Windhawk làm runtime khi chưa có yêu cầu mới.
- **Don't** tự xác nhận đã sửa crash/shell ổn định trước QA cuối.
- **Don't** hứa mức RAM cố định, gọi capture là chỉ vùng pill, hoặc coi đóng editor là gỡ DLL.

## Bản sửa đàn hồi và ô chữ nhật

Núm dùng spring GPU k=625, damping=30; bước tích phân không quá 1/120 s, dt không quá 50 ms. Khi giữ, scale đích 1.14 × 1.10; khi thả trở về 1.0 có nảy nhẹ. Độ trễ thị giác bị chặn 18 px ngang/10 px dọc, không thay đổi value native. Rim GPU di chuyển cùng lens; native rim là fallback. Xóa toàn bộ vận tốc/scale khi mất scene hoặc mở lại.

Ô nhanh bo ngoài 8 DIP; hai nửa Wi-Fi/Bluetooth nối bằng góc trong vuông để tạo một chữ nhật chung, giữ nút mũi tên hoạt động. Quick giữ nền native ít nhất 220 ms sau mở và 90 ms ổn định hình học trước khi chuyển sang kính cache hợp lệ.

Lab đã đạt 200 lần cập nhật, 10 chu kỳ popup hai surface và kiểm tra spring (peak 1.072263; settled 1.000000). Mode/Stop native v13 đã đạt. Chưa xác nhận hết nháy hoặc trải nghiệm Quick/thumbnail của v14 bằng thao tác người dùng. Không dùng ảnh screenshot bị exclude để kết luận native rendering hoàn chỉnh.


Current V13/engine18 update: separate Quick and Thumbnail material controls (refraction/blur/radius/tint/bezel/rim); slider expansion, bounce and dedicated refraction. UI opens on Quick Settings. Geometry polls at 33ms while active and wakes renderer on changes; removed fixed 220/90ms gates. Cross-shell exchange uses explicitly file-backed mapped views with OS writer locks and seqlock readers, including interrupted-writer recovery. Quick/thumbnail capture exclusion is mandatory to avoid reflecting their own controls, independent of taskbar capture preference. Native thumb visual hides only after GPU slider readiness; native input is retained. Native Quick verified two valid slider geometries and ready=true, own GPU/UI exports show separate backgrounds and a single displayed slider layer. Final visual satisfaction and full native latency remain unconfirmed; do not reuse the v15 lab 50ms mean/78ms max as a final native latency claim. Source review passed live redraw after settings-only changes.


V14 / engine19: Quick lighting checkbox controls cached panel rim/specular reflection and lens rim, with QuickRim intensity; taskbar lighting is unchanged. Volume Mixer capacity rises from 2 to 32 visible sliders; ready contains exact successfully drawn IDs, so only covered native rows hide. Cached native parts, per-row exception cleanup, ancestor visibility/viewport filtering and one host query reduce scanning work; lens maps capped16 and brush resources reused. Springs reset on slider identity changes. Empty publications from the owning UI thread clear immediately. Private 4-slider lab passes all four individually ready IDs, interrupted publisher test and lens cache eviction/dimension tests. Native observation has reached three validated sliders. Full native visual satisfaction, all mixer configurations, and clean-process resource profiling remain unverified.


V15 branding: use the user-owned red Zuzong Z/dragon symbol from TKĐH V7 vector source. Embedded multi-size EXE icon and WPF window/header icon; material controls, shader and engine19 unchanged. Verified icon extraction and WPF export.

## V16 — Dynamic Notch độc lập

V16 thêm mô-đun WPF .NET 8 riêng, bật/tắt độc lập với taskbar. Notch có nhạc từ Windows media sessions, master volume, thông tin pin, ghim trạng thái mở và khay tham chiếu tối đa 40 file; bỏ khỏi khay giữ nguyên file gốc. Kích thước mặc định thu gọn 260×48, mở rộng 520×390, nằm giữa phía trên vùng làm việc với khoảng lùi 14; chuyển kích thước dùng spring. Phần kính lấy/khúc xạ vùng nhỏ bằng pipeline CPU/native được chọn từ V-Notch (Apache-2.0), không dùng pipeline full-monitor của taskbar. Nhịp theo cài đặt 20/30/60 FPS, mặc định 30; idle giảm nhịp. Không có hard cap 30 FPS.

Công tắc Dynamic Notch không thay công tắc Quick Settings/thumbnail; hai vùng này vẫn có checkbox riêng trong tab Các vùng kính. Source tương ứng ở `work/ZuzongNotch/NotchWindow.xaml`, `.xaml.cs`, `Program.cs` và `work/TaskbarGlass/src/NotchEditor.cs`. QA V16 đã cung cấp: runtime `glassReady=true`, audio available, 7 self-test và tích hợp editor PASS. Đây chưa là kiểm thử thực các lệnh phát/tạm dừng/chuyển bài/tua với media app, chưa có benchmark dài; V16 cũng chưa sửa thumbnail. Không dùng kết quả module Notch để xác nhận thumbnail hoặc trải nghiệm shell cũ. Lần bổ sung tài liệu này không điều khiển UI hay chụp màn hình.

V16 capture tuning: notch uses window capture exclusion and the upstream CPU/native refraction path; the optional Magnifier path was measured and not selected for this machine. Compact notch has a 5 FPS capture limit even on moving backgrounds; expanded/size animation uses the selected 20/30/60 rate. A controlled red/blue grid lab verified visible refraction in the module export; this is an app render, not a desktop screenshot.

## V17 — Notch sát mép và công tắc panel rõ hơn

Theo video iPhone notch 19 giây do Zuzong gửi, V17 thêm kiểu `attached` sát mép trên màn hình (`top=0`, hai góc trên vuông, hai góc dưới bo) và `floating` có khoảng lùi. Kích thước mặc định mới là compact 260×36, expanded 360×254; các giá trị V16 ở trên là lịch sử. Chế độ đen giữ nền đen, ẩn rim; logo có công tắc riêng. Chất liệu kính có thể theo taskbar trực tiếp qua tint/refraction/blur/bezel/rim, hoặc dùng bộ giá trị riêng. UI Notch được rút gọn; spring vẫn dùng cho mở/thu.

Quick Settings và thumbnail có checkbox độc lập ngay trong tab tương ứng. Quick có kết nối lại thủ công và tự thử kết nối lại khi PID shell đổi trong lúc bộ chỉnh đang chạy; không mô tả đây là giám sát tự phục hồi khi editor đã đóng. Chẩn đoán trước đó thấy ShellHost PID 3068 chưa nạp DLL; sau nối lại engine19, đã có `ready kind=2`, hai slider và chỉ báo `quickReady=139`. Đây là bằng chứng kết nối/frame, chưa xác nhận cảm nhận hình ảnh hoàn toàn đúng.

Thumbnail engine19 từng có `ready kind=1` nhưng nền UI còn opaque vì chưa được theo dõi/xóa tại `StyleElement`. Engine20 bổ sung tracking nền scope1 theo ready và khôi phục khi tắt; native QA engine20 còn chờ, chưa được gọi là thumbnail đã sửa thành công. QA Notch V17 được cung cấp: 7 unit test, 5 assert layout/material và 6 editor-integration check PASS. Chưa có xác nhận visual satisfaction của native20, kiểm thử media command với app đang phát hoặc benchmark dài. Lần cập nhật này chỉ ghi tài liệu, không điều khiển UI, chụp màn hình hay sửa mã.

## V18 — Notch theo ảnh iPhone, nhạc và đồng hồ

Panel media V18 theo ảnh iPhone mới: album 64×64, tên bài/nghệ sĩ, wave, thanh seek và ba nút trước/phát-tạm dừng/tiếp; bỏ thanh volume, nút ghim và hàng nút tab trên panel. Cài đặt và khay file chuyển sang menu chuột phải. Compact giữ cao 36; chiều rộng mở mặc định 360, cao theo nội dung: media 196, clock 128, files 240. Những số V17 ở trên là lịch sử.

Không có bài đang phát hoặc đang idle thì chuyển sang đồng hồ. Bấm tiêu đề đổi nhạc/đồng hồ và có thể trở lại media; lựa chọn ép hiện media được xóa khi trạng thái chuyển từ playing sang paused hoặc mất session, tránh giữ panel nhạc khi đã dừng. Chuyển kích thước dùng damped spring 400 ms, tham số .86; nội dung hiện bằng opacity 260 ms, blur 9→0 trong 300 ms và dịch Y -5→0 trong 300 ms. Wave dừng khi idle/ẩn bởi fullscreen; khi Windows giảm/tắt animation, nhánh reduced-motion bỏ các chuyển động tương ứng.

QA V18 được cung cấp: 7 unit test và 5 appearance checks cập nhật bao phủ chuỗi idle/bấm tiêu đề/pause đều PASS; smoke idle thực 10 giây mở đúng view clock và kết thúc sạch. Engine20 không thay đổi so với V17; kiểm tra native WUX thumbnail alpha/restore đã PASS, thay cho trạng thái còn chờ của ghi chú V17. Chưa nhận phản hồi hài lòng hình ảnh native hiện tại từ Zuzong; kiểm thử media command thực với app đang phát và benchmark dài vẫn chưa có. Lần ghi này chỉ append hai tài liệu, không UI automation, screenshot hoặc sửa code.

## V19/V20 — Khôi phục hai pill, motion mượt và thông báo Notch

V19 sửa tình huống taskbar chỉ còn tray `kind3`, mất nhóm ứng dụng `kind0`. Native engine22 giữ weak root taskbar trên UI thread sở hữu, nhận diện frame của hai vùng 0/3; quét lúc khởi tạo và chỉ phục hồi khi thiếu vùng, tối đa 5000 node mỗi lượt, giới hạn chu kỳ 2 giây. Theo dõi object/property được khử trùng để không ghi đè original cần khôi phục; dùng ID theo object, dọn weak record đã chết định kỳ và có fallback transform geometry. `RecoveryCheck` trên XAML do app tự dựng đã PASS initial-both, duplicate/replacement và original-dedup; quan sát live engine22 đã phục hồi nhóm 0+3 và renderer `ready kind0`. Kết quả này không thay kiểm thử dài cho mọi lần shell tái tạo cây.

Theo yêu cầu mới nhất của Zuzong về hiệu ứng mượt hơn, V20 chuyển resize Notch sang nghiệm spring giảm chấn vật lý trong `NotchMotion.cs` (ω≈20.49, ζ=.82). Trạng thái vị trí riêng tránh lấy kích thước native đã làm tròn pixel làm phản hồi; đổi đích khi đang chạy giữ vận tốc. Render handler và timer fallback 40 ms chỉ hoạt động lúc còn chuyển động, dừng khi ổn định. Nội dung expanded có layout cố định trong lúc đổi khung; reveal mới trễ 65 ms, fade 320 ms, blur10→0 trong 380 ms, scale .94→1. Thông số V18 trước đó là lịch sử. Kiểm tra rapid motion open/media/clock/notice/close/retarget PASS, driver inactive khi settle; chưa coi đây là xác nhận cảm nhận mượt cuối của người dùng.

V20 thêm notice Clipboard và Codex cùng công tắc riêng, thêm công tắc Startup. Clipboard chỉ xem sequence/format, không gọi `GetClipboardData`, debounce 140 ms; notice hiện 2.7 giây, không đọc/lưu nội dung đã copy. Bridge Codex giữ nguyên và chuyển tiếp lệnh computer-use notify cũ, chỉ ghi loại sự kiện cục bộ cho Notch, không lưu nội dung chat. Cài đặt thực có backup; kiểm tra TOML xác nhận các giá trị khác không đổi về ngữ nghĩa. Bridge end-to-end smoke đã ghi `noticesShown=1`, `clipboardListener=true`; chưa xác nhận một sự kiện hoàn tất do Codex live tự phát đã đi hết đường này.

HKCU Run đã bật EXE V20 với `--startup`. Chạy thử one-shot startup exit0 và không tạo module trùng; chưa kiểm thử đăng xuất/đăng nhập hoặc khởi động lại máy thật. Các QA trên không bổ sung kiểm thử media-app command thực hoặc benchmark dài. Chỉ append tài liệu ở bước này, không UI automation, screenshot hay sửa code.


## V21 — Notch nhạc theo bản V-Notch người dùng gửi (2026-11-01)

Giữ tất cả tùy chỉnh có sẵn. Compact media thay title bằng art24 ở trái và năm cột sóng ở phải; clock khi idle, logo vẫn tùy chọn. Attached shape thêm concave ears10, floating giữ bo tròn. Expanded media vẫn art64/title/artist/seek/transport; title đổi clock. Shared cover overlay24→64 đi cùng normalized spring height, chỉ một cover visible; settle trao lại đúng ảnh gốc. Open spring ω17.5/ζ.86, closeω21.5; liên tục vị trí/vận tốc khi đảo chiều. Header không bị clock.Tick ghi đè opacity trong lúc đóng.

Wave có một timer33ms cho cả hai nơi, đọc IAudioMeterInformation scalar của output, smoothing năm bar và artwork-derived color. Không microphone/sample recording; scalar chung output, không frequency spectrum. Dừng timer/nhả meter khi idle, hidden fullscreen, notice và dispose. Media chọn playing session khi current paused, theo dõi playback events của mọi session; cache bìa khi title/artist/session không đổi.

Release build,8self-checks,4appearance variants +taskbar tint, compact art/wave/single-cover/idle-stop assertions, rapid open/media/clock/notice/close/retarget và6editor integration PASS. Read-only actual Windows probe nhận available/playing/artwork true, meter available. Live V21 PID được xác nhận, giữ custom settings chỉ đổi enabled theo yêu cầu; có lúc hidden-fullscreen vì Chrome, sau đó compact media. Không mouse control hoặc full-desktop screenshot. Chưa bấm transport vào nhạc thật, chưa reboot/đa màn hình benchmark dài hay user visual acceptance. Engine22 không thay đổi.

HKCU Run của app chuyển V21, Codex bridge chuyển V21 và giữ nguyên notify hiện tại theo bản backup TOML mới (forward hiện4args; không dùng ghi chú V20 cũ2args làm bằng chứng). V-Notch gốc đã đóng trước khi mở V21; mục startup của app gốc vẫn cần lựa chọn riêng của người dùng.


## V22 — Full Notch từ nguồn người dùng, 2026-11-01

Nguồn V-Notch2.2.2 Apache-2.0 trong ZIP người dùng được fork thành mô-đun ZuzongNotch riêng. Giữ shell/thumbnail morph, media visualization, clipboard/file tray, timer, mixer, camera, battery/Bluetooth/calendar/sysmon/weather/privacy, WhiteNoise, Spotlight/AI/translation/lyrics/subtitles/Canvas và Settings gốc. Thêm panel feature switches tiếng Việt và hai nút ở tab Notch của bộ chỉnh. Cập nhật Z brand, dùng font Segoe hệ thống, không chạy installer gốc. Engine22 không đổi.

Feature off ngừng media detection/visualizer và module tương ứng; timer pause, audio polling stop, camera preview stop; history pause dừng debounce/retention. Lịch sử clipboard opt-in, thông báo clipboard chỉ sự kiện vẫn riêng. Không prewarm Spotlight index/smart-crop model; disable auto updater gốc. Cho phép giữ các thông số advanced khi map custom; không reset height/radius/noise/opacity/behavior. Native artwork rộng bị bỏ để đợi ảnh mạng được sửa cho local-only, đồng thời không khởi tạo network fetch tasks khi bị tắt. Pause về clock với debounce650ms.

Full preferences/cache/models ở %LOCALAPPDATA%/ZuzongTaskbarGlass/NotchFull. active-data.json chung theo engine; UI/CLI connect cập nhật route. Restart giữ --settings/--editor; guard singleton theo stable data root, QA tách store riêng. Timer polling ngừng thao tác khi app shutdown. Main editor merge theo baseline từng field để không ghi đè các thay đổi từ full settings. Các chỉnh sửa trong fallback profile do restart cũ được merge theo baseline và giữ backup; không xóa nguồn.

24 checks native WPF/private desktop +3 editor merge checks PASS; stderr QA rỗng. Kiểm tra dừng/resume media, module off, clipboard pause/resume, camera gate, persistence, advanced parameters, appearance/settings, media/timer/mixer open/close, rapid interruption, notification restoration và IPC. Live module duy nhất PID19140 đã compact media/playing/artwork/glassReady=true. V-Notch gốc không chạy, HKCURun gốc đã tắt, own startup V22 bật, pinned shortcut V20 đổi V22 với backup. User được mở Notch theo phản hồi chưa thấy Dynamic. Không CUA/mouse/full-desktop capture; chỉ visual-tree exports. Chưa physical reboot, media transport user-app commands, camera hardware, network/API/model/account integrations hoặc benchmark dài. Bộ ZIP phải loại cả Modules/ZuzongNotch/Data fallback và mọi machine-specific bridge config.
